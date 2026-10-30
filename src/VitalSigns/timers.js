// The Timers tab in the Mod Manager: every visible buff with a timer, how long it lasts in the game, and how long it
// lasts with the multipliers set here. Talks to the plugin through vuplex.postMessage("slmod|vitalsigns|timers|...") and is
// answered through window.__slvtimers. The plugin sends this script again whenever the web view loses it.
(function () {
  if (window.__slvtimers) return;

  const PREFIX = 'slmod|vitalsigns|timers|';
  const GUID = 'com.dev.survivallog.vitalsigns';
  const TAB_ID = 'timers';
  const TITLE = { en: 'Timers', zh: '计时' };
  const FILE_NAME = 'VitalSigns.Timers.json';
  const STYLE_ID = 'slvt-css';
  const MAX_PART = 500 * 86400;
  const CATEGORIES = [
    ['Calm', '平静'],
    ['Food and sickness', '饮食与疾病'],
    ['Mood', '心态'],
    ['Energy and sleep', '精力与睡眠'],
    ['Exercise and weather', '运动与天气'],
    ['Health and injury', '健康与伤病'],
    ['Exploration', '探索']
  ];

  // ===== Style =====

  const VARS = '--hl:#FFC83A;--hl-glow:rgba(255,200,58,0.35);--tm:#f0e8d8;--td:#a89a82;--tmeta:#7a6e5a;--bl:rgba(220,188,120,0.16);--bd2:rgba(220,188,120,0.10);--good:#7fd38a;--bad:#ff8a7a;';

  const CSS = [
    '&{' + VARS + 'position:relative;min-height:677px;padding:16px 26px 26px;color:var(--tm);background:radial-gradient(ellipse at 20% 0%,rgba(120,80,30,0.28),transparent 60%),linear-gradient(180deg,#1f1a15,#16120e)}',
    '&::before{content:"";position:absolute;top:0;left:0;right:0;height:2px;background:linear-gradient(90deg,transparent 10%,var(--hl) 50%,transparent 90%);opacity:.5}',
    'input{user-select:text}',
    '.top{display:flex;align-items:center;gap:16px;margin-bottom:12px}',
    '.title{font-size:1.5rem;font-weight:700;letter-spacing:4px;color:#fff;text-shadow:0 2px 10px rgba(0,0,0,.6);white-space:nowrap}',
    '.sub{color:var(--td);font-size:.9rem}',
    '.sp{flex:1}',
    '.key-btn{white-space:nowrap}',
    '.key-btn.is-off{opacity:.4;pointer-events:none}',
    '.probs{margin:0 0 12px;padding:8px 14px;border:1px solid rgba(255,82,82,0.4);background:rgba(255,82,82,0.08);color:#e9b;font-size:.85rem;line-height:1.5}',
    '.wait{padding:60px;text-align:center;color:var(--td)}',
    '.mults{display:grid;grid-template-columns:repeat(9,minmax(0,1fr));gap:8px;margin-bottom:12px;padding:10px 12px;border:1px solid var(--bd2);background:rgba(0,0,0,0.22)}',
    '.mult .k{display:block;color:var(--td);font-size:.78rem;margin-bottom:4px;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}',
    '.mult.good .k{color:var(--good)}',
    '.mult.bad .k{color:var(--bad)}',
    '.num{width:100%;box-sizing:border-box;background:rgba(0,0,0,0.4);border:1px solid var(--bd2);color:#fff;padding:5px 8px;font:400 .92rem var(--mono);outline:none}',
    '.num:focus{border-color:var(--hl)}',
    '.num::placeholder{color:#6d6253}',
    '.num.is-bad{border-color:#ff5252;background:rgba(255,82,82,0.12)}',
    '.num.is-set{border-color:rgba(255,200,58,0.45)}',
    '.bar{display:flex;align-items:center;gap:10px;margin-bottom:8px}',
    '.q{flex:1;max-width:320px;background:rgba(0,0,0,0.4);border:1px solid var(--bd2);color:#fff;padding:6px 10px;font:400 .92rem var(--body);outline:none}',
    '.q:focus{border-color:var(--hl)}',
    'select{background:#1d1813;border:1px solid var(--bl);color:#fff;padding:5px 8px;font:400 .88rem var(--body);outline:none}',
    '.chips{display:flex;border:1px solid var(--bl)}',
    '.chips span{padding:5px 12px;font-size:.82rem;color:var(--td);cursor:pointer;white-space:nowrap}',
    '.chips span.is-on{background:rgba(255,200,58,0.14);color:var(--hl)}',
    '.count{color:var(--tmeta);font-size:.8rem;white-space:nowrap}',
    '.list{height:470px;overflow-y:auto;border:1px solid var(--bd2);background:rgba(0,0,0,0.22)}',
    '::-webkit-scrollbar{width:6px}',
    '::-webkit-scrollbar-thumb{background:rgba(255,200,58,0.3)}',
    'table{width:100%;border-collapse:collapse;font-size:.88rem}',
    'th{position:sticky;top:0;z-index:2;padding:7px 10px;text-align:left;color:var(--hl);font-weight:700;letter-spacing:2px;font-size:.75rem;background:#1d1813;border-bottom:1px solid var(--bl);white-space:nowrap}',
    'td{padding:6px 10px;border-bottom:1px solid rgba(255,255,255,0.04);vertical-align:top}',
    'tr:hover td{background:rgba(255,200,58,0.04)}',
    '.nm{font-weight:700;white-space:nowrap}',
    'tr.good .nm{color:var(--good)}',
    'tr.bad .nm{color:var(--bad)}',
    '.nm .id{font:400 .72rem var(--mono);color:var(--tmeta);margin-left:6px}',
    '.ds{color:var(--td);font-size:.8rem;margin-top:2px;line-height:1.4;max-width:420px}',
    '.ct{color:var(--td);white-space:nowrap}',
    '.tm{font-family:var(--mono);white-space:nowrap}',
    '.tm small{display:block;color:var(--tmeta);font-size:.72rem}',
    '.fin{color:#fff}',
    '.fin.is-chg{color:var(--hl)}',
    '.tg{white-space:nowrap;font-family:var(--mono);font-size:.66rem;letter-spacing:1px;padding:1px 6px;border:1px solid;color:var(--tmeta);border-color:rgba(255,255,255,0.15)}',
    '.tg.category{color:#9ab;border-color:rgba(150,180,200,0.35)}',
    '.tg.buff{color:var(--hl);border-color:rgba(255,200,58,0.45)}',
    'td.own{width:76px}',
    '.none{padding:30px;text-align:center;color:var(--tmeta)}'
  ];

  function scopedCss() {
    return CSS.map(rule => {
      const brace = rule.indexOf('{');
      const sel = rule.slice(0, brace);
      const body = rule.slice(brace);

      return sel.split(',').map(s => {
        s = s.trim();

        return s.charAt(0) === '&' ? '#slmm .slvt' + s.slice(1) : '#slmm .slvt ' + s;
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
    edit: null,
    dirty: false,
    saving: false,
    note: '',
    noteWarn: false,
    lang: 'en',
    drawnLang: '',
    root: null,
    host: null,
    ctx: null,
    asked: false,
    q: '',
    cat: -1,
    kind: 'all',
    onlyOwn: false
  };

  const isNum = v => typeof v === 'number' && isFinite(v);
  const isArr = Array.isArray;
  const isObj = v => !!v && typeof v === 'object' && !isArr(v);
  const L = (en, zh) => st.lang === 'zh' ? zh : en;
  const esc = s => String(s == null ? '' : s).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
  const plain = s => String(s || '').replace(/<[^>]*>/g, '').replace(/\s+/g, ' ').trim();
  const numText = v => String(Math.round(v * 10000) / 10000);
  const minOf = () => st.data && isNum(st.data.min) ? st.data.min : 0.1;
  const maxOf = () => st.data && isNum(st.data.max) ? st.data.max : 100;

  function send(msg) {
    try { window.vuplex.postMessage(PREFIX + msg); } catch (e) {}
  }

  // ===== Numbers =====

  // A box's text as a multiplier. Empty reads as 0 where 0 means "use the one above". NaN marks the box wrong.
  function readBox(text, zeroAllowed) {
    const t = String(text).trim().replace(',', '.');

    if (t === '') return zeroAllowed ? 0 : NaN;
    if (!/^\d*\.?\d+$|^\d+\.$/.test(t)) return NaN;

    const v = parseFloat(t);

    if (zeroAllowed && v === 0) return 0;

    return v >= minOf() - 1e-9 && v <= maxOf() + 1e-9 ? v : NaN;
  }

  function resolve(b) {
    const e = st.edit;
    const own = e.own[b.id];

    if (isNum(own) && own > 0) return { m: own, from: 'buff' };

    const cat = b.cat >= 0 && b.cat < e.cats.length ? e.cats[b.cat] : 0;

    if (isNum(cat) && cat > 0) return { m: cat, from: 'category' };

    const g = b.good ? e.good : e.bad;

    return { m: isNum(g) ? g : 1, from: 'global' };
  }

  function duration(seconds) {
    const s = Math.max(0, Math.round(seconds));
    const d = Math.floor(s / 86400);
    const h = Math.floor((s % 86400) / 3600);
    const m = Math.floor((s % 3600) / 60);
    const parts = [];

    if (d) parts.push(d + L('d', '天'));
    if (h) parts.push(h + L('h', '小时'));
    if (m || !parts.length) parts.push(m + L('m', '分钟'));

    return parts.slice(0, 2).join(' ');
  }

  // Game seconds pass Toolset.TimeScale times faster than real ones; Day Length changes that number too.
  function realTime(seconds) {
    const scale = st.data && isNum(st.data.timeScale) && st.data.timeScale > 0 ? st.data.timeScale : 0;

    if (!scale) return '';

    const real = seconds / scale;

    if (real < 60) return Math.max(1, Math.round(real)) + L(' s real', ' 秒现实时间');

    const min = real / 60;

    return (min < 10 ? Math.round(min * 10) / 10 : Math.round(min)) + L(' min real', ' 分钟现实时间');
  }

  function finalSeconds(b, m) {
    return Math.min(b.seconds * m, MAX_PART);
  }

  // ===== Filters =====

  function nameOf(b) {
    return (st.lang === 'zh' ? b.nameZh || b.nameEn : b.nameEn || b.nameZh) || '#' + b.id;
  }

  function descOf(b) {
    return plain(st.lang === 'zh' ? b.descZh || b.descEn : b.descEn || b.descZh);
  }

  function shown() {
    const buffs = st.data && isArr(st.data.buffs) ? st.data.buffs : [];
    const q = st.q.trim().toLowerCase();

    return buffs.filter(b => {
      if (st.cat >= 0 && b.cat !== st.cat) return false;
      if (st.kind === 'good' && !b.good) return false;
      if (st.kind === 'bad' && b.good) return false;
      if (st.onlyOwn && !(st.edit.own[b.id] > 0)) return false;
      if (!q) return true;

      return String(b.id) === q || [b.nameEn, b.nameZh, b.descEn, b.descZh].some(t => String(t || '').toLowerCase().indexOf(q) >= 0);
    });
  }

  // ===== Drawing =====

  function render() {
    if (!st.root) return;

    st.drawnLang = st.lang;

    if (!st.data) {
      st.root.innerHTML = '<div class="wait">' + L('Reading the buffs...', '正在读取状态...') + '</div>';
      footer();

      return;
    }

    const d = st.data;
    const probs = isArr(d.problems) && d.problems.length ? '<div class="probs">' + d.problems.map(esc).join('<br>') + '</div>' : '';
    const scale = isNum(d.timeScale) && d.timeScale > 0
      ? L('One game day is ' + numText(86400 / d.timeScale / 60) + ' real minutes.', '游戏中的一天为现实时间 ' + numText(86400 / d.timeScale / 60) + ' 分钟。')
      : '';

    st.root.innerHTML =
      '<div class="top"><div class="title">' + L('BUFF TIMERS', '状态计时') + '</div><div class="sub">' +
      L('How long buffs last. Changes apply at once, to buffs already running too.', '状态的持续时间。修改立即生效，正在生效的状态也会改变。') + ' ' + scale + '</div>' +
      '<div class="sp"></div>' +
      '<span class="key-btn" data-a="discard">' + L('DISCARD', '放弃') + '</span>' +
      '<span class="key-btn" data-a="save">' + L('SAVE', '保存') + '</span></div>' +
      probs +
      '<div class="mults">' + multsHtml() + '</div>' +
      '<div class="bar">' +
      '<input class="q" data-f="q" placeholder="' + esc(L('Search buffs', '搜索状态')) + '" value="' + esc(st.q) + '">' +
      '<select data-f="cat"><option value="-1">' + L('All categories', '所有类别') + '</option>' +
      CATEGORIES.map((c, i) => '<option value="' + i + '"' + (st.cat === i ? ' selected' : '') + '>' + esc(L(c[0], c[1])) + '</option>').join('') + '</select>' +
      '<div class="chips">' + chip('kind', 'all', L('All', '全部'), st.kind === 'all') + chip('kind', 'good', L('Good', '增益'), st.kind === 'good') + chip('kind', 'bad', L('Bad', '负面'), st.kind === 'bad') + '</div>' +
      '<div class="chips">' + chip('own', '1', L('Own multiplier', '单独倍数'), st.onlyOwn) + '</div>' +
      '<div class="sp"></div><span class="count"></span></div>' +
      '<div class="list"></div>';

    renderList();
    refreshState();
  }

  function chip(kind, value, label, isOn) {
    return '<span data-c="' + kind + '" data-v="' + value + '"' + (isOn ? ' class="is-on"' : '') + '>' + esc(label) + '</span>';
  }

  function box(field, value, zeroAllowed, title) {
    const text = !isNum(value) || (zeroAllowed && value === 0) ? '' : numText(value);
    const cls = !isNum(value) ? ' is-bad' : zeroAllowed && value > 0 ? ' is-set' : '';

    return '<input class="num' + cls + '" data-m="' + field + '" value="' + esc(text) + '"' + (zeroAllowed ? ' placeholder="' + esc(L('global', '全局')) + '"' : '') + ' title="' + esc(title) + '">';
  }

  function multsHtml() {
    const e = st.edit;
    const range = numText(minOf()) + ' - ' + numText(maxOf());
    const catTip = L('Empty or 0 uses the global multiplier. ', '留空或 0 为使用全局倍数。') + range;

    return '<label class="mult good"><span class="k">' + L('Good buffs', '增益状态') + '</span>' + box('good', e.good, false, range) + '</label>' +
      '<label class="mult bad"><span class="k">' + L('Bad buffs', '负面状态') + '</span>' + box('bad', e.bad, false, range) + '</label>' +
      CATEGORIES.map((c, i) => '<label class="mult"><span class="k" title="' + esc(L(c[0], c[1])) + '">' + esc(L(c[0], c[1])) + '</span>' + box('cat' + i, e.cats[i], true, catTip) + '</label>').join('');
  }

  function renderList() {
    const list = st.root && st.root.querySelector('.list');

    if (!list) return;

    const rows = shown();
    const count = st.root.querySelector('.count');

    if (count) count.textContent = rows.length + ' / ' + st.data.buffs.length;

    if (!rows.length) {
      list.innerHTML = '<div class="none">' + L('No buff matches.', '没有匹配的状态。') + '</div>';

      return;
    }

    const ownTip = L('Empty or 0 uses the category or global multiplier. ', '留空或 0 为使用类别或全局倍数。') + numText(minOf()) + ' - ' + numText(maxOf());

    list.innerHTML = '<table><thead><tr><th>' + L('BUFF', '状态') + '</th><th>' + L('CATEGORY', '类别') + '</th><th>' + L('GAME', '原本') + '</th><th>' +
      L('USES', '使用') + '</th><th>' + L('LASTS', '持续') + '</th><th>' + L('OWN', '单独') + '</th></tr></thead><tbody>' +
      rows.map(b => {
        const own = st.edit.own[b.id];
        const cat = CATEGORIES[b.cat];

        return '<tr data-id="' + b.id + '" class="' + (b.good ? 'good' : 'bad') + '">' +
          '<td><div class="nm">' + esc(nameOf(b)) + ' <span class="id">' + b.id + '</span></div><div class="ds">' + esc(descOf(b)) + '</div></td>' +
          '<td class="ct">' + esc(cat ? L(cat[0], cat[1]) : '-') + '</td>' +
          '<td class="tm">' + esc(duration(b.seconds)) + '<small>' + esc(realTime(b.seconds)) + '</small></td>' +
          '<td class="src"></td><td class="tm fin"></td>' +
          '<td class="own"><input class="num' + (own > 0 ? ' is-set' : '') + '" data-own="' + b.id + '" value="' + esc(own > 0 ? numText(own) : '') + '" placeholder="-" title="' + esc(ownTip) + '"></td></tr>';
      }).join('') + '</tbody></table>';

    refreshRows();
  }

  const fromLabel = from => from === 'buff' ? L('own', '单独') : from === 'category' ? L('category', '类别') : L('global', '全局');

  // Only the worked-out cells change while the player types, so the box keeps its focus.
  function refreshRows() {
    if (!st.root) return;

    const byId = {};

    st.data.buffs.forEach(b => { byId[b.id] = b; });
    st.root.querySelectorAll('tr[data-id]').forEach(tr => {
      const b = byId[tr.getAttribute('data-id')];

      if (!b) return;

      const r = resolve(b);
      const secs = finalSeconds(b, r.m);
      const src = tr.querySelector('.src');
      const fin = tr.querySelector('.fin');

      if (src) src.innerHTML = '<span class="tg ' + r.from + '">' + esc(fromLabel(r.from)) + ' x' + esc(numText(r.m)) + '</span>';

      if (fin) {
        fin.className = 'tm fin' + (Math.abs(r.m - 1) > 1e-9 ? ' is-chg' : '');
        fin.innerHTML = esc(duration(secs)) + '<small>' + esc(realTime(secs)) + '</small>';
      }
    });
  }

  function hasBadBox() {
    return !!(st.root && st.root.querySelector('.num.is-bad')) || !isNum(st.edit.good) || !isNum(st.edit.bad) || st.edit.cats.some(c => !isNum(c));
  }

  function refreshState() {
    if (!st.root) return;

    const save = st.root.querySelector('[data-a="save"]');
    const discard = st.root.querySelector('[data-a="discard"]');

    if (save) save.classList.toggle('is-off', !st.dirty || st.saving || hasBadBox());
    if (discard) discard.classList.toggle('is-off', !st.dirty || st.saving);

    footer();
  }

  function footer() {
    if (!st.ctx || typeof st.ctx.footer !== 'function') return;

    try {
      if (hasBadBoxSafe()) st.ctx.footer('<em>' + L('A multiplier must be from ', '倍数必须在 ') + numText(minOf()) + L(' to ', ' 到 ') + numText(maxOf()) + L('', ' 之间') + '</em>', true);
      else if (st.dirty) st.ctx.footer('<em>' + L('Unsaved changes', '有未保存的修改') + '</em>', true);
      else if (st.note) st.ctx.footer(esc(st.note), st.noteWarn);
      else st.ctx.footer('<b>' + FILE_NAME + '</b> &middot; ' + ownCount() + L(' buffs with their own multiplier', ' 个状态设置了单独倍数'), false);
    } catch (err) {}
  }

  function hasBadBoxSafe() {
    return !!st.edit && hasBadBox();
  }

  function ownCount() {
    return st.edit ? Object.keys(st.edit.own).filter(k => st.edit.own[k] > 0).length : 0;
  }

  // ===== Events =====

  function onInput(ev) {
    const t = ev.target;

    if (!t || !st.edit) return;

    if (t.getAttribute('data-f') === 'q') {
      st.q = t.value;
      renderList();

      return;
    }

    const m = t.getAttribute('data-m');
    const own = t.getAttribute('data-own');

    if (!m && !own) return;

    const isGlobal = m === 'good' || m === 'bad';
    const v = readBox(t.value, !isGlobal);

    t.classList.toggle('is-bad', !isNum(v));
    t.classList.toggle('is-set', isNum(v) && v > 0 && !isGlobal);

    if (m === 'good') st.edit.good = v;
    else if (m === 'bad') st.edit.bad = v;
    else if (m) st.edit.cats[+m.slice(3)] = v;
    // A wrong buff box keeps the buff's last good number for the preview; Save stays off until it is fixed.
    else if (isNum(v) && v > 0) st.edit.own[own] = v;
    else if (isNum(v)) delete st.edit.own[own];

    st.dirty = true;
    st.note = '';

    refreshRows();
    refreshState();
  }

  function onChange(ev) {
    const t = ev.target;

    if (t && t.getAttribute('data-f') === 'cat') {
      st.cat = +t.value;
      renderList();
    }
  }

  function onClick(ev) {
    const t = ev.target && ev.target.closest ? ev.target.closest('[data-a],[data-c]') : null;

    if (!t || !st.data) return;

    const c = t.getAttribute('data-c');

    if (c === 'kind') {
      st.kind = t.getAttribute('data-v');
      t.parentNode.querySelectorAll('span').forEach(s => s.classList.toggle('is-on', s === t));
      renderList();

      return;
    }

    if (c === 'own') {
      st.onlyOwn = !st.onlyOwn;
      t.classList.toggle('is-on', st.onlyOwn);
      renderList();

      return;
    }

    const a = t.getAttribute('data-a');

    if (a === 'save') save();
    else if (a === 'discard') {
      startEdit();
      render();
    }
  }

  function save() {
    if (!st.dirty || st.saving || hasBadBox()) return;

    const buffs = {};

    Object.keys(st.edit.own).forEach(k => { if (st.edit.own[k] > 0) buffs[k] = st.edit.own[k]; });
    st.saving = true;
    refreshState();
    send('save|' + JSON.stringify({ good: st.edit.good, bad: st.edit.bad, categories: st.edit.cats.slice(), buffs }));
  }

  // ===== From the plugin =====

  function startEdit() {
    const d = st.data;
    const cats = isArr(d.categories) ? d.categories.slice(0, CATEGORIES.length) : [];
    const own = {};

    while (cats.length < CATEGORIES.length) cats.push(0);

    (isArr(d.buffs) ? d.buffs : []).forEach(b => { if (isNum(b.own) && b.own > 0) own[b.id] = b.own; });
    st.edit = { good: isNum(d.good) ? d.good : 1, bad: isNum(d.bad) ? d.bad : 1, cats: cats.map(c => isNum(c) ? c : 0), own };
    st.dirty = false;
  }

  function data(d) {
    if (!isObj(d)) return;

    d.buffs = isArr(d.buffs) ? d.buffs.filter(b => isObj(b) && isNum(b.id) && isNum(b.seconds) && b.seconds > 0) : [];
    st.data = d;
    startEdit();
    render();
  }

  function saved(ok, msg) {
    st.saving = false;
    st.note = ok ? L('Saved. Buffs already running were changed too.', '已保存。正在生效的状态也已改变。') : L('Not saved: ', '未保存：') + (msg || '');
    st.noteWarn = !ok;
    refreshState();
  }

  // ===== Wiring =====

  function load() {
    if (st.asked) return;

    st.asked = true;

    if (window.vuplex) send('load');
    else window.addEventListener('vuplexready', () => send('load'), { once: true });
  }

  function showTab(el, ctx) {
    injectStyle();
    st.ctx = ctx || null;
    st.lang = ctx && ctx.lang === 'zh' ? 'zh' : 'en';

    if (!st.root) {
      st.root = document.createElement('div');
      st.root.className = 'slvt';
      st.root.addEventListener('click', onClick);
      st.root.addEventListener('change', onChange);
      st.root.addEventListener('input', onInput);
    }

    load();

    // show runs on every redraw of the screen: draw again only when the node is new or the language changed.
    if (st.root.parentNode !== el) {
      el.appendChild(st.root);
      render();
    } else if (st.drawnLang !== st.lang) render();
    else footer();
  }

  function hideTab() {}

  function escapeTab() {
    return false;
  }

  window.__slvtimers = { data, saved };

  if (!isArr(window.__modmanagerTabs)) window.__modmanagerTabs = [];

  window.__modmanagerTabs.push({ guid: GUID, id: TAB_ID, title: TITLE, show: showTab, hide: hideTab, escape: escapeTab });

  try {
    if (window.__modmanager && typeof window.__modmanager.tabsChanged === 'function') window.__modmanager.tabsChanged();
  } catch (err) {}

  // Asked once the script is in, so the tab has its data before the player opens it.
  load();
})();
