using System.Collections.Generic;
using System.Linq;
using Jint;
using VitalSigns.Core;
using VitalSigns.Core.Acts;
using Xunit;

namespace VitalSigns.Tests;

/// <summary>
/// The Actions tab: the settings it may write, what the plugin sends it, the scaling behind the numbers it shows, and
/// actions.js exactly as the plugin embeds it, drawn into a bare element that only keeps its HTML.
/// </summary>
public class ActionsTabTests
{
    private static readonly TabSetting Flag = new("Actions/Toasts", TabValueKind.Flag);
    private static readonly TabSetting Whole = new("Actions/FoodAbovePercent", TabValueKind.Whole, 0, 100);
    private static readonly TabSetting Goal = new("Actions/FoodGoal", TabValueKind.Choice, Choices: FoodPlan.Goals);

    [Fact]
    public void A_setting_takes_only_values_of_its_kind_and_writes_them_as_the_config_does()
    {
        Assert.Equal("true", Flag.Normal("1"));
        Assert.Equal("false", Flag.Normal("false"));
        Assert.Null(Flag.Normal("yes"));

        Assert.Equal("100", Whole.Normal("250"));
        Assert.Equal("0", Whole.Normal("-3"));
        Assert.Equal("13", Whole.Normal("12.6"));
        Assert.Null(Whole.Normal("NaN"));

        Assert.Equal("WellFed", Goal.Normal("wellfed"));
        Assert.Null(Goal.Normal("Lots"));
    }

    [Fact]
    public void Keys_and_bed_modes_are_cleaned_and_auto_beds_drop_out()
    {
        Assert.Equal("i12,f7", new TabSetting("Actions/MoraleRoutine", TabValueKind.Keys).Normal(" 12 , f7 , junk "));
        Assert.Equal("10:Nap,9076:Sleep", new TabSetting("Actions/BedModes", TabValueKind.BedModes).Normal("9076:Sleep,10:Nap,11:Auto"));
        Assert.Null(Flag.Normal(new string('1', TabSetting.MostTextLength + 1)));
    }

    [Fact]
    public void Food_gains_take_the_food_type_ratio_then_the_stat_gain_ratio_and_losses_the_loss_ratio()
    {
        var ratios = new[] { new StatRatios(0.5, 0, 0, 0), new StatRatios(0, 1, 0, 0), new StatRatios(0, 0, 0, 0), new StatRatios(0, 0, 0, 0) };
        var scale = new StatScale(ratios, new[] { new FoodMod(0, 3, 0.2) }, 0.25);

        var meat = scale.Item(new StatDelta(10, -2, 0, 4), 3);

        Assert.Equal(10 * 1.2 * 1.5, meat.Satiety, 6);
        Assert.Equal(-4, meat.Morale, 6);
        Assert.Equal(5, meat.Life, 6);

        // Another food type gets no food ratio, only the stat's.
        Assert.Equal(15, scale.Item(new StatDelta(10, 0, 0, 0), 1).Satiety, 6);
    }

    [Fact]
    public void The_stat_difficulty_scales_what_a_plan_counts_on_as_it_scales_the_change_in_game()
    {
        // Satiety gains twice, Morale falls three times as fast, Life gains nothing: a plan must count on that, or a
        // click to the maximum eats twice what fits.
        var difficulty = new Difficulty(new[] { 1.0, 3, 1, 1 }, new[] { 2.0, 1, 1, 0 });
        var scale = StatScale.None with { Difficulty = difficulty };

        var meal = scale.Item(new StatDelta(10, -2, 0, 4), 3);

        Assert.Equal(20, meal.Satiety, 6);
        Assert.Equal(-6, meal.Morale, 6);
        Assert.Equal(0, meal.Life, 6);

        var bread = new ActOption(ActKeys.Item(5), ActKind.Item, 5, "Bread", "", new StatDelta(10, 0, 0, 0), ActState.Ready, Count: 1, InBag: 1);
        var view = ViewScale.Of(new BaseView(new[] { bread }, new ActOption[0], new Bed[0], true, 1), scale, new Dictionary<int, int>(), new Dictionary<int, int>());

        Assert.Equal(20, Assert.Single(view.Items).Delta.Satiety, 6);
    }

    [Fact]
    public void Uses_waiting_in_the_queue_come_off_the_bag_first_and_an_item_with_none_left_drops_out()
    {
        var bread = new ActOption(ActKeys.Item(5), ActKind.Item, 5, "Bread", "", new StatDelta(10, 0, 0, 0), ActState.Ready, Count: 3, InBag: 2);
        var tea = new ActOption(ActKeys.Item(6), ActKind.Item, 6, "Tea", "", new StatDelta(0, 5, 0, 0), ActState.Ready, Count: 1, InBag: 1);
        var raw = new BaseView(new[] { bread, tea }, new ActOption[0], new Bed[0], true, 1);

        var view = ViewScale.Of(raw, StatScale.None, new Dictionary<int, int>(), new Dictionary<int, int> { [5] = 1, [6] = 1 });

        var left = Assert.Single(view.Items);
        Assert.Equal("i5", left.Key);
        Assert.Equal(2, left.Count);
        Assert.Equal(1, left.InBag);
    }

