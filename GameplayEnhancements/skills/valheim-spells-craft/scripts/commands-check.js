#!/usr/bin/env node
// Read-only check that the HomeCheat code and its docs describe the same commands, and that the doc sections the
// valheim-spells-craft skill relies on still exist. Nothing is written.
// Usage: node commands-check.js [--list] [--json] [--baseline <baseline.md>] [--mods <ValheimMods folder>] [--pkg <GameplayEnhancements folder>]
//   (no flag)   compare: command names in src/ (Core built-ins + Features/<F>), the catalog docs/homecheat-commands.md,
//               the command index GameplayEnhancements/docs/HomeCheat-plugin/README.md, the op= values in spells.txt
//   --list      print every command: name, feature, class, file
//   --baseline  also check the anchors listed in baseline.md and how far its knowledge-sha is behind HEAD (git, HomeOS only)
// Exit: 0 clean, 1 differences found, 2 a folder or file was not found.
// Folders, first found: --pkg / --mods; the package copy (GameplayEnhancements/skills/valheim-spells-craft/scripts ->
// GameplayEnhancements, docs in GameplayEnhancements/docs/architecture); HomeOS (.claude/skills/valheim-spells-craft/scripts
// -> references/ValheimCustomMods, docs in references/ValheimCustomMods/docs).
const fs = require('fs');
const path = require('path');
const { execFileSync } = require('child_process');

const argv = process.argv.slice(2);
const flag = k => argv.includes(k);
const opt = k => { const i = argv.indexOf(k); return i >= 0 ? argv[i + 1] : null; };
const fail = m => { console.error('commands-check: ' + m); process.exit(2); };

// ---- where things are ----------------------------------------------------------------------------------------------
function roots() {
  let pkg = opt('--pkg'), mods = opt('--mods'), docs = null, mode = null;
  if (mods) { mods = path.resolve(mods); pkg = path.join(mods, 'GameplayEnhancements'); docs = path.join(mods, 'docs'); mode = 'repo'; }
  else if (pkg) { pkg = path.resolve(pkg); docs = path.join(pkg, 'docs', 'architecture'); mode = 'package'; }
  else {
    const asPackage = path.resolve(__dirname, '..', '..', '..');
    const asHomeOS = path.resolve(__dirname, '..', '..', '..', '..', 'references', 'ValheimCustomMods');
    if (fs.existsSync(path.join(asPackage, 'src', 'Features'))) { pkg = asPackage; docs = path.join(pkg, 'docs', 'architecture'); mode = 'package'; }
    else if (fs.existsSync(path.join(asHomeOS, 'GameplayEnhancements', 'src', 'Features'))) { mods = asHomeOS; pkg = path.join(mods, 'GameplayEnhancements'); docs = path.join(mods, 'docs'); mode = 'repo'; }
  }
  if (!pkg || !fs.existsSync(path.join(pkg, 'src', 'Features'))) fail('GameplayEnhancements/src/Features not found (pass --mods or --pkg)');
  if (!fs.existsSync(docs)) fail('docs folder not found: ' + docs);
  return { pkg, mods, docs, mode };
}

const walk = d => fs.readdirSync(d, { withFileTypes: true })
  .flatMap(e => e.isDirectory() ? walk(path.join(d, e.name)) : [path.join(d, e.name)]);
const read = f => fs.readFileSync(f, 'utf8');

// ---- lists ---------------------------------------------------------------------------------------------------------
// every class with `override string Name { get { return "<name>"; } }`
function codeCommands(src) {
  const out = [];
  for (const f of walk(src).filter(f => f.endsWith('.cs'))) {
    const t = read(f);
    const m = /override\s+string\s+Name\s*\{\s*get\s*\{\s*return\s+"([^"]+)"/.exec(t);
    if (!m) continue;
    const cls = (/class\s+(\w+)\s*:/.exec(t) || [])[1] || '?';
    const rel = path.relative(src, f).split(path.sep);
    const feature = rel[0] === 'Core' ? 'core' : rel[0] === 'Features' ? rel[1].toLowerCase() : rel[0];
    out.push({ name: m[1], feature, cls, file: path.relative(path.dirname(src), f).split(path.sep).join('/') });
  }
  return out.sort((a, b) => a.name.localeCompare(b.name));
}

