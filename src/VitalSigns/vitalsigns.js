// Vital Signs: more about the player's stats on the HUD (the CoreUI1 page). Each stat bar gets its gain multiplier,
// shading for the losses still to come and a fuller tooltip; the Satiety bar gets the Well Fed lines; each buff with
// an end gets a timer stripe and each buff tooltip its effects, time and cures; the Inventory button shows the weight
// carried; a Fitness bar, which the game never shows, goes under the stat rows. The plugin calls apply every couple of seconds with the game clock, and with a new reading only when it
// changed. Reads and draws only; nothing the game keeps is changed.
(function () {
    if (window.__slvitals) return;

    var STYLE_ID = 'vs-style';
    var ZOOM_ID = 'vs-zoom';
    // How attrList marks each row, in the order the plugin sends the stats: Satiety, Morale, Stamina, Life.
    var ICONS = ['\uD83C\uDF56', '\uD83E\uDDE0', '\u26A1', '\u2764'];
    var SATIETY = 0;
    var MORALE = 1;
    var LIFE = 3;
    var HEART = '\u2764';
    // The plugin calls every two seconds; the stripe slides over that time instead of jumping.
    var STEP_SECONDS = 2;
    // The weight turns amber this close to Overweight.
    var NEAR_OVERWEIGHT = 0.9;
    // A timer stripe turns amber when this little of the buff is left.
    var LOW_TIME = 0.15;
    // Each cure list shows at most this many items.
    var MOST_ITEMS = 10;
    // How far up from the hovered node a buff badge is looked for.
    var BADGE_DEPTH = 8;
    // The game's own line: below 30 Fitness the player is Weakened. A warning comes before it.
    var WEAKENED_BELOW = 30;
    var LOW_FITNESS_BELOW = 40;
    var FIT_ROW = 'vs-fit-row';
    var FIT_ICON = '\u271A';
    // Put on the stat panel while the Fitness row is in it, so the row above keeps the hover boxes that open upward.
    var FIT_MARK = 'data-vs-fit';
    // What BarSize, BuffSize and BarTooltipSize zoom.
    var BAR_BOXES = '.status-panel > .status-row > .progress-bar-wrapper,.status-panel > .' + FIT_ROW + ' > .progress-bar-wrapper';
    var BUFF_BOXES = '.status-panel > .status-row > .buff-container';
    var BAR_TIPS = '.status-panel .progress-bar-wrapper > .status-tooltip';
    var DRAWN = ['vs-chip', 'vs-mark', 'vs-loss', 'vs-st', 'vs-timer', 'vs-bt', 'vs-btime', 'vs-weight', 'vs-wbar', 'vs-weight-tip', FIT_ROW];
    // Attributes put on the game's own tooltips, taken off again by clear. Attributes, not classes: Vue writes the whole
    // class of a tooltip each time it shows or hides it, which wiped a class of ours and showed the game's own look until
    // the next call drew it again. Vue leaves attributes it does not own alone.
    var MARKS = ['data-vs-no-game-text', 'data-vs-tip', 'data-vs-timed'];
    var MARKED_TIPS = ['buff-tooltip', 'status-tooltip'];

    var TEXTS = {
        en: {
            stats: ['Satiety', 'Morale', 'Stamina', 'Life'],
            value: function (value, max) { return 'Value ' + value + ' / ' + max; },
            gain: 'Gains ',
            loss: 'Losses ',
            naturalGain: 'Natural recovery ',
            naturalLoss: 'Natural drain ',
            food: function (food, change) { return food + ' ' + change; },
            coming: function (name, amount, time) { return name + ': ' + amount + ' over ' + time; },
            wellFed: function (name, from, until) { return name + ' at ' + from + ' or more, lost below ' + until + '.'; },
            every: function (stat, hours) { return stat + (hours === 1 ? ' each hour' : ' every ' + hours + 'h'); },
            start: function (stat) { return stat + ' when it starts'; },
            end: function (stat) { return stat + ' when it ends'; },
            max: function (stat) { return 'Max ' + stat; },
            gainFx: function (stat) { return stat + ' gain'; },
            lossFx: function (stat) { return stat + ' loss'; },
            speed: 'Move speed',
            satietyFood: function (food) { return food + ' satiety'; },
            moraleFood: function (food) { return food + ' morale'; },
            layers: 'Layers',
            suppressed: function (by) { return 'Suppressed by ' + by; },
            suppressor: 'Analgesia',
            cureWith: 'Cure with',
            suppressWith: 'Suppress with',
            days: 'd',
            hours: 'h',
            minutes: 'm',
            carrying: function (carried, max) { return 'Carrying ' + carried + ' / ' + max + ' kg'; },
            line: function (name, at) { return name + ' above ' + at + ' kg'; },
            left: function (name, left) { return left + ' kg left before ' + name; },
            over: function (name, over) { return name + ': ' + over + ' kg over'; },
            fitness: 'Fitness',
            fitAbout: 'High Fitness lowers your chance of getting sick. Below 30 you become Weakened. Staying Hungry or Fatigued for too long lowers it. Food that gives Fitness raises it.',
            weakened: 'Below 30, so you are Weakened.',
            nearWeakened: function (left) { return 'Close to Weakened: ' + left + ' more lost and it drops below 30.'; },
            aboveWeakened: function (above) { return above + ' above 30, where Weakened starts.'; }
        },
        zh: {
            stats: ['饱腹', '心情', '体力', '生命'],
            value: function (value, max) { return '数值 ' + value + ' / ' + max; },
            gain: '增加 ',
            loss: '减少 ',
            naturalGain: '自然恢复 ',
            naturalLoss: '自然消耗 ',
            food: function (food, change) { return food + ' ' + change; },
            coming: function (name, amount, time) { return '「' + name + '」：' + time + '内 ' + amount; },
            wellFed: function (name, from, until) { return '饱腹达到' + from + '获得「' + name + '」，低于' + until + '失去。'; },
            every: function (stat, hours) { return stat + (hours === 1 ? '每小时' : '每' + hours + '小时'); },
            start: function (stat) { return '开始时' + stat; },
            end: function (stat) { return '结束时' + stat; },
            max: function (stat) { return stat + '上限'; },
            gainFx: function (stat) { return stat + '增加'; },
            lossFx: function (stat) { return stat + '减少'; },
            speed: '移动速度',
            satietyFood: function (food) { return food + '饱腹'; },
            moraleFood: function (food) { return food + '心情'; },
            layers: '层数',
            suppressed: function (by) { return '已被「' + by + '」压制'; },
            suppressor: '镇痛',
            cureWith: '治愈',
            suppressWith: '压制',
            days: '天',
            hours: '小时',
            minutes: '分',
            carrying: function (carried, max) { return '负重 ' + carried + ' / ' + max + ' 千克'; },
            line: function (name, at) { return '超过 ' + at + ' 千克即「' + name + '」'; },
            left: function (name, left) { return '距离「' + name + '」还有 ' + left + ' 千克'; },
            over: function (name, over) { return '「' + name + '」：已超出 ' + over + ' 千克'; },
            fitness: '健康',
            fitAbout: '良好的健康可以降低生病的概率。低于30会陷入「亚健康」。长期「饥饿」或「疲劳」会降低健康。食用带有健康加成的食物可以提高健康。',
            weakened: '低于30，你处于「亚健康」状态。',
            nearWeakened: function (left) { return '接近「亚健康」：再降低 ' + left + ' 点就会低于30。'; },
            aboveWeakened: function (above) { return '比「亚健康」的界线30高 ' + above + ' 点。'; }
        }
    };

    // Under the game's own look: the chip, lines and shading sit inside its bar, the tooltip blocks use its text styles.
    var CSS = [
        '.status-panel .progress-text{text-shadow:-.6px -.6px 0 rgba(0,0,0,.85),.6px -.6px 0 rgba(0,0,0,.85),-.6px .6px 0 rgba(0,0,0,.85),.6px .6px 0 rgba(0,0,0,.85)}',
        // In the name's own line, after the name, so it can never sit over or under the label.
        '.vs-chip{flex-shrink:0;margin-left:2px;padding:0 3px;border-radius:2px;background:rgba(0,0,0,.45);font-size:8.5px;line-height:11px;font-weight:bold;pointer-events:none;text-shadow:.7px .7px 1.4px #000}',
        '.vs-chip-up{color:#a5d6a7}',
        '.vs-chip-down{color:#ff8a80}',
        '.vs-mark{position:absolute;top:0;bottom:0;width:1px;margin-left:-.5px;background:rgba(255,255,255,.6);pointer-events:none}',
        '.vs-mark-until{background:rgba(255,255,255,.3)}',
        '.vs-loss{position:absolute;top:0;bottom:0;pointer-events:none;background:repeating-linear-gradient(135deg,rgba(0,0,0,.55) 0 2px,rgba(0,0,0,.25) 2px 4px)}',
        '.vs-st{margin-bottom:6px;padding-bottom:5px;border-bottom:1px solid rgba(255,255,255,.15)}',
        '.vs-st:last-child{margin-bottom:0;padding-bottom:0;border-bottom:none}',
        '.status-tooltip[data-vs-no-game-text] > .st-body:not(.vs-st){display:none}',
        '.vs-row{font-size:11px;line-height:1.5;color:#ccc}',
        '.vs-good{color:#a5d6a7}',
        '.vs-bad{color:#ff8a80}',
        '.vs-warn{color:#ffc107}',
        // The stat hover boxes as dark as the buff ones, so the HUD behind them does not show through.
        '.status-panel .status-tooltip{background-color:rgba(14,16,19,.93);border-color:rgba(255,255,255,.14)}',
        '.status-panel .status-tooltip::after{border-left-color:rgba(14,16,19,.93)}',
        // The Fitness row is built like the game's own bars, so the game's styles draw it. It is the panel's last child,
        // so the game's last-row rules (hover boxes that open upward, to stay on screen) are given to it and to the row
        // above it, which they no longer reach.
        '.' + FIT_ROW + '{display:flex;align-items:center;justify-content:flex-end}',
        '.' + FIT_ROW + ' .status-tooltip{top:auto;bottom:0;transform:none}',
        '.' + FIT_ROW + ' .status-tooltip::after{top:auto;bottom:6px;transform:none}',
        '.' + FIT_ROW + ' .progress-bar-wrapper:hover .status-tooltip{transform:translateX(-5px)}',
        '.status-panel[' + FIT_MARK + '] > .status-row:nth-last-child(2) .buff .buff-tooltip{top:auto;bottom:-4px;transform:none}',
        '.status-panel[' + FIT_MARK + '] > .status-row:nth-last-child(2) .buff .buff-tooltip::after{top:auto;bottom:8px;transform:none}',
        '.status-panel[' + FIT_MARK + '] > .status-row:nth-last-child(2) .progress-bar-wrapper .status-tooltip{top:auto;bottom:0;transform:none}',
        '.status-panel[' + FIT_MARK + '] > .status-row:nth-last-child(2) .progress-bar-wrapper .status-tooltip::after{top:auto;bottom:6px;transform:none}',
        '.status-panel[' + FIT_MARK + '] > .status-row:nth-last-child(2) .progress-bar-wrapper:hover .status-tooltip{transform:translateX(-5px)}',
        '.vs-head{color:#eee;margin-top:4px}',
        '.vs-timer{position:absolute;left:0;right:0;bottom:0;height:2px;background:rgba(255,255,255,.75);transform-origin:left;transition:transform ' + STEP_SECONDS + 's linear;pointer-events:none}',
        '.vs-timer.vs-timer-low{background:#ffc107}',
        '.bf-leave-active .vs-timer{transition:none}',
        // Badges near the bar's own label size, so at the same BuffSize and BarSize they sit in proportion to the bar.
        // Their zoom (BuffSize) is set apart from the bar's.
        '.status-panel .buff-container{gap:3.5px;margin-right:5.6px}',
        '.status-panel .buff{padding:3px 6px;font-size:10.5px;line-height:13px}',
        // A buff tooltip with our block: the name and time on one line, then the effects, the cures and the game text
        // last in smaller type. The children are placed by order, because Vue adds the header and text after our block.
        '.buff-tooltip[data-vs-tip]{display:flex;flex-wrap:wrap;align-items:baseline;width:196px;padding:6px 8px 7px;background-color:rgba(14,16,19,.93);border-color:rgba(255,255,255,.14)}',
        '.buff-tooltip[data-vs-tip]::after{border-left-color:rgba(14,16,19,.93)}',
        '[data-vs-tip] > .bt-header{order:0;flex:1 1 0;min-width:0;margin:0;padding:0;border:0}',
        '[data-vs-tip] .bt-title{font-size:12px;line-height:15px;letter-spacing:.2px}',
        '[data-vs-tip][data-vs-timed] .bt-timer{display:none}',
        '.vs-btime{order:1;flex:0 0 auto;margin-left:8px;font-family:Consolas,monospace;font-size:10.5px;font-weight:bold;color:#eee}',
        '[data-vs-tip] > .vs-bt{order:2;flex:0 0 100%;margin-top:4px;padding-top:3px;border-top:1px solid rgba(255,255,255,.14)}',
        '[data-vs-tip] > .vs-bt.vs-bt-timed{margin-top:0;padding-top:0;border-top:0}',
        '[data-vs-tip] > .bt-body{order:3;flex:0 0 100%;margin-top:5px;padding-top:4px;border-top:1px dashed rgba(255,255,255,.16);font-size:10px;line-height:1.4;color:#a3abb4}',
        '.vs-bt .vs-row{font-size:10.5px;line-height:1.45}',
        '.vs-kv{display:flex;justify-content:space-between;align-items:baseline;gap:8px}',
        '.vs-kv .vs-k{min-width:0;color:#c4c9ce}',
        '.vs-kv .vs-v{flex-shrink:0;font-family:Consolas,monospace;font-weight:bold;color:#eee}',
        '.vs-kv.vs-good .vs-v{color:#a5d6a7}',
        '.vs-kv.vs-bad .vs-v{color:#ff8a80}',
        '.vs-bt .vs-head{margin-top:5px;font-size:9.5px;line-height:1.4;letter-spacing:.4px;text-transform:uppercase;color:#8f98a1}',
        '.vs-tbar{height:2px;margin:4px 0;background:rgba(255,255,255,.15)}',
        '.vs-tfill{height:100%;background:rgba(255,255,255,.75)}',
        '.vs-tbar.vs-timer-low .vs-tfill{background:#ffc107}',
        '.vs-item{display:flex;align-items:center;gap:4px;font-size:10.5px;line-height:1.5;color:#ddd}',
        '.vs-item img{width:13px;height:13px;object-fit:contain;flex-shrink:0}',
        '.vs-item .vs-count{margin-left:auto;font-family:Consolas,monospace;font-size:10px;color:#eee}',
        '.vs-item.vs-none{opacity:.45}',
        '.vs-weight{position:absolute;left:0;right:0;bottom:4px;font-size:9px;line-height:10px;text-align:center;color:#cfd3d5;pointer-events:none;font-variant-numeric:tabular-nums}',
        '.vs-weight.vs-near{color:#ffc107}',
        '.vs-weight.vs-over{color:#ff8a80}',
        '.vs-wbar{position:absolute;left:4px;right:4px;bottom:1px;height:2px;background:rgba(255,255,255,.18);pointer-events:none}',
        '.vs-wfill{position:absolute;left:0;top:0;bottom:0;background:#81c784}',
        '.vs-wbar.vs-near .vs-wfill{background:#ffc107}',
        '.vs-wbar.vs-over .vs-wfill{background:#ff8a80}',
        '.vs-wgap{position:absolute;top:-1px;bottom:-1px;width:2px;margin-left:-1px;background:#111}',
        '.vs-weight-tip{margin-top:4px;white-space:pre-line}'
    ].join('\n');

    var held;
    var isHeld = false;
    var clock = 0;
    // Each running timer's full length, keyed by buff id and its place among buffs of that id: the longest time left
    // seen since its end last moved later, or the buff's own length when that is longer.
    var lengths = {};
    // The badge under the mouse, and the tooltip our block was last drawn in.
    var hovered = null;
    var drawnTip = null;

    function apply(now, data) {
        clock = now;

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

        drawZoom(held.zoom, held.buffZoom, held.tipZoom);
        drawWeight(held);

        var rows = document.querySelectorAll('.status-panel > .status-row');

        if (!rows.length) {
            dropFitness();

            return 'nobar';
        }

        var list = attrList();
        var isListed = !!list && list.length === rows.length;
        var names = statNames(isListed ? list : null, rows.length);

        for (var r = 0; r < rows.length; r++) {
            var stat = statOf(isListed ? list[r] : null, r);

            drawStat(rows[r], stat, isListed ? list[r] : null, names);
        }

        drawTimers(rows, isListed ? list : null, held.buffs, now);
        drawFitness(rows[rows.length - 1].parentElement);

        if (hovered) drawBuffTip(hovered, names);

        return 'ok';
    }

    function say() {
        return held && held.zh ? TEXTS.zh : TEXTS.en;
    }

    function option(index) {
        return !held || !held.opt || held.opt[index] !== 0;
    }

    function isWeightOn() { return option(0); }
    function isStripeOn() { return option(1); }
    function isTooltipOn() { return option(2); }
    function isGameTextOn() { return option(3); }
    function isCureOn() { return option(4); }

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

    // The game's own name for each stat, in the plugin's order, from its rows; ours where a row cannot be read.
    function statNames(list, count) {
        var names = say().stats.slice();

        for (var r = 0; list && r < count; r++) {
            var stat = statOf(list[r], r);
            var name = list[r].attrName || list[r].name;

            if (stat >= 0 && name) names[stat] = String(name);
        }

        return names;
    }

    function drawStat(row, stat, item, names) {
        var bar = row.querySelector('.progress-bar-wrapper');
        var mask = bar ? bar.querySelector('.bar-mask') : null;
        var tip = bar ? bar.querySelector('.status-tooltip') : null;
        var ratios = stat >= 0 && held.stats ? held.stats[stat] : null;
        var wellFed = stat === SATIETY ? held.wellFed : null;

        if (!mask) return;

        drawChip(mask, ratios ? ratios[0] : 0);
        drawMarks(mask, wellFed);
        drawLoss(mask, stat, item);

        if (stat === LIFE) drawHeart(mask);

        if (tip) drawStatTip(tip, stat, item, ratios, wellFed, names);
    }

    // The game shows a shield on the Life row; the heart is the stat's own mark.
    function drawHeart(mask) {
        var name = mask.querySelector('.attr-name');
        var icon = name && name.children.length ? name.children[0] : null;

        if (icon) setText(icon, HEART);
    }

    function drawChip(mask, gain) {
        var text = scaleText(gain);
        var line = mask.querySelector('.attr-name') || mask;
        var chip = ownChild(line, 'vs-chip');

        if (!text) {
            if (chip) line.removeChild(chip);

            return;
        }

        if (!chip) chip = line.appendChild(element('span', 'vs-chip'));

        setClass(chip, 'vs-chip-up', gain > 0);
        setClass(chip, 'vs-chip-down', gain < 0);
        setText(chip, 'x' + text);
    }

    // The game multiplies a change by 1 + ratio. Empty when that rounds to 1, so an unscaled stat shows nothing.
    function scaleText(ratio) {
        var scale = Math.round((1 + ratio) * 100) / 100;

        return scale === 1 ? '' : String(scale);
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

            setStyle(mark, 'left', percent(marks[i][1] / wellFed[2]));
        }
    }

    // Hatching over the end of the filled bar: what the running timed buffs will still take, stopping at zero.
    function drawLoss(mask, stat, item) {
        var shade = ownChild(mask, 'vs-loss');
        var lost = stat >= 0 ? comingTotal(stat) : 0;
        var max = held.max && stat >= 0 ? held.max[stat] : 0;
        var value = item ? Number(item.value) : NaN;
        var part = max > 0 && lost > 0 && value > 0 ? Math.min(value, lost / max) : 0;

        if (!(part > 0.0005)) {
            if (shade) mask.removeChild(shade);

            return;
        }

        if (!shade) {
            shade = element('div', 'vs-loss');
            mask.insertBefore(shade, ownChild(mask, 'progress-text'));
        }

        setStyle(shade, 'left', percent(value - part));
        setStyle(shade, 'width', percent(part));
    }

    function comingTotal(stat) {
        var lines = held.coming ? held.coming[stat] : null;
        var total = 0;

        for (var i = 0; lines && i < lines.length; i++) total += lines[i][1];

        return total;
    }

    // Our block at the top of the stat tooltip, above the game's description (hidden when the player turned it off).
    function drawStatTip(tip, stat, item, ratios, wellFed, names) {
        var isOn = isTooltipOn() && stat >= 0;
        var rows = isOn ? statRows(stat, item, ratios, wellFed) : [];

        setMark(tip, 'data-vs-no-game-text', isOn && !isGameTextOn());
        drawBlock(tip, 'vs-st', 'div', 'st-body vs-st', rows, ownChild(tip, 'st-body'));
    }

    function statRows(stat, item, ratios, wellFed) {
        var text = say();
        var rows = [];
        var max = held.max ? held.max[stat] : 0;
        var value = item ? Number(item.value) : NaN;
        var i;

        if (max > 0 && value >= 0) rows.push([text.value(number(value * max), number(max))]);

        if (ratios) {
            addPercent(rows, text.gain, ratios[0], false);
            addPercent(rows, text.loss, ratios[1], true);
            addPercent(rows, text.naturalGain, ratios[2], false);
            addPercent(rows, text.naturalLoss, ratios[3], true);
        }

        var foods = held.food || [];

        for (i = 0; i < foods.length; i++) {
            if (foods[i][0] !== (stat === SATIETY ? 0 : stat === MORALE ? 1 : -1)) continue;

            var change = percentChange(foods[i][2]);

            if (change) rows.push([text.food(foodName(foods[i][1]), change), foods[i][2] > 0 ? 'vs-good' : 'vs-bad']);
        }

        var coming = held.coming ? held.coming[stat] || [] : [];

        for (i = 0; i < coming.length; i++) {
            var info = held.info ? held.info[coming[i][0]] : null;

            rows.push([text.coming(info ? info[0] : '#' + coming[i][0], '-' + number(coming[i][1]), duration(coming[i][2] - clock)), 'vs-bad']);
        }

        if (wellFed) rows.push([text.wellFed(held.names.wellFed, wellFed[0], wellFed[1])]);

        return rows;
    }

    // A ratio as the percentage it adds or takes; nothing when it rounds to no change. A loss growing is bad news.
    function addPercent(rows, label, ratio, isLoss) {
        var change = percentChange(ratio);

        if (change) rows.push([label + change, (ratio > 0) !== isLoss ? 'vs-good' : 'vs-bad']);
    }

    function percentChange(ratio) {
        var whole = Math.round(ratio * 100);

        return whole === 0 ? '' : signed(whole) + '%';
    }

    function foodName(type) {
        return held.foods && held.foods[type - 1] ? held.foods[type - 1] : '#' + type;
    }

    // ===== Fitness =====

    // The Fitness row sits in the stat panel after the rows Vue draws. Vue puts a new row before it, and the row is
    // moved back to the end if anything ever lands after it.
    function drawFitness(panel) {
        var reading = held.fit;

        if (!(reading && reading[1] > 0 && reading[0] >= 0)) {
            dropFitness();

            return;
        }

        var row = ownChild(panel, FIT_ROW);
        var isNew = !row;

        if (isNew) row = panel.appendChild(fitnessRow());
        else if (panel.children[panel.children.length - 1] !== row) panel.appendChild(row);

        setMark(panel, FIT_MARK, true);
        drawFitnessBar(ownChild(row, 'progress-bar-wrapper'), reading[0], reading[1]);

        if (isNew) syncClicks();
    }

    // The game's bar, as its template draws one: the mask with the fill, the name and the value, then the hover box.
    function fitnessRow() {
        var row = element('div', FIT_ROW);
        var bar = row.appendChild(element('div', 'progress-bar-wrapper'));
        var mask = bar.appendChild(element('div', 'bar-mask'));
        var text = element('div', 'progress-text');
        var name = element('span', 'attr-name');
        var tip = element('div', 'status-tooltip last-row-fix');
        var header = tip.appendChild(element('div', 'st-header'));

        // The page hands the game only the areas marked this way, so the pointer over the bar is not taken as a click
        // on the world behind it.
        bar.setAttribute('data-interactive', '');
        mask.appendChild(element('div', 'progress-fill'));
        mask.appendChild(text);
        text.appendChild(name);
        name.appendChild(element('span', '')).textContent = FIT_ICON;
        name.appendChild(element('span', 'vs-fit-name'));
        text.appendChild(element('span', 'vs-fit-value'));
        header.appendChild(element('div', 'st-title'));
        header.appendChild(element('div', 'st-value'));
        tip.appendChild(element('div', 'st-body vs-fit-about'));
        bar.appendChild(tip);

        return row;
    }

    function drawFitnessBar(bar, value, max) {
        var text = say();
        var level = fitnessLevel(value);
        var fill = bar.querySelector('.progress-fill');
        var tip = ownChild(bar, 'status-tooltip');
        var about = ownChild(tip, 'vs-fit-about');
        var state = level === 'weak' ? [text.weakened, 'vs-bad']
            : level === 'low' ? [text.nearWeakened(value - WEAKENED_BELOW + 1), 'vs-warn']
            : [text.aboveWeakened(value - WEAKENED_BELOW)];

        // The game's own colors, and the pulsing red it gives a bar that is nearly empty for Weakened.
        setClass(bar, 'is-low', level === 'weak');
        setClass(fill, 'bar-green', level === 'ok');
        setClass(fill, 'bar-yellow', level === 'low');
        setStyle(fill, 'transform', 'scaleX(' + Math.max(0, Math.min(1, value / max)).toFixed(4) + ')');
        setText(bar.querySelector('.vs-fit-name'), text.fitness);
        setText(bar.querySelector('.vs-fit-value'), String(value));
        setText(tip.querySelector('.st-title'), text.fitness);
        setText(tip.querySelector('.st-value'), value + '/' + max);
        setText(about, text.fitAbout);
        setMark(tip, 'data-vs-no-game-text', isTooltipOn() && !isGameTextOn());
        drawBlock(tip, 'vs-st', 'div', 'st-body vs-st', isTooltipOn() ? [state] : [], about);
    }

    function fitnessLevel(value) {
        if (value < WEAKENED_BELOW) return 'weak';
        if (value < LOW_FITNESS_BELOW) return 'low';

        return 'ok';
    }

    function dropFitness() {
        var rows = document.querySelectorAll('.' + FIT_ROW);
        var panels = document.querySelectorAll('.status-panel');
        var i;

        for (i = 0; i < rows.length; i++) {
            if (rows[i].parentElement) rows[i].parentElement.removeChild(rows[i]);
        }

        for (i = 0; i < panels.length; i++) setMark(panels[i], FIT_MARK, false);

        if (rows.length) syncClicks();
    }

    // The page's click-through measures the marked areas again after a change of layout; without this the Fitness
    // bar's area is learned only at the page's next own redraw.
    function syncClicks() {
        var core = pageGlobal('core');

        if (core && core.clickThrough && typeof core.clickThrough.syncNowAfterReflow === 'function') core.clickThrough.syncNowAfterReflow();
    }

    function pageGlobal(name) {
        try {
            return (0, eval)(name);
        } catch (e) {
            return undefined;
        }
    }

    // ===== Buff timers =====

    // Badges follow their row's attrList buffs in order, except a badge on its way out, which the game keeps in place
    // for its leaving animation. A row whose badges and list do not line up (a change in flight) waits for the next call.
    // Each badge keeps the buff it shows (its id and running copy) for its tooltip.
    function drawTimers(rows, list, buffs, now) {
        var running = {};
        var seen = {};
        var i;

        for (i = 0; i < buffs.length; i++) (running[buffs[i][0]] = running[buffs[i][0]] || []).push(buffs[i]);

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
                var copy = running[id] ? running[id][place - 1] : null;
                var key = id + '#' + place;
                var isTimed = !!copy && copy[1] >= 0;

                badges[i].__vs = { id: id, copy: copy, part: isTimed ? share(key, copy, now) : -1 };

                if (isTimed) seen[key] = true;

                drawTimer(badges[i], isTimed && isStripeOn() ? badges[i].__vs.part : -1);
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
        }

        setStyle(stripe, 'transform', transform);
        setClass(stripe, 'vs-timer-low', part < LOW_TIME);
    }

    // ===== Buff tooltips =====

    // One capturing listener for the whole page: the badge under the mouse gets our block in its tooltip.
    function onOver(event) {
        var badge = badgeOf(event.target);

        if (!badge || badge === hovered) return;

        hovered = badge;

        if (held) drawBuffTip(badge, statNames(attrList(), ICONS.length));
    }

    function badgeOf(node) {
        for (var depth = 0; node && depth < BADGE_DEPTH; depth++, node = node.parentElement) {
            if (node.classList && node.classList.contains('buff')) return node;
        }

        return null;
    }

    function drawBuffTip(badge, names) {
        var tip = ownChild(badge, 'buff-tooltip');

        if (drawnTip && drawnTip !== tip) undrawBuffTip(drawnTip);

        drawnTip = tip;

        if (!tip) return;

        var shown = badge.__vs;
        var info = shown && held.info && isTooltipOn() ? held.info[shown.id] : null;

        if (!info) {
            undrawBuffTip(tip);

            return;
        }

        var isTimed = !!shown.copy && shown.copy[1] >= 0;
        var rows = buffRows(info, shown, names);

        drawBlock(tip, 'vs-bt', 'div', 'vs-bt', rows, null);
        setOwnText(tip, 'vs-btime', 'span', 'vs-btime', isTimed ? duration(shown.copy[1] - clock) : '');
        setMark(tip, 'data-vs-tip', true);
        setMark(tip, 'data-vs-timed', isTimed);

        var block = ownChild(tip, 'vs-bt');

        if (block) setClass(block, 'vs-bt-timed', isTimed);
    }

    function undrawBuffTip(tip) {
        drawBlock(tip, 'vs-bt', 'div', 'vs-bt', [], null);
        setOwnText(tip, 'vs-btime', 'span', 'vs-btime', '');
        setMark(tip, 'data-vs-tip', false);
        setMark(tip, 'data-vs-timed', false);
    }

    // info: [name, is good, interval hours, effect lines, cure items, suppressor items, is suppressible]
    // copy: [id, end, length, layers, is suppressed]
    // The time left goes on the header line; its bar opens the block, under the name.
    function buffRows(info, shown, names) {
        var text = say();
        var copy = shown.copy;
        var layers = copy ? Math.max(1, copy[3]) : 1;
        var rows = [];

        if (copy && copy[1] >= 0) rows.push({ bar: shown.part });

        if (copy && copy[4]) rows.push([text.suppressed(held.by || text.suppressor), 'vs-good']);
        else addEffects(rows, info, layers, names);

        if (layers > 1) rows.push({ k: text.layers, v: String(layers) });

        if (!info[1] && isCureOn()) {
            addItems(rows, text.cureWith, info[4]);
            addItems(rows, text.suppressWith, info[5]);
        }

        return rows;
    }

    function addEffects(rows, info, layers, names) {
        var text = say();
        var lines = info[3] || [];

        for (var i = 0; i < lines.length; i++) {
            var kind = lines[i][0];
            var at = lines[i][1];
            var value = lines[i][2] * layers;
            var stat = names[at] || '';
            var amount = signed(number(value));
            var label = null;

            if (kind === 'e') label = text.every(stat, number(info[2]));
            else if (kind === 's') label = text.start(stat);
            else if (kind === 'n') label = text.end(stat);
            else if (kind === 'm') label = text.max(stat);
            else if (kind === 'g') label = text.gainFx(stat);
            else if (kind === 'l') label = text.lossFx(stat);
            else if (kind === 'v') label = text.speed;
            else if (kind === 'f') label = text.satietyFood(foodName(at));
            else if (kind === 'o') label = text.moraleFood(foodName(at));

            if ('glvfo'.indexOf(kind) >= 0) amount = percentChange(value);

            // A bigger loss ratio is bad; for everything else up is good.
            if (label) rows.push({ k: label, v: amount, c: (value > 0) !== (kind === 'l') ? 'vs-good' : 'vs-bad' });
        }
    }

    // Items the player has come first; the rest are dimmed. At most MOST_ITEMS.
    function addItems(rows, title, ids) {
        var owned = [];
        var none = [];

        for (var i = 0; ids && i < ids.length; i++) {
            var item = held.items ? held.items[ids[i]] : null;

            if (item) (item[2] > 0 ? owned : none).push(item);
        }

        var items = owned.concat(none).slice(0, MOST_ITEMS);

        if (!items.length) return;

        rows.push([title, 'vs-head']);

        for (var n = 0; n < items.length; n++) rows.push({ item: items[n] });
    }

    // ===== Blocks of rows =====

    // Our block in a tooltip, before the given node (or last). Rows are [text, class], { k: label, v: value, c: class },
    // { bar } or { item }; the block
    // is rebuilt only when what it shows changed, and removed when there is nothing to show.
    function drawBlock(parent, marker, tagName, className, rows, before) {
        var box = ownChild(parent, marker);

        if (!rows.length) {
            if (box) parent.removeChild(box);

            return;
        }

        var key = JSON.stringify(rows);

        if (box && box.__vsKey === key) return;

        if (!box) {
            box = element(tagName, className);

            if (before) parent.insertBefore(box, before);
            else parent.appendChild(box);
        }

        while (box.children.length) box.removeChild(box.children[0]);

        for (var i = 0; i < rows.length; i++) box.appendChild(rowNode(rows[i]));

        box.__vsKey = key;
    }

    function rowNode(row) {
        if (row.bar !== undefined) {
            var bar = element('div', 'vs-tbar' + (row.bar < LOW_TIME ? ' vs-timer-low' : ''));
            var fill = bar.appendChild(element('div', 'vs-tfill'));

            fill.style.width = percent(Math.max(0, row.bar));

            return bar;
        }

        if (row.k !== undefined) {
            var pair = element('div', 'vs-row vs-kv' + (row.c ? ' ' + row.c : ''));

            pair.appendChild(element('span', 'vs-k')).textContent = row.k;
            pair.appendChild(element('span', 'vs-v')).textContent = row.v;

            return pair;
        }

        if (row.item) {
            var node = element('div', 'vs-item' + (row.item[2] > 0 ? '' : ' vs-none'));

            if (row.item[1]) {
                var icon = node.appendChild(element('img', ''));

                icon.setAttribute('src', row.item[1]);
            }

            node.appendChild(element('span', 'vs-name')).textContent = row.item[0];
            node.appendChild(element('span', 'vs-count')).textContent = 'x' + row.item[2];

            return node;
        }

        var line = element('div', 'vs-row' + (row[1] ? ' ' + row[1] : ''));

        line.textContent = row[0];

        return line;
    }

    // ===== Carried weight =====

    function drawWeight(data) {
        var button = bagButton();

        if (!button) return;

        var weight = isWeightOn() ? data.weight : null;
        var tip = button.querySelector('.entry-tip');
        var label = ownChild(button, 'vs-weight');
        var bar = ownChild(button, 'vs-wbar');

        if (!weight) {
            if (label) button.removeChild(label);
            if (bar) button.removeChild(bar);
            if (tip) setOwnText(tip, 'vs-weight-tip', 'div', 'entry-tip-body vs-weight-tip', '');

            return;
        }

        var text = say();
        var name = data.names.overweight;
        var carried = weight[0];
        var max = weight[1];
        var line = weight[2];
        var isOver = line > 0 && carried > line;
        var isNear = !isOver && line > 0 && carried >= line * NEAR_OVERWEIGHT;
        var lines = [text.carrying(kgUp(carried), kgDown(max))];

        if (line > 0) {
            lines.push(text.line(name, kgDown(line)));
            lines.push(isOver ? text.over(name, kgUp(carried - line)) : text.left(name, kgDown(line - carried)));
        }

        if (!label) label = button.appendChild(element('span', 'vs-weight'));

        setClass(label, 'vs-over', isOver);
        setClass(label, 'vs-near', isNear);
        setText(label, kgUp(carried));

        if (!bar) {
            bar = button.appendChild(element('div', 'vs-wbar'));
            bar.appendChild(element('div', 'vs-wfill'));
            bar.appendChild(element('div', 'vs-wgap'));
        }

        setClass(bar, 'vs-over', isOver);
        setClass(bar, 'vs-near', isNear);
        setStyle(ownChild(bar, 'vs-wfill'), 'width', percent(max > 0 ? Math.min(1, carried / max) : 0));

        var gap = ownChild(bar, 'vs-wgap');
        var isGapShown = line > 0 && line < max;

        setStyle(gap, 'display', isGapShown ? '' : 'none');

        if (isGapShown) setStyle(gap, 'left', percent(line / max));

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

    // ===== Numbers =====

    // One place, without a trailing .0.
    function number(value) {
        return String(Math.round(value * 10) / 10);
    }

    function signed(value) {
        return (Number(value) > 0 ? '+' : '') + value;
    }

    function percent(part) {
        return (part * 100).toFixed(2) + '%';
    }

    // Game seconds as days, hours and minutes, the two largest parts.
    function duration(seconds) {
        var text = say();
        var minutes = Math.max(0, Math.ceil(seconds / 60));
        var days = Math.floor(minutes / 1440);
        var hours = Math.floor(minutes % 1440 / 60);
        var rest = minutes % 60;

        if (days > 0) return days + text.days + (hours ? ' ' + hours + text.hours : '');
        if (hours > 0) return hours + text.hours + (rest ? ' ' + rest + text.minutes : '');

        return rest + text.minutes;
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
        dropFitness();

        for (var i = 0; i < DRAWN.length; i++) {
            var nodes = document.querySelectorAll('.' + DRAWN[i]);

            for (var n = 0; n < nodes.length; n++) {
                if (nodes[n].parentElement) nodes[n].parentElement.removeChild(nodes[n]);
            }
        }

        for (var t = 0; t < MARKED_TIPS.length; t++) {
            var tips = document.querySelectorAll('.' + MARKED_TIPS[t]);

            for (var h = 0; h < tips.length; h++) {
                for (var m = 0; m < MARKS.length; m++) setMark(tips[h], MARKS[m], false);
            }
        }

        var zoom = document.getElementById(ZOOM_ID);

        if (zoom && zoom.parentElement) zoom.parentElement.removeChild(zoom);

        lengths = {};
        drawnTip = null;
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

    function setStyle(target, name, value) {
        if (target && target.style[name] !== value) target.style[name] = value;
    }

    function setClass(target, className, isOn) {
        if (target.classList.contains(className) === isOn) return;

        if (isOn) target.classList.add(className);
        else target.classList.remove(className);
    }

    function setMark(target, name, isOn) {
        if (target.hasAttribute(name) === isOn) return;

        if (isOn) target.setAttribute(name, '');
        else target.removeAttribute(name);
    }

    function element(tagName, className) {
        var made = document.createElement(tagName);

        if (className) made.className = className;

        return made;
    }

    // The bars drawn at BarSize, the buffs beside them at BuffSize and the bars' hover boxes at BarTooltipSize: rules in
    // their own style node, rewritten only when a size changes. A size of 1 needs no rule. A hover box sits inside its
    // zoomed bar and zoom multiplies, so the box's own zoom is its size over the bar's.
    function drawZoom(barZoom, buffZoom, tipZoom) {
        var style = document.getElementById(ZOOM_ID);

        if (!style) {
            style = document.createElement('style');
            style.id = ZOOM_ID;
            document.head.appendChild(style);
        }

        var barSize = sizeOf(barZoom);
        var tipSize = Math.round(sizeOf(tipZoom) / barSize * 10000) / 10000;

        setText(style, zoomRule(BAR_BOXES, barSize) + zoomRule(BUFF_BOXES, sizeOf(buffZoom)) + zoomRule(BAR_TIPS, tipSize));
    }

    function sizeOf(zoom) {
        return zoom > 0 ? Number(zoom) : 1;
    }

    function zoomRule(selector, size) {
        return size === 1 ? '' : selector + '{zoom:' + size + '}';
    }

    function addStyle() {
        if (document.getElementById(STYLE_ID)) return;

        var style = document.createElement('style');

        style.id = STYLE_ID;
        style.textContent = CSS;
        document.head.appendChild(style);
    }

    document.addEventListener('mouseover', onOver, true);

    window.__slvitals = { apply: apply, texts: TEXTS };
})();
