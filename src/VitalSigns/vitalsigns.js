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
    // The game's line: below 30 Fitness the player is Weakened. A warning comes before it.
    var WEAKENED_BELOW = 30;
    var LOW_FITNESS_BELOW = 40;
    var FIT_ROW = 'vs-fit-row';
    var FIT_ICON = '\u271A';
    // Put on the stat panel while the Fitness row is in it, so the row above keeps the hover boxes that open upward.
    var FIT_MARK = 'data-vs-fit';
    // What BarSize, BuffSize and BarTooltipSize zoom.
    var BAR_BOXES = '.status-panel > .status-row > .progress-bar-wrapper,.status-panel > .' + FIT_ROW + ' > .progress-bar-wrapper,' +
        '.status-panel > .status-row > .vs-chip';
    var BUFF_BOXES = '.status-panel > .status-row > .buff-container';
    var BAR_TIPS = '.status-panel .progress-bar-wrapper > .status-tooltip,.status-panel .vs-chip > .vs-ct';
    var DRAWN = ['vs-trend', 'vs-chip', 'vs-mark', 'vs-loss', 'vs-st', 'vs-timer', 'vs-bt', 'vs-btime', 'vs-bd', 'vs-weight', 'vs-wbar', 'vs-weight-tip', FIT_ROW, 'vs-proj', 'vs-ftime'];
    // Attributes put on the game's tooltips, taken off again by clear. Attributes, not classes: Vue writes the whole
    // class of a tooltip each time it shows or hides it, which wiped a class of ours and showed the game's look until
    // the next call drew it again. Vue leaves attributes it does not own alone.
    var MARKS = ['data-vs-no-game-text', 'data-vs-tip', 'data-vs-timed'];
    // The HUD style colours are taken only from a Toolkit look that is not the classic one.
    var MATCH = 'html[data-vs-match][data-etk-look]:not([data-etk-look="classic"])';
    var LOOK_MARKS = ['data-vs-match', 'data-vs-cb', 'data-vs-bars'];
    var BAR_LOOKS = [null, 'flat', 'segmented'];
    var ENDING = 'data-vs-ending';
    // Trend arrows: the change over this much game time, once at least the shorter span has been seen; a reading kept
    // every minute of game time; a change smaller than this part of the bar is no change.
    var TREND_SECONDS = 3600;
    var TREND_MIN_SECONDS = 600;
    var TREND_STEP = 60;
    var TREND_EPSILON = 0.005;
    var MARKED_TIPS = ['buff-tooltip', 'status-tooltip'];
    // How close a hover box may come to the top or bottom of the window, in the window's pixels.
    var EDGE = 6;
    // The arrow of a moved hover box stays this far inside the box's top and bottom, in the box's own pixels.
    var ARROW_ROOM = 12;

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
            wellFed: function (name, from, until) { return 'You get ' + name + ' at ' + from + ' or more and lose it below ' + until + '.'; },
            every: function (stat, hours) { return stat + (hours === 1 ? ' each hour' : ' every ' + hours + 'h'); },
            start: function (stat) { return stat + ' when it starts'; },
            end: function (stat) { return stat + ' when it ends'; },
            max: function (stat) { return 'Max ' + stat; },
            gainFx: function (stat) { return stat + ' gain'; },
            other: 'Other',
            lossFx: function (stat) { return stat + ' loss'; },
            speed: 'Move speed',
            satietyFood: function (food) { return food + ' Satiety'; },
            moraleFood: function (food) { return food + ' Morale'; },
            layers: 'Layers',
            suppressed: function (by) { return 'Suppressed by ' + by; },
            suppressor: 'Analgesia',
            cureWith: 'Cure with',
            suppressWith: 'Suppress with',
            days: 'd',
            hours: 'h',
            minutes: 'm',
            carrying: 'Carrying',
            load: function (carried, max) { return carried + ' / ' + max + ' kg'; },
            room: function (name) { return 'Room before ' + name; },
            over: function (over) { return over + ' kg over'; },
            fitness: 'Fitness',
            fitAbout: 'High Fitness lowers your chance of getting sick. Below 30 you become Weakened. Staying Hungry or Fatigued for too long lowers it. Food that gives Fitness raises it.',
            weakened: 'Below 30, so you are Weakened.',
            nearWeakened: function (left) { return 'Close to Weakened: lose ' + left + ' more and it drops below 30.'; },
            aboveWeakened: function (above) { return above + ' above 30, where Weakened starts.'; },
            heaviest: 'Heaviest items',
            heavy: function (name, count) { return name + (count > 1 ? ' x' + count : ''); },
            kg: function (kg) { return kg + ' kg'; },
            otherFitnessBar: 'An old Fitness Bar mod is also installed, so you see two Fitness bars. Remove FitnessBar.dll from BepInEx/plugins, or update it to 0.1.2 or newer.',
            afterQueue: function (value, max) { return 'After the queue ' + value + ' / ' + max; },
            click: function (what) { return 'Click: ' + what; },
            clickCure: 'Click to queue a cure',
            pin: 'Shift+click to pin',
            unpin: 'Shift+click to unpin',
            editTitle: 'Move the bars',
            editHelp: 'Drag a bar to move it, scroll on it to resize it. Hold Shift to move or resize all of them together.',
            tray: 'Buffs: ',
            trays: ['Left', 'Right', 'Above', 'Below'],
            collapse: 'Collapse buffs',
            reset: 'Reset',
            cancel: 'Cancel',
            save: 'Save'
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
            other: '其他',
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
            carrying: '负重',
            load: function (carried, max) { return carried + ' / ' + max + ' 千克'; },
            room: function (name) { return '距离「' + name + '」还有'; },
            over: function (over) { return '超出 ' + over + ' 千克'; },
            fitness: '健康',
            fitAbout: '良好的健康可以降低生病的概率。低于30会陷入「亚健康」。长期「饥饿」或「疲劳」会降低健康。食用带有健康加成的食物可以提高健康。',
            weakened: '低于30，你处于「亚健康」状态。',
            nearWeakened: function (left) { return '接近「亚健康」：再降低 ' + left + ' 点就会低于30。'; },
            aboveWeakened: function (above) { return '比「亚健康」的界线30高 ' + above + ' 点。'; },
            heaviest: '最重的物品',
            heavy: function (name, count) { return name + (count > 1 ? ' x' + count : ''); },
            kg: function (kg) { return kg + ' 千克'; },
            otherFitnessBar: '同时安装了旧版 Fitness Bar 模组，所以会显示两个健康条。请从 BepInEx/plugins 中删除 FitnessBar.dll，或将其更新到 0.1.2 或更高版本。',
            afterQueue: function (value, max) { return '队列完成后 ' + value + ' / ' + max; },
            click: function (what) { return '点击：' + what; },
            clickCure: '点击加入治疗',
            pin: 'Shift+点击置顶',
            unpin: 'Shift+点击取消置顶',
            editTitle: '调整属性条',
            editHelp: '拖动属性条可移动，在上面滚动滚轮可缩放。按住 Shift 可一起移动或缩放全部属性条。',
            tray: '状态位置：',
            trays: ['左侧', '右侧', '上方', '下方'],
            collapse: '收起状态',
            reset: '重置',
            cancel: '取消',
            save: '保存'
        }
    };

    // Under the game's look: the chip, lines and shading sit inside its bar, the tooltip blocks use its text styles.
    var CSS = [
        // Our colours, in one place. MatchHudStyle takes them from the HUD style picked in Exiled's Toolkit (its
        // data-etk-look and --etk-* variables, each with our colour as the fallback); the classic style keeps ours.
        ':root{--vs-panel:rgba(14,16,19,.93);--vs-edge:rgba(255,255,255,.14);--vs-text:#eee;--vs-muted:#ccc;--vs-key:#c4c9ce;--vs-faint:#b4bbc2;--vs-head:#8f98a1;' +
            '--vs-good:#a5d6a7;--vs-bad:#ff8a80;--vs-warn:#ffc107;--vs-good-head:#7bd88a;--vs-up:#2e7d51;--vs-up-edge:#4caf50;--vs-down:#a83030;' +
            '--vs-down-edge:#e05252;--vs-wfill:#81c784;--vs-radius:2.8px}',
        MATCH + '{--vs-panel:var(--etk-panel,rgba(14,16,19,.93));--vs-edge:var(--etk-edge,rgba(255,255,255,.14));--vs-text:var(--etk-text,#eee);' +
            '--vs-muted:var(--etk-muted,#ccc);--vs-key:var(--etk-muted,#c4c9ce);--vs-faint:var(--etk-muted,#b4bbc2);--vs-head:var(--etk-accent,#8f98a1);' +
            '--vs-radius:var(--etk-radius,2.8px)}',
        MATCH + ' .status-panel .bar-mask{background:var(--etk-track,rgba(0,0,0,.5));border-color:var(--etk-edge,rgba(255,255,255,.12));border-radius:var(--etk-radius,2.8px)}',
        // Colour blind: blue for good and orange for bad (the Okabe-Ito colours), on our parts and the game's bars and badges.
        'html[data-vs-cb]{--vs-good:#56b4e9;--vs-bad:#e69f00;--vs-warn:#f0e442;--vs-good-head:#56b4e9;--vs-up:#0072b2;--vs-up-edge:#56b4e9;' +
            '--vs-down:#d55e00;--vs-down-edge:#e69f00;--vs-wfill:#56b4e9}',
        'html[data-vs-cb] .status-panel .progress-fill.bar-green{background-color:rgba(0,114,178,.7)}',
        'html[data-vs-cb] .status-panel .progress-fill.bar-yellow{background-color:rgba(240,228,66,.55)}',
        'html[data-vs-cb] .status-panel .progress-fill.bar-orange{background-color:rgba(230,159,0,.6)}',
        'html[data-vs-cb] .status-panel .is-low .progress-fill{background-color:rgba(204,121,167,.75)!important}',
        'html[data-vs-cb] .buff.buff-pos{background-color:rgba(0,114,178,.6);border-color:rgba(86,180,233,.6)}',
        'html[data-vs-cb] .buff.buff-neg{background-color:rgba(213,94,0,.6);border-color:rgba(230,159,0,.6)}',
        // BarLook: Flat drops the bar's frame and shadow, Segmented marks every tenth.
        'html[data-vs-bars="flat"] .status-panel .bar-mask{box-shadow:none;border-color:transparent}',
        'html[data-vs-bars="segmented"] .status-panel .bar-mask::after{content:"";position:absolute;inset:0;z-index:5;pointer-events:none;' +
            'background:repeating-linear-gradient(90deg,transparent 0 calc(10% - 1px),rgba(0,0,0,.45) calc(10% - 1px) 10%)}',
        // A stat that rose or fell over the last game hour: the arrow sits just left of the value.
        '.status-panel .progress-text > .vs-trend{order:1;margin-left:auto;margin-right:3px;font-style:normal;font-size:8px}',
        '.status-panel .progress-text > :not(.attr-name):not(.vs-trend){order:2}',
        '.vs-trend-up{color:var(--vs-good)}',
        '.vs-trend-down{color:var(--vs-bad)}',
        // A buff about to end pulses. Before, not after: the game's after is its new-buff flash.
        '.buff[data-vs-ending]::before{content:"";position:absolute;inset:-1px;border-radius:inherit;border:1px solid var(--vs-warn);' +
            'box-shadow:0 0 6px 1px var(--vs-warn);opacity:0;animation:vs-ending 1.6s ease-in-out infinite;pointer-events:none}',
        '@keyframes vs-ending{0%,100%{opacity:0}50%{opacity:1}}',
        // HideFullBars: a bar full enough is taken out of the panel.
        '.status-panel > [data-vs-hide]{display:none!important}',
        // WarnPulse: an amber glow at the warning line, before the game's red one at 20%.
        '.status-panel .progress-bar-wrapper[data-vs-warn]::before{content:"";position:absolute;inset:-2px;z-index:6;border-radius:var(--vs-radius);' +
            'border:1px solid var(--vs-warn);box-shadow:0 0 7px 1px var(--vs-warn);opacity:0;animation:vs-ending 1.8s ease-in-out infinite;pointer-events:none}',
        '.status-panel .progress-bar-wrapper[data-vs-act="1"]{cursor:pointer}',
        '.status-panel .progress-bar-wrapper[data-vs-act="1"]:hover .bar-mask{filter:brightness(1.18)}',
        // The level after the queue: a striped segment from the bar's end, light when it rises, dark when it falls.
        '.vs-proj{position:absolute;top:0;bottom:0;z-index:2;pointer-events:none;transition:left .5s ease,width .5s ease;' +
            'background:repeating-linear-gradient(135deg,rgba(165,214,167,.55) 0 3px,rgba(165,214,167,.22) 3px 6px);box-shadow:inset 0 0 0 1px rgba(165,214,167,.5)}',
        '.vs-proj.vs-proj-down{background:repeating-linear-gradient(135deg,rgba(0,0,0,.55) 0 3px,rgba(0,0,0,.3) 3px 6px);box-shadow:inset 0 0 0 1px rgba(255,138,128,.55)}',
        'html[data-vs-cb] .vs-proj{background:repeating-linear-gradient(135deg,rgba(86,180,233,.55) 0 3px,rgba(86,180,233,.22) 3px 6px);box-shadow:inset 0 0 0 1px rgba(86,180,233,.5)}',
        // Pinned buffs: bigger, with their time on the badge.
        '.status-panel .buff[data-vs-fav]{zoom:var(--vs-fav,1.3);box-shadow:0 0 0 1px rgba(255,255,255,.35),.7px .7px 2.1px rgba(0,0,0,.5)}',
        '.vs-ftime{position:absolute;left:50%;bottom:-1px;transform:translate(-50%,100%);padding:0 2px;font-family:Consolas,monospace;font-size:7.5px;line-height:9px;' +
            'font-weight:bold;color:#fff;white-space:nowrap;background:rgba(0,0,0,.7);border-radius:2px;pointer-events:none;z-index:3}',
        '.status-panel .buff[data-vs-fav]{margin-bottom:8px}',
        // A debuff with a cure at hand: a small green corner, and a click queues it.
        '.status-panel .buff[data-vs-cure]{cursor:pointer;background-image:radial-gradient(circle at 85% 15%,var(--vs-good) 0 2.4px,transparent 2.9px)}',
        // CollapseBuffs: other buffs shrink to dots until the row is hovered.
        '.status-panel[data-vs-col] .status-row:not(:hover) .buff-container > .buff:not([data-vs-fav]):not(.bf-leave-active){width:7px!important;height:7px!important;' +
            'min-width:0!important;padding:0!important;font-size:0!important;border-radius:50%;overflow:hidden;transition:width .15s,height .15s}',
        '.status-panel[data-vs-col] .status-row:not(:hover) .buff-container > .buff:not([data-vs-fav]) > *{display:none}',
        // BuffTray: the buffs right of, above or below their bar.
        '.status-panel[data-vs-tray="right"] .status-row > .buff-container{order:3;justify-content:flex-start}',
        '.status-panel[data-vs-tray="right"] .status-row > .progress-bar-wrapper{order:2}',
        '.status-panel[data-vs-tray="above"] .status-row,.status-panel[data-vs-tray="below"] .status-row{flex-wrap:wrap}',
        '.status-panel[data-vs-tray="above"] .status-row > .buff-container,.status-panel[data-vs-tray="below"] .status-row > .buff-container{flex-basis:100%;justify-content:flex-end;order:0}',
        '.status-panel[data-vs-tray="above"] .status-row > :not(.buff-container){order:1}',
        '.status-panel[data-vs-tray="below"] .status-row > .buff-container{order:3}',
        '.status-panel [data-vs-moved]:hover,.status-panel[data-vs-moved]:hover{z-index:50}',
        '.status-panel > [data-vs-moved]{position:relative}',
        // The layout editor: each part outlined, the picked one bright, a dim backdrop that keeps clicks from the world.
        '.status-panel[data-vs-editing],.status-panel[data-vs-editing] > [data-vs-part]{outline:1px dashed rgba(255,255,255,.45);outline-offset:2px;cursor:move;user-select:none}',
        '.status-panel[data-vs-editing]{outline-color:rgba(255,193,7,.35);outline-offset:6px}',
        '.status-panel[data-vs-editing] [data-vs-pick]{outline:2px solid var(--vs-warn)}',
        '.status-panel[data-vs-editing] *{pointer-events:none}',
        '.status-panel[data-vs-editing] > [data-vs-part]{pointer-events:auto}',
        '#vs-edit{position:fixed;inset:0;z-index:-1;background:rgba(0,0,0,.35);pointer-events:auto}',
        '#vs-edit-bar{position:fixed;top:18px;left:50%;transform:translateX(-50%);z-index:100000;display:flex;flex-direction:column;align-items:center;gap:6px;' +
            'padding:10px 14px;max-width:560px;background:var(--vs-panel);border:1px solid var(--vs-edge);border-radius:6px;box-shadow:0 4px 18px rgba(0,0,0,.55);color:var(--vs-text);text-align:center}',
        '.vs-et{font-size:14px;font-weight:900;color:var(--vs-good-head)}',
        '.vs-eh{font-size:11px;line-height:15px;color:var(--vs-muted)}',
        '.vs-eb{display:flex;gap:6px;flex-wrap:wrap;justify-content:center;margin-top:2px}',
        '.vs-btn{font:inherit;font-size:11.5px;padding:4px 10px;border-radius:4px;border:1px solid var(--vs-edge);background:rgba(255,255,255,.07);color:var(--vs-text);cursor:pointer}',
        '.vs-btn:hover{background:rgba(255,255,255,.16)}',
        '.vs-btn.vs-on{border-color:var(--vs-warn);color:var(--vs-warn)}',
        '.vs-btn.vs-main{background:var(--vs-up);border-color:var(--vs-up-edge);color:#fff;font-weight:bold}',
        '.vs-act{color:var(--vs-good)}',
        '.vs-hint{color:var(--vs-faint);font-style:italic}',
        '.status-panel .progress-text{text-shadow:-.6px -.6px 0 rgba(0,0,0,.85),.6px -.6px 0 rgba(0,0,0,.85),-.6px .6px 0 rgba(0,0,0,.85),.6px .6px 0 rgba(0,0,0,.85)}',
        // The multiplier: a solid chip of its own left of the bar, with the bar's corners and the buffs' shadow, so the
        // bar's name and value keep their whole width. Zoomed with the bar (BarSize).
        '.vs-chip{flex-shrink:0;align-self:center;box-sizing:border-box;min-width:31px;height:18px;margin-right:4px;padding:0 4px;border:1px solid;border-radius:var(--vs-radius);font-size:10.5px;line-height:16px;font-weight:bold;text-align:center;color:#fff;font-variant-numeric:tabular-nums;box-shadow:.7px .7px 2.1px rgba(0,0,0,.5);text-shadow:0 .7px 1px rgba(0,0,0,.45);pointer-events:none;position:relative}',
        '.vs-chip-up{background:var(--vs-up);border-color:var(--vs-up-edge)}',
        '.vs-chip-down{background:var(--vs-down);border-color:var(--vs-down-edge)}',
        // The multiplier's hover box: what makes it up, buff by buff, in the look of the game's buff boxes. It opens left
        // of the chip as the bars' boxes do. The chip takes the mouse only while it has a box.
        '.vs-chip[data-interactive]{pointer-events:auto}',
        '.vs-ct{position:absolute;right:calc(100% + 9px);top:50%;transform:translateY(-50%);box-sizing:border-box;width:180px;padding:7px 9px;background:var(--vs-panel);border:1px solid var(--vs-edge);border-radius:4px;z-index:30;opacity:0;visibility:hidden;pointer-events:none;text-align:left;font-weight:normal;text-shadow:none;white-space:normal}',
        '.vs-chip:hover > .vs-ct{opacity:1;visibility:visible}',
        '.vs-ct::after{content:"";position:absolute;left:100%;top:50%;transform:translateY(-50%);margin-top:var(--vs-arrow,0px);border:6px solid transparent;border-left-color:var(--vs-panel)}',
        '.vs-ch{display:flex;justify-content:space-between;align-items:baseline;gap:8px;margin-bottom:5px;padding-bottom:4px;border-bottom:1px solid var(--vs-edge)}',
        '.vs-cn{min-width:0;font-size:12px;line-height:15px;font-weight:900}',
        '.vs-ct-up .vs-cn{color:var(--vs-good-head)}',
        '.vs-ct-down .vs-cn{color:var(--vs-bad)}',
        '.vs-cm{flex-shrink:0;font-family:Consolas,monospace;font-size:11px;font-weight:bold;color:var(--vs-text)}',
        // A hover box moved to stay on screen: its arrow is moved back by the same amount, so it still points at its bar
        // or badge. The game anchors a box by its top or, on the last rows, by its bottom; the margins move either.
        '.status-panel .status-tooltip::after,.status-panel .buff-tooltip::after{margin-top:var(--vs-arrow,0px);margin-bottom:calc(-1 * var(--vs-arrow,0px))}',
        '.vs-mark{position:absolute;top:0;bottom:0;width:1px;margin-left:-.5px;background:rgba(255,255,255,.6);pointer-events:none}',
        '.vs-mark-until{background:rgba(255,255,255,.3)}',
        '.vs-loss{position:absolute;top:0;bottom:0;pointer-events:none;background:repeating-linear-gradient(135deg,rgba(0,0,0,.55) 0 2px,rgba(0,0,0,.25) 2px 4px)}',
        '.vs-st{margin-bottom:6px;padding-bottom:5px;border-bottom:1px solid var(--vs-edge)}',
        '.vs-st:last-child{margin-bottom:0;padding-bottom:0;border-bottom:none}',
        '.status-tooltip[data-vs-no-game-text] > .st-body:not(.vs-st):not(.vs-fit-dup){display:none}',
        '.vs-row{font-size:11px;line-height:1.5;color:var(--vs-muted)}',
        '.vs-good{color:var(--vs-good)}',
        '.vs-bad{color:var(--vs-bad)}',
        '.vs-warn{color:var(--vs-warn)}',
        // The stat hover boxes as dark as the buff ones, so the HUD behind them does not show through.
        '.status-panel .status-tooltip{background-color:var(--vs-panel);border-color:var(--vs-edge)}',
        '.status-panel .status-tooltip::after{border-left-color:var(--vs-panel)}',
        // The Fitness row is built like the game's bars, so the game's styles draw it. It is the panel's last child,
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
        '.vs-head{color:var(--vs-text);margin-top:4px}',
        '.vs-timer{position:absolute;left:0;right:0;bottom:0;height:2px;background:rgba(255,255,255,.75);transform-origin:left;transition:transform ' + STEP_SECONDS + 's linear;pointer-events:none}',
        '.vs-timer.vs-timer-low{background:var(--vs-warn)}',
        '.bf-leave-active .vs-timer{transition:none}',
        // Badges near the bar's own label size, so at the same BuffSize and BarSize they sit in proportion to the bar.
        // Their zoom (BuffSize) is set apart from the bar's.
        '.status-panel .buff-container{gap:3.5px;margin-right:5.6px}',
        '.status-panel .buff{padding:3px 6px;font-size:10.5px;line-height:13px}',
        // A buff tooltip with our block: the name and time on one line, then the effects, the cures and the game text
        // last in smaller type. The children are placed by order, because Vue adds the header and text after our block.
        '.buff-tooltip[data-vs-tip]{display:flex;flex-wrap:wrap;align-items:baseline;width:196px;padding:6px 8px 7px;background-color:var(--vs-panel);border-color:var(--vs-edge)}',
        '.buff-tooltip[data-vs-tip]::after{border-left-color:var(--vs-panel)}',
        '[data-vs-tip] > .bt-header{order:0;flex:1 1 0;min-width:0;margin:0;padding:0;border:0}',
        '[data-vs-tip] .bt-title{font-size:12px;line-height:15px;letter-spacing:.2px}',
        '[data-vs-tip][data-vs-timed] .bt-timer{display:none}',
        '.vs-btime{order:1;flex:0 0 auto;margin-left:8px;font-family:Consolas,monospace;font-size:10.5px;font-weight:bold;color:var(--vs-text)}',
        '[data-vs-tip] > .vs-bt{order:2;flex:0 0 100%;margin-top:4px;padding-top:3px;border-top:1px solid var(--vs-edge)}',
        '[data-vs-tip] > .vs-bt.vs-bt-timed{margin-top:0;padding-top:0;border-top:0}',
        // The game's text drawn again by us, a sentence to a line, in place of its one block.
        '[data-vs-tip] > .bt-body{display:none}',
        '[data-vs-tip] > .vs-bd{order:3;flex:0 0 100%;margin-top:6px;padding-top:5px;border-top:1px solid var(--vs-edge)}',
        '.vs-bd .vs-row{font-size:10.5px;line-height:1.45;color:var(--vs-faint)}',
        '.vs-bd .vs-row + .vs-row{margin-top:3px}',
        '.vs-bt .vs-row,.vs-weight-tip .vs-row{font-size:10.5px;line-height:1.45}',
        '.vs-kv{display:flex;justify-content:space-between;align-items:baseline;gap:8px}',
        '.vs-kv .vs-k{min-width:0;color:var(--vs-key)}',
        '.vs-kv .vs-v{flex-shrink:0;font-family:Consolas,monospace;font-weight:bold;color:var(--vs-text)}',
        '.vs-kv.vs-good .vs-v{color:var(--vs-good)}',
        '.vs-kv.vs-bad .vs-v{color:var(--vs-bad)}',
        '.vs-kv.vs-warn .vs-v{color:var(--vs-warn)}',
        '.vs-bt .vs-head,.vs-weight-tip .vs-head{margin-top:5px;font-size:9.5px;line-height:1.4;letter-spacing:.4px;text-transform:uppercase;color:var(--vs-head)}',
        '.vs-tbar{height:2px;margin:4px 0;background:rgba(255,255,255,.15)}',
        '.vs-tfill{height:100%;background:rgba(255,255,255,.75)}',
        '.vs-tbar.vs-timer-low .vs-tfill{background:var(--vs-warn)}',
        '.vs-item{display:flex;align-items:center;gap:4px;font-size:10.5px;line-height:1.5;color:var(--vs-text)}',
        '.vs-item img{width:13px;height:13px;object-fit:contain;flex-shrink:0}',
        '.vs-item .vs-count{margin-left:auto;font-family:Consolas,monospace;font-size:10px;color:var(--vs-text)}',
        '.vs-item.vs-none{opacity:.45}',
        // The weight sits under the bag icon. The game's button is 45px with a 1px border and centres a 20px icon, so the
        // icon ends 31.5px down the 43px inside; the label's line starts at 32px and ends where the weight bar starts.
        '.vs-weight{position:absolute;left:0;right:0;bottom:3px;font-size:9px;line-height:8px;text-align:center;color:#cfd3d5;pointer-events:none;font-variant-numeric:tabular-nums}',
        '.vs-weight.vs-near{color:var(--vs-warn)}',
        '.vs-weight.vs-over{color:var(--vs-bad)}',
        '.vs-wbar{position:absolute;left:4px;right:4px;bottom:1px;height:2px;background:rgba(255,255,255,.18);pointer-events:none}',
        '.vs-wfill{position:absolute;left:0;top:0;bottom:0;background:var(--vs-wfill)}',
        '.vs-wbar.vs-near .vs-wfill{background:var(--vs-warn)}',
        '.vs-wbar.vs-over .vs-wfill{background:var(--vs-bad)}',
        '.vs-wgap{position:absolute;top:0;bottom:-1px;width:2px;margin-left:-1px;background:#111}',
        '.vs-weight-tip{margin-top:6px;padding-top:5px;border-top:1px solid var(--vs-edge)}'
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
    // The bar or badge under the mouse, whose hover box is kept on screen.
    var fitHost = null;
    // Per stat, readings of its bar [game second, part full], oldest first, for the trend arrows.
    var samples = {};

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

        drawLook(held.look);
        drawZoom(held.zoom, held.buffZoom, held.tipZoom);
        drawWeight(held);

        var list = attrList();

        drawExplore(list);

        var rows = document.querySelectorAll('.status-panel > .status-row');

        if (!rows.length) {
            dropFitness();

            return 'nobar';
        }

        var isListed = !!list && list.length === rows.length;
        var names = statNames(isListed ? list : null, rows.length);

        for (var r = 0; r < rows.length; r++) {
            var stat = statOf(isListed ? list[r] : null, r);

            drawStat(rows[r], stat, isListed ? list[r] : null, names);
        }

        drawTimers(rows, isListed ? list : null, held.buffs, now);
        drawFitness(rows[rows.length - 1].parentElement);
        drawHud(rows, isListed ? list : null);

        if (hovered) drawBuffTip(hovered, names);

        if (fitHost) fitTip(tipOfHost(fitHost));

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
    function isTrendOn() { return option(5); }

    // ===== Look =====

    // On the page's root, where our stylesheet reads them: the HUD style match, colour-blind colours, the bar look.
    function drawLook(look) {
        var root = document.documentElement;
        var parts = look || [1, 0, 0];

        if (!root) return;

        setMark(root, 'data-vs-match', parts[0] === 1);
        setMark(root, 'data-vs-cb', parts[2] === 1);
        setValue(root, 'data-vs-bars', BAR_LOOKS[parts[1]] || null);
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

    // The game's name for each stat, in the plugin's order, from its rows; ours where a row cannot be read.
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

        drawChip(row, bar, ratios ? ratios[0] : 0, stat, names);
        drawMarks(mask, wellFed);
        drawLoss(mask, stat, item);
        drawTrend(mask, stat, item);

        if (stat === LIFE) drawHeart(mask);

        if (tip) drawStatTip(tip, stat, item, ratios, wellFed, names);
    }

    // An arrow by the value when the stat rose or fell over the last game hour; none until ten game minutes are seen.
    function drawTrend(mask, stat, item) {
        var text = mask.querySelector('.progress-text');
        var arrow = text ? ownChild(text, 'vs-trend') : null;
        var value = item ? Number(item.value) : NaN;
        var way = text && isTrendOn() && stat >= 0 && isFinite(value) ? trendOf(stat, value) : 0;

        if (!way) {
            if (arrow) text.removeChild(arrow);

            return;
        }

        if (!arrow) arrow = text.appendChild(element('i', 'vs-trend'));

        setClass(arrow, 'vs-trend-up', way > 0);
        setClass(arrow, 'vs-trend-down', way < 0);
        setText(arrow, way > 0 ? '\u25B2' : '\u25BC');
    }

    // 1 risen, -1 fallen, 0 neither, against the oldest reading kept: one about an hour old once there is one. A
    // clock that went back (another save loaded) starts the readings again.
    function trendOf(stat, value) {
        var list = samples[stat] || (samples[stat] = []);
        var last = list.length ? list[list.length - 1] : null;

        if (last && clock < last[0]) {
            list.length = 0;
            last = null;
        }

        if (!last || clock - last[0] >= TREND_STEP) list.push([clock, value]);

        while (list.length > 1 && clock - list[1][0] >= TREND_SECONDS) list.shift();

        if (clock - list[0][0] < TREND_MIN_SECONDS) return 0;

        var change = value - list[0][1];

        return change > TREND_EPSILON ? 1 : change < -TREND_EPSILON ? -1 : 0;
    }

    // The game shows a shield on the Life row; the heart is the stat's own mark.
    function drawHeart(mask) {
        var name = mask.querySelector('.attr-name');
        var icon = name && name.children.length ? name.children[0] : null;

        if (icon) setText(icon, HEART);
    }

    // In the row, just before the bar. Put back there when the game adds a node in between.
    function drawChip(row, bar, gain, stat, names) {
        var text = scaleText(gain);
        var chip = ownChild(row, 'vs-chip');

        if (!text) {
            if (chip) row.removeChild(chip);

            return;
        }

        if (!chip) chip = element('span', 'vs-chip');

        if (row.children[Array.prototype.indexOf.call(row.children, bar) - 1] !== chip) row.insertBefore(chip, bar);

        setClass(chip, 'vs-chip-up', gain > 0);
        setClass(chip, 'vs-chip-down', gain < 0);
        setText(ownChild(chip, 'vs-cx') || chip.appendChild(element('span', 'vs-cx')), 'x' + text);

        var hasTip = drawChipTip(chip, stat, gain, text, names);

        if (chip.hasAttribute('data-interactive') === hasTip) return;

        setMark(chip, 'data-interactive', hasTip);
        syncClicks();
    }

    // The multiplier's hover box: the stat's gain and its multiplier, then each buff's share and whatever else the
    // game adds that no running buff accounts for. Rebuilt only when what it shows changed.
    function drawChipTip(chip, stat, gain, text, names) {
        var tip = ownChild(chip, 'vs-ct');

        if (!isTooltipOn() || stat < 0) {
            if (tip) chip.removeChild(tip);

            return false;
        }

        var title = say().gainFx(names[stat]);
        var rows = chipRows(stat, gain);
        var key = JSON.stringify([title, text, gain > 0, rows]);

        if (tip && tip.__vsKey === key) return true;

        if (!tip) tip = chip.appendChild(element('div', 'vs-ct'));

        setClass(tip, 'vs-ct-up', gain > 0);
        setClass(tip, 'vs-ct-down', gain < 0);

        while (tip.children.length) tip.removeChild(tip.children[0]);

        var head = tip.appendChild(element('div', 'vs-ch'));

        head.appendChild(element('span', 'vs-cn')).textContent = title;
        head.appendChild(element('span', 'vs-cm')).textContent = 'x' + text;

        for (var i = 0; i < rows.length; i++) tip.appendChild(rowNode(rows[i]));

        tip.__vsKey = key;

        return true;
    }

    function chipRows(stat, gain) {
        var sources = held.src ? held.src[stat] || [] : [];
        var rows = [];
        var rest = gain;

        for (var i = 0; i < sources.length; i++) {
            rest -= sources[i][1];
            addShare(rows, sources[i][0], sources[i][1]);
        }

        addShare(rows, say().other, rest);

        return rows;
    }

    function addShare(rows, name, ratio) {
        var change = percentChange(ratio);

        if (change) rows.push({ k: name, v: change, c: ratio > 0 ? 'vs-good' : 'vs-bad' });
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

        return rows.concat(hudRows(stat, value));
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

    // ===== HUD: hiding, warning line, buff tray, layout, explore strip, clicks =====

    // What the plugin sends as hud: hide [percent, keep with buffs], warn (percent, 0 none), sort and group (0 the
    // game's), fav (pinned buff ids), favSize, col (collapse), lay (per part [dx, dy, size], or null), tray (0 left,
    // 1 right, 2 above, 3 below), exp ([corner, size] or null), proj (per stat the level after the queue, or null),
    // cure (debuff ids with a cure at hand), cureOn, act (per stat 1 when a click has something to do, or null when
    // clicks are off), tips (per stat [what a click does, 1 when it can]).
    var HUD_PREFIX = 'slmod|vitalsigns|hud|';
    var TRAYS = [null, 'right', 'above', 'below'];
    var CORNERS = [null, 'tl', 'tr', 'bl', 'br'];
    var PARTS = 6;
    var FIT_PART = 4;
    var PANEL_PART = 5;
    var PART_MARK = 'data-vs-part';
    var HOME = [0, 0, 100];
    var SIZE_STEP = 5;
    var MIN_PART_SIZE = 50;
    var MAX_PART_SIZE = 200;
    var MAX_SHIFT = 4000;
    var EDIT_ID = 'vs-edit';
    var EDIT_BAR_ID = 'vs-edit-bar';
    var EXPLORE_ID = 'vs-explore';
    var EXPLORE_STYLE_ID = 'vs-explore-style';
    var ROW_MARKS = ['data-vs-hide', PART_MARK, 'data-vs-moved'];
    var BAR_MARKS = ['data-vs-warn', 'data-vs-act'];
    var BADGE_MARKS = ['data-vs-fav', 'data-vs-cure'];
    var PANEL_MARKS = ['data-vs-tray', 'data-vs-col', 'data-vs-editing', PART_MARK, 'data-vs-moved'];
    var EXPLORE_CSS = [
        '#' + EXPLORE_ID + '{position:fixed;z-index:999990;display:flex;flex-direction:column;gap:4px;padding:7px 8px;min-width:176px;box-sizing:border-box;' +
            'background:rgba(14,16,19,.82);border:1px solid rgba(255,255,255,.14);border-radius:4px;box-shadow:0 2px 10px rgba(0,0,0,.45);pointer-events:none;' +
            'font-family:inherit;color:#eee}',
        '#' + EXPLORE_ID + '[data-corner="tl"]{top:14px;left:14px}',
        '#' + EXPLORE_ID + '[data-corner="tr"]{top:14px;right:14px}',
        '#' + EXPLORE_ID + '[data-corner="bl"]{bottom:14px;left:14px}',
        '#' + EXPLORE_ID + '[data-corner="br"]{bottom:14px;right:14px}',
        '.vse-row{position:relative;height:17px;border-radius:2.8px;overflow:hidden;background:rgba(0,0,0,.5);border:1px solid rgba(255,255,255,.12);box-sizing:border-box}',
        '.vse-fill{position:absolute;left:0;top:0;bottom:0;background:rgba(76,175,80,.7);transition:width .6s ease}',
        '.vse-row.vse-mid .vse-fill{background:rgba(255,193,7,.6)}',
        '.vse-row.vse-low .vse-fill{background:rgba(211,47,47,.75)}',
        '#' + EXPLORE_ID + '[data-cb] .vse-fill{background:rgba(0,114,178,.7)}',
        '#' + EXPLORE_ID + '[data-cb] .vse-row.vse-mid .vse-fill{background:rgba(240,228,66,.55)}',
        '#' + EXPLORE_ID + '[data-cb] .vse-row.vse-low .vse-fill{background:rgba(204,121,167,.75)}',
        '.vse-text{position:absolute;inset:0;display:flex;align-items:center;justify-content:space-between;padding:0 6px;font-size:10.5px;font-weight:bold;' +
            'line-height:15px;text-shadow:0 0 2px rgba(0,0,0,.9);font-variant-numeric:tabular-nums}',
        '.vse-name{display:flex;gap:4px;align-items:center;min-width:0;white-space:nowrap;overflow:hidden}'
    ].join('\n');

    // The layout being edited, or null: per part [dx, dy, size], the tray side, collapse, the part picked and a drag.
    var editing = null;
    // What the last call left in place that the page's click areas depend on; a change measures them again.
    var hudKey = '';
    // The Root frame list watched for the travel map opening and closing.
    var exploreWatch = null;
    var exploreSoon = 0;

    function hudOf() {
        return held && held.hud ? held.hud : null;
    }

    function drawHud(rows, list) {
        var h = hudOf();
        var panel = rows[0].parentElement;
        var parts = editing ? editing.parts : h && h.lay ? h.lay : null;
        var key = [];

        setValue(panel, 'data-vs-tray', TRAYS[editing ? editing.tray : h ? h.tray : 0] || null);
        setMark(panel, 'data-vs-col', editing ? editing.col === 1 : !!h && h.col === 1);
        setMark(panel, 'data-vs-editing', !!editing);
        setVar(panel, '--vs-fav', h && h.favSize > 0 ? String(h.favSize) : '');

        for (var r = 0; r < rows.length; r++) {
            var item = list ? list[r] : null;

            key.push(drawRowHud(rows[r], statOf(item, r), item ? Number(item.value) : NaN, h, parts));
        }

        var fit = ownChild(panel, FIT_ROW);

        if (fit) key.push(drawRowHud(fit, FIT_PART, held.fit && held.fit[1] > 0 ? held.fit[0] / held.fit[1] : NaN, h, parts));

        place(panel, PANEL_PART, parts ? parts[PANEL_PART] : null, 'right top');
        key.push(panel.style.transform, panel.getAttribute('data-vs-tray'), panel.hasAttribute('data-vs-col'));

        var joined = key.join('|');

        if (joined !== hudKey) {
            hudKey = joined;
            syncClicks();
        }
    }

    // One row's part of the HUD: hidden when full enough, pulsing at the warning line, the level after the queue,
    // clickable, and moved. Returns what changes the page's click areas.
    function drawRowHud(row, part, level, h, parts) {
        var bar = ownChild(row, 'progress-bar-wrapper');
        var container = ownChild(row, 'buff-container');
        var hasBuffs = !!container && Array.prototype.some.call(container.children, isBadge);
        var isKnown = part >= 0 && isFinite(level);
        var isHidden = !editing && !!h && !!h.hide && isKnown && level * 100 >= h.hide[0] - 1e-6 && !(h.hide[1] === 1 && hasBuffs);
        var isWarned = !!h && h.warn > 0 && isKnown && level * 100 <= h.warn && !!bar && !bar.classList.contains('is-low');
        var canClick = !editing && !!h && !!h.act && part >= 0 && part < FIT_PART;

        setMark(row, 'data-vs-hide', isHidden);

        if (bar) {
            setMark(bar, 'data-vs-warn', isWarned);
            setValue(bar, 'data-vs-act', canClick ? (h.act[part] ? '1' : '0') : null);
            drawProjection(bar, part, level, h);
        }

        place(row, part, part >= 0 && parts ? parts[part] : null, 'right center');

        return (isHidden ? 'h' : '') + row.style.transform;
    }

    // A ghost segment from the bar's level to where the queue leaves it: light when it rises, dark when it falls.
    function drawProjection(bar, part, level, h) {
        var mask = ownChild(bar, 'bar-mask');
        var shade = mask ? ownChild(mask, 'vs-proj') : null;
        var to = h && h.proj && part >= 0 && part < FIT_PART ? h.proj[part] : null;

        if (!mask) return;

        if (typeof to !== 'number' || !isFinite(level) || Math.abs(to - level) < 0.005) {
            if (shade) mask.removeChild(shade);

            return;
        }

        to = Math.max(0, Math.min(1, to));

        if (!shade) shade = mask.appendChild(element('div', 'vs-proj'));

        setClass(shade, 'vs-proj-down', to < level);
        setStyle(shade, 'left', percent(Math.min(to, Math.max(0, level))));
        setStyle(shade, 'width', percent(Math.abs(to - Math.max(0, level))));
    }

    // Moves and sizes a part from where the game puts it; the transform's origin keeps the right edge, where the HUD
    // is anchored, in place as it grows.
    function place(node, part, spot, origin) {
        var s = spot || HOME;
        var isHome = s[0] === 0 && s[1] === 0 && s[2] === 100;

        setValue(node, PART_MARK, part >= 0 ? String(part) : null);
        setMark(node, 'data-vs-moved', !isHome);
        setStyle(node, 'transform', isHome ? '' : 'translate(' + s[0] + 'px,' + s[1] + 'px) scale(' + s[2] / 100 + ')');
        setStyle(node, 'transformOrigin', isHome ? '' : origin);
    }

    // Pinned buffs, then good or bad apart, then by time left, each kept in the game's order otherwise. The first sits
    // next to the bar: at the far right of a tray left of, above or below it, at the far left of one right of it.
    function drawTray(container, badges, now) {
        var h = hudOf();
        var fav = h && h.fav ? h.fav : [];
        var sort = h ? h.sort : 0;
        var group = h ? h.group : 0;
        var ranked = [];
        var i;

        for (i = 0; i < badges.length; i++) {
            var shown = badges[i].__vs || {};
            var pin = fav.indexOf(shown.id);
            var left = shown.copy && shown.copy[1] >= 0 ? shown.copy[1] - now : Infinity;

            setMark(badges[i], 'data-vs-fav', pin >= 0);
            setMark(badges[i], 'data-vs-cure', isCurable(shown.id));
            drawPinTime(badges[i], pin >= 0 && isFinite(left) ? left : -1);
            ranked.push({ badge: badges[i], at: i, pin: pin, good: badges[i].classList.contains('buff-pos'), left: left });
        }

        if (!(sort > 0 || group > 0 || fav.length > 0)) {
            for (i = 0; i < badges.length; i++) setStyle(badges[i], 'order', '');

            return;
        }

        ranked.sort(function (a, b) {
            if ((a.pin >= 0) !== (b.pin >= 0)) return a.pin >= 0 ? -1 : 1;
            if (a.pin >= 0) return a.pin - b.pin;
            if (group > 0 && a.good !== b.good) return a.good === (group === 1) ? -1 : 1;
            if (sort === 1 && a.left !== b.left) return a.left < b.left ? -1 : 1;
            if (sort === 2 && a.left !== b.left) return a.left > b.left ? -1 : 1;

            return a.at - b.at;
        });

        var isRight = (editing ? editing.tray : h.tray) === 1;

        for (i = 0; i < ranked.length; i++) setStyle(ranked[i].badge, 'order', String(isRight ? i : ranked.length - 1 - i));
    }

    function isCurable(id) {
        var h = hudOf();

        return !!h && h.cureOn === 1 && !!h.cure && h.cure.indexOf(id) >= 0;
    }

    // A pinned buff's time left, written on its badge.
    function drawPinTime(badge, left) {
        var time = ownChild(badge, 'vs-ftime');

        if (left < 0) {
            if (time) badge.removeChild(time);

            return;
        }

        if (!time) time = badge.insertBefore(element('span', 'vs-ftime'), ownChild(badge, 'buff-tooltip'));

        setText(time, duration(left));
    }

    // The rows a stat's hover box gets from the HUD settings: its level after the queue, and what a click does.
    function hudRows(stat, value) {
        var h = hudOf();
        var text = say();
        var rows = [];
        var max = held.max ? held.max[stat] : 0;
        var to = h && h.proj ? h.proj[stat] : null;

        if (typeof to === 'number' && max > 0 && isFinite(value) && Math.abs(to - value) >= 0.005) {
            rows.push([text.afterQueue(number(Math.max(0, Math.min(1, to)) * max), number(max)), to > value ? 'vs-good' : 'vs-bad']);
        }

        var tip = h && h.act && h.tips ? h.tips[stat] : null;

        if (tip && tip[0]) rows.push([text.click(tip[0]), tip[1] ? 'vs-act' : 'vs-hint']);

        return rows;
    }

    // The rows a buff's hover box gets from the HUD settings: its cure on a click, pinning on a shift-click.
    function hudBuffRows(id) {
        var h = hudOf();
        var text = say();
        var rows = [];

        if (!h) return rows;

        if (isCurable(id)) rows.push([text.clickCure, 'vs-act']);

        rows.push([(h.fav || []).indexOf(id) >= 0 ? text.unpin : text.pin, 'vs-hint']);

        return rows;
    }

    // One capturing listener: a click on a bar queues its action, on a curable debuff its cure, and a shift-click on a
    // buff pins or unpins it. The game's click still runs.
    function onClick(event) {
        if (editing || !hudOf()) return;

        var badge = badgeOf(event.target);

        if (badge) {
            var id = badge.__vs ? badge.__vs.id : null;

            if (typeof id !== 'number') return;

            if (event.shiftKey) post('fav|' + id);
            else if (badge.hasAttribute('data-vs-cure')) post('cure|' + id);

            return;
        }

        var host = hostOf(event.target);

        if (!host || !host.classList.contains('progress-bar-wrapper') || !host.hasAttribute('data-vs-act')) return;

        var part = Number(host.parentElement.getAttribute(PART_MARK));

        if (part >= 0 && part < FIT_PART) post('act|' + part);
    }

    function post(message) {
        var bridge = window.vuplex;

        try {
            if (!bridge && window.top) bridge = window.top.vuplex;
        } catch (e) {
            bridge = null;
        }

        if (bridge && typeof bridge.postMessage === 'function') bridge.postMessage(HUD_PREFIX + message);
    }

    // ===== Layout editor =====

    // The plugin turns it on and off (its key, or the Mod Manager's button). Drag a part to move it, scroll on it to
    // size it, Shift for the whole panel. Save sends the layout to the plugin, which keeps it in the config.
    function edit(isOn) {
        if (!isOn) {
            stopEdit();

            return 'ok';
        }

        if (editing) return 'ok';

        var h = hudOf();
        var parts = [];

        for (var i = 0; i < PARTS; i++) parts.push((h && h.lay && h.lay[i] ? h.lay[i] : HOME).slice());

        editing = { parts: parts, tray: h ? h.tray : 0, col: h ? h.col : 0, pick: -1, drag: null };
        drawEditor();

        if (held) apply(clock);

        return 'ok';
    }

    function stopEdit() {
        if (!editing) return;

        editing = null;
        removeById(document, EDIT_ID);
        removeById(document, EDIT_BAR_ID);

        var picked = document.querySelectorAll('[data-vs-pick]');

        for (var i = 0; i < picked.length; i++) setMark(picked[i], 'data-vs-pick', false);

        if (held) apply(clock);

        syncClicks();
    }

    // A dim backdrop under the HUD that keeps clicks from the world while editing, and the bar of buttons.
    function drawEditor() {
        var text = say();
        var back = element('div', '');
        var bar = element('div', '');

        back.id = EDIT_ID;
        back.setAttribute('data-interactive', '');
        bar.id = EDIT_BAR_ID;
        bar.setAttribute('data-interactive', '');
        bar.appendChild(element('div', 'vs-et')).textContent = text.editTitle;
        bar.appendChild(element('div', 'vs-eh')).textContent = text.editHelp;

        var buttons = bar.appendChild(element('div', 'vs-eb'));

        button(buttons, 'vs-tray', trayLabel(), function () {
            editing.tray = (editing.tray + 1) % TRAYS.length;
            this.textContent = trayLabel();
            apply(clock);
        });
        button(buttons, 'vs-col', text.collapse, function () {
            editing.col = editing.col === 1 ? 0 : 1;
            setClass(this, 'vs-on', editing.col === 1);
            apply(clock);
        });
        setClass(ownChild(buttons, 'vs-col'), 'vs-on', editing.col === 1);
        button(buttons, 'vs-reset', text.reset, function () {
            for (var i = 0; i < PARTS; i++) editing.parts[i] = HOME.slice();

            placeAll();
        });
        button(buttons, 'vs-cancel', text.cancel, function () {
            stopEdit();
            post('edit|0');
        });
        button(buttons, 'vs-save vs-main', text.save, function () {
            var saved = editing;
            var h = hudOf();

            // Kept on the page until the plugin sends the saved settings back, so nothing jumps.
            if (h) {
                h.lay = isHomeLayout(saved.parts) ? null : saved.parts;
                h.tray = saved.tray;
                h.col = saved.col;
            }

            stopEdit();
            post('layout|' + layoutText(saved.parts) + '|' + saved.tray + '|' + saved.col);
        });

        document.body.appendChild(back);
        document.body.appendChild(bar);
        syncClicks();
    }

    function button(parent, className, label, onPress) {
        var made = parent.appendChild(element('button', 'vs-btn ' + className));

        made.type = 'button';
        made.textContent = label;
        made.addEventListener('click', function (event) {
            event.stopPropagation();

            if (editing) onPress.call(made, event);
        });

        return made;
    }

    function trayLabel() {
        var text = say();

        return text.tray + text.trays[editing ? editing.tray : 0];
    }

    function isHomeLayout(parts) {
        for (var i = 0; i < parts.length; i++) {
            if (parts[i][0] !== 0 || parts[i][1] !== 0 || parts[i][2] !== 100) return false;
        }

        return true;
    }

    function layoutText(parts) {
        if (isHomeLayout(parts)) return '';

        return parts.map(function (part) { return part.join(','); }).join(';');
    }

    // Every part placed from the layout being edited, without a whole redraw; for drags and the wheel.
    function placeAll() {
        var panel = document.querySelector('.status-panel');

        if (!panel || !editing) return;

        var nodes = panel.querySelectorAll('[' + PART_MARK + ']');

        for (var i = 0; i < nodes.length; i++) {
            var part = Number(nodes[i].getAttribute(PART_MARK));

            place(nodes[i], part, editing.parts[part], 'right center');
            setMark(nodes[i], 'data-vs-pick', part === editing.pick);
        }

        place(panel, PANEL_PART, editing.parts[PANEL_PART], 'right top');
        setMark(panel, 'data-vs-pick', editing.pick === PANEL_PART);
    }

    // The part under the pointer: a row, the Fitness row, or the panel itself between them.
    function partOf(node) {
        for (; node && node !== document.body; node = node.parentElement) {
            if (node.id === EDIT_BAR_ID) return -1;

            if (node.hasAttribute && node.hasAttribute(PART_MARK) && (node.classList.contains('status-panel') || node.parentElement.classList.contains('status-panel'))) {
                return Number(node.getAttribute(PART_MARK));
            }
        }

        return -1;
    }

    function partNode(part) {
        var panel = document.querySelector('.status-panel');

        if (!panel || part === PANEL_PART) return panel;

        return panel.querySelector('[' + PART_MARK + '="' + part + '"]');
    }

    // How many screen pixels one of the part's own pixels is, from its parent's drawn size: a move is in its parent's.
    function scaleOf(node) {
        var parent = node ? node.parentElement : null;
        var width = parent ? parent.offsetWidth : 0;
        var scale = width > 0 ? parent.getBoundingClientRect().width / width : 1;

        return scale > 0 && isFinite(scale) ? scale : 1;
    }

    function onDown(event) {
        if (!editing || event.button !== 0) return;

        var part = partOf(event.target);

        if (part < 0) return;

        if (event.shiftKey) part = PANEL_PART;

        editing.pick = part;
        editing.drag = { part: part, x: event.clientX, y: event.clientY, from: editing.parts[part].slice(), scale: scaleOf(partNode(part)) };
        placeAll();
        event.preventDefault();
        event.stopPropagation();
    }

    function onMove(event) {
        var drag = editing ? editing.drag : null;

        if (!drag) return;

        var spot = editing.parts[drag.part];

        spot[0] = clampShift(drag.from[0] + (event.clientX - drag.x) / drag.scale);
        spot[1] = clampShift(drag.from[1] + (event.clientY - drag.y) / drag.scale);
        placeAll();
        event.preventDefault();
    }

    function onUp() {
        if (!editing || !editing.drag) return;

        editing.drag = null;
        syncClicks();
    }

    function onWheel(event) {
        if (!editing) return;

        var part = partOf(event.target);

        if (part < 0) return;

        if (event.shiftKey) part = PANEL_PART;

        var spot = editing.parts[part];

        spot[2] = Math.max(MIN_PART_SIZE, Math.min(MAX_PART_SIZE, spot[2] + (event.deltaY < 0 ? SIZE_STEP : -SIZE_STEP)));
        editing.pick = part;
        placeAll();
        syncClicks();
        event.preventDefault();
    }

    function clampShift(value) {
        return Math.max(-MAX_SHIFT, Math.min(MAX_SHIFT, Math.round(value)));
    }

    // ===== Bars on the travel map =====

    // The travel map is a page of its own over the HUD. While it shows, a small copy of the bars sits in a corner of
    // the window above every page; it takes no clicks.
    function drawExplore(list) {
        var h = hudOf();
        var top = topDocument();

        if (!top) return;

        var frame = exploreFrame(top);
        var isShown = !!h && !!h.exp && !!list && list.length > 0 && isFrameShown(frame);

        watchExplore(top, !!h && !!h.exp);

        var strip = top.getElementById(EXPLORE_ID);

        if (!isShown) {
            if (strip) strip.parentElement.removeChild(strip);

            return;
        }

        addExploreStyle(top);

        if (!strip) {
            strip = top.createElement('div');
            strip.id = EXPLORE_ID;
            top.body.appendChild(strip);
        }

        setValue(strip, 'data-corner', CORNERS[h.exp[0]] || 'tr');
        setMark(strip, 'data-cb', !!held.look && held.look[2] === 1);
        setStyle(strip, 'zoom', String(h.exp[1] > 0 ? h.exp[1] : 1));

        var names = statNames(list, list.length);
        var lines = [];

        for (var r = 0; r < list.length; r++) {
            var stat = statOf(list[r], r);
            var value = Number(list[r].value);

            if (stat < 0 || !isFinite(value)) continue;

            lines.push([stat === LIFE ? HEART : ICONS[stat], names[stat], value, String(list[r].barValue === undefined ? '' : list[r].barValue)]);
        }

        if (held.fit && held.fit[1] > 0) lines.push([FIT_ICON, say().fitness, held.fit[0] / held.fit[1], String(held.fit[0])]);

        drawExploreRows(top, strip, lines);
    }

    function drawExploreRows(top, strip, lines) {
        var key = JSON.stringify(lines);

        if (strip.__vsKey === key) return;

        strip.__vsKey = key;

        while (strip.children.length < lines.length) {
            var row = strip.appendChild(top.createElement('div'));
            var fill = row.appendChild(top.createElement('div'));
            var text = row.appendChild(top.createElement('div'));
            var name = text.appendChild(top.createElement('span'));

            row.className = 'vse-row';
            fill.className = 'vse-fill';
            text.className = 'vse-text';
            name.className = 'vse-name';
            text.appendChild(top.createElement('span'));
        }

        while (strip.children.length > lines.length) strip.removeChild(strip.lastChild);

        for (var i = 0; i < lines.length; i++) {
            var line = lines[i];
            var node = strip.children[i];
            var level = Math.max(0, Math.min(1, line[2]));

            setClass(node, 'vse-low', level <= 0.2);
            setClass(node, 'vse-mid', level > 0.2 && level <= 0.5);
            setStyle(node.children[0], 'width', percent(level));
            setText(node.children[1].children[0], line[0] + ' ' + line[1]);
            setText(node.children[1].children[1], line[3]);
        }
    }

    function topDocument() {
        try {
            return window.top && window.top !== window ? window.top.document : null;
        } catch (e) {
            return null;
        }
    }

    function isExploreFrame(frame) {
        return /\/ExploreMap\//i.test(String(frame.getAttribute('src') || ''));
    }

    // The frame found last is kept on the Root page and reused while it is still there with the same page; the frame
    // list is a live one, so a miss walks it without a search.
    function exploreFrame(top) {
        var kept = top.__slvExploreFrame;

        if (kept && kept.isConnected && isExploreFrame(kept)) return kept;

        var frames = top.__slvFrames || (top.__slvFrames = top.getElementsByTagName('iframe'));

        for (var i = 0; i < frames.length; i++) {
            if (isExploreFrame(frames[i])) return (top.__slvExploreFrame = frames[i]);
        }

        top.__slvExploreFrame = null;

        return null;
    }

    function isFrameShown(frame) {
        if (!frame || frame.style.display === 'none') return false;

        var view = frame.ownerDocument.defaultView;
        var style = view ? view.getComputedStyle(frame) : null;

        return !style || (style.display !== 'none' && style.visibility !== 'hidden' && Number(style.opacity) > 0.05);
    }

    // Root shows and hides pages by their frame's style: one watcher on its frame list redraws soon after, so the
    // strip does not wait for the plugin's next check. Only while the strip is on.
    function watchExplore(top, isOn) {
        if (!isOn) {
            unwatchExplore();

            return;
        }

        if (exploreWatch) return;

        var list = top.getElementById('iframe-container') || top.body;
        var Watcher = (top.defaultView && top.defaultView.MutationObserver) || window.MutationObserver;

        if (!list || !Watcher) return;

        // A watcher left by a HUD page that has since gone away lives on in the Root page; this one replaces it.
        if (top.__slvExploreWatch) {
            try {
                top.__slvExploreWatch.disconnect();
            } catch (e) {
                // Its page is gone; nothing left to stop.
            }
        }

        exploreWatch = new Watcher(function () {
            // This HUD page was taken off the screen: stop watching, the next install watches again.
            if (!document.defaultView) {
                exploreWatch.disconnect();

                return;
            }

            if (exploreSoon) return;

            exploreSoon = setTimeout(function () {
                exploreSoon = 0;

                if (held && isHeld) drawExplore(attrList());
            }, 60);
        });
        exploreWatch.observe(list, { childList: true, subtree: true, attributes: true, attributeFilter: ['style', 'src'] });
        top.__slvExploreWatch = exploreWatch;
    }

    function unwatchExplore() {
        if (exploreWatch) {
            exploreWatch.disconnect();

            var top = topDocument();

            if (top && top.__slvExploreWatch === exploreWatch) top.__slvExploreWatch = null;
        }

        if (exploreSoon) clearTimeout(exploreSoon);

        exploreWatch = null;
        exploreSoon = 0;
    }

    function addExploreStyle(top) {
        if (top.getElementById(EXPLORE_STYLE_ID)) return;

        var style = top.createElement('style');

        style.id = EXPLORE_STYLE_ID;
        style.textContent = EXPLORE_CSS;
        top.head.appendChild(style);
    }

    function removeById(doc, id) {
        var node = doc ? doc.getElementById(id) : null;

        if (node && node.parentElement) node.parentElement.removeChild(node);
    }

    function setVar(target, name, value) {
        if (!target.style.setProperty) return;

        if (!value) {
            if (target.style.getPropertyValue(name)) target.style.removeProperty(name);
        } else if (target.style.getPropertyValue(name) !== value) {
            target.style.setProperty(name, value);
        }
    }

    // Everything the HUD settings left on the game's nodes and the Root page.
    function clearHud() {
        stopEdit();

        var i;
        var m;
        var nodes = document.querySelectorAll('[' + PART_MARK + '],.status-panel');

        for (i = 0; i < nodes.length; i++) {
            for (m = 0; m < ROW_MARKS.length; m++) setValue(nodes[i], ROW_MARKS[m], null);
            for (m = 0; m < PANEL_MARKS.length; m++) setValue(nodes[i], PANEL_MARKS[m], null);

            setStyle(nodes[i], 'transform', '');
            setStyle(nodes[i], 'transformOrigin', '');
            setVar(nodes[i], '--vs-fav', '');
        }

        var bars = document.querySelectorAll('[data-vs-warn],[data-vs-act]');

        for (i = 0; i < bars.length; i++) {
            for (m = 0; m < BAR_MARKS.length; m++) setValue(bars[i], BAR_MARKS[m], null);
        }

        var badges = document.querySelectorAll('.buff');

        for (i = 0; i < badges.length; i++) {
            for (m = 0; m < BADGE_MARKS.length; m++) setValue(badges[i], BADGE_MARKS[m], null);

            setStyle(badges[i], 'order', '');
        }

        unwatchExplore();

        var top = topDocument();

        removeById(top, EXPLORE_ID);
        removeById(top, EXPLORE_STYLE_ID);
        hudKey = '';
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
        drawFitnessBar(ownChild(row, 'progress-bar-wrapper'), reading[0], reading[1], reading[2] === 1);

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

    function drawFitnessBar(bar, value, max, isOther) {
        var text = say();
        var level = fitnessLevel(value);
        var fill = bar.querySelector('.progress-fill');
        var tip = ownChild(bar, 'status-tooltip');
        var about = ownChild(tip, 'vs-fit-about');
        var state = level === 'weak' ? [text.weakened, 'vs-bad']
            : level === 'low' ? [text.nearWeakened(value - WEAKENED_BELOW + 1), 'vs-warn']
            : [text.aboveWeakened(value - WEAKENED_BELOW)];

        // The game's colors, and the pulsing red it gives a bar that is nearly empty for Weakened.
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
        // Shown whatever the tooltip settings: with the old mod in too, the player sees two bars and should know why.
        drawBlock(tip, 'vs-fit-dup', 'div', 'st-body vs-fit-dup', isOther ? [[text.otherFitnessBar, 'vs-warn']] : [], ownChild(tip, 'vs-st') || about);
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
                setMark(badges[i], ENDING, isTimed && isEnding(id, copy, now));
            }

            drawTray(container, badges, now);
        }

        for (var k in lengths) {
            if (!seen[k]) delete lengths[k];
        }
    }

    // BuffEndingPulse: a good buff, or with BuffEndingDebuffs any buff, with no more than the set time left.
    function isEnding(id, copy, now) {
        var ending = held.ending;

        if (!ending || copy[1] - now > ending[0]) return false;

        return ending[1] === 1 || (!!held.good && held.good.indexOf(id) >= 0);
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
        var host = hostOf(event.target);

        if (host !== fitHost) {
            fitHost = host;

            if (host) fitSoon(host);
        }

        var badge = badgeOf(event.target);

        if (!badge || badge === hovered) return;

        hovered = badge;

        if (held) drawBuffTip(badge, statNames(attrList(), ICONS.length));
    }

    // The game fills a buff's hover box after the mouse reaches it, so the box is drawn and measured again a frame later.
    function fitSoon(host) {
        fitTip(tipOfHost(host));

        if (typeof requestAnimationFrame !== 'function') return;

        requestAnimationFrame(function () {
            if (host !== fitHost) return;

            if (held && host === hovered) drawBuffTip(host, statNames(attrList(), ICONS.length));

            fitTip(tipOfHost(host));
        });
    }

    // A stat bar, a multiplier chip or a buff badge in the stat panel, the node itself or one above it.
    function hostOf(node) {
        for (var depth = 0; node && depth < BADGE_DEPTH; depth++, node = node.parentElement) {
            if (!node.classList) continue;

            if (node.classList.contains('buff') || node.classList.contains('progress-bar-wrapper') || node.classList.contains('vs-chip')) return node;

            if (node.classList.contains('status-panel')) return null;
        }

        return null;
    }

    function tipOfHost(host) {
        if (host.classList.contains('vs-chip')) return ownChild(host, 'vs-ct');

        return ownChild(host, host.classList.contains('buff') ? 'buff-tooltip' : 'status-tooltip');
    }

    // Moves a hover box up or down by as little as keeps it inside the window, its top first when it is taller than
    // the window. The box is measured in the window's pixels and moved in its own, which differ by every zoom above it.
    function fitTip(tip) {
        if (!tip || typeof tip.getBoundingClientRect !== 'function') return;

        var box = tip.getBoundingClientRect();
        var scale = tip.offsetHeight > 0 ? box.height / tip.offsetHeight : 0;
        var room = window.innerHeight;

        if (!(scale > 0) || !(room > 0)) return;

        var shift = tip.__vsShift || 0;
        var top = box.top + shift * scale;
        var up = Math.max(0, top + box.height - (room - EDGE));

        if (top - up < EDGE) up = top - EDGE;

        var moved = Math.round(up / scale);

        if (moved === shift) return;

        var arrowRoom = Math.max(0, tip.offsetHeight / 2 - ARROW_ROOM);
        var arrow = Math.round(Math.max(-arrowRoom, Math.min(arrowRoom, moved)));

        tip.__vsShift = moved;
        setStyle(tip, 'marginTop', moved ? -moved + 'px' : '');
        setStyle(tip, 'marginBottom', moved ? moved + 'px' : '');

        if (tip.style.setProperty) tip.style.setProperty('--vs-arrow', arrow + 'px');
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
        drawBlock(tip, 'vs-bd', 'div', 'vs-bd', sentenceRows(ownChild(tip, 'bt-body')), null);
        setOwnText(tip, 'vs-btime', 'span', 'vs-btime', isTimed ? duration(shown.copy[1] - clock) : '');
        setMark(tip, 'data-vs-tip', true);
        setMark(tip, 'data-vs-timed', isTimed);

        var block = ownChild(tip, 'vs-bt');

        if (block) setClass(block, 'vs-bt-timed', isTimed);
    }

    // The game's description a sentence to a row. A sentence ends at . ! ? before a space, or at a Chinese 。！？.
    function sentenceRows(body) {
        var text = body ? String(body.textContent || '') : '';
        var parts = text.replace(/([.!?])\s+/g, '$1\n').replace(/([\u3002\uff01\uff1f])\s*/g, '$1\n').split('\n');
        var rows = [];

        for (var i = 0; i < parts.length; i++) {
            var part = parts[i].trim();

            if (part) rows.push([part]);
        }

        return rows;
    }

    function undrawBuffTip(tip) {
        drawBlock(tip, 'vs-bt', 'div', 'vs-bt', [], null);
        drawBlock(tip, 'vs-bd', 'div', 'vs-bd', [], null);
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

        return rows.concat(hudBuffRows(shown.id));
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
            if (tip) drawBlock(tip, 'vs-weight-tip', 'div', 'vs-weight-tip', []);

            return;
        }

        var carried = weight[0];
        var max = weight[1];
        var line = weight[2];
        var isOver = line > 0 && carried > line;
        var isNear = !isOver && line > 0 && carried >= line * NEAR_OVERWEIGHT;

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

        if (tip) drawBlock(tip, 'vs-weight-tip', 'div', 'vs-weight-tip', weightRows(weight, data.names.overweight, data.heavy || [], isOver, isNear));
    }

    // The Inventory hover box: what you carry against the most you can carry, then the room left before Overweight or
    // how far past it you are, then the heaviest items, each name with its weight on the right.
    function weightRows(weight, name, heavy, isOver, isNear) {
        var text = say();
        var carried = weight[0];
        var line = weight[2];
        var rows = [{ k: text.carrying, v: text.load(kgUp(carried), kgDown(weight[1])) }];

        if (isOver) rows.push({ k: name, v: text.over(kgUp(carried - line)), c: 'vs-bad' });
        else if (line > 0) rows.push({ k: text.room(name), v: text.kg(kgDown(line - carried)), c: isNear ? 'vs-warn' : '' });

        if (heavy.length) rows.push([text.heaviest, 'vs-head']);

        for (var h = 0; h < heavy.length; h++) rows.push({ k: text.heavy(heavy[h][0], heavy[h][2]), v: text.kg(kgUp(heavy[h][1])) });

        return rows;
    }

    // The Inventory button: the HUD tool whose hover title is the game's word for the bag.
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
        clearHud();
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

                unfitTip(tips[h]);
            }
        }

        var zoom = document.getElementById(ZOOM_ID);

        if (zoom && zoom.parentElement) zoom.parentElement.removeChild(zoom);

        var badges = document.querySelectorAll('.buff');

        for (var b = 0; b < badges.length; b++) setMark(badges[b], ENDING, false);

        var root = document.documentElement;

        for (var l = 0; root && l < LOOK_MARKS.length; l++) setValue(root, LOOK_MARKS[l], null);

        samples = {};
        lengths = {};
        drawnTip = null;
        fitHost = null;
    }

    function unfitTip(tip) {
        if (!tip.__vsShift) return;

        tip.__vsShift = 0;
        setStyle(tip, 'marginTop', '');
        setStyle(tip, 'marginBottom', '');

        if (tip.style.removeProperty) tip.style.removeProperty('--vs-arrow');
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

    // An attribute with a value; null takes it away. Written only when it changed.
    function setValue(target, name, value) {
        if (value === null) {
            if (target.hasAttribute(name)) target.removeAttribute(name);
        } else if (target.getAttribute(name) !== value) {
            target.setAttribute(name, value);
        }
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
    document.addEventListener('click', onClick, true);
    document.addEventListener('mousedown', onDown, true);
    document.addEventListener('mousemove', onMove, true);
    document.addEventListener('mouseup', onUp, true);
    document.addEventListener('wheel', onWheel, { capture: true, passive: false });

    window.__slvitals = { apply: apply, texts: TEXTS, edit: edit };
})();
