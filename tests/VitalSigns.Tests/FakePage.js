// Just enough of the CoreUI1 page for vitalsigns.js to run in Jint: elements with classes, attributes, text, style
// and a parent, the few selectors the script uses, the Vue app's state, and the HUD's stat rows, buff badges and tool
// buttons drawn the way the game's template draws them. Test code only; nothing here ships.
var window = this;

var FakeElement = class {
    constructor(tagName, className) {
        var element = this;

        this.tagName = tagName;
        this.id = '';
        this.parentElement = null;
        this.children = [];
        this.textContent = '';
        this.attrs = {};
        this.classes = className ? className.split(' ') : [];
        this.style = {};
        this.classList = {
            contains: function (name) { return element.classes.indexOf(name) >= 0; },
            add: function (name) { if (element.classes.indexOf(name) < 0) element.classes.push(name); },
            remove: function (name) { element.classes = element.classes.filter(function (c) { return c !== name; }); }
        };
    }

    get className() { return this.classes.join(' '); }

    set className(value) { this.classes = String(value).split(' ').filter(function (c) { return c; }); }

    hasAttribute(name) { return Object.prototype.hasOwnProperty.call(this.attrs, name); }

    setAttribute(name, value) { this.attrs[name] = String(value); }

    removeAttribute(name) { delete this.attrs[name]; }

    appendChild(child) {
        if (child.parentElement) child.parentElement.removeChild(child);

        child.parentElement = this;
        this.children.push(child);

        return child;
    }

    insertBefore(child, before) {
        if (!before) return this.appendChild(child);

        if (child.parentElement) child.parentElement.removeChild(child);

        var at = this.children.indexOf(before);

        if (at < 0) throw new Error('insertBefore: not a child of this element');

        child.parentElement = this;
        this.children.splice(at, 0, child);

        return child;
    }

    removeChild(child) {
        var at = this.children.indexOf(child);

        if (at < 0) throw new Error('removeChild: not a child of this element');

        this.children.splice(at, 1);
        child.parentElement = null;

        return child;
    }

    // ".a > .b" is the child combinator: the last step names this element, each step before it the parent above.
    matches(selector) {
        var steps = selector.split('>').map(function (step) { return step.trim(); });
        var element = this;

        for (var i = steps.length - 1; i >= 0; i--) {
            if (!element || !matchesStep(element, steps[i])) return false;

            element = element.parentElement;
        }

        return true;
    }

    querySelectorAll(selector) {
        return descendantsOf(this).filter(function (element) { return element.matches(selector); });
    }

    querySelector(selector) {
        return this.querySelectorAll(selector)[0] || null;
    }
};

function matchesStep(element, step) {
    if (/^\.[\w-]+$/.test(step)) return element.classList.contains(step.slice(1));

    throw new Error('the fake page does not know the selector "' + step + '"');
}

function descendantsOf(root) {
    var found = [];

    root.children.forEach(function walk(child) {
        found.push(child);
        child.children.forEach(walk);
    });

    return found;
}

// Listeners the script adds to the document, by event name; hover() hands them a mouseover on a node.
var listeners = {};

var document = {
    documentElement: new FakeElement('html'),
    addEventListener: function (name, listener) { (listeners[name] = listeners[name] || []).push(listener); },
    getElementById: function (id) {
        return descendantsOf(document.documentElement).filter(function (element) { return element.id === id; })[0] || null;
    },
    createElement: function (tagName) { return new FakeElement(tagName); },
    querySelector: function (selector) { return document.documentElement.querySelector(selector); },
    querySelectorAll: function (selector) { return document.documentElement.querySelectorAll(selector); }
};

document.head = document.documentElement.appendChild(new FakeElement('head'));
document.body = document.documentElement.appendChild(new FakeElement('body'));

