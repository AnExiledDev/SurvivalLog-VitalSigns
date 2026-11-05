// The Actions tab in the Mod Manager: what a click on each HUD bar does, set up by hand. It shows what the base has
// (furniture actions and items, greyed out when the base lacks them), what each click would queue right now, and every
// setting behind it. Talks to the plugin through vuplex.postMessage("slmod|vitalsigns|actions|...") and is answered
// through window.__slvactions. Every change is saved at once, as on the Mods screen. The plugin sends this script again
// whenever the web view loses it.
(function () {
  if (window.__slvactions) return;

  const PREFIX = 'slmod|vitalsigns|actions|';
  const GUID = 'com.dev.survivallog.vitalsigns';
  const TAB_ID = 'actions';
  const TITLE = { en: 'Actions', zh: '行动' };
  const STYLE_ID = 'slva-css';

  const K = {
    bars: 'Hud/ClickBars',
    cures: 'Hud/ClickCures',
    home: 'Actions/UseHomeStorage',
    queue: 'Actions/AddToQueue',
    toasts: 'Actions/Toasts',
    clicks: ['Actions/ClickSatiety', 'Actions/ClickMorale', 'Actions/ClickStamina', 'Actions/ClickLife'],
    goal: 'Actions/FoodGoal',
    above: 'Actions/FoodAbovePercent',
    foodMost: 'Actions/FoodMostItems',
    skip: 'Actions/FoodSkip',
    avoidMorale: 'Actions/FoodAvoidMoraleLoss',
    beds: 'Actions/BedModes',
    nap: 'Actions/NapBelow',
    mode: 'Actions/MoraleMode',
    routine: 'Actions/MoraleRoutine',
    moraleItems: 'Actions/MoraleItems',
    avoidLife: 'Actions/AvoidLifeLoss',
    life: 'Actions/LifeItems',
    lifeMost: 'Actions/LifeMostItems'
  };

  const STATS = [
    { c: '#E8A85A', en: 'Satiety', zh: '饱腹', short: ['Sat', '饱腹'] },
    { c: '#D98BD0', en: 'Morale', zh: '心情', short: ['Mor', '心情'] },
    { c: '#6FC3E8', en: 'Stamina', zh: '体力', short: ['Sta', '体力'] },
    { c: '#E86A6A', en: 'Life', zh: '生命', short: ['Life', '生命'] }
  ];

  // ===== Style =====

  const VARS = '--hl:#FFC83A;--tm:#f0e8d8;--td:#a89a82;--tmeta:#7a6e5a;--bl:rgba(220,188,120,0.16);--bd2:rgba(220,188,120,0.10);--good:#7fd38a;--bad:#ff8a7a;--busy:#e0b860;';

  const CSS = [
    '&{' + VARS + 'position:relative;min-height:677px;padding:16px 26px 22px;color:var(--tm);background:radial-gradient(ellipse at 20% 0%,rgba(120,80,30,0.28),transparent 60%),linear-gradient(180deg,#1f1a15,#16120e)}',
    '&::before{content:"";position:absolute;top:0;left:0;right:0;height:2px;background:linear-gradient(90deg,transparent 10%,var(--hl) 50%,transparent 90%);opacity:.5}',
    'input{user-select:text}',
    '.top{display:flex;align-items:center;gap:16px;margin-bottom:10px}',
    '.title{font-size:1.5rem;font-weight:700;letter-spacing:4px;color:#fff;text-shadow:0 2px 10px rgba(0,0,0,.6);white-space:nowrap}',
    '.sub{color:var(--td);font-size:.9rem;line-height:1.4}',
    '.sp{flex:1}',
    '.key-btn{white-space:nowrap}',
    '.opts{display:flex;flex-wrap:wrap;gap:8px;margin-bottom:12px}',
    '.tgl{display:inline-flex;align-items:center;gap:8px;padding:6px 12px 6px 8px;border:1px solid var(--bd2);background:rgba(0,0,0,0.25);color:var(--td);font-size:.85rem;cursor:pointer;white-space:nowrap;transition:border-color .15s,color .15s,background .15s}',
    '.tgl:hover{border-color:var(--bl);color:var(--tm)}',
    '.tgl i{position:relative;width:28px;height:15px;border-radius:8px;background:rgba(255,255,255,0.14);flex:none;transition:background .15s}',
    '.tgl i::after{content:"";position:absolute;top:2px;left:2px;width:11px;height:11px;border-radius:50%;background:#bbb;transition:transform .18s cubic-bezier(.2,1,.4,1),background .15s}',
    '.tgl.is-on{color:var(--tm);border-color:rgba(255,200,58,0.35);background:rgba(255,200,58,0.06)}',
    '.tgl.is-on i{background:rgba(255,200,58,0.6)}',
    '.tgl.is-on i::after{transform:translateX(13px);background:#fff}',
    '.note{margin:0 0 12px;padding:8px 14px;border:1px solid rgba(255,200,58,0.3);background:rgba(255,200,58,0.06);color:#e8d9a8;font-size:.85rem;line-height:1.5}',
    '.note.away{border-color:rgba(111,195,232,0.3);background:rgba(111,195,232,0.06);color:#bfe0ee}',
    '.note .dot{display:inline-block;width:7px;height:7px;border-radius:50%;background:var(--hl);margin-right:8px;animation:slva-pulse 1.1s ease-in-out infinite}',
    '@keyframes slva-pulse{0%,100%{opacity:.25}50%{opacity:1}}',
    '.body{display:flex;gap:14px;align-items:stretch}',
    '.nav{flex:none;width:250px;display:flex;flex-direction:column;gap:8px}',
    '.card{position:relative;padding:10px 12px 10px 16px;border:1px solid var(--bd2);background:rgba(0,0,0,0.25);cursor:pointer;transition:border-color .15s,background .15s}',
    '.card::before{content:"";position:absolute;left:0;top:0;bottom:0;width:4px;background:var(--c);opacity:.55;transition:opacity .15s}',
    '.card:hover{border-color:var(--bl)}',
    '.card.is-sel{border-color:var(--c);background:linear-gradient(90deg,rgba(255,255,255,0.06),rgba(0,0,0,0.25))}',
    '.card.is-sel::before{opacity:1;box-shadow:0 0 12px var(--c)}',
    '.card.is-off .cn,.card.is-off .tip{opacity:.45}',
    '.card .row1{display:flex;align-items:center;gap:8px}',
    '.cn{font-weight:700;letter-spacing:2px;color:var(--c);font-size:1rem}',
    '.tip{margin-top:5px;color:var(--td);font-size:.8rem;line-height:1.35;min-height:2.1em}',
    '.tip.can{color:var(--tm)}',
    '.tip b{color:var(--c);font-weight:400}',
    '.pill{font:400 .7rem var(--mono);letter-spacing:1px;padding:2px 8px;border:1px solid rgba(255,255,255,0.2);color:var(--tmeta);cursor:pointer}',
    '.pill.is-on{border-color:var(--c);color:var(--c)}',
    '.panel{flex:1;min-width:0;display:flex;flex-direction:column;gap:10px;padding:14px 16px;border:1px solid var(--bd2);background:rgba(0,0,0,0.22);border-top:2px solid var(--c)}',
    '.ph{display:flex;align-items:baseline;gap:12px}',
    '.ph .cn{font-size:1.15rem}',
    '.ph .sub{font-size:.82rem}',
    '.grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:10px}',
    '.box{padding:10px 12px;border:1px solid var(--bd2);background:rgba(0,0,0,0.2)}',
    '.lbl{display:block;color:var(--td);font-size:.78rem;letter-spacing:1px;margin-bottom:6px}',
    '.hint{color:var(--tmeta);font-size:.76rem;line-height:1.4;margin-top:6px}',
    // The Mod Manager has its own .chips and .list: these rules undo the padding, wrap and border those add.
    '.chips{display:flex;flex-wrap:nowrap;gap:0;padding:0;border:1px solid var(--bl);width:max-content;max-width:100%}',
    '.chips span{padding:5px 12px;font-size:.82rem;color:var(--td);cursor:pointer;white-space:nowrap}',
    '.chips span:hover{color:var(--tm)}',
    '.chips span.is-on{background:rgba(255,200,58,0.14);color:var(--hl)}',
    '.chips.sm span{padding:3px 8px;font-size:.74rem}',
    '.rng{display:flex;align-items:center;gap:10px}',
    '.rng input{flex:1;accent-color:var(--hl)}',
    '.rng b{min-width:44px;text-align:right;font:700 .95rem var(--mono);color:#fff}',
    '.step{display:inline-flex;align-items:center;border:1px solid var(--bl)}',
    '.step span{width:28px;text-align:center;padding:3px 0;cursor:pointer;color:var(--td);font:700 .95rem var(--mono)}',
    '.step span:hover{color:var(--hl);background:rgba(255,200,58,0.08)}',
    '.step b{min-width:34px;text-align:center;font:700 .95rem var(--mono);color:#fff}',
    '.bar{display:flex;align-items:center;gap:10px}',
    '.q{flex:1;max-width:280px;background:rgba(0,0,0,0.4);border:1px solid var(--bd2);color:#fff;padding:5px 10px;font:400 .88rem var(--body);outline:none}',
    '.q:focus{border-color:var(--hl)}',
    '.count{color:var(--tmeta);font-size:.78rem;white-space:nowrap}',
    '.list{flex:none;padding:0;overflow-y:auto;border:1px solid var(--bd2);background:rgba(0,0,0,0.18)}',
    '.list.tall{height:330px}',
    '.list.med{height:220px}',
    '.list.short{max-height:200px}',
    '::-webkit-scrollbar{width:6px}',
    '::-webkit-scrollbar-thumb{background:rgba(255,200,58,0.3)}',
    '.it{display:flex;align-items:center;gap:10px;padding:5px 10px;border-bottom:1px solid rgba(255,255,255,0.04);min-height:34px}',
    '.it:hover{background:rgba(255,200,58,0.04)}',
    '.it.is-busy .nm{color:var(--busy)}',
    '.it.is-missing{opacity:.42}',
    '.it.is-missing:hover{opacity:.7}',
    '.it.is-skip .nm{text-decoration:line-through;color:var(--tmeta)}',
    '.ic{width:26px;height:26px;flex:none;object-fit:contain;filter:drop-shadow(0 1px 2px rgba(0,0,0,.7))}',
    '.ic.blank{display:inline-block;border:1px dashed rgba(255,255,255,0.12)}',
    '.no{flex:none;width:22px;text-align:right;font:700 .8rem var(--mono);color:var(--tmeta)}',
    '.no.first{color:var(--hl)}',
    '.mid{flex:1;min-width:0}',
    '.nm{font-weight:700;font-size:.88rem;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}',
    '.at{color:var(--tmeta);font-weight:400;font-size:.76rem;margin-left:6px}',
    '.why{color:var(--tmeta);font-size:.74rem;margin-top:1px}',
    '.it.is-busy .why{color:var(--busy);opacity:.85}',
    '.ds{display:flex;flex-wrap:wrap;gap:4px;flex:none;justify-content:flex-end;max-width:46%}',
    '.d{font:400 .72rem var(--mono);padding:1px 6px;border:1px solid;border-radius:2px;white-space:nowrap}',
    '.d.neg{border-style:dashed;opacity:.85}',
    '.cnt{flex:none;min-width:46px;text-align:right;font:400 .78rem var(--mono);color:var(--td)}',
    '.cnt small{display:block;color:var(--tmeta);font-size:.66rem}',
    '.act{flex:none;display:flex;gap:2px}',
    '.btn{min-width:26px;text-align:center;padding:2px 7px;border:1px solid rgba(255,255,255,0.14);color:var(--td);cursor:pointer;font:700 .8rem var(--mono);white-space:nowrap}',
    '.btn:hover{border-color:var(--hl);color:var(--hl);background:rgba(255,200,58,0.08)}',
    '.btn.off{opacity:.25;pointer-events:none}',
    '.btn.add{border-color:rgba(127,211,138,0.4);color:var(--good)}',
    '.btn.del:hover{border-color:#ff5252;color:var(--bad);background:rgba(255,82,82,0.1)}',
    '.btn.use{min-width:64px}',
    '.btn.use.is-skip{border-color:rgba(255,82,82,0.4);color:var(--bad)}',
    '.sec{display:flex;align-items:center;gap:10px;margin-top:2px}',
    '.sec .lbl{margin:0}',
    '.empty{padding:16px;text-align:center;color:var(--tmeta);font-size:.84rem;line-height:1.5}',
    '.bed{display:flex;align-items:center;gap:12px;padding:9px 12px;border-bottom:1px solid rgba(255,255,255,0.05)}',
    '.bed.is-missing{opacity:.45}',
    '.bed.is-skip .nm{color:var(--tmeta);text-decoration:line-through}',
    '.rests{display:flex;gap:6px;flex-wrap:wrap}',
    '.rest{font-size:.74rem;padding:2px 8px;border:1px solid;white-space:nowrap}',
    '.rest.st0{color:var(--good);border-color:rgba(127,211,138,0.4)}',
    '.rest.st1{color:var(--busy);border-color:rgba(224,184,96,0.4)}',
    '.rest.st2{color:var(--tmeta);border-color:rgba(255,255,255,0.12)}',
    '.rest small{font-family:var(--mono);margin-left:4px;opacity:.85}',
    '.wait{padding:60px;text-align:center;color:var(--td)}'
  ];

  function scopedCss() {
    return CSS.map(rule => {
      if (rule.charAt(0) === '@') return rule;

      const brace = rule.indexOf('{');
      const sel = rule.slice(0, brace);
      const body = rule.slice(brace);

      return sel.split(',').map(s => {
        s = s.trim();

        return s.charAt(0) === '&' ? '#slmm .slva' + s.slice(1) : '#slmm .slva ' + s;
      }).join(',') + body;
    }).join('\n');
  }

  function injectStyle() {
    if (document.getElementById(STYLE_ID)) return;

    const style = document.createElement('style');

    style.id = STYLE_ID;
    style.textContent = scopedCss();
    document.head.appendChild(style);
  }

  // ===== State =====

  const st = {
    data: null,
    lang: 'en',
    drawnLang: '',
    root: null,
    ctx: null,
    shown: false,
    stat: 0,
    holding: false,
    stale: false,
    q: { food: '', add: '', life: '' },
    have: { food: false, add: false, life: false },
    note: '',
    noteWarn: false
  };

  const isNum = v => typeof v === 'number' && isFinite(v);
  const isArr = Array.isArray;
  const isObj = v => !!v && typeof v === 'object' && !isArr(v);
  const L = (en, zh) => st.lang === 'zh' ? zh : en;
  const esc = s => String(s == null ? '' : s).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
  const arr = v => isArr(v) ? v : [];
  const statName = i => L(STATS[i].en, STATS[i].zh);

  function send(msg) {
    try { window.vuplex.postMessage(PREFIX + msg); } catch (e) {}
  }

  // ===== Settings =====

  function S(name) {
    const s = st.data && isObj(st.data.s) ? st.data.s : {};

    return typeof s[name] === 'string' ? s[name] : '';
  }

  const flag = name => S(name).toLowerCase() === 'true';

  function num(name, fallback) {
    const v = parseFloat(S(name));

    return isFinite(v) ? v : fallback;
  }

  function lim(name, a, b) {
    const r = st.data && isObj(st.data.lim) ? st.data.lim[name] : null;

    return isArr(r) && r.length === 2 ? r : [a, b];
  }

  function choices(name, fallback) {
    const r = st.data && isObj(st.data.lim) ? st.data.lim[name] : null;

    return isArr(r) && r.length ? r : fallback;
  }

  // The value is shown at once and saved; the plugin answers with the settings as saved, which replace it.
  function set(name, value) {
    if (!st.data) return;

    if (!isObj(st.data.s)) st.data.s = {};

    st.data.s[name] = String(value);
    send('set|' + name + '|' + value);
    render();
  }

  function keys(name) {
    const out = [];

    S(name).split(/[,; ]+/).forEach(part => {
      let t = part.trim().toLowerCase();

      if (!t) return;
      if (/^\d+$/.test(t)) t = 'i' + t;
      if (/^[if]\d+$/.test(t) && out.indexOf(t) < 0) out.push(t);
    });

    return out;
  }

  function toggleKey(name, key) {
    const list = keys(name);
    const at = list.indexOf(key);

    if (at >= 0) list.splice(at, 1);
    else list.push(key);

    set(name, list.join(','));
  }

  function moveKey(name, key, by) {
    const list = keys(name);
    const at = list.indexOf(key);
    const to = at + by;

    if (at < 0 || to < 0 || to >= list.length) return;

    list[at] = list[to];
    list[to] = key;
    set(name, list.join(','));
  }

  function bedModes() {
    const out = {};

    S(K.beds).split(/[,; ]+/).forEach(part => {
      const m = /^(\d+):(\w+)$/.exec(part.trim());

      if (m) out[m[1]] = m[2];
    });

    return out;
  }

  function setBedMode(type, mode) {
    const modes = bedModes();

    if (mode === 'Auto') delete modes[type];
    else modes[type] = mode;

    set(K.beds, Object.keys(modes).sort((a, b) => a - b).map(t => t + ':' + modes[t]).join(','));
  }

  // ===== Data =====

  function options() {
    return arr(st.data && st.data.funcs).concat(arr(st.data && st.data.items));
  }

  function byKey() {
    const map = {};

    options().forEach(o => { if (o && typeof o.k === 'string') map[o.k] = o; });

    return map;
  }

  const delta = (o, i) => o && isArr(o.d) && isNum(o.d[i]) ? o.d[i] : 0;
  const stateOf = o => o && isNum(o.st) ? o.st : 2;
  const isItem = o => !!o && o.k.charAt(0) === 'i';

  function matches(o, q) {
    if (!q) return true;

    return String(o.n || '').toLowerCase().indexOf(q.toLowerCase()) >= 0 || (o.at && String(o.at).toLowerCase().indexOf(q.toLowerCase()) >= 0);
  }

  function data(d) {
    if (!isObj(d)) return;

    st.data = d;

    // A slider being dragged would jump under the mouse: draw once it is let go.
    if (st.holding) {
      st.stale = true;

      return;
    }

    if (st.root && st.root.isConnected) render();
  }

  // ===== Drawing pieces =====

  function iconUrl(ic) {
    if (!ic) return '';
    if (/^(https?:|data:|file:|blob:)/i.test(ic)) return ic;

    const href = String(location.href);
    const at = href.indexOf('/WebUI/');

    return at < 0 ? ic : href.slice(0, at + 7) + ic.replace(/^(\.\.\/)+/, '').replace(/^\/+/, '');
  }

  function icon(o) {
    const url = iconUrl(o && o.ic);

    return url ? '<img class="ic" src="' + esc(url) + '" alt="" onerror="this.style.visibility=\'hidden\'">' : '<span class="ic blank"></span>';
  }

  function fmt(v) {
    const r = Math.round(v * 10) / 10;

    return (r > 0 ? '+' : '') + (Math.abs(r - Math.round(r)) < 1e-9 ? String(Math.round(r)) : r.toFixed(1));
  }

  function deltas(o) {
    const out = [];

    for (let i = 0; i < 4; i++) {
      const v = delta(o, i);

      if (Math.abs(v) >= 0.05) out.push(chip(v, STATS[i].c, L(STATS[i].short[0], STATS[i].short[1])));
    }

    if (Math.abs(delta(o, 4)) >= 0.05) out.push(chip(delta(o, 4), STATS[1].c, L('Max Mor', '心情上限')));
    if (Math.abs(delta(o, 5)) >= 0.05) out.push(chip(delta(o, 5), STATS[2].c, L('Max Sta', '体力上限')));

    return '<span class="ds">' + out.join('') + '</span>';
  }

  function chip(v, color, label) {
    return '<span class="d' + (v < 0 ? ' neg' : '') + '" style="color:' + color + ';border-color:' + color + '66">' + fmt(v) + ' ' + esc(label) + '</span>';
  }

  function toggle(name, label, attrs) {
    return '<span class="tgl' + (flag(name) ? ' is-on' : '') + '" data-a="flag" data-k="' + esc(name) + '"' + (attrs || '') + '><i></i>' + esc(label) + '</span>';
  }

  function choiceChips(name, list, labels, small) {
    const cur = S(name).toLowerCase();

    return '<div class="chips' + (small ? ' sm' : '') + '">' + list.map(c =>
      '<span class="' + (c.toLowerCase() === cur ? 'is-on' : '') + '" data-a="choice" data-k="' + esc(name) + '" data-v="' + esc(c) + '">' + esc(labels[c] || c) + '</span>').join('') + '</div>';
  }

  function slider(name, range, fallback, suffix) {
    const v = num(name, fallback);

    return '<div class="rng"><input type="range" data-a="range" data-k="' + esc(name) + '" min="' + range[0] + '" max="' + range[1] + '" step="1" value="' + v + '">' +
      '<b data-out="' + esc(name) + '">' + v + (suffix || '') + '</b></div>';
  }

  function stepper(name, range, fallback) {
    const v = num(name, fallback);

    return '<div class="step"><span data-a="step" data-k="' + esc(name) + '" data-v="' + Math.max(range[0], v - 1) + '">&minus;</span><b>' + v + '</b>' +
      '<span data-a="step" data-k="' + esc(name) + '" data-v="' + Math.min(range[1], v + 1) + '">+</span></div>';
  }

  function searchBar(id, placeholder, total, shown) {
    return '<div class="bar"><input class="q" data-f="q-' + id + '" data-a="q" data-k="' + id + '" placeholder="' + esc(placeholder) + '" value="' + esc(st.q[id]) + '">' +
      '<div class="chips sm"><span class="' + (st.have[id] ? '' : 'is-on') + '" data-a="have" data-k="' + id + '" data-v="0">' + L('All', '全部') + '</span>' +
      '<span class="' + (st.have[id] ? 'is-on' : '') + '" data-a="have" data-k="' + id + '" data-v="1">' + L('Available', '可用') + '</span></div>' +
      '<span class="count">' + shown + ' / ' + total + '</span></div>';
  }

  function stateClass(o) {
    const s = stateOf(o);

    return s === 0 ? '' : s === 1 ? ' is-busy' : ' is-missing';
  }

  function count(o) {
    if (!isItem(o)) return '';

    const c = isNum(o.c) ? o.c : 0;
    const b = isNum(o.b) ? o.b : 0;

    return '<span class="cnt">x' + c + (c > 0 ? '<small>' + L('bag ', '背包 ') + b + '</small>' : '') + '</span>';
  }

  // One action or item: icon, name, where it is done, why it is greyed out, what it gives, how many, and the buttons.
  function row(o, opts) {
    opts = opts || {};

    const why = stateOf(o) !== 0 && o.why ? '<div class="why">' + esc(o.why) + '</div>' : '';
    const at = o.at ? '<span class="at">' + esc(o.at) + '</span>' : '';
    const no = opts.no ? '<span class="no' + (opts.first ? ' first' : '') + '">' + opts.no + '</span>' : '';

    return '<div class="it' + stateClass(o) + (opts.cls || '') + '" title="' + esc(o.why || '') + '">' + no + icon(o) +
      '<div class="mid"><div class="nm">' + esc(o.n || o.k) + at + '</div>' + why + '</div>' + deltas(o) + count(o) +
      (opts.act ? '<span class="act">' + opts.act + '</span>' : '') + '</div>';
  }

  function btn(cls, a, k, v, label, title, isOff) {
    return '<span class="btn ' + cls + (isOff ? ' off' : '') + '" data-a="' + a + '" data-k="' + esc(k) + '" data-v="' + esc(v) + '"' +
      (title ? ' title="' + esc(title) + '"' : '') + '>' + label + '</span>';
  }

  // Ready first, then Busy, then Missing; inside each, the biggest gain of the stat first.
  function sorted(list, stat) {
    return list.slice().sort((a, b) => (stateOf(a) - stateOf(b)) || (delta(b, stat) - delta(a, stat)) || String(a.n).localeCompare(String(b.n)));
  }

  // An ordered list the player keeps (the routine, the Life items): each with up, down and remove.
  function orderedList(name, map, emptyText) {
    const list = keys(name);

    if (!list.length) return '<div class="list short" data-scroll="' + name + '"><div class="empty">' + emptyText + '</div></div>';

    return '<div class="list short" data-scroll="' + name + '">' + list.map((k, i) => {
      const o = map[k] || { k, n: k, st: 2, why: L('not in this game', '游戏中没有') };
      const act = btn('', 'move', name, k + '|-1', '&uarr;', L('Earlier', '提前'), i === 0) +
        btn('', 'move', name, k + '|1', '&darr;', L('Later', '推后'), i === list.length - 1) +
        btn('del', 'toggle', name, k, '&times;', L('Take out', '移除'));

      return row(o, { no: i + 1, first: i === 0, act });
    }).join('') + '</div>';
  }

  // The things that can be added to an ordered list: search, a filter to what is there now, and "+".
  function addList(name, id, pool, stat, size) {
    const inList = keys(name);
    const all = pool.filter(o => inList.indexOf(o.k) < 0);
    const shown = sorted(all.filter(o => matches(o, st.q[id]) && (!st.have[id] || stateOf(o) === 0)), stat);
    const rows = shown.map(o => row(o, { act: btn('add', 'toggle', name, o.k, '+', L('Add', '添加')) })).join('');

    return searchBar(id, L('Search', '搜索'), all.length, shown.length) +
      '<div class="list ' + size + '" data-scroll="add-' + id + '">' + (rows || '<div class="empty">' + L('Nothing to add here', '没有可添加的') + '</div>') + '</div>';
  }

  // ===== Panels =====

  function satietyPanel() {
    const d = st.data;
    const goal = S(K.goal);
    const skip = keys(K.skip);
    const food = arr(d.items).filter(o => delta(o, 0) > 0);
    const shown = sorted(food.filter(o => matches(o, st.q.food) && (!st.have.food || stateOf(o) === 0)), 0);
    const rows = shown.map(o => {
      const isSkip = skip.indexOf(o.k) >= 0;
      const avoid = flag(K.avoidMorale) && delta(o, 1) < 0;
      const label = isSkip ? L('Skipped', '跳过') : avoid ? L('Avoided', '避开') : L('Eat', '可吃');

      return row(o, { cls: isSkip || avoid ? ' is-skip' : '', act: btn('use' + (isSkip ? ' is-skip' : ''), 'toggle', K.skip, o.k, label, isSkip ? L('Click to eat it again', '点击恢复食用') : L('Click to never eat it', '点击不再食用')) });
    }).join('');
    const labels = { Max: L('To the maximum', '吃到最满'), WellFed: L('To Well Fed', '吃到「饱食」'), AboveWellFed: L('Above Well Fed', '高于「饱食」') };

    return '<div class="grid"><div class="box"><span class="lbl">' + L('EAT UNTIL', '吃到') + '</span>' + choiceChips(K.goal, choices('goals', ['Max', 'WellFed', 'AboveWellFed']), labels) +
      (goal === 'AboveWellFed' ? '<div style="margin-top:8px">' + slider(K.above, lim('foodAbove', 0, 100), 10, '%') + '<div class="hint">' + L('How far above Well Fed, as a share of the maximum.', '比「饱食」高出多少，按上限的百分比计。') + '</div></div>' : '') +
      '<div class="hint">' + L('Food that would go past the maximum is not eaten.', '会超出上限的食物不会吃。') + '</div></div>' +
      '<div class="box"><span class="lbl">' + L('AT MOST PER CLICK', '每次点击最多') + '</span>' + stepper(K.foodMost, lim('foodMost', 1, 10), 4) +
      '<span class="count" style="margin-left:8px">' + L('items', '件食物') + '</span><div style="margin-top:10px">' + toggle(K.avoidMorale, L('Avoid food that lowers Morale', '避开降低心情的食物')) + '</div></div></div>' +
      '<div class="sec"><span class="lbl">' + L('FOOD', '食物') + '</span><span class="hint" style="margin:0">' + L('A click eats just enough to reach the goal, never past full. Skip any you want to keep.', '每次点击只吃到目标所需的量，绝不吃撑。不想吃的可以跳过。') + '</span></div>' +
      searchBar('food', L('Search food', '搜索食物'), food.length, shown.length) +
      '<div class="list tall" data-scroll="food">' + (rows || '<div class="empty">' + L('No food found', '没有找到食物') + '</div>') + '</div>';
  }

  function moralePanel(map) {
    const d = st.data;
    const mode = S(K.mode);
    const labels = { Best: L('Best action', '最佳行动'), Routine: L('My routine', '我的日常') };
    let html = '<div class="grid"><div class="box"><span class="lbl">' + L('EACH CLICK', '每次点击') + '</span>' + choiceChips(K.mode, choices('moraleModes', ['Best', 'Routine']), labels) +
      '<div class="hint">' + (mode === 'Routine' ?
        L('Queues every action of your routine that can be done now, in your order.', '按你的顺序，把日常安排中现在能做的行动全部加入队列。') :
        L('Queues one action: ones that raise another stat too come first, then the biggest gain. Clicking again picks the next one.', '加入一个行动：同时提升其他属性的优先，然后是提升最多的。再次点击会选下一个。')) + '</div></div>' +
      '<div class="box">' + toggle(K.avoidLife, L('Skip actions that cost Life', '跳过会减少生命的行动')) +
      (mode === 'Routine' ? '' : '<div style="margin-top:8px">' + toggle(K.moraleItems, L('Use items too', '也使用物品')) + '</div>') + '</div></div>';

    const pool = arr(d.funcs).concat(flag(K.moraleItems) || mode === 'Routine' ? arr(d.items) : []).filter(o => delta(o, 1) > 0);

    if (mode === 'Routine') {
      html += '<div class="sec"><span class="lbl">' + L('MY ROUTINE', '我的日常') + '</span></div>' +
        orderedList(K.routine, map, L('Your routine is empty. Add actions below.', '日常安排是空的。请从下方添加。')) +
        '<div class="sec"><span class="lbl">' + L('ADD TO THE ROUTINE', '添加到日常') + '</span></div>' + addList(K.routine, 'add', pool, 1, 'med');
    } else {
      const best = arr(d.best).map(k => map[k]).filter(Boolean);
      const rows = best.map((o, i) => row(o, { no: i + 1, first: i === 0 && stateOf(o) === 0 })).join('');
      const all = sorted(pool.filter(o => best.indexOf(o) < 0 && matches(o, st.q.add) && (!st.have.add || stateOf(o) === 0)), 1);

      html += '<div class="sec"><span class="lbl">' + L('CLICKS GO DOWN THIS LIST', '点击按此顺序') + '</span></div>' +
        '<div class="list med" data-scroll="best">' + (rows || '<div class="empty">' + L('Nothing raises Morale here yet.', '这里还没有能提升心情的行动。') + '</div>') + '</div>' +
        '<div class="sec"><span class="lbl">' + L('EVERYTHING ELSE THAT RAISES MORALE', '其他能提升心情的') + '</span></div>' +
        searchBar('add', L('Search', '搜索'), pool.length - best.length, all.length) +
        '<div class="list short" data-scroll="rest">' + (all.map(o => row(o)).join('') || '<div class="empty">' + L('None', '没有') + '</div>') + '</div>';
    }

    return html;
  }

  function staminaPanel() {
    const beds = arr(st.data.beds);
    const modes = bedModes();
    const list = choices('bedModes', ['Auto', 'Sleep', 'Nap', 'Skip']);
    const labels = { Auto: L('Auto', '自动'), Sleep: L('Sleep', '睡觉'), Nap: L('Nap', '小睡'), Skip: L('Skip', '不用') };
    const rows = beds.map(b => {
      const mode = modes[b.t] || 'Auto';
      const c = isNum(b.c) ? b.c : 0;
      const rest = (r, en, zh) => isObj(r) && r.st >= 0 ?
        '<span class="rest st' + r.st + '" title="' + esc(r.why || '') + '">' + L(en, zh) + (isNum(r.g) && r.g > 0 ? '<small>' + fmt(r.g) + '</small>' : '') + '</span>' : '';
      const cur = mode.toLowerCase();

      return '<div class="bed' + (c === 0 ? ' is-missing' : '') + (mode === 'Skip' ? ' is-skip' : '') + '">' +
        '<div class="mid"><div class="nm">' + esc(b.n) + '<span class="at">' + (c > 0 ? 'x' + c : L('none at home', '家中没有')) +
        (isNum(b.r) && b.r > 0 ? ' &middot; ' + L('up to ', '最多 ') + fmt(b.r) + ' ' + L('Stamina', '体力') : '') + '</span></div>' +
        '<div class="rests" style="margin-top:4px">' + rest(b.sleep, 'Sleep', '睡觉') + rest(b.nap, 'Nap', '小睡') + rest(b.beauty, 'Beauty sleep', '美容觉') + '</div></div>' +
        '<div class="chips sm">' + list.map(m => '<span class="' + (m.toLowerCase() === cur ? 'is-on' : '') + '" data-a="bed" data-k="' + b.t + '" data-v="' + esc(m) + '">' + esc(labels[m] || m) + '</span>').join('') + '</div></div>';
    }).join('');

    return '<div class="grid"><div class="box"><span class="lbl">' + L('AUTO NAPS WHEN THIS LITTLE IS MISSING', '「自动」在缺少这么多时小睡') + '</span>' + slider(K.nap, lim('napBelow', 0, 100), 25, '%') +
      '<div class="hint">' + L('As a share of the maximum. More missing than that, Auto sleeps.', '按上限的百分比计。缺得更多时，「自动」会睡觉。') + '</div></div>' +
      '<div class="box"><span class="lbl">' + L('WHICH BED', '用哪张床') + '</span><div class="hint" style="margin-top:0">' +
      L('The best bed for the rest wanted: a beauty sleep first, then the biggest restore, then one on your floor. When the rest wanted is not ready, the other one is used.', '选最适合的床：先选美容觉，再选恢复最多的，再选同一层的。想要的休息不可用时，会用另一种。') + '</div></div></div>' +
      '<div class="sec"><span class="lbl">' + L('BEDS', '床') + '</span><span class="hint" style="margin:0">' + L('Set each bed type: Auto, always Sleep, always Nap, or Skip it.', '为每种床设置：自动、总是睡觉、总是小睡，或不用。') + '</span></div>' +
      '<div class="list tall" data-scroll="beds">' + (rows || '<div class="empty">' + L('No bed at home yet.', '家中还没有床。') + '</div>') + '</div>';
  }

  function lifePanel(map) {
    const pool = arr(st.data.items).filter(o => delta(o, 3) > 0);

    return '<div class="grid"><div class="box"><span class="lbl">' + L('AT MOST PER CLICK', '每次点击最多') + '</span>' + stepper(K.lifeMost, lim('lifeMost', 1, 10), 1) +
      '<span class="count" style="margin-left:8px">' + L('items', '件物品') + '</span><div class="hint">' + L('Stops once Life is full.', '生命满了就停止。') + '</div></div>' +
      '<div class="box"><span class="lbl">' + L('ORDER', '顺序') + '</span><div class="hint" style="margin-top:0">' +
      L('Your Life items are used from the top, and only those. With none chosen, every Life item is used, smallest first, so a big one is kept for a big need.', '从上往下使用你选的物品，只用这些。没有选择时，使用所有恢复生命物品，先用小的，大的留给大伤。') + '</div></div></div>' +
      '<div class="sec"><span class="lbl">' + L('MY LIFE ITEMS', '我的恢复生命物品') + '</span></div>' +
      orderedList(K.life, map, L('None chosen: every Life item is used, smallest first. Add items below to pick them and their order.', '未选择：使用所有恢复生命物品，先用小的。从下方添加物品可指定物品和顺序。')) +
      '<div class="sec"><span class="lbl">' + L('ITEMS THAT RESTORE LIFE', '能恢复生命的物品') + '</span></div>' + addList(K.life, 'life', pool, 3, 'med');
  }

  // ===== Render =====

  function navCard(i) {
    const s = arr(st.data.stats)[i] || {};
    const on = flag(K.clicks[i]);

    return '<div class="card' + (st.stat === i ? ' is-sel' : '') + (on ? '' : ' is-off') + '" data-a="stat" data-v="' + i + '" style="--c:' + STATS[i].c + '">' +
      '<div class="row1"><span class="cn">' + esc(statName(i)) + '</span><span class="sp"></span>' +
      '<span class="pill' + (on ? ' is-on' : '') + '" data-a="flag" data-k="' + K.clicks[i] + '">' + (on ? L('CLICK ON', '点击开') : L('CLICK OFF', '点击关')) + '</span></div>' +
      '<div class="tip' + (s.can ? ' can' : '') + '">' + (s.tip ? (s.can ? '<b>' + L('Now: ', '现在：') + '</b>' : '') + esc(s.tip) : '&nbsp;') + '</div></div>';
  }

  function render() {
    if (!st.root) return;

    st.drawnLang = st.lang;
    st.stale = false;

    if (!st.data) {
      st.root.innerHTML = '<div class="wait">' + L('Reading the base...', '正在读取基地...') + '</div>';
      footer();

      return;
    }

    const saved = keep();
    const d = st.data;
    const map = byKey();
    const panels = [satietyPanel, moralePanel, staminaPanel, lifePanel];
    const subs = [
      L('What a click on Satiety eats.', '点击「饱腹」时吃什么。'),
      L('What a click on Morale does.', '点击「心情」时做什么。'),
      L('Which bed a click on Stamina uses, and how.', '点击「体力」时用哪张床，怎么休息。'),
      L('Which items a click on Life uses.', '点击「生命」时用哪些物品。')
    ];
    let html = '<div class="top"><div class="title">' + L('ACTIONS', '行动') + '</div><div class="sub">' +
      L('Click a bar on the HUD and its action joins the queue. Set up what each click does here; changes apply at once.', '点击界面上的属性条，对应行动就会加入队列。在这里设置每次点击做什么，修改立即生效。') +
      '</div><span class="sp"></span><span class="key-btn" data-a="edit">' + L('Edit HUD layout', '编辑界面布局') + '</span></div>' +
      '<div class="opts">' + toggle(K.bars, L('Click bars to act', '点击属性条行动')) + toggle(K.cures, L('Click a debuff to cure it', '点击负面状态治疗')) +
      toggle(K.home, L('Use the home storage', '使用家中储物')) + toggle(K.queue, L('Add to the queue (off: replace it)', '加入队列（关：替换队列）')) + toggle(K.toasts, L('Toasts', '提示')) + '</div>';

    if (d.scanning) html += '<div class="note"><span class="dot"></span>' + L('Looking around the base. The lists fill in as it goes.', '正在查看基地，列表会陆续出现。') + '</div>';
    else if (!d.atHome) html += '<div class="note away">' + L('You are away from home: only what is in the bag can be used, and furniture waits until you are back.', '你不在家：只能使用背包里的物品，家具要等回家后才能用。') + '</div>';

    if (!flag(K.bars)) html += '<div class="note">' + L('Clicking bars is off. Turn on 「Click bars to act」 above for any of this to happen.', '点击属性条已关闭。请打开上方的「点击属性条行动」。') + '</div>';

    html += '<div class="body"><div class="nav">' + [0, 1, 2, 3].map(navCard).join('') + '</div>' +
      '<div class="panel" style="--c:' + STATS[st.stat].c + '"><div class="ph"><span class="cn">' + esc(statName(st.stat)) + '</span><span class="sub">' + subs[st.stat] + '</span></div>' +
      panels[st.stat](map) + '</div></div>';

    st.root.innerHTML = html;
    restore(saved);
    footer();
  }

  // What a redraw would lose: the search box being typed in, with its cursor, and how far each list is scrolled.
  function keep() {
    const saved = { focus: null, start: 0, end: 0, scroll: {} };
    const active = document.activeElement;

    if (active && st.root.contains(active) && active.getAttribute('data-f')) {
      saved.focus = active.getAttribute('data-f');
      saved.start = active.selectionStart;
      saved.end = active.selectionEnd;
    }

    st.root.querySelectorAll('[data-scroll]').forEach(el => { saved.scroll[el.getAttribute('data-scroll')] = el.scrollTop; });

    return saved;
  }

  function restore(saved) {
    st.root.querySelectorAll('[data-scroll]').forEach(el => {
      const top = saved.scroll[el.getAttribute('data-scroll')];

      if (isNum(top)) el.scrollTop = top;
    });

    if (!saved.focus) return;

    const el = st.root.querySelector('[data-f="' + saved.focus + '"]');

    if (!el) return;

    el.focus();

    try { el.setSelectionRange(saved.start, saved.end); } catch (e) {}
  }

  function footer() {
    if (!st.ctx || typeof st.ctx.footer !== 'function') return;

    try {
      if (st.note) st.ctx.footer(esc(st.note), st.noteWarn);
      else if (!st.data) st.ctx.footer(L('Waiting for the game...', '等待游戏...'), false);
      else {
        const all = options();
        const ready = all.filter(o => stateOf(o) === 0).length;

        st.ctx.footer('<b>' + ready + '</b> ' + L('of ', '/ ') + all.length + L(' actions and items ready', ' 个行动和物品可用') + ' &middot; ' + L('Greyed out: not in the base', '灰色：基地中没有'), false);
      }
    } catch (err) {}
  }

  // ===== Events =====

  function onClick(e) {
    const el = e.target.closest('[data-a]');

    if (!el || !st.root.contains(el) || !st.data) return;

    const a = el.getAttribute('data-a');
    const k = el.getAttribute('data-k') || '';
    const v = el.getAttribute('data-v') || '';

    if (a === 'stat') {
      st.stat = Math.max(0, Math.min(3, parseInt(v, 10) || 0));
      render();
    } else if (a === 'flag') set(k, flag(k) ? 'false' : 'true');
    else if (a === 'choice') set(k, v);
    else if (a === 'step') set(k, v);
    else if (a === 'toggle') toggleKey(k, v);
    else if (a === 'move') {
      const bar = v.lastIndexOf('|');

      moveKey(k, v.slice(0, bar), parseInt(v.slice(bar + 1), 10) || 0);
    } else if (a === 'bed') setBedMode(k, v);
    else if (a === 'have') {
      st.have[k] = v === '1';
      render();
    } else if (a === 'edit') editLayout();
  }

  function onInput(e) {
    const el = e.target;
    const a = el.getAttribute && el.getAttribute('data-a');

    if (a === 'q') {
      st.q[el.getAttribute('data-k')] = el.value;
      render();
    } else if (a === 'range') {
      st.holding = true;

      const out = st.root.querySelector('[data-out="' + el.getAttribute('data-k') + '"]');

      if (out) out.textContent = el.value + '%';
    }
  }

  function onChange(e) {
    const el = e.target;

    if (!el.getAttribute || el.getAttribute('data-a') !== 'range') return;

    st.holding = false;
    set(el.getAttribute('data-k'), el.value);
  }

  // A slider let go outside the page sends no change: draw what came in meanwhile.
  function onRelease() {
    if (!st.holding) return;

    st.holding = false;

    if (st.stale) render();
  }

  // The layout is edited on the HUD itself, so the Mod Manager closes first.
  function editLayout() {
    try {
      if (window.__modmanager && typeof window.__modmanager.close === 'function') window.__modmanager.close();
    } catch (err) {}

    try { window.vuplex.postMessage('slmod|modmanager|close'); } catch (e) {}

    send('edit|1');
  }

  function showTab(el, ctx) {
    injectStyle();
    st.ctx = ctx || null;
    st.lang = ctx && ctx.lang === 'zh' ? 'zh' : 'en';

    if (!st.root) {
      st.root = document.createElement('div');
      st.root.className = 'slva';
      st.root.addEventListener('click', onClick);
      st.root.addEventListener('change', onChange);
      st.root.addEventListener('input', onInput);
      document.addEventListener('mouseup', onRelease, true);
    }

    // The plugin reads the base only while the tab is open: say it was opened.
    if (!st.shown) {
      st.shown = true;
      send('load');
    }

    // show runs on every redraw of the screen: draw again only when the node is new or the language changed.
    if (st.root.parentNode !== el) {
      el.appendChild(st.root);
      render();
    } else if (st.drawnLang !== st.lang) render();
    else footer();
  }

  function hideTab() {
    if (!st.shown) return;

    st.shown = false;
    st.holding = false;
    send('show|0');
  }

  function escapeTab() {
    return false;
  }

  window.__slvactions = { data };

  if (!isArr(window.__modmanagerTabs)) window.__modmanagerTabs = [];

  window.__modmanagerTabs.push({ guid: GUID, id: TAB_ID, title: TITLE, show: showTab, hide: hideTab, escape: escapeTab });

  try {
    if (window.__modmanager && typeof window.__modmanager.tabsChanged === 'function') window.__modmanager.tabsChanged();
  } catch (err) {}
})();