// first cell of each row of the first table after a heading; cells may list several names: "heal, cleanse, bless"
function tableNames(file, heading) {
  const lines = read(file).split(/\r?\n/);
  let i = lines.findIndex(l => l.trim() === heading);
  if (i < 0) return null;
  const names = [];
  let inTable = false;
  for (i++; i < lines.length; i++) {
    const l = lines[i].trim();
    if (l.startsWith('#')) break;
    if (!l.startsWith('|')) { if (inTable) break; continue; }
    inTable = true;
    const cell = l.split('|')[1].trim();
    if (/^-+$/.test(cell) || cell === 'Command') continue;
    for (const part of cell.split(',')) {
      const n = part.replace(/\(.*?\)/g, '').replace(/`/g, '').trim().toLowerCase();
      if (n) names.push(n);
    }
  }
  return names;
}

function spellFile(pkg) {
  const game = process.env.VALHEIM_DIR_WIN || 'D:\\Steam\\steamapps\\common\\Valheim';
  const cands = [path.join(game, 'BepInEx', 'config', 'homecheat', 'spells.txt'),
    path.join(pkg, 'payload', 'BepInEx', 'config', 'homecheat', 'spells.txt')];
  return cands.find(f => fs.existsSync(f)) || null;
}

function spellOps(file) {
  const ops = [];
  read(file).split(/\r?\n/).forEach((l, i) => {
    if (l.trim().startsWith('#')) return;
    const re = /op=([A-Za-z]+)/g;
    let m;
    while ((m = re.exec(l))) ops.push({ op: m[1].toLowerCase(), line: i + 1, text: l.trim() });
  });
  return ops;
}

// headings of a markdown file as GitHub anchors
function anchorsOf(file) {
  return new Set(read(file).split(/\r?\n/).filter(l => /^#{1,6} /.test(l))
    .map(l => l.replace(/^#+ /, '').trim().toLowerCase().replace(/[^\w\- ]/g, '').replace(/ /g, '-')));
}

// ---- main ----------------------------------------------------------------------------------------------------------
const R = roots();
const src = path.join(R.pkg, 'src');
const cmds = codeCommands(src);

if (flag('--list')) {
  if (flag('--json')) console.log(JSON.stringify(cmds, null, 1));
  else for (const c of cmds) console.log(`${c.name.padEnd(13)} ${c.feature.padEnd(13)} ${c.cls.padEnd(22)} ${c.file}`);
  process.exit(0);
}

const problems = [], notes = [];
const names = new Set(cmds.map(c => c.name));

// 1. one owner per name (the registry refuses the second at load)
const seen = {};
for (const c of cmds) {
  if (seen[c.name]) problems.push(`name '${c.name}' in two places: ${seen[c.name].file} and ${c.file} (the second fails at load)`);
  else seen[c.name] = c;
}

// 2. catalog
const catalogFile = path.join(R.docs, 'homecheat-commands.md');
if (!fs.existsSync(catalogFile)) fail('missing ' + catalogFile);
const catalog = tableNames(catalogFile, '## Catalog');
if (!catalog) problems.push('homecheat-commands.md has no "## Catalog" table');
else {
  for (const n of names) if (!catalog.includes(n)) problems.push(`'${n}' is in the code but has no row in homecheat-commands.md`);
  for (const n of catalog) if (!names.has(n)) problems.push(`homecheat-commands.md row '${n}' has no command in the code`);
  const dup = catalog.filter((n, i) => catalog.indexOf(n) !== i);
  for (const n of new Set(dup)) problems.push(`homecheat-commands.md has '${n}' more than once`);
}

// 3. command index
const indexFile = path.join(R.pkg, 'docs', 'HomeCheat-plugin', 'README.md');
if (!fs.existsSync(indexFile)) fail('missing ' + indexFile);
const index = tableNames(indexFile, '## Command index');
if (!index) problems.push('command index: no "## Command index" table');
else {
  for (const n of names) if (!index.includes(n)) problems.push(`'${n}' is in the code but not in the command index`);
  for (const n of index) if (!names.has(n)) problems.push(`command index row '${n}' has no command in the code`);
}

// 4. spell file
const sf = spellFile(R.pkg);
if (!sf) notes.push('spells.txt not found (game folder or payload): spell ops not checked');
else {
  notes.push('spells.txt: ' + sf);
  for (const o of spellOps(sf)) if (!names.has(o.op)) problems.push(`spells.txt line ${o.line} uses unknown op '${o.op}': ${o.text}`);
}

// 5. feature folders without a command
for (const d of fs.readdirSync(path.join(src, 'Features'))) {
  if (!cmds.some(c => c.feature === d.toLowerCase())) problems.push(`src/Features/${d} has no command class`);
}

// 6. baseline: anchors and knowledge-sha
const baseline = opt('--baseline');
if (baseline) {
  if (!fs.existsSync(baseline)) fail('missing ' + baseline);
  const text = read(baseline);
  const rows = [...text.matchAll(/^\|\s*`([\w.-]+\.md)`\s*\|\s*`#([\w-]+)`\s*\|/gm)];
  if (rows.length === 0) problems.push('baseline.md lists no anchors');
  for (const [, doc, anchor] of rows) {
    const f = path.join(R.docs, doc);
    if (!fs.existsSync(f)) { problems.push(`baseline: ${doc} not found in ${R.docs}`); continue; }
    if (!anchorsOf(f).has(anchor)) problems.push(`baseline: ${doc}#${anchor} no longer exists`);
  }
  const sha = (/knowledge-sha:\s*`?([0-9a-f]{7,40})/.exec(text) || [])[1];
  if (!sha) problems.push('baseline.md has no knowledge-sha');
  else if (R.mode !== 'repo') notes.push(`knowledge-sha ${sha}: not compared (package copy, no git)`);
  else {
    try {
      const git = a => execFileSync('git', ['-C', R.mods, ...a], { encoding: 'utf8' }).trim();
      const head = git(['rev-parse', '--short', 'HEAD']);
      const log = git(['log', '--oneline', `${sha}..HEAD`, '--', 'GameplayEnhancements/src', 'GameplayEnhancements/docs', 'docs', ':(exclude)GameplayEnhancements/docs/architecture']);
      const n = log ? log.split('\n').length : 0;
      const core = git(['diff', '--name-only', `${sha}..HEAD`, '--', 'GameplayEnhancements/src/Core', 'docs/homecheat-architecture.md']);
      notes.push(`knowledge-sha ${sha}, HEAD ${head}: ${n} commit(s) since touch src/ or docs/` +
        (core ? `; core or architecture doc changed (${core.split('\n').length} file(s)): run the refresh` : ''));
      if (core) problems.push('knowledge-sha is behind a change in src/Core or homecheat-architecture.md: refresh the skill first');
    } catch (e) { notes.push('knowledge-sha: git not available (' + e.message.split('\n')[0] + ')'); }
  }
}

const result = { mode: R.mode, docs: R.docs, commands: cmds.length, features: new Set(cmds.map(c => c.feature)).size, problems, notes };
if (flag('--json')) console.log(JSON.stringify(result, null, 1));
else {
  console.log(`commands-check (${R.mode}): ${cmds.length} commands in ${result.features} DLLs (core + features); docs ${R.docs}`);
  for (const n of notes) console.log('  note: ' + n);
  if (problems.length === 0) console.log('OK: code, catalog, command index and spell file agree');
  else { console.log(`${problems.length} difference(s):`); for (const p of problems) console.log('  - ' + p); }
}
process.exit(problems.length ? 1 : 0);
