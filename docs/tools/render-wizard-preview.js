// Renders a preview of Visual Studio's "Additional information" page for this template,
// built from .template.config/template.json + ide.host.json: the same options, order, labels,
// choices and defaults Visual Studio shows. The layout copies Visual Studio 2022's page
// (one column, an (i) tooltip per option, tags next to the template name).
// It is an illustration for the README, not a capture of Visual Studio.
//
//   node docs/tools/render-wizard-preview.js        (needs Playwright: npm i -D playwright)
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
const info = '<span class="i" aria-hidden="true">i</span>';

function control(item, state) {
  const sym = template.symbols[item.id];
  const label = (item.name && item.name.text) || sym.displayName || item.id;
  const value = state.values[item.id] ?? sym.defaultValue;
  if (sym.datatype === 'bool') {
    const on = String(value) === 'true';
    return `<div class="row chk"><span class="box ${on ? 'on' : ''}">${on ? '✓' : ''}</span><span>${esc(label)}</span>${info}</div>`;
  }
  if (sym.datatype === 'choice') {
    const cur = sym.choices.find(c => c.choice === value) || sym.choices[0];
    const open = state.open === item.id;
    const list = open ? `<div class="list">${sym.choices.map(c =>
      `<div class="opt ${c.choice === cur.choice ? 'sel' : ''}">${esc(c.displayName || c.choice)}</div>`).join('')}</div>` : '';
    return `<div class="lbl">${esc(label)}${info}</div>
      <div class="dd ${open ? 'open' : ''}"><span>${esc(cur.displayName || cur.choice)}</span><span class="car">▾</span>${list}</div>`;
  }
  return `<div class="lbl">${esc(label)}${info}</div><div class="txt">${esc(state.values[item.id] || '')}</div>`;
}

function page(state) {
  const items = ide.symbolInfo.filter(s => s.isVisible && template.symbols[s.id]);
  const tags = ['C#', ...(template.classifications || [])].map(t => `<span class="tag">${esc(t)}</span>`).join('');
  return `<!doctype html><html class="${state.theme}"><head><meta charset="utf-8"><style>
    :root { --bg:#ffffff; --fg:#1e1e1e; --muted:#5f5f5f; --ctl:#ffffff; --bd:#a0a0a0; --acc:#005fb8; --tag:#ededed; --sel:#cce4f7; --chk:#005fb8; }
    .dark { --bg:#1f1f1f; --fg:#f1f1f1; --muted:#b3b3b3; --ctl:#2b2b2b; --bd:#3f3f46; --acc:#4cc2ff; --tag:#333337; --sel:#094771; --chk:#4cc2ff; }
    * { box-sizing:border-box } body { margin:0; background:var(--bg); color:var(--fg); font:13px "Segoe UI", system-ui, sans-serif; }
    .wrap { padding:24px 18px 20px; width:1040px; position:relative; }
    h1 { font-weight:400; font-size:26px; margin:0 0 26px }
    .tpl { display:flex; flex-wrap:wrap; gap:8px; align-items:center; margin-bottom:22px }
    .tpl .name { font-size:19px; margin-right:10px }
    .tag { background:var(--tag); padding:3px 9px; border-radius:2px; font-size:12px; white-space:nowrap }
    .col { width:578px }
    .lbl { margin:10px 0 6px; display:flex; align-items:center; gap:6px }
    .i { display:inline-flex; width:14px; height:14px; border:1px solid var(--acc); color:var(--acc); border-radius:50%;
         font:600 9px "Segoe UI", sans-serif; align-items:center; justify-content:center; margin-left:4px }
    .dd, .txt { position:relative; height:26px; padding:4px 7px; background:var(--ctl); border:1px solid var(--bd);
                display:flex; justify-content:space-between; align-items:center; margin-bottom:4px }
    .dd.open { border-color:var(--acc) } .car { font-size:10px; color:var(--muted) }
    .list { position:absolute; left:-1px; right:-1px; top:25px; background:var(--ctl); border:1px solid var(--acc); z-index:5;
            box-shadow:0 8px 18px rgba(0,0,0,.35); max-height:430px; overflow:hidden }
    .opt { padding:4px 7px } .opt.sel { background:var(--sel) }
    .row.chk { display:flex; align-items:center; gap:8px; margin:9px 0 }
    .box { width:15px; height:15px; border:1px solid var(--bd); background:var(--ctl); display:inline-flex;
           align-items:center; justify-content:center; font-size:11px; color:var(--chk) }
    .box.on { border-color:var(--fg) }
    .foot { display:flex; justify-content:flex-end; gap:6px; margin-top:34px }
    .btn { width:95px; height:30px; display:inline-flex; align-items:center; justify-content:center; background:var(--tag); border:1px solid var(--bd) }
    .btn.pri { background:var(--acc); border-color:var(--acc); color:#000 }
    html:not(.dark) .btn.pri { color:#fff }
    .note { position:absolute; left:18px; bottom:26px; color:var(--muted); font-size:11px; font-style:italic }
  </style></head><body><div class="wrap">
    <h1>Additional information</h1>
    <div class="tpl"><span class="name">${esc(template.name)}</span>${tags}</div>
    <div class="col">${items.map(i => control(i, state)).join('\n')}</div>
    <div class="foot"><span class="btn">Back</span><span class="btn pri">Create</span></div>
    <div class="note">Preview rendered from ide.host.json / template.json, not a Visual Studio screenshot.</div>
  </div></body></html>`;
}

async function shot(pg, file, state, clipToOpen) {
  await pg.setContent(page(state));
  if (clipToOpen) {
    // Crop around the open drop-down so the picker is readable in the README.
    const dd = await pg.locator('.dd.open').boundingBox();
    const list = await pg.locator('.dd.open .list').boundingBox();
    const top = Math.max(0, dd.y - 120);
    const bottom = list.y + list.height + 30;
    await pg.screenshot({ path: path.join(outDir, file), fullPage: true, clip: { x: 0, y: top, width: 640, height: bottom - top } });
  } else {
    await pg.screenshot({ path: path.join(outDir, file), fullPage: true });
  }
  console.log('wrote', file);
}

(async () => {
  const browser = await chromium.launch();
  const pg = await browser.newPage({ viewport: { width: 1040, height: 900 } });
  const values = {};
  await shot(pg, 'wizard-additional-information-dark.png', { theme: 'dark', values });
  await shot(pg, 'wizard-additional-information.png', { theme: 'light', values });
  const pickers = [
    ['wizard-framework-picker.png', 'Framework'],
    ['wizard-database-picker.png', 'DatabaseProvider'],
    ['wizard-sql-version-picker.png', 'SqlServerVersion'],
    ['wizard-internal-auth-picker.png', 'InternalAuthentication'],
    ['wizard-jquery-picker.png', 'JQueryVersion'],
    ['wizard-jquery-ui-picker.png', 'JQueryUIVersion'],
    ['wizard-jquery-ui-theme-picker.png', 'JQueryUITheme'],
    ['wizard-bootstrap-picker.png', 'BootstrapVersion'],
    ['wizard-bootstrap-theme-picker.png', 'BootstrapTheme'],
    ['wizard-fontawesome-picker.png', 'FontAwesomeVersion'],
  ];
  for (const [file, open] of pickers) {
    await shot(pg, file, { theme: 'dark', values, open }, true);
  }
  await browser.close();
})();