    [Fact]
    public void Best_morale_ranks_the_ones_raising_another_stat_first_and_puts_life_costs_last()
    {
        var plain = new ActOption(ActKeys.Furniture(1), ActKind.Furniture, 1, "Read", "", new StatDelta(0, 20, 0, 0), ActState.Ready);
        var both = new ActOption(ActKeys.Furniture(2), ActKind.Furniture, 2, "Snack", "", new StatDelta(5, 8, 0, 0), ActState.Ready);
        var hurts = new ActOption(ActKeys.Furniture(3), ActKind.Furniture, 3, "Brawl", "", new StatDelta(0, 30, 0, -5), ActState.Ready);

        var ranked = MoralePlan.Ranked(new[] { plain, both, hurts }, avoidLifeLoss: true).Select(o => o.Key).ToList();

        Assert.Equal(new[] { "f2", "f1", "f3" }, ranked);
    }

    [Fact]
    public void The_tab_messages_parse_into_the_commands_the_plugin_handles()
    {
        Assert.IsType<HudCommand.TabLoad>(HudCommand.Parse(HudCommand.TabPrefix + "load"));
        Assert.Equal(new HudCommand.Shown(false), HudCommand.Parse(HudCommand.TabPrefix + "show|0"));
        Assert.Equal(new HudCommand.Edit(true), HudCommand.Parse(HudCommand.TabPrefix + "edit|1"));
        Assert.Equal(new HudCommand.Set("Actions/LifeItems", "i1|x"), HudCommand.Parse(HudCommand.TabPrefix + "set|Actions/LifeItems|i1|x"));
        Assert.Equal(new HudCommand.Layout("abc", 2, true), HudCommand.Parse(HudCommand.HudPrefix + "layout|abc|2|1"));
        Assert.IsType<HudCommand.Unknown>(HudCommand.Parse(HudCommand.HudPrefix + "layout|abc|9|1"));
    }

    [Fact]
    public void The_data_call_carries_the_settings_the_stats_and_greyed_out_items_and_stays_inert_in_a_script()
    {
        var json = ActionsPage.DataJson(View("Bread </script>"));

        Assert.Contains("\"Actions/FoodGoal\":\"Max\"", json);
        Assert.Contains("\"can\":true", json);
        Assert.DoesNotContain("</script>", json);
        Assert.Contains("\"st\":2", json);
    }

    [Fact]
    public void The_tab_goes_on_the_vital_signs_page_and_is_added_once()
    {
        var tab = new TabScript();

        Assert.Equal("com.dev.survivallog.vitalsigns", tab.Text("window.__modmanagerTabs[0].guid"));
        Assert.Equal("actions", tab.Text("window.__modmanagerTabs[0].id"));

        tab.Reinstall();

        Assert.Equal(1, tab.Number("window.__modmanagerTabs.length"));
    }

    [Fact]
    public void Opening_the_tab_asks_for_its_data_and_closing_it_says_so()
    {
        var tab = new TabScript();

        tab.Show();
        tab.Run("window.__modmanagerTabs[0].hide()");

        Assert.IsType<HudCommand.TabLoad>(HudCommand.Parse(tab.Text("sent[0]")));
        Assert.Equal(new HudCommand.Shown(false), HudCommand.Parse(tab.Text("sent[1]")));
    }

    [Fact]
    public void Every_panel_draws_what_the_plugin_sent_and_greys_out_what_the_base_lacks()
    {
        var tab = new TabScript();

        tab.Show();
        tab.Run(ActionsPage.DataCall(View("Bread")));

        Assert.Contains("Bread", tab.Html());
        Assert.Contains("is-missing", tab.Html());
        Assert.Contains("Greyed out", tab.Text("footer"));

        for (var stat = 1; stat < 4; stat++)
        {
            tab.Click("stat", "", stat.ToString());

            Assert.Contains(ActTexts.StatName(stat, false), tab.Html());
        }

        tab.Click("stat", "", "2");

        Assert.Contains("Wooden Bed", tab.Html());
    }

    [Fact]
    public void A_click_on_a_switch_saves_a_value_the_setting_takes()
    {
        var tab = new TabScript();

        tab.Show();
        tab.Run(ActionsPage.DataCall(View("Bread")));
        tab.Click("flag", "Actions/Toasts", "");

        var set = Assert.IsType<HudCommand.Set>(HudCommand.Parse(tab.Text("sent[sent.length-1]")));

        Assert.Equal("Actions/Toasts", set.Key);
        Assert.Equal("false", Flag.Normal(set.Value));
        Assert.Contains("CLICK", tab.Html());
    }

    [Fact]
    public void The_layout_button_closes_the_mod_manager_and_asks_for_the_editor()
    {
        var tab = new TabScript();

        tab.Show();
        tab.Run(ActionsPage.DataCall(View("Bread")));
        tab.Click("edit", "", "");

        Assert.True(tab.Is("closed"));
        Assert.Equal(new HudCommand.Edit(true), HudCommand.Parse(tab.Text("sent[sent.length-1]")));
    }