// Draws the HUD as CoreUI1's template does: the stat panel with one row per attrList item (its buff badges on the
// left, the bar on the right), and the tool column with the phone and bag buttons.
// spec: { icons, buffs: [[configId, ...] per row], values: [0 to 1 per row], names: [per row], hasNoVue, hasNoBag, bagName }
function buildPage(spec) {
    var icons = spec.icons || ['\uD83C\uDF56', '\uD83E\uDDE0', '\u26A1', '\u2764'];
    var buffs = spec.buffs || [];
    var bagName = spec.bagName || 'Inventory';
    var app = document.body.appendChild(new FakeElement('div'));
    var container = app.appendChild(new FakeElement('div', 'bottom-right-container'));
    var panel = container.appendChild(new FakeElement('div', 'status-panel'));
    var attrList = icons.map(function (icon, r) {
        return { attrIcon: icon, attrName: spec.names ? spec.names[r] : undefined, value: spec.values ? spec.values[r] : 0.5, buffs: (buffs[r] || []).map(function (id) { return { buffConfigId: id }; }) };
    });

    app.id = 'app';

    if (!spec.hasNoVue) app._vnode = { component: { setupState: { state: { attrList: attrList, i18n_bag: bagName, i18n_phone: 'Phone' } } } };

    var rows = icons.map(function (icon, r) {
        var row = panel.appendChild(new FakeElement('div', 'status-row'));
        var badges = row.appendChild(new FakeElement('div', 'buff-container'));
        var bar = new FakeElement('div', 'progress-bar-wrapper');
        var mask = bar.appendChild(new FakeElement('div', 'bar-mask'));
        var tip = bar.appendChild(new FakeElement('div', 'status-tooltip'));

        (buffs[r] || []).forEach(function (id) { addBadge(badges, id); });
        row.appendChild(bar).setAttribute('data-interactive', '');
        mask.appendChild(new FakeElement('div', 'progress-fill'));
        var text = mask.appendChild(new FakeElement('div', 'progress-text'));
        var name = text.appendChild(new FakeElement('span', 'attr-name'));

        name.appendChild(new FakeElement('span')).textContent = icon;
        name.appendChild(new FakeElement('span')).textContent = 'Stat ' + r;
        tip.appendChild(new FakeElement('div', 'st-header'));
        tip.appendChild(new FakeElement('div', 'st-body'));

        return row;
    });

    var tools = app.appendChild(new FakeElement('div', 'vertical-tools'));
    var phone = toolButton(tools, 'Phone');
    var bag = spec.hasNoBag ? null : toolButton(tools, bagName);

    return { panel: panel, rows: rows, attrList: attrList, phone: phone, bag: bag };
}

function addBadge(container, id) {
    var badge = container.appendChild(new FakeElement('div', 'buff buff-pos'));

    badge.textContent = 'buff ' + id;
    var tip = badge.appendChild(new FakeElement('div', 'buff-tooltip bt-pos'));

    tip.appendChild(new FakeElement('div', 'bt-header'));
    tip.appendChild(new FakeElement('div', 'bt-body')).textContent = 'The game text.';

    return badge;
}

// A tool button with its hover box; the title carries the hotkey after the name, as the game's does.
function toolButton(tools, name) {
    var button = tools.appendChild(new FakeElement('button', 'ui-btn icon-sq interactive-btn has-entry-tip'));
    var tip = button.appendChild(new FakeElement('div', 'entry-tip'));
    var title = tip.appendChild(new FakeElement('div', 'entry-tip-title'));
    var body = tip.appendChild(new FakeElement('div', 'entry-tip-body'));

    button.setAttribute('data-interactive', '');
    title.textContent = name + 'B';
    body.textContent = 'Open the ' + name + '.';

    return button;
}

// A child of the element with the class, or null.
// A stat row's gain multiplier, which sits in the name's line after the name.
function chipOf(row) {
    return childWith(maskOf(row).querySelector('.attr-name'), 'vs-chip');
}

function childWith(element, className) {
    return element.children.filter(function (child) { return child.classList.contains(className); })[0] || null;
}

// The timer stripe on a row's nth badge, or null.
function stripeOn(row, n) {
    return childWith(row.querySelectorAll('.buff')[n], 'vs-timer');
}

function maskOf(row) {
    return row.querySelector('.bar-mask');
}

function tipOf(row) {
    return row.querySelector('.status-tooltip');
}

// A mouseover on the node, as the browser sends it to the document's listeners.
function hover(node) {
    (listeners.mouseover || []).forEach(function (listener) { listener({ target: node }); });
}

// The text of each row of our block in a tooltip, one per line, or '' when there is none.
function blockText(tip, className) {
    var block = childWith(tip, className);

    return block ? block.children.map(function (row) { return row.textContent || row.children.map(function (c) { return c.textContent; }).filter(function (t) { return t; }).join(' '); }).join('\n') : '';
}

// What Vue does to a tooltip when it shows or hides it: the whole class is written again from its template.
function vueRewritesClass(element, className) {
    element.className = className;
}

// Every game tooltip that carries one of our marks.
function markedTips() {
    return document.querySelectorAll('.buff-tooltip').concat(document.querySelectorAll('.status-tooltip')).filter(function (tip) {
        return tip.hasAttribute('data-vs-tip') || tip.hasAttribute('data-vs-timed') || tip.hasAttribute('data-vs-no-game-text');
    });
}

function buffTip(row, n) {
    return row.querySelectorAll('.buff')[n].querySelector('.buff-tooltip');
}
