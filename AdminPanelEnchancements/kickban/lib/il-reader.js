// Minimal dependency-free ECMA-335 reader: PE sections, #~ / #Strings / #US / #Blob, tables 0x00 to 0x0A,
// IL decoding with tokens resolved. Needed because .NET reflection on the game DLLs is blocked by Smart App Control.
// module: const { load } = require('./il-reader'); const d = load(pathOrBuffer);
// cli:    node il-reader.js <dll> <TypeName> <methodNameRegex> [--list]
const fs = require('fs');

function load(src) {
  const b = Buffer.isBuffer(src) ? src : fs.readFileSync(src);
  const u16 = o => b.readUInt16LE(o), u32 = o => b.readUInt32LE(o);

  const pe = u32(0x3c), nsec = u16(pe + 6), optSize = u16(pe + 20), opt = pe + 24;
  const dd = opt + (u16(opt) === 0x20b ? 112 : 96);
  const secs = []; for (let i = 0; i < nsec; i++) { const s = opt + optSize + i * 40; secs.push({ va: u32(s + 12), vs: u32(s + 8), raw: u32(s + 20) }); }
  const rva = r => { for (const s of secs) if (r >= s.va && r < s.va + s.vs) return r - s.va + s.raw; throw new Error('rva ' + r); };
  const cli = rva(u32(dd + 14 * 8)), md = rva(u32(cli + 8));
  const vlen = u32(md + 12); let p = md + 16 + vlen + 2; const nstr = u16(p); p += 2;
  const streams = {}; for (let i = 0; i < nstr; i++) { const off = u32(p), size = u32(p + 4); p += 8; let n = ''; while (b[p]) n += String.fromCharCode(b[p++]); p = (p + 4) & ~3; streams[n] = { off: md + off, size }; }

  const T = streams['#~'] || streams['#-'], hs = b[T.off + 6];
  const valid = b.readBigUInt64LE(T.off + 8); const rows = []; p = T.off + 24;
  for (let i = 0; i < 64; i++) rows[i] = (valid >> BigInt(i)) & 1n ? (p += 4, u32(p - 4)) : 0;
  const SI = hs & 1 ? 4 : 2, GI = hs & 2 ? 4 : 2, BI = hs & 4 ? 4 : 2;
  const idx = t => rows[t] < 65536 ? 2 : 4, coded = (bits, ts) => Math.max(...ts.map(t => rows[t])) < (1 << (16 - bits)) ? 2 : 4;
  const TDOR = coded(2, [2, 1, 0x1b]), RS = coded(2, [0, 0x1a, 0x23, 1]), MRP = coded(3, [2, 1, 0x1a, 6, 0x1b]);
  const schema = {
    0: [2, SI, GI, GI, GI], 1: [RS, SI, SI], 2: [4, SI, SI, TDOR, idx(4), idx(6)], 3: [idx(4)], 4: [2, SI, BI], 5: [idx(6)],
    6: [4, 2, 2, SI, BI, idx(8)], 7: [idx(8)], 8: [2, 2, SI], 9: [idx(2), TDOR], 10: [MRP, SI, BI],
  };
  const tab = {}; p = T.off + 24 + 4 * rows.filter(Boolean).length;
  for (let t = 0; t <= 10; t++) { const cols = schema[t], size = cols.reduce((a, c) => a + c, 0); tab[t] = { off: p, size, cols }; p += size * rows[t]; }
  const rd = (o, n) => n === 2 ? u16(o) : u32(o);
  const row = (t, i) => { const { off, size, cols } = tab[t]; let o = off + (i - 1) * size; return cols.map(c => { const v = rd(o, c); o += c; return v; }); };
  const str = o => { let s = '', q = streams['#Strings'].off + o; while (b[q]) s += String.fromCharCode(b[q++]); return s; };
  const us = o => { let q = streams['#US'].off + o, len = b[q]; if (len & 0x80) { if ((len & 0xc0) === 0x80) { len = ((len & 0x3f) << 8) | b[q + 1]; q += 2; } else { len = ((len & 0x1f) << 24) | (b[q + 1] << 16) | (b[q + 2] << 8) | b[q + 3]; q += 4; } } else q += 1; return b.toString('utf16le', q, q + len - 1); };
  const blob = o => { let q = streams['#Blob'].off + o, len = b[q]; if (len & 0x80) { if ((len & 0xc0) === 0x80) { len = ((len & 0x3f) << 8) | b[q + 1]; q += 2; } else { len = ((len & 0x1f) << 24) | (b[q + 1] << 16) | (b[q + 2] << 8) | b[q + 3]; q += 4; } } else q += 1; return b.subarray(q, q + len); };

  const types = []; for (let i = 1; i <= rows[2]; i++) { const r = row(2, i); types.push({ i, name: str(r[1]), ns: str(r[2]), fl: r[4], ml: r[5] }); }
  const ownerOfMethod = m => { let o; for (const t of types) if (t.ml <= m) o = t; return o; };
  const ownerOfField = f => { let o; for (const t of types) if (t.fl <= f) o = t; return o; };
  const methodName = m => str(row(6, m)[3]);
  const mname = m => { const t = ownerOfMethod(m); return `${t ? t.name : '?'}::${methodName(m)}`; };
  const methodSig = m => Buffer.from(blob(row(6, m)[4]));
  const refName = i => { const r = row(10, i), tag = r[0] & 7, ix = r[0] >> 3; let par = '?';
    if (tag === 1) par = str(row(1, ix)[1]); else if (tag === 0) par = types[ix - 1].name; else if (tag === 4) par = 'TypeSpec#' + ix; return `${par}::${str(r[1])}`; };
  const tok = t => { const tb = t >>> 24, i = t & 0xffffff;
    if (tb === 0x70) return JSON.stringify(us(i)); if (tb === 6) return mname(i); if (tb === 10) return refName(i);
    if (tb === 4) { const o = ownerOfField(i); return `${o ? o.name : '?'}.${str(row(4, i)[1])}`; }
    if (tb === 2) return types[i - 1].name; if (tb === 1) return str(row(1, i)[1]); return '0x' + t.toString(16); };

  const N1 = { 0: 'nop', 2: 'ldarg.0', 3: 'ldarg.1', 4: 'ldarg.2', 5: 'ldarg.3', 6: 'ldloc.0', 7: 'ldloc.1', 8: 'ldloc.2', 9: 'ldloc.3', 0xa: 'stloc.0', 0xb: 'stloc.1', 0xc: 'stloc.2', 0xd: 'stloc.3', 0x14: 'ldnull', 0x16: 'ldc.i4.0', 0x17: 'ldc.i4.1', 0x25: 'dup', 0x26: 'pop', 0x2a: 'ret', 0x28: 'call', 0x6f: 'callvirt', 0x72: 'ldstr', 0x73: 'newobj', 0x7b: 'ldfld', 0x7d: 'stfld', 0x7e: 'ldsfld', 0x80: 'stsfld', 0x8c: 'box', 0x8d: 'newarr', 0xd0: 'ldtoken', 0x2c: 'brfalse.s', 0x2d: 'brtrue.s', 0x2b: 'br.s', 0x39: 'brfalse', 0x3a: 'brtrue', 0x38: 'br', 0x11: 'ldloc.s', 0x13: 'stloc.s', 0x1f: 'ldc.i4.s', 0x20: 'ldc.i4', 0x74: 'castclass', 0x75: 'isinst', 0xdd: 'leave', 0xde: 'leave.s', 0xdc: 'endfinally', 0x9a: 'ldelem.ref', 0xa2: 'stelem.ref', 0x8e: 'ldlen' };
  const size1 = op => { if ([0x0e, 0x0f, 0x10, 0x11, 0x12, 0x13, 0x1f, 0xde].includes(op) || (op >= 0x2b && op <= 0x37)) return 1;
    if ([0x21, 0x23].includes(op)) return 8;
    if ([0x20, 0x22, 0x27, 0x28, 0x29, 0x6f, 0x70, 0x71, 0x72, 0x73, 0x74, 0x75, 0x79, 0x7b, 0x7c, 0x7d, 0x7e, 0x7f, 0x80, 0x81, 0x8c, 0x8d, 0x8f, 0xa3, 0xa4, 0xa5, 0xc2, 0xc6, 0xd0, 0xdd].includes(op) || (op >= 0x38 && op <= 0x44)) return 4; return 0; };
  const isTok = op => [0x27, 0x28, 0x29, 0x6f, 0x70, 0x71, 0x72, 0x73, 0x74, 0x75, 0x79, 0x7b, 0x7c, 0x7d, 0x7e, 0x7f, 0x80, 0x81, 0x8c, 0x8d, 0x8f, 0xa3, 0xa4, 0xa5, 0xc2, 0xc6, 0xd0].includes(op);

  // -> { codeOffset, length, instrs: [{ at, name, token?, argAt?, text }] } ; token is the raw 4-byte value, argAt its file offset
  function decode(m) {
    const r = row(6, m); if (!r[0]) return { codeOffset: 0, length: 0, instrs: [] };
    let o = rva(r[0]), code, len; const h = b[o];
    if ((h & 3) === 2) { len = h >> 2; code = o + 1; } else { len = u32(o + 4); code = o + ((u16(o) >> 12) * 4); }
    const instrs = []; let q = code;
    while (q < code + len) {
      const at = q - code; let op = b[q++], name, arg = '', token, argAt;
      if (op === 0xfe) { const o2 = b[q++]; name = 'fe' + o2.toString(16); const s = [6, 7, 0x15, 0x16, 0x1c].includes(o2) ? 4 : (o2 >= 9 && o2 <= 0xe) ? 2 : [0x12, 0x19].includes(o2) ? 1 : 0;
        if (s === 4) { token = u32(q); argAt = q; arg = tok(token); if (o2 === 6) name = 'ldftn'; } q += s; }
      else if (op === 0x45) { const n = u32(q); q += 4 + 4 * n; name = 'switch'; }
      else { name = N1[op] || ('op' + op.toString(16)); const s = size1(op);
        if (s === 4 && isTok(op)) { token = u32(q); argAt = q; arg = tok(token); } else if (s === 1) arg = b.readInt8(q); else if (s === 4) arg = b.readInt32LE(q); q += s; }
      instrs.push({ at, name, token, argAt, text: `${name} ${arg}` });
    }
    return { codeOffset: code, length: len, instrs };
  }
  const disasm = m => { const d = decode(m); return d.instrs.length || d.length ? [`  code at file offset 0x${d.codeOffset.toString(16)}, ${d.length} bytes`, ...d.instrs.map(i => `  ${String(i.at).padStart(4)} ${i.text}`)] : ['  (no body)']; };

  // methods of every type called typeName (several nested types are called <>c): [{ m, type, name }]
  function methodsOf(typeName, re) {
    const out = [];
    for (const t of types.filter(x => x.name === typeName)) {
      const ti = types.indexOf(t), end = ti + 1 < types.length ? types[ti + 1].ml : rows[6] + 1;
      for (let m = t.ml; m < end; m++) { const n = methodName(m); if (!re || re.test(n)) out.push({ m, type: t, name: n }); }
    }
    return out;
  }

  return { buf: b, types, rows, str, us, blob, tok, row, mname, methodName, methodSig, ownerOfMethod, decode, disasm, methodsOf };
}

module.exports = { load };

if (require.main === module) {
  const [dll, typeName, methodRe, ...rest] = process.argv.slice(2);
  if (!dll || !typeName) { console.error('usage: node il-reader.js <dll> <TypeName> <methodNameRegex> [--list]'); process.exit(2); }
  const d = load(dll);
  for (const { m, type, name } of d.methodsOf(typeName, new RegExp(methodRe || '.'))) {
    console.log(`== ${d.mname(m)} (#${m}, type #${type.i})`);
    if (!rest.includes('--list')) console.log(d.disasm(m).join('\n'));
  }
}
