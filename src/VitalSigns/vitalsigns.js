// Vital Signs: more about the player's stats on the HUD (the CoreUI1 page). Each stat bar gets its gain multiplier and
// hover lines for how its changes are scaled, the Satiety bar gets the Well Fed lines, each buff with an end gets a
// timer stripe, and the Inventory button shows the weight carried. The plugin calls apply every couple of seconds with
// the game clock, and with a new reading only when it changed. Reads and draws only; nothing the game keeps is changed.
(function () {
    if (window.__slvitals) return;

    var STYLE_ID = 'vs-style';
    // How attrList marks each row, in the order the plugin sends the stats: Satiety, Morale, Stamina, Life.
    var ICONS = ['\uD83C\uDF56', '\uD83E\uDDE0', '\u26A1', '\u2764'];
    var SATIETY = 0;
    // The plugin calls every two seconds; the stripe slides over that time instead of jumping.
    var STEP_SECONDS = 2;
    // The weight turns amber this close to Overweight.
    var NEAR_OVERWEIGHT = 0.9;
    var DRAWN = ['vs-chip', 'vs-mark', 'vs-lines', 'vs-timer', 'vs-weight', 'vs-weight-tip'];

    var TEXTS = {
        en: {
            gain: 'Gains x',
            loss: 'Losses x',
            naturalGain: 'Natural recovery x',
            naturalLoss: 'Natural drain x',
            wellFed: function (name, from, until) { return name + ' at ' + from + ' or more, lost below ' + until + '.'; },
            carrying: function (carried, max) { return 'Carrying ' + carried + ' / ' + max + ' kg'; },
            line: function (name, at) { return name + ' above ' + at + ' kg'; },
            left: function (name, left) { return left + ' kg left before ' + name; },
            over: function (name, over) { return name + ': ' + over + ' kg over'; }
        },
        zh: {
            gain: '增加 x',
            loss: '减少 x',
            naturalGain: '自然恢复 x',
            naturalLoss: '自然消耗 x',
            wellFed: function (name, from, until) { return '饱腹达到' + from + '获得「' + name + '」，低于' + until + '失去。'; },
            carrying: function (carried, max) { return '负重 ' + carried + ' / ' + max + ' 千克'; },
            line: function (name, at) { return '超过 ' + at + ' 千克即「' + name + '」'; },
            left: function (name, left) { return '距离「' + name + '」还有 ' + left + ' 千克'; },
            over: function (name, over) { return '「' + name + '」：已超出 ' + over + ' 千克'; }
        }
    };

    // Under the game's own look: the chip and lines sit inside its bar, the hover lines follow its .st-body text.
    var CSS = [
        '.vs-chip{position:absolute;left:50%;top:50%;transform:translate(-50%,-50%);padding:0 3px;border-radius:2px;background:rgba(0,0,0,.45);font-size:8.5px;line-height:11px;font-weight:bold;pointer-events:none;text-shadow:.7px .7px 1.4px #000}',
        '.vs-chip-up{color:#a5d6a7}',
        '.vs-chip-down{color:#ff8a80}',
        '.vs-mark{position:absolute;top:0;bottom:0;width:1px;margin-left:-.5px;background:rgba(255,255,255,.6);pointer-events:none}',
        '.vs-mark-until{background:rgba(255,255,255,.3)}',
        '.vs-lines{margin-top:6px;padding-top:5px;border-top:1px solid rgba(255,255,255,.15);white-space:pre-line}',
        '.vs-timer{position:absolute;left:0;right:0;bottom:0;height:2px;background:rgba(255,255,255,.75);transform-origin:left;transition:transform ' + STEP_SECONDS + 's linear;pointer-events:none}',
        '.bf-leave-active .vs-timer{transition:none}',
        '.vs-weight{position:absolute;left:0;right:0;bottom:1px;font-size:9px;line-height:10px;text-align:center;color:#cfd3d5;pointer-events:none;font-variant-numeric:tabular-nums}',
        '.vs-weight.vs-near{color:#ffc107}',
        '.vs-weight.vs-over{color:#ff8a80}',
        '.vs-weight-tip{margin-top:4px;white-space:pre-line}'
    ].join('\n');

    var held;
    var isHeld = false;
    // Each running timer's full length, keyed by buff id and its place among buffs of that id: the longest time left
    // seen since its end last moved later, or the buff's own length when that is longer.
    var lengths = {};

    function apply(now, data) {
        if (arguments.length > 1) {
            held = data;
            isHeld = true;
        }

        if (!isHeld) return 'missing';

        addStyle();

        if (!held) {
            clear();

            return 'noplayer';
        }

        drawWeight(held);

        var rows = document.querySelectorAll('.status-panel > .status-row');

        if (!rows.length) return 'nobar';

        var list = attrList();
        var isListed = !!list && list.length === rows.length;

        for (var r = 0; r < rows.length; r++) {
            var stat = statOf(isListed ? list[r] : null, r);

            drawStat(rows[r], stat >= 0 ? held.stats[stat] : null, stat === SATIETY ? held.wellFed : null);
        }

        drawTimers(rows, isListed ? list : null, held.buffs, now);

        return 'ok';
    }

    // ===== Stat bars =====

    // The row's stat by its attrList mark, or by its place when the Vue state cannot be read; -1 for a row it does not know.
    function statOf(item, index) {
        if (item) {
            for (var i = 0; i < ICONS.length; i++) {
                if (String(item.attrIcon || '').indexOf(ICONS[i]) >= 0) return i;
            }

            return -1;
        }

        return index < ICONS.length ? index : -1;
    }

    function drawStat(row, stat, wellFed) {
        var bar = row.querySelector('.progress-bar-wrapper');
        var mask = bar ? bar.querySelector('.bar-mask') : null;
        var tip = bar ? bar.querySelector('.status-tooltip') : null;
        var say = held.zh ? TEXTS.zh : TEXTS.en;
        var lines = [];

        if (!mask) return;

        if (stat) {
            addScale(lines, say.gain, stat[0]);
            addScale(lines, say.loss, stat[1]);
            addScale(lines, say.naturalGain, stat[2]);
            addScale(lines, say.naturalLoss, stat[3]);
        }

        if (wellFed) lines.push(say.wellFed(held.names.wellFed, wellFed[0], wellFed[1]));

        drawChip(mask, stat ? stat[0] : 0);
        drawMarks(mask, wellFed);

        if (tip) setOwnText(tip, 'vs-lines', 'div', 'st-body vs-lines', lines.join('\n'));
    }

    function addScale(lines, label, ratio) {
        var text = scaleText(ratio);

        if (text) lines.push(label + text);
    }

    // The game multiplies a change by 1 + ratio. Empty when that rounds to 1, so an unscaled stat shows nothing.
    function scaleText(ratio) {
        var scale = Math.round((1 + ratio) * 100) / 100;

        return scale === 1 ? '' : String(scale);
    }

    function drawChip(mask, gain) {
        var text = scaleText(gain);
        var chip = ownChild(mask, 'vs-chip');

        if (!text) {
            if (chip) mask.removeChild(chip);

            return;
        }

        if (!chip) chip = mask.appendChild(element('div', 'vs-chip'));

        setClass(chip, 'vs-chip-up', gain > 0);
        setClass(chip, 'vs-chip-down', gain < 0);
        setText(chip, 'x' + text);
    }

    // Two thin lines across the Satiety bar: where Well Fed is gained, and where it is lost.
    function drawMarks(mask, wellFed) {
        var marks = [['vs-mark-from', wellFed ? wellFed[0] : 0], ['vs-mark-until', wellFed ? wellFed[1] : 0]];

        for (var i = 0; i < marks.length; i++) {
            var mark = ownChild(mask, marks[i][0]);
            var isShown = !!wellFed && wellFed[2] > 0 && marks[i][1] > 0 && marks[i][1] < wellFed[2];

            if (!isShown) {
                if (mark) mask.removeChild(mark);

                continue;
            }

            if (!mark) mark = mask.appendChild(element('div', 'vs-mark ' + marks[i][0]));

            var left = (marks[i][1] / wellFed[2] * 100).toFixed(2) + '%';

            if (mark.style.left !== left) mark.style.left = left;
        }
    }

    // ===== Buff timers =====

    // Badges follow their row's attrList buffs in order, except a badge on its way out, which the game keeps in place
    // for its leaving animation. A row whose badges and list do not line up (a change in flight) waits for the next call.
    function drawTimers(rows, list, buffs, now) {
        var ends = {};
        var seen = {};
        var i;

        for (i = 0; i < buffs.length; i++) (ends[buffs[i][0]] = ends[buffs[i][0]] || []).push(buffs[i]);

        for (var r = 0; r < rows.length; r++) {
            var container = rows[r].querySelector('.buff-container');
            var listed = list && list[r] && Array.isArray(list[r].buffs) ? list[r].buffs : null;

            if (!container) continue;

            var badges = Array.prototype.filter.call(container.children, isBadge);

            if (!listed || listed.length !== badges.length) continue;

            var places = {};

            for (i = 0; i < badges.length; i++) {
                var id = listed[i] ? listed[i].buffConfigId : null;
                var place = places[id] = (places[id] || 0) + 1;
                var timed = ends[id] ? ends[id][place - 1] : null;
                var key = id + '#' + place;

                if (timed) seen[key] = true;

                drawTimer(badges[i], timed ? share(key, timed, now) : -1);
            }
        }

        for (var k in lengths) {
            if (!seen[k]) delete lengths[k];
        }
    }

    function isBadge(node) {
        return node.classList.contains('buff') && !node.classList.contains('bf-leave-active');
    }

    // The part of the buff still to run, 0 to 1.
    function share(key, timed, now) {
        var end = timed[1];
        var left = Math.max(0, end - now);
        var known = lengths[key];

        if (!known || end > known.end) known = lengths[key] = { end: end, length: Math.max(timed[2], left) };

        known.end = end;
        known.length = Math.max(known.length, left);

        return known.length > 0 ? Math.min(1, left / known.length) : 0;
    }

    function drawTimer(badge, part) {
        var stripe = ownChild(badge, 'vs-timer');

        if (part < 0) {
            if (stripe) badge.removeChild(stripe);

            return;
        }

        var transform = 'scaleX(' + part.toFixed(4) + ')';

        if (!stripe) {
            stripe = element('div', 'vs-timer');
            // Set before it is placed, so a new stripe starts where it is instead of sliding in from full.
            stripe.style.transform = transform;
            badge.appendChild(stripe);

            return;
        }

        if (stripe.style.transform !== transform) stripe.style.transform = transform;
    }

    // ===== Carried weight =====

    function drawWeight(data) {
        var button = bagButton();

        if (!button) return;

        var weight = data.weight;
        var tip = button.querySelector('.entry-tip');
        var label = ownChild(button, 'vs-weight');

        if (!weight) {
            if (label) button.removeChild(label);
            if (tip) setOwnText(tip, 'vs-weight-tip', 'div', 'entry-tip-body vs-weight-tip', '');

            return;
        }

        var say = data.zh ? TEXTS.zh : TEXTS.en;
        var name = data.names.overweight;
        var carried = weight[0];
        var line = weight[2];
        var isOver = line > 0 && carried > line;
        var lines = [say.carrying(kgUp(carried), kgDown(weight[1]))];

        if (line > 0) {
            lines.push(say.line(name, kgDown(line)));
            lines.push(isOver ? say.over(name, kgUp(carried - line)) : say.left(name, kgDown(line - carried)));
        }

        if (!label) label = button.appendChild(element('span', 'vs-weight'));

        setClass(label, 'vs-over', isOver);
        setClass(label, 'vs-near', !isOver && line > 0 && carried >= line * NEAR_OVERWEIGHT);
        setText(label, kgUp(carried));

        if (tip) setOwnText(tip, 'vs-weight-tip', 'div', 'entry-tip-body vs-weight-tip', lines.join('\n'));
    }

    // The Inventory button: the HUD tool whose hover title is the game's own word for the bag.
    function bagButton() {
        var state = vueState();
        var name = state ? String(state.i18n_bag || '') : '';
        var buttons = name ? document.querySelectorAll('.vertical-tools > .has-entry-tip') : [];

        for (var i = 0; i < buttons.length; i++) {
            var title = buttons[i].querySelector('.entry-tip-title');

            if (title && String(title.textContent).indexOf(name) === 0) return buttons[i];
        }

        return null;
    }

    // Grams to kilograms at one place, the way the game's bag screen rounds: what is carried up, limits down.
    function kgUp(grams) {
        return (Math.ceil(grams / 100) / 10).toFixed(1);
    }

    function kgDown(grams) {
        return (Math.floor(grams / 100) / 10).toFixed(1);
    }

    // ===== Page helpers =====

    function attrList() {
        var state = vueState();

        return state && Array.isArray(state.attrList) ? state.attrList : null;
    }

    function vueState() {
        var app = document.getElementById('app');
        var setup = app && app._vnode && app._vnode.component ? app._vnode.component.setupState : null;

        return setup && setup.state ? setup.state : null;
    }

    function clear() {
        for (var i = 0; i < DRAWN.length; i++) {
            var nodes = document.querySelectorAll('.' + DRAWN[i]);

            for (var n = 0; n < nodes.length; n++) {
                if (nodes[n].parentElement) nodes[n].parentElement.removeChild(nodes[n]);
            }
        }

        lengths = {};
    }

    function ownChild(parent, className) {
        for (var i = 0; i < parent.children.length; i++) {
            if (parent.children[i].classList.contains(className)) return parent.children[i];
        }

        return null;
    }

    // A child of ours holding text; empty text takes it away.
    function setOwnText(parent, marker, tagName, className, text) {
        var box = ownChild(parent, marker);

        if (!text) {
            if (box) parent.removeChild(box);

            return;
        }

        if (!box) box = parent.appendChild(element(tagName, className));

        setText(box, text);
    }

    function setText(target, text) {
        if (target.textContent !== text) target.textContent = text;
    }

    function setClass(target, className, isOn) {
        if (isOn) target.classList.add(className);
        else target.classList.remove(className);
    }

    function element(tagName, className) {
        var made = document.createElement(tagName);

        made.className = className;

        return made;
    }

    function addStyle() {
        if (document.getElementById(STYLE_ID)) return;

        var style = document.createElement('style');

        style.id = STYLE_ID;
        style.textContent = CSS;
        document.head.appendChild(style);
    }

    window.__slvitals = { apply: apply, texts: TEXTS };
})();
