// Renders a preview of Visual Studio's "Additional information" page for this template,
// built from .template.config/template.json + ide.host.json (labels, choices and defaults are the real ones).
// It is an illustration for the README, not a capture of Visual Studio.
//
//   npm i -D playwright   (or use a global Playwright install)
//   node docs/tools/render-wizard-preview.js
//
const fs = require('fs');
const path = require('path');
const { chromium } = require('playwright');

const root = path.resolve(__dirname, '..', '..');
const cfg = path.join(root, 'templates', 'RazorSolution', '.template.config');
const template = JSON.parse(fs.readFileSync(path.join(cfg, 'template.json'), 'utf8'));
const ide = JSON.parse(fs.readFileSync(path.join(cfg, 'ide.host.json'), 'utf8'));
const outDir = path.join(root, 'docs', 'images');

const esc = s => String(s).replace(/[&<>"]/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' }[c]));

function field(info, openId) {
  const sym = template.symbols[info.id];
  const label = (info.name && info.name.text) || sym.displayName || info.id;
  const help = (info.description && info.description.text) || sym.description || '';
  if (sym.datatype === 'bool') {
    const checked = sym.defaultValue === 'true';
    return `<label class="check" title="${esc(help)}"><span class="box ${checked ? 'on' : ''}">${checked ? '✓' : ''}</span>${esc(label)}</label>`;
  }
  if (sym.datatype === 'choice') {
    const def = sym.choices.find(c => c.choice === sym.defaultValue) || sym.choices[0];
    const list = info.id === openId
      ? `<div class="list">${sym.choices.map(c => `<div class="opt ${c.choice === def.choice ? 'sel' : ''}">${esc(c.displayName || c.choice)}</div>`).join('')}</div>`
      : '';
    return `<div class="fld"><div class="lbl">${esc(label)} <span class="i" title="${esc(help)}">ⓘ</span></div>
      <div class="dd ${info.id === openId ? 'open' : ''}"><span>${esc(def.displayName || def.choice)}</span><span class="chev">⌄</span>${list}</div></div>`;
  }
  return `<div class="fld"><div class="lbl">${esc(label)} <span class="i" title="${esc(help)}">ⓘ</span></div>
    <div class="txt"><span class="ph">${esc(help)}</span></div></div>`;
}

function page(openId, theme) {
  const visible = ide.symbolInfo.filter(s => s.isVisible && template.symbols[s.id]);
  const groups = [
    ['Framework', ['Framework']],
    ['Projects', ['IncludeInternal', 'IncludeExternal', 'IncludeApi', 'IncludeServices', 'IncludeSqlRepository']],
    ['Database', ['DatabaseProvider', 'SqlServerVersion', 'PostgresVersion', 'DbAuthentication', 'DbServer', 'DbName', 'DbUser', 'DbPassword', 'ConnectionString']],
    ['Client-side libraries (LibMan)', ['JQueryVersion', 'JQueryUIVersion', 'BootstrapVersion', 'FontAwesomeVersion']],
  ];
  const body = groups.map(([title, ids]) => {
    const items = visible.filter(v => ids.includes(v.id)).sort((a, b) => ids.indexOf(a.id) - ids.indexOf(b.id));
    if (!items.length) return '';
    const cls = title === 'Projects' ? 'checks' : 'grid';
    return `<section><h3>${esc(title)}</h3><div class="${cls}">${items.map(i => field(i, openId)).join('')}</div></section>`;
  }).join('');
  const tags = (template.classifications || []).slice(0, 5).map(t => `<span class="tag">${esc(t)}</span>`).join('');
  return `<!doctype html><html class="${theme}"><head><meta charset="utf-8"><style>
    :root { --bg:#f5f5f5; --fg:#1e1e1e; --muted:#616161; --ctl:#fff; --bd:#bdbdbd; --acc:#5c2d91; --tag:#e6e6e6; --sel:#e8e0f3; }
    .dark { --bg:#1f1f1f; --fg:#f1f1f1; --muted:#a8a8a8; --ctl:#2d2d30; --bd:#555; --acc:#b392f0; --tag:#3a3a3d; --sel:#3e3653; }
    * { box-sizing:border-box } body { margin:0; background:var(--bg); color:var(--fg); font:13px "Segoe UI", system-ui, sans-serif; }
    .wrap { padding:28px 40px 24px; width:1100px; min-height:100vh; }
    h1 { font-weight:300; font-size:26px; margin:0 0 14px }
    .tpl { display:flex; gap:10px; align-items:center; font-size:15px; margin-bottom:6px }
    .tags { margin-bottom:18px } .tag { background:var(--tag); padding:2px 8px; margin-right:6px; border-radius:2px; font-size:12px }
    h3 { font-size:13px; font-weight:600; margin:16px 0 8px; color:var(--muted); text-transform:uppercase; letter-spacing:.04em }
    .grid { display:grid; grid-template-columns:minmax(0,1fr) minmax(0,1fr); gap:10px 28px }
    .checks { display:grid; grid-template-columns:minmax(0,1fr) minmax(0,1fr); gap:8px 28px }
    .lbl { margin-bottom:4px } .i { color:var(--muted); font-size:11px }
    .dd, .txt { position:relative; background:var(--ctl); border:1px solid var(--bd); height:30px; padding:5px 8px; display:flex; justify-content:space-between; align-items:center }
    .dd.open { border-color:var(--acc) } .chev { color:var(--muted) }
    .ph { color:var(--muted); font-size:12px; white-space:nowrap; overflow:hidden; text-overflow:ellipsis }
    .list { position:absolute; left:-1px; right:-1px; top:29px; background:var(--ctl); border:1px solid var(--acc); z-index:5; box-shadow:0 6px 18px rgba(0,0,0,.25) }
    .opt { padding:6px 8px } .opt.sel { background:var(--sel) }
    .check { display:flex; align-items:center; gap:8px } .box { width:16px; height:16px; border:1px solid var(--bd); background:var(--ctl); display:inline-flex; align-items:center; justify-content:center; font-size:12px }
    .box.on { background:var(--acc); border-color:var(--acc); color:#fff }
    .foot { display:flex; justify-content:space-between; align-items:center; margin-top:26px }
    .note { color:var(--muted); font-size:11px; font-style:italic }
    .btn { display:inline-block; padding:6px 22px; border:1px solid var(--bd); background:var(--ctl); margin-left:8px }
    .btn.pri { background:var(--acc); border-color:var(--acc); color:#fff }
  </style></head><body><div class="wrap">
    <h1>Additional information</h1>
    <div class="tpl"><strong>${esc(template.name)}</strong></div>
    <div class="tags"><span class="tag">C#</span>${tags}</div>
    ${body}
    <div class="foot"><span class="note">Preview rendered from ide.host.json / template.json. Not a Visual Studio screenshot.</span>
      <span><span class="btn">Back</span><span class="btn pri">Create</span></span></div>
  </div></body></html>`;
}

(async () => {
  const browser = await chromium.launch();
  const pg = await browser.newPage({ viewport: { width: 1100, height: 900 } });
  const shots = [
    ['wizard-additional-information.png', null, 'light'],
    ['wizard-additional-information-dark.png', null, 'dark'],
    ['wizard-framework-picker.png', 'Framework', 'light'],
    ['wizard-bootstrap-picker.png', 'BootstrapVersion', 'light'],
    ['wizard-sql-version-picker.png', 'SqlServerVersion', 'light'],
  ];
  for (const [file, openId, theme] of shots) {
    await pg.setContent(page(openId, theme));
    if (openId) await pg.addStyleTag({ content: '.wrap{padding-bottom:140px}' });
    await pg.screenshot({ path: path.join(outDir, file), fullPage: true });
    console.log('wrote', file);
  }
  await browser.close();
})();
