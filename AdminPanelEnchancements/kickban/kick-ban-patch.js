#!/usr/bin/env node
// Makes every host-side kick in Valheim also ban: re-points two calls from ZNet.Kick to ZNet.Ban in assembly_valheim.dll
// (a 4-byte IL token change each):
//   1. console `kick <name>`                      (Terminal.InitTerminal lambda)
//   2. Escape > players list > Kick user button   (SessionPlayerListEntry.OnKick lambda)
// Design and lessons: references/ValheimCustomMods/AdminPanelEnchancements/docs/kick-bans-lessons-learned.md
//
//   node scripts/valheim/kick-ban-patch.js check   [--dll <path>]   prints original | partial | patched | unknown
//   node scripts/valheim/kick-ban-patch.js apply   [--dll <path>]   backs up, patches what is not patched yet, re-reads
//   node scripts/valheim/kick-ban-patch.js undo    [--dll <path>]   restores the backup
//
// Exit codes: check 0 patched, 1 original or partial (apply needed), 2 unknown, 3 error. apply/undo 0 done, 3 refused or failed.
// Sites are found by meaning (ldstr "kick" / "ban" in Terminal.InitTerminal -> ldftn lambda; first ldftn in
// SessionPlayerListEntry.OnKick; the ZNet call inside), never by a fixed offset, so the script survives a game update.
const fs = require('fs');
const path = require('path');
const crypto = require('crypto');
const { execFileSync } = require('child_process');
const { load } = require('./lib/il-reader');

const DEFAULT_DLL = 'D:\\Steam\\steamapps\\common\\Valheim\\valheim_Data\\Managed\\assembly_valheim.dll';
const BACKUP_ROOT = process.env.VALHEIM_BACKUP_ROOT || 'D:\\ValheimBackups';
const BACKUP_NAME = 'assembly_valheim.dll.orig';

const sha256 = buf => crypto.createHash('sha256').update(buf).digest('hex');
const hex = t => '0x' + t.toString(16).padStart(8, '0');
const die = (msg, code = 3) => { (code ? console.error : console.log)(msg); process.exit(code); };

// -> { state, sites: [{ label, handler, callee, token, at, state }], banToken, why? }
function analyse(buf) {
  const d = load(buf);
  const one = (type, name) => { const r = d.methodsOf(type, new RegExp(`^${name}$`)); return r.length === 1 ? r[0].m : null; };
  const unknown = why => ({ state: 'unknown', sites: [], why });

  const init = one('Terminal', 'InitTerminal'), onKick = one('SessionPlayerListEntry', 'OnKick');
  if (!init) return unknown('Terminal.InitTerminal not found exactly once');
  if (!onKick) return unknown('SessionPlayerListEntry.OnKick not found exactly once');

  // the lambda behind a console command: first ldftn after the command's name literal
  const code = d.decode(init).instrs;
  const lambdaOf = cmd => {
    const hits = [];
    code.forEach((ins, i) => {
      if (ins.name !== 'ldstr' || d.us(ins.token & 0xffffff) !== cmd) return;
      const f = code.slice(i + 1, i + 40).find(x => x.name === 'ldftn');
      if (f && f.token >>> 24 === 6) hits.push(f.token & 0xffffff);
    });
    return hits.length === 1 ? hits[0] : null;
  };
  // the players-list Kick button: first ldftn in OnKick is the "yes" callback
  const btn = d.decode(onKick).instrs.find(x => x.name === 'ldftn' && x.token >>> 24 === 6);
  // the single ZNet.Kick / ZNet.Ban call inside a handler
  const zcall = m => {
    const calls = d.decode(m).instrs.filter(x => (x.name === 'call' || x.name === 'callvirt') && x.token >>> 24 === 6)
      .map(x => ({ ...x, m: x.token & 0xffffff }))
      .filter(x => d.ownerOfMethod(x.m)?.name === 'ZNet' && /^(Kick|Ban)$/.test(d.methodName(x.m)));
    return calls.length === 1 ? { ...calls[0], opAt: calls[0].argAt - 1, name: d.methodName(calls[0].m) } : null;
  };

  const banM = lambdaOf('ban');
  const ban = banM && zcall(banM);
  if (!ban || ban.name !== 'Ban') return unknown('console ban handler does not call ZNet.Ban');
  if (buf[ban.opAt] !== 0x6f) return unknown('ban call is not a callvirt');

  const wanted = [['console kick', lambdaOf('kick')], ['players-list Kick button', btn && (btn.token & 0xffffff)]];
  const sites = [];
  for (const [label, m] of wanted) {
    if (!m) return unknown(`${label}: handler lambda not found`);
    const c = zcall(m);
    if (!c) return unknown(`${label}: ZNet.Kick / ZNet.Ban call not found`);
    const s = { label, handler: d.mname(m), callee: c.name, token: c.token, at: c.argAt };
    if (buf[c.opAt] !== 0x6f) return unknown(`${label}: call is not a callvirt`);
    if (c.token === ban.token) s.state = 'patched';
    else if (c.name === 'Kick' && Buffer.compare(d.methodSig(c.m), d.methodSig(ban.m)) === 0) s.state = 'original';
    else return unknown(`${label}: calls ZNet.${c.name} with a different signature than Ban`);
    sites.push(s);
  }
  const n = sites.filter(s => s.state === 'patched').length;
  return { state: n === sites.length ? 'patched' : n === 0 ? 'original' : 'partial', sites, banToken: ban.token };
}

