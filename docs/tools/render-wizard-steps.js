// Renders previews of the multi-step Visual Studio wizard (wizard/src/RazorSolution.Wizard/WizardWindow.cs)
// from the same sources it uses at run time: WizardSteps.json plus the template's template.json /
// ide.host.json / dotnetcli.host.json. Layout mirrors WizardWindow (step list, title, fields, Back/Next).
// Illustrations for the README, not captures of the WPF window.
//
//   node docs/tools/render-wizard-steps.js [pg-result.json] [sql-result.json]
//   (optional: real ConnectionTester outputs to show in the Test connection previews)
//
const fs = require('fs');
const path = require('path');
const { chromium } = require('playwright');

const root = path.resolve(__dirname, '..', '..');
const cfg = path.join(root, 'templates', 'RazorSolution', '.template.config');
const read = f => JSON.parse(fs.readFileSync(f, 'utf8'));
const template = read(path.join(cfg, 'template.json'));
const ide = read(path.join(cfg, 'ide.host.json'));
const cli = read(path.join(cfg, 'dotnetcli.host.json'));
const wiz = read(path.join(root, 'wizard', 'src', 'RazorSolution.Wizard.Core', 'WizardSteps.json'));
const outDir = path.join(root, 'docs', 'images');
const results = process.argv.slice(2).map(f => (fs.existsSync(f) ? read(f) : null));

