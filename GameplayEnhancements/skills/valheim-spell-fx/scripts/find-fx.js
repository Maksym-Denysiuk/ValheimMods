#!/usr/bin/env node
// Search the Valheim effect description files (fx_, vfx_, sfx_) for candidates that match spell words.
// Usage: node find-fx.js [--prefix fx|vfx|sfx] [--limit 15] [--all] [--fx-dir <dir>] word [word ...]
//   words are OR-ed; each word also matches its synonyms (fire -> flame, ember, burn ...).
//   Rows checked in game (✔) rank first. Rows marked "DO NOT USE" are hidden unless --all.
// Reads <FX docs>/<prefix>_descriptions.md. The FX docs folder is, first found:
//   --fx-dir, env VALHEIM_FX_DIR,
//   the package copy: GameplayEnhancements/skills/valheim-spell-fx/scripts -> GameplayEnhancements/docs/FX,
//   HomeOS: .claude/skills/valheim-spell-fx/scripts -> references/ValheimCustomMods/GameplayEnhancements/docs/FX.
const fs = require('fs');
const path = require('path');

const FX_CANDIDATES = [
  path.resolve(__dirname, '..', '..', '..', 'docs', 'FX'),
  path.resolve(__dirname, '..', '..', '..', '..', 'references', 'ValheimCustomMods', 'GameplayEnhancements', 'docs', 'FX'),
];
let FX_DIR = null;

const SYN = {
  fire: ['fire', 'flame', 'ember', 'burn', 'fireball', 'lava', 'cinder', 'heat'],
  ice: ['ice', 'frost', 'cold', 'freez', 'snow', 'icy', 'crystal'],
  lightning: ['lightning', 'thunder', 'electric', 'zap', 'spark', 'storm', 'bolt'],
  poison: ['poison', 'toxic', 'venom', 'slime', 'puke', 'acid', 'radiation'],
  nature: ['nature', 'root', 'vine', 'leaf', 'leaves', 'spore', 'tentaroot', 'green'],
  blood: ['blood', 'gore', 'red'],
  spirit: ['spirit', 'ghost', 'wraith', 'ethereal', 'soul', 'undead', 'vortex'],
  shadow: ['dark', 'black', 'shadow', 'evil', 'whisper'],
  earth: ['rock', 'stone', 'ground', 'dust', 'pebble', 'slam', 'stomp', 'quake', 'rumble'],
  water: ['water', 'splash', 'wave', 'drown'],
  wind: ['wind', 'tornado', 'whirl', 'swirl', 'gust'],
  explosion: ['explo', 'blast', 'burst', 'boom', 'meteor', 'bomb'],
  nova: ['nova', 'ring', 'shockwave', 'wave', 'aoe', 'area'],
  heal: ['heal', 'health', 'potion', 'mead', 'regen', 'restor'],
  buff: ['buff', 'potion', 'mead', 'swirl', 'glow', 'adrenaline', 'charge'],
  shield: ['shield', 'dome', 'protect', 'ward', 'barrier', 'block'],
  summon: ['summon', 'spawn', 'rise', 'arise', 'appear'],
  hit: ['hit', 'impact', 'strike'],
  death: ['death', 'dies', 'die'],
  cast: ['cast', 'launch', 'shot', 'fire', 'charge', 'start'],
  beam: ['beam', 'breath', 'stream', 'spray', 'line'],
  light: ['glow', 'light', 'flash', 'flare'],
  love: ['heart', 'love', 'pet', 'tame', 'charm', 'sooth'],
  teleport: ['portal', 'teleport', 'vanish', 'despawn', 'appear'],
};

function parseArgs(argv) {
  const o = { prefixes: ['fx', 'vfx', 'sfx'], limit: 15, all: false, words: [] };
  for (let i = 0; i < argv.length; i++) {
    const a = argv[i];
    if (a === '--prefix') o.prefixes = argv[++i].split(',');
    else if (a === '--limit') o.limit = parseInt(argv[++i], 10) || 15;
    else if (a === '--all') o.all = true;
    else if (a === '--fx-dir') o.fxDir = argv[++i];
    else o.words.push(a.toLowerCase());
  }
  return o;
}

function loadRows(prefix) {
  const file = path.join(FX_DIR, prefix + '_descriptions.md');
  if (!fs.existsSync(file)) return [];
  return fs.readFileSync(file, 'utf8').split('\n')
    .filter(l => l.startsWith('| `'))
    .map(l => {
      const c = l.split(' | ');
      const id = c[0].replace(/^\| `|`$/g, '');
      const name = (c[1] || '').trim();
      return {
        prefix, id,
        name: name.replace(' ✔', ''),
        checked: name.includes('✔'),
        desc: (c[2] || '').trim(),
        notes: (c[3] || '').replace(/\s*\|\s*$/, '').trim(),
      };
    });
}

function main() {
  const o = parseArgs(process.argv.slice(2));
  const dirs = [o.fxDir, process.env.VALHEIM_FX_DIR].filter(Boolean).concat(FX_CANDIDATES);
  FX_DIR = dirs.find(d => fs.existsSync(path.join(d, 'fx_descriptions.md')));
  if (!FX_DIR) {
    console.error('FX description files not found. Looked in:\n  ' + dirs.join('\n  ') + '\nPass --fx-dir <GameplayEnhancements/docs/FX> or set VALHEIM_FX_DIR.');
    process.exit(2);
  }
  if (!o.words.length) {
    console.log('usage: node find-fx.js [--prefix fx,vfx,sfx] [--limit 15] [--all] word [word ...]');
    console.log('synonym groups: ' + Object.keys(SYN).join(', '));
    process.exit(1);
  }
  const terms = new Set();
  for (const w of o.words) { terms.add(w); (SYN[w] || []).forEach(s => terms.add(s)); }
  for (const prefix of o.prefixes) {
    const scored = loadRows(prefix).map(r => {
      const hay = (r.id + ' ' + r.name + ' ' + r.desc + ' ' + r.notes).toLowerCase();
      let score = 0;
      const noBolt = hay.replace(/lightning/g, '');
      for (const t of terms) if ((t === 'light' ? noBolt : hay).includes(t)) score += o.words.includes(t) ? 3 : 1;
      if (!score) return null;
      if (/do not use/i.test(r.notes) && !o.all) return null;
      if (r.checked) score += 2;
      if (/not placed by any prefab/.test(r.notes)) score -= 1;
      if (/^Nothing visible/.test(r.desc) && prefix !== 'sfx') score -= 2;
      return Object.assign({ score }, r);
    }).filter(Boolean).sort((a, b) => b.score - a.score || a.id.localeCompare(b.id));
    console.log('## ' + prefix + '_ (' + scored.length + ' matches, top ' + Math.min(o.limit, scored.length) + ')');
    for (const r of scored.slice(0, o.limit)) {
      const d = r.desc.length > 220 ? r.desc.slice(0, 217) + '...' : r.desc;
      console.log('- `' + r.id + '`' + (r.checked ? ' ✔' : '') + ' | ' + d + (r.notes ? ' | ' + r.notes : ''));
    }
    console.log('');
  }
}

main();
