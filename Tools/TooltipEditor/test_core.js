// pulls the page's pure template core out of index.html and round-trips every real template
const fs = require('fs');
const path = require('path');
const html = fs.readFileSync(path.join(__dirname, 'index.html'), 'utf8');
const core = html.match(/<script id="core">([\s\S]*?)<\/script>/)[1];
const page = html.match(/<\/script>\s*<script>([\s\S]*?)<\/script>/)[1];
// syntax check both scripts
new Function(core); new Function(page);
const T = new Function(core + '; return { parseTemplate, serialize, sameItems, humanize };')();
const { parseTemplate, serialize, sameItems, humanize } = T;
const data = JSON.parse(fs.readFileSync(path.join(__dirname, 'descriptions.json'), 'utf8'));
let n = 0, fail = 0, holes = 0, raws = new Set();
const check = (t, where) => {
  if (!t) return;
  n++;
  const a = parseTemplate(t);
  const back = serialize(a);
  const b = parseTemplate(back);
  holes += a.filter(x => x.t === 'h').length;
  a.filter(x => x.t === 'r').forEach(x => raws.add(x.raw));
  // every hole must come back exactly, and the visible content and styles must match
  const holesA = a.filter(x => x.t === 'h').map(x => x.expr).join('|');
  const holesB = (back.match(/\{(?!\{)/g) || []).length;
  if (!sameItems(a, b) || serialize(b) !== back) { fail++; if (fail < 6) console.log('FAIL', where, '\n  in :', t.slice(0, 160), '\n  out:', back.slice(0, 160)); }
};
for (const p of data.plants) {
  check(p.overview, p.id + ' overview'); check(p.nameTemplate, p.id + ' name');
  for (const s of p.sections) {
    check(s.overview, p.id + ' ' + s.key + ' overview'); check(s.details, p.id + ' ' + s.key + ' details');
    s.upgrades.forEach((u, i) => check(u, p.id + ' ' + s.key + ' upgrade ' + i));
    check(s.maxBonus, p.id + ' ' + s.key + ' max'); check(s.maxBonusDetails, p.id + ' ' + s.key + ' maxd');
  }
}
console.log('templates:', n, 'round-trip failures:', fail, 'holes:', holes);
console.log('unknown TMP tags kept as-is:', [...raws].slice(0, 10));
// an edit: make the first word of a template orange and bold, check the C# it produces
const t = data.plants.find(p => p.id === 'AcornKnight').sections[0].upgrades[0];
const items = parseTemplate(t);
for (let k = 0; k < 8; k++) items[k] = { ...items[k], st: { ...items[k].st, c: 'orange', b: true } };
console.log('edited ->', serialize(items));
console.log('humanize samples:', ['adpl * effectivePath1Level:F0', '(AcornData?.baseBashDamagePercent ?? 0.5f) * 100f:F0', 'armor * BashDamagePercent:F0'].map(humanize));
// escaping round trip of awkward characters
const odd = 'He said \\"hi\\" {{braces}} 50% <size=85%>small</size>\\n<sprite=3>';
console.log('odd:', serialize(parseTemplate(odd)) === odd ? 'exact' : serialize(parseTemplate(odd)));