const esc = s => String(s ?? '').replace(/[&<>"]/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' }[c]));
const ideInfo = Object.fromEntries(ide.symbolInfo.map(s => [s.id, s]));
const field = id => {
  const s = template.symbols[id];
  return {
    id, kind: s.datatype === 'bool' ? 'bool' : s.datatype === 'choice' ? 'choice' : 'text',
    label: ideInfo[id]?.name?.text || s.displayName || id,
    description: ideInfo[id]?.description?.text || s.description || '',
    def: s.defaultValue ?? '', choices: s.choices || [],
    cliName: id === 'Framework' ? '--framework' : '--' + (cli.symbolInfo[id]?.longName || id),
  };
};
const matches = (conds, v) => !conds || conds.length === 0 || conds.some(c => Object.entries(c).every(([k, vals]) => vals.includes(v[k])));

function defaults(overrides) {
  const v = {};
  for (const [id, s] of Object.entries(template.symbols)) if (s.type === 'parameter') v[id] = s.defaultValue ?? '';
  return Object.assign(v, overrides);
}

function connectionString(v, name) {
  if (v.ConnectionString) return v.ConnectionString;
  const p = v.DatabaseProvider, azure = p === 'SqlServer' && v.SqlServerVersion === 'AzureSql';
  const server = v.DbServer || (p === 'SqlServer' ? (azure ? 'your-server.database.windows.net' : '(localdb)\\MSSQLLocalDB') : 'localhost');
  const db = v.DbName || name, user = v.DbUser || (p === 'PostgreSQL' ? 'postgres' : 'sa'), pw = v.DbPassword ? '****' : 'Your_password123';
  if (p === 'Sqlite') return `Data Source=${db}.db`;
  if (p === 'PostgreSQL') return `Host=${server};Port=5432;Database=${db};Username=${user};Password=${pw}`;
  if (v.DbAuthentication === 'Integrated') return `Server=${server};Database=${db};Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True`;
  return `Server=${server};Database=${db};User Id=${user};Password=${pw};MultipleActiveResultSets=true;TrustServerCertificate=True`;
}

function command(v, name, dir) {
  const parts = ['dotnet', 'new', 'razorsln', '-n', name, '-o', `"${dir}"`];
  for (const [id, s] of Object.entries(template.symbols)) {
    if (s.type !== 'parameter' || (v[id] ?? '') === (s.defaultValue ?? '')) continue;
    const f = field(id);
    parts.push(f.cliName, id === 'DbPassword' ? '****' : (/\s/.test(v[id]) ? `"${v[id]}"` : v[id]));
  }
  return parts.join(' ');
}

const TL = wiz.tiles || {};
const icon = (ic, fallback) => `<span class="ico" style="background:${esc(ic?.color || '#5c2d91')}">${ic?.fa ? `<i class="${esc(ic.fa)}"></i>` : esc(fallback.split(/[ .\-]/).filter(Boolean).slice(0, 2).map(w => w[0].toUpperCase()).join(''))}</span>`;

// Mirrors wizard/src/RazorSolution.Wizard/Tiles.cs: radio tiles for choices, toggle tiles for features.
function choiceTiles(f, v) {
  const layout = (TL.fields || {})[f.id];
  return `<div class="tiles ${layout}">` + f.choices.map(c => {
    const h = ((TL.choices || {})[f.id] || {})[c.choice] || {}, on = v[f.id] === c.choice, disp = c.displayName || c.choice;
    let body;
    if (layout === 'swatch') {
      const sw = (h.swatch && h.swatch.length ? h.swatch : ['#ddd']).concat(['transparent', 'transparent']).slice(0, 3);
      body = `<span class="sw">${sw.map(x => `<i style="background:${esc(x)}"></i>`).join('')}</span><b>${esc(disp.replace('Bootswatch ', ''))}</b>`;
    } else if (layout === 'compact') {
      body = `<b>${esc(disp)}</b>${h.badge ? `<span class="badge">${esc(h.badge)}</span>` : ''}`;
    } else {
      body = `${icon(h.icon, disp)}<span class="t"><b>${esc(disp)} ${h.badge ? `<span class="badge">${esc(h.badge)}</span>` : ''}</b>${h.note ? `<span>${esc(h.note)}</span>` : ''}${h.recommended ? '<em>Recommended</em>' : ''}</span>`;
    }
    return `<div class="tile ${on ? 'on' : ''}">${on ? '<span class="tick">✓</span>' : ''}${body}</div>`;
  }).join('') + '</div>';
}
function toggleTile(f, v) {
  const on = v[f.id] === 'true', m = f.label.match(/\(([^)]+)\)\s*$/);
  const short = (TL.shortLabels || {})[f.id] || f.label;
  return `<div class="tile ${on ? 'on' : ''}">${on ? '<span class="tick">✓</span>' : ''}${icon((TL.icons || {})[f.id], short)}<span class="t"><b>${esc(short)}</b><span>${esc(m ? m[1] : f.description)}</span></span></div>`;
}
function control(f, v) {
  const head = `<div class="lbl">${esc(f.label)}<span class="i">i</span></div>`;
  if (f.kind === 'choice' && (TL.fields || {})[f.id]) return `<div class="row">${head}${choiceTiles(f, v)}</div>`;
  if (f.kind === 'bool') return `<div class="row"><div class="tiles">${toggleTile(f, v)}</div></div>`;
  let input;
  if (f.kind === 'choice') {
    const cur = f.choices.find(c => c.choice === v[f.id]) || f.choices[0];
    input = `<div class="dd"><span>${esc(cur.displayName || cur.choice)}</span><span class="car">⌄</span></div>`;
  } else {
    const pw = (wiz.passwordFields || []).includes(f.id);
    input = `<div class="txt">${esc(pw && v[f.id] ? '•'.repeat(v[f.id].length) : v[f.id])}</div>`;
  }
  return `<div class="row">${head}${input}<div class="help">${esc(f.description)}</div></div>`;
}
function fieldsHtml(s, v) {
  const groups = (TL.groups || {})[s.id] || [], grouped = new Set(groups.flatMap(g => g[1])), done = new Set();
  let html = '';
  for (const id of s.fields) {
    if (grouped.has(id)) {
      const g = groups.find(x => x[1].includes(id));
      if (!done.has(g[0])) {
        done.add(g[0]);
        const ids = g[1].filter(x => s.fields.includes(x) && matches(wiz.fieldRules[x], v));
        html += `<div class="row"><div class="grp">${esc(g[0])}</div><div class="tiles">${ids.map(x => toggleTile(field(x), v)).join('')}</div></div>`;
      }
      continue;
    }
    if (matches(wiz.fieldRules[id], v)) html += control(field(id), v);
  }
  return html;
}

function page({ step, v, name, dir, test }) {
  const steps = wiz.steps.filter(s => matches(s.when, v));
  const idx = steps.findIndex(s => s.id === step);
  const s = steps[idx];
  let body = '';
  if (s.id === 'summary') {
    const projects = [];
    if (v.IncludeInternal === 'true') projects.push(name + '.Internal');
    if (v.IncludeExternal === 'true') projects.push(name + '.External');
    projects.push(name + '.API');
    if (v.IncludeServices === 'true') projects.push(name + '.Services');
    projects.push(name + '.SqlRepository');
    if (v.IncludeTests === 'true') projects.push(name + '.Tests');
    const disp = id => { const f = field(id); return (f.choices.find(c => c.choice === v[id]) || {}).displayName || v[id]; };
    const line = (l, x) => `<div class="sum"><span>${esc(l)}</span><span>${esc(x)}</span></div>`;
    const changed = Object.keys(template.symbols).filter(id => template.symbols[id].type === 'parameter' && template.symbols[id].datatype === 'bool' && v[id] !== template.symbols[id].defaultValue)
      .map(id => (v[id] === 'true' ? '+ ' : '− ') + field(id).label).join('\n') || '(defaults)';
    body = line('Location', dir) + line('Framework', disp('Framework')) + line('Projects', projects.join(', ')) +
      line('Database', connectionString(v, name)) +
      line('Front end', `${disp('JQueryVersion')}, ${disp('JQueryUIVersion')} (${disp('JQueryUITheme')}), ${disp('BootstrapVersion')} (${disp('BootstrapTheme')}), ${disp('FontAwesomeVersion')}`) +
      line('Changed options', changed) +
      `<div class="lbl" style="margin-top:12px;font-weight:600">Command</div><div class="cmd">${esc(command(v, name, dir))}</div><span class="btn sm">Copy command</span>`;
  } else {
    body = fieldsHtml(s, v);
    if (s.testConnection) {
      const r = test;
      const res = r ? `<span class="res ${r.success ? 'ok' : 'bad'}">${r.success ? '✔' : '✖'} ${esc(r.message)}</span>` : '';
      body += `<div class="tc"><div style="font-weight:600">Connection string the API will use</div>
        <div class="mono">${esc(connectionString(v, name))}</div>
        <div class="bar"><span class="btn">Test connection</span>${res}</div>${r && r.detail ? `<div class="help">${esc(r.detail)}</div>` : ''}</div>`;
    }
  }
  const nav = steps.map((x, i) => `<div class="st ${i === idx ? 'cur' : ''}">${i + 1}.&nbsp; ${esc(x.title)}</div>`).join('');
  const last = idx === steps.length - 1;
  return `<!doctype html><html><head><meta charset="utf-8"><script src="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.7.2/js/all.min.js"></script><style>
    * { box-sizing:border-box } body { margin:0; font:12px "Segoe UI", system-ui, sans-serif; color:#1b1b1b; background:#fff }
    .win { width:1060px; border:1px solid #c8c8c8; box-shadow:0 8px 24px rgba(0,0,0,.18); margin:12px; background:#fff }
    .tb { height:32px; display:flex; align-items:center; padding:0 12px; border-bottom:1px solid #e3e3e3; font-size:12px }
    .tb .x { margin-left:auto; color:#555 }
    .main { display:flex; min-height:560px }
    .side { width:210px; background:#f3f3f3; border-right:1px solid #d6d6d6; padding:16px }
    .side h4 { margin:0; font-size:14px; font-weight:600 } .side .sub { color:#616161; font-size:11px; margin:2px 0 12px }
    .st { padding:6px 6px 6px 8px; border-left:3px solid transparent; margin-bottom:2px }
    .st.cur { background:#efe8f7; border-left-color:#5c2d91; color:#5c2d91; font-weight:600 }
    .content { flex:1; padding:18px 24px 10px }
    h2 { margin:0; font-size:20px; font-weight:600 } .subt { color:#616161; margin:4px 0 14px; max-width:640px }
    .row { margin-bottom:14px } .lbl { font-weight:600; font-size:13px }
    .grp { font-weight:600; font-size:13px; margin-bottom:8px }
    .tiles { display:flex; flex-wrap:wrap; gap:10px }
    .tile { position:relative; width:236px; min-height:72px; border:2px solid #dddddd; border-radius:8px; padding:10px 12px; display:flex; gap:10px; align-items:center; background:#fff }
    .tile.on { border-color:#5c2d91; background:#f4eefb }
    .tick { position:absolute; top:6px; right:6px; width:18px; height:18px; border-radius:50%; background:#5c2d91; color:#fff; font-size:11px; display:grid; place-items:center; font-weight:700 }
    .ico { width:38px; height:38px; border-radius:50%; display:grid; place-items:center; color:#fff; flex:none; font-size:16px; font-weight:600 }
    .t { display:grid; gap:2px; padding-right:14px; min-width:0 } .t span { overflow-wrap:anywhere } .t b { font-size:13px } .t span { font-size:11px; color:#616161 } .t em { font-style:normal; font-size:10px; color:#107c10; font-weight:600 }
    .badge { display:inline-block; font-size:10px; font-weight:600; padding:1px 6px; border-radius:8px; background:#efe8f7; color:#5c2d91; white-space:nowrap }
    .tiles.compact .tile { width:136px; min-height:52px; flex-direction:column; align-items:flex-start; gap:4px; padding:8px 26px 8px 10px }
    .tiles.compact .tile b { font-size:12px }
    .tiles.swatch .tile { width:112px; min-height:0; flex-direction:column; align-items:stretch; gap:6px; padding:8px }
    .tiles.swatch .tile b { font-size:11px; white-space:nowrap; overflow:hidden; text-overflow:ellipsis }
    .sw { display:grid; grid-template-columns:2fr 1fr 1fr; height:34px; border-radius:4px; overflow:hidden; border:1px solid #d6d6d6 } .sw i { display:block } .lbl { margin-bottom:4px; display:flex; align-items:center }
    .i { display:inline-flex; width:14px; height:14px; border:1px solid #5c2d91; color:#5c2d91; border-radius:50%; font:700 9px sans-serif; align-items:center; justify-content:center; margin-left:6px }
    .dd, .txt { height:26px; border:1px solid #a8a8a8; padding:4px 7px; display:flex; justify-content:space-between; align-items:center; background:#fff }
    .car { color:#666 } .help { color:#616161; font-size:11px; margin-top:3px }
    .chk { display:flex; align-items:center; gap:7px } .box { width:14px; height:14px; border:1px solid #333; display:inline-flex; align-items:center; justify-content:center; font-size:11px }
    .box.on { background:#5c2d91; border-color:#5c2d91; color:#fff }
    .tc { border:1px solid #d6d6d6; background:#f3f3f3; padding:12px; max-width:740px; margin-top:6px }
    .dd, .txt { max-width:560px }
    .mono { font-family:Consolas, "DejaVu Sans Mono", monospace; font-size:11px; color:#616161; margin:4px 0 10px; word-break:break-all }
    .bar { display:flex; align-items:center; gap:12px } .res { font-size:12px } .res.ok { color:#107c10 } .res.bad { color:#c42b1c }
    .btn { display:inline-flex; align-items:center; justify-content:center; height:28px; min-width:96px; padding:0 12px; white-space:nowrap; flex-shrink:0; border:1px solid #adadad; background:#e1e1e1 }
    .btn.pri { background:#5c2d91; border-color:#5c2d91; color:#fff } .btn.sm { margin-top:6px }
    .foot { border-top:1px solid #d6d6d6; padding:10px 16px 12px; display:flex; justify-content:flex-end; gap:6px; align-items:center }
    .foot .note { margin-right:auto; color:#888; font-size:11px; font-style:italic }
    .sum { display:grid; grid-template-columns:130px 1fr; margin-bottom:6px; max-width:660px; white-space:pre-line } .sum span:first-child { color:#616161; font-weight:600 }
    .cmd { font-family:Consolas, "DejaVu Sans Mono", monospace; font-size:11px; border:1px solid #a8a8a8; padding:6px; max-width:660px; word-break:break-all }
  </style></head><body><div class="win">
    <div class="tb">Razor Pages Solution – ${esc(name)}<span class="x">✕</span></div>
    <div class="main"><div class="side"><h4>Razor Pages Solution</h4><div class="sub">${esc(name)}</div>${nav}</div>
      <div class="content"><h2>${esc(s.title)}</h2><div class="subt">${esc(s.subtitle)}</div>${body}</div></div>
    <div class="foot"><span class="note">Preview rendered from WizardSteps.json + template.json, not a Visual Studio capture.</span>
      <span class="btn" style="${idx === 0 ? 'color:#a0a0a0' : ''}">&lt; Back</span><span class="btn pri">${last ? 'Create' : 'Next &gt;'}</span><span class="btn">Cancel</span></div>
  </div></body></html>`;
}

(async () => {
  const name = 'MyCompany.Portal', dir = 'C:\\Source\\MyCompany.Portal';
  const [pg, sql] = results;
  const shots = [
    ['wizard-step-1-projects.png', { step: 'projects', v: defaults({}) }],
    ['wizard-step-2-database-postgres.png', { step: 'database', v: defaults({ DatabaseProvider: 'PostgreSQL', DbPassword: 'Dev_pass1' }), test: pg }],
    ['wizard-step-2-database-sqlserver.png', { step: 'database', v: defaults({ DbAuthentication: 'SqlLogin', DbServer: 'sql01', DbUser: 'portal_app', DbPassword: 'secret' }), test: sql }],
    ['wizard-step-3-api-security.png', { step: 'api', v: defaults({ Framework: 'net10.0', InternalAuthentication: 'Identity', ExternalAuthentication: 'Identity', IdentitySignIn: 'Shared', ApiDocumentation: 'Scalar', IncludeApiKey: 'true', IncludeSerilog: 'true', ErrorMonitoring: 'Sentry', ErrorMonitoringKey: 'https://4f2a9c…@o123456.ingest.sentry.io/789' }) }],
    ['wizard-step-4-front-end.png', { step: 'frontend', v: defaults({ BootstrapTheme: 'flatly', JQueryUITheme: 'redmond', IncludeSweetAlert: 'true', IncludeChartJs: 'true' }) }],
    ['wizard-step-5-summary.png', { step: 'summary', v: defaults({ DatabaseProvider: 'PostgreSQL', DbPassword: 'Dev_pass1', BootstrapTheme: 'flatly', JQueryUITheme: 'redmond', IncludeSweetAlert: 'true', IncludeChartJs: 'true', InternalAuthentication: 'Windows', IncludeApiKey: 'true' }) }],
  ];
  const browser = await chromium.launch();
  const pgPage = await browser.newPage({ viewport: { width: 1090, height: 760 }, deviceScaleFactor: 2 });
  for (const [file, opts] of shots) {
    await pgPage.setContent(page({ name, dir, ...opts }));
    await pgPage.waitForTimeout(400); // Font Awesome SVG icons
    const box = await pgPage.locator('.win').boundingBox();
    await pgPage.screenshot({ path: path.join(outDir, file), fullPage: true, clip: { x: 0, y: 0, width: box.width + 24, height: box.y + box.height + 12 } });
    console.log('wrote', file);
  }
  await browser.close();
})();