function valheimRunning() {
  try { return /valheim\.exe/i.test(execFileSync('tasklist', ['/FI', 'IMAGENAME eq valheim.exe', '/NH'], { encoding: 'utf8' })); }
  catch (e) { die('cannot list processes: ' + e.message); }
}

// newest <root>\*-kick-ban\assembly_valheim.dll.orig first
function backups() {
  if (!fs.existsSync(BACKUP_ROOT)) return [];
  return fs.readdirSync(BACKUP_ROOT).filter(n => /-kick-ban$/.test(n)).sort().reverse()
    .map(n => path.join(BACKUP_ROOT, n, BACKUP_NAME)).filter(f => fs.existsSync(f));
}

// A backup is for this game version when it is an original-shaped DLL of the same size and the current DLL,
// with the patch windows put back from the backup, equals it byte for byte.
function matchingBackup(buf, a) {
  for (const f of backups()) {
    const b = fs.readFileSync(f);
    if (b.length !== buf.length) continue;
    const o = analyse(b);
    if (o.state !== 'original' || o.sites.some((s, i) => s.at !== a.sites[i].at)) continue;
    const r = Buffer.from(buf); for (const s of o.sites) b.copy(r, s.at, s.at, s.at + 4);
    if (Buffer.compare(r, b) === 0) return { f, b };
  }
  return null;
}

function report(dll, buf, a) {
  console.log(`dll:     ${dll} (${buf.length} bytes)`);
  console.log(`sha256:  ${sha256(buf)}`);
  for (const s of a.sites) console.log(`site:    ${s.label}: ${s.handler} -> ZNet::${s.callee} (token ${hex(s.token)} at 0x${s.at.toString(16)}) ${s.state}`);
  if (a.why) console.log(`why:     ${a.why}`);
  console.log(`state:   ${a.state}`);
}

const [cmd, ...rest] = process.argv.slice(2);
const dll = rest.includes('--dll') ? rest[rest.indexOf('--dll') + 1] : (process.env.VALHEIM_DLL || DEFAULT_DLL);
if (!['check', 'apply', 'undo'].includes(cmd)) die('usage: node kick-ban-patch.js check|apply|undo [--dll <path>]', 3);
if (!fs.existsSync(dll)) die(`not found: ${dll}`);

const buf = fs.readFileSync(dll);
const a = analyse(buf);

if (cmd === 'check') {
  report(dll, buf, a);
  process.exit({ patched: 0, original: 1, partial: 1, unknown: 2 }[a.state]);
}

// VALHEIM_ALLOW_RUNNING=1 skips the check, only for rehearsing on a copy of the DLL (with --dll)
if (!process.env.VALHEIM_ALLOW_RUNNING && valheimRunning()) die('refused: valheim.exe is running. Close the game first (Unity rewrites files on exit).');
report(dll, buf, a);

if (cmd === 'apply') {
  if (a.state === 'patched') die('already patched, nothing to do', 0);
  if (a.state === 'unknown') die('refused: handler code is not in the known shape. Nothing changed.');

  let bak = matchingBackup(buf, a);
  if (bak) console.log(`backup:  ${bak.f} (already exists, matches this game version)`);
  else {
    if (a.state !== 'original') die('refused: DLL is partly patched and no matching backup exists. Use Steam: Verify integrity of game files, then run apply again.');
    const dir = path.join(BACKUP_ROOT, new Date().toISOString().slice(0, 10) + '-kick-ban');
    fs.mkdirSync(dir, { recursive: true });
    const f = path.join(dir, BACKUP_NAME);
    if (fs.existsSync(f)) die(`refused: ${f} exists but is a different game version. Move it aside first.`);
    fs.copyFileSync(dll, f, fs.constants.COPYFILE_EXCL);
    if (sha256(fs.readFileSync(f)) !== sha256(buf)) die('backup hash differs from the DLL; nothing patched');
    console.log(`backup:  ${f} (hash verified)`);
  }

  const todo = a.sites.filter(s => s.state === 'original');
  const patched = Buffer.from(buf);
  for (const s of todo) patched.writeUInt32LE(a.banToken, s.at);
  const fd = fs.openSync(dll, 'r+');
  try { for (const s of todo) fs.writeSync(fd, patched, s.at, 4, s.at); } finally { fs.closeSync(fd); }

  const after = fs.readFileSync(dll), diff = [];
  for (let i = 0; i < after.length; i++) if (after[i] !== buf[i]) diff.push(i);
  const again = analyse(after);
  if (after.length !== buf.length || again.state !== 'patched' || diff.some(i => !todo.some(s => i >= s.at && i < s.at + 4))) {
    console.error(`verification failed (state ${again.state}, ${diff.length} bytes differ). Run: node scripts/valheim/kick-ban-patch.js undo`);
    process.exit(3);
  }
  console.log(`patched: ${todo.length} site(s), ${diff.length} byte(s) changed, state ${again.state}, sha256 ${sha256(after)}`);
  process.exit(0);
}

// undo
if (a.state === 'original') die('already original, nothing to undo', 0);
const fail = why => die(`${why}\nUse Steam: Valheim > Properties > Installed Files > Verify integrity of game files.`);
if (a.state === 'unknown') fail('refused: DLL is not in a known shape.');
const bak = matchingBackup(buf, a);
if (!bak) fail(`refused: no backup under ${BACKUP_ROOT} matches this game version.`);
fs.copyFileSync(bak.f, dll);
const back = fs.readFileSync(dll);
if (Buffer.compare(back, bak.b) !== 0 || analyse(back).state !== 'original') die('restore verification failed');
console.log(`restored from ${bak.f}, state original, sha256 ${sha256(back)}`);