    private static ActionsView View(string foodName)
    {
        var settings = new Dictionary<string, string>
        {
            ["Hud/ClickBars"] = "true", ["Hud/ClickCures"] = "true", ["Actions/Toasts"] = "true", ["Actions/FoodGoal"] = "Max", ["Actions/ClickSatiety"] = "true",
            ["Actions/MoraleMode"] = "Best", ["Actions/BedModes"] = "", ["Actions/LifeItems"] = "",
        };
        var bread = new ActOption(ActKeys.Item(5), ActKind.Item, 5, foodName, "../icons/bread.png", new StatDelta(10, 0, 0, 0), ActState.Ready, Count: 2, InBag: 1);
        var cake = new ActOption(ActKeys.Item(8), ActKind.Item, 8, "Cake", "", new StatDelta(20, 5, 0, 0), ActState.Missing, "none in the bag or at home");
        var bandage = new ActOption(ActKeys.Item(9), ActKind.Item, 9, "Bandage", "", new StatDelta(0, 0, 0, 15), ActState.Ready, Count: 1, InBag: 1);
        var guitar = new ActOption(ActKeys.Furniture(40), ActKind.Furniture, 40, "Play", "", new StatDelta(0, 12, -3, 0), ActState.Ready, Place: "Guitar");
        var sleep = new ActOption(ActKeys.Furniture(70), ActKind.Furniture, 70, "Sleep", "", new StatDelta(0, 0, 60, 0), ActState.Ready, Place: "Wooden Bed");
        var bed = new Bed(1, 9076, "Wooden Bed", sleep, null, null, 60, true);
        var plans = new BarPlan?[]
        {
            new(0, ActWhy.Planned, new[] { new PlanStep(bread, 1) }, "eat Bread"), BarPlan.Nothing(1, ActWhy.NoMorale, false), null, BarPlan.Nothing(3, ActWhy.Full, false),
        };

        return new ActionsView(settings, false, true, plans, new[] { bread, cake, bandage }, new[] { guitar, sleep }, new[] { bed }, new[] { "f40" });
    }

    /// <summary>actions.js in Jint, on a page with just enough DOM to draw into: HTML is kept as a string.</summary>
    private sealed class TabScript
    {
        private const string Dom = """
            var window = this; var sent = []; var footer = ''; var closed = false;
            window.vuplex = { postMessage: function (m) { sent.push(m); } };
            window.__modmanager = { close: function () { closed = true; }, tabsChanged: function () {} };
            window.location = { href: 'https://ui/WebUI/CoreUI1/index.html' };
            var location = window.location;
            function El(tag) { this.tagName = tag; this.innerHTML = ''; this.textContent = ''; this.children = []; this.parentNode = null; this.isConnected = false; this.attrs = {}; this.listeners = {}; }
            El.prototype.appendChild = function (c) { this.children.push(c); c.parentNode = this; c.isConnected = true; return c; };
            El.prototype.addEventListener = function (n, f) { (this.listeners[n] = this.listeners[n] || []).push(f); };
            El.prototype.querySelectorAll = function () { return []; };
            El.prototype.querySelector = function () { return null; };
            El.prototype.contains = function () { return true; };
            El.prototype.getAttribute = function (n) { return Object.prototype.hasOwnProperty.call(this.attrs, n) ? this.attrs[n] : null; };
            El.prototype.closest = function () { return this; };
            var head = new El('head');
            var document = { head: head, activeElement: null, getElementById: function () { return null; }, createElement: function (t) { return new El(t); }, addEventListener: function () {} };
            var host = new El('div'); host.isConnected = true;
            var ctx = { lang: 'en', footer: function (h) { footer = h; } };
            function click(a, k, v) { var t = new El('span'); t.attrs = { 'data-a': a, 'data-k': k, 'data-v': v }; root().listeners.click.forEach(function (f) { f({ target: t }); }); }
            function root() { return host.children[0]; }
            """;

        private readonly Engine _engine = new();

        public TabScript()
        {
            _engine.Execute(Dom);
            Reinstall();
        }

        public void Reinstall() => _engine.Execute(Resource());

        public void Show() => _engine.Execute("window.__modmanagerTabs[0].show(host, ctx)");

        public void Click(string action, string key, string value) => _engine.Execute($"click('{action}', '{key}', '{value}')");

        public string Html() => Text("root().innerHTML");

        public void Run(string script) => _engine.Execute(script);

        public string Text(string expression) => _engine.Evaluate($"String({expression})").AsString();

        public bool Is(string expression) => _engine.Evaluate($"!!({expression})").AsBoolean();

        public double Number(string expression) => _engine.Evaluate($"Number({expression})").AsNumber();

        private static string Resource()
        {
            using var stream = typeof(TabScript).Assembly.GetManifestResourceStream("VitalSigns.actions.js") ?? throw new InvalidOperationException("actions.js is not embedded");
            using var reader = new StreamReader(stream);

            return reader.ReadToEnd();
        }
    }
}
