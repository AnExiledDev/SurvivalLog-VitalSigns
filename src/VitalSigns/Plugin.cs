using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using VitalSigns.Adapters;
using VitalSigns.Core;
using VitalSigns.Timers.Adapters;
using VitalSigns.Timers.Core;

namespace VitalSigns;

[BepInPlugin(Guid, "Vital Signs", Version)]
[BepInDependency(OptionalLive.ToolkitGuid, OptionalLive.ToolkitVersion)]
[BepInDependency(ModManagerGuid, BepInDependency.DependencyFlags.SoftDependency)]
public sealed class Plugin : BasePlugin
{
    private const string Guid = "com.dev.survivallog.vitalsigns";
    private const string Version = "0.7.1";
    private const string FitnessBarGuid = "com.dev.survivallog.fitnessbar";
    private const string ModManagerGuid = "com.dev.survivallog.modmanager";
    private const string TimersFileName = "VitalSigns.Timers.json";

    // BuffDisplayType 0 to 6. The game has no names for these; they are ours, from the buffs in each.
    private static readonly (string Key, string English, string Chinese)[] Categories =
    {
        ("Calm", "calm buffs (Tranquility, Steady Focus)", "平静类状态（如「宁静」）"),
        ("FoodAndSickness", "food and sickness buffs (Satiety, sweets, poisoning, diarrhea)", "饮食与疾病类状态（如「饱腹」、甜食、中毒、腹泻）"),
        ("Mood", "mood buffs (Vim and Vigor, Wish Fulfilled, Lonely)", "心态类状态（如「孤独」）"),
        ("EnergyAndSleep", "energy and sleep buffs (Sweet Dreams, Sore Muscles, Energy Drink)", "精力与睡眠类状态（如「肌肉酸痛」、「功能饮料-兴奋」）"),
        ("ExerciseAndWeather", "exercise and weather buffs (Athlete, Vigorous Exercise, Drowned Rat)", "运动与天气类状态（如「活力运动」、「暴雨潮湿」）"),
        ("HealthAndInjury", "health and injury buffs (Bleeding, Severely Infected, Wind Chill)", "健康与伤病类状态（如「受伤流血」、「重度感染」、「风寒」）"),
        ("Exploration", "exploration buffs (Knuckle Down, Rest and Recovery, Fine Haul)", "探索类状态（如「休养生息」）"),
    };

    public override void Load()
    {
        var enabled = Config.Bind("VitalSigns", "Enabled", true, new ConfigDescription(
            "Show more on the HUD: how each stat's gains and losses are scaled, shading for the losses still to come, the Well Fed lines on the Satiety bar, a timer stripe on each buff that ends, buff effects and cures in the tooltips, the weight you carry on the Inventory button and a Fitness bar under the Life bar. Also applies the buff time multipliers and shows the low stat warnings. Off: everything the mod draws goes away, no warnings show and every buff goes back to the game's own time.\n" +
            "在屏幕上显示更多信息：各项属性的增减倍率、即将扣除数值的阴影、「饱腹」条上的「饱腹富余」界线、每个有时限的状态上的计时条、提示中的状态效果与解药，「背包」按钮上的当前负重，以及「生命」条下方的健康条。同时应用状态持续时间倍数并显示属性偏低提醒。关闭后本模组显示的内容全部消失，不再提醒，所有状态恢复游戏原本的时间。"));
        var log = new VsLog(Log, Path.Combine(Paths.BepInExRootPath, "vital-signs.log"));

        log.Write($"===== Vital Signs {Version} started" + (enabled.Value ? "" : ": off, VitalSigns/Enabled is false"));

        var page = new VsPage(log.Write, () => OtherFitnessBar.Draws(
            IL2CPPChainloader.Instance.Plugins.TryGetValue(FitnessBarGuid, out var other) ? other.Metadata.Version?.ToString() : null));
        var display = DisplayEntries();
        var alerts = AlertEntries();

        OptionalLive.Watch(() => page.Options = ReadOptions(display), display.All);
        OptionalLive.Watch(() => page.Alerts = ReadAlerts(alerts), alerts.All);

        VsTicker.Log = log.Write;
        VsTicker.Page = page;
        AddComponent<VsTicker>();

        var settings = StartTimers(enabled.Value, log.Write);
        var wasOn = (bool?)null;

        OptionalLive.Watch(enabled, isOn =>
        {
            page.IsOn = isOn;
            TabTicker.IsOn = isOn;

            if (settings != null)
            {
                settings.IsOn = isOn;

                // The multipliers watch applies the plan at start; only a change made later needs it again.
                if (wasOn != null && wasOn != isOn) settings.Apply();
            }

            if (wasOn != null && wasOn != isOn) log.Write(isOn ? "turned on" : "turned off: the HUD goes back to the game's own, and every buff to its own time");

            wasOn = isOn;
        });
    }

    /// <summary>
    /// The buff time multipliers and their Timers tab. If the buff hooks cannot all go in, the HUD part still runs and
    /// this returns null. The hooks go in even with the mod off: they then leave every time as the game's, and turning
    /// the mod on needs no restart.
    /// </summary>
    private Multipliers? StartTimers(bool isOn, System.Action<string> log)
    {
        var good = Global("GlobalGood", "good buffs", "增益状态");
        var bad = Global("GlobalBad", "bad buffs (debuffs)", "负面状态");
        var categories = Categories.Select(c => Category(c.Key, c.English, c.Chinese)).ToList();
        var settings = new Multipliers(good, bad, categories, Path.Combine(Paths.ConfigPath, TimersFileName), log);

        // Its own Harmony id: if the buff hooks cannot all go in, they take out only themselves.
        if (!BuffHooks.Apply(new Harmony(Guid + ".buffs"), log)) return null;

        settings.IsOn = isOn;
        OptionalLive.Watch(settings.Apply, settings.Entries.ToArray());
        PageMessages.Apply(new Harmony(Guid + ".timers"), log);
        TabTicker.Page = new WebPage(log);
        TabTicker.Settings = settings;
        TabTicker.Log = log;
        AddComponent<TabTicker>();

        return settings;
    }

    private ConfigEntry<float> Global(string key, string english, string chinese) =>
        Config.Bind("Timers", key, (float)Limits.Default, new ConfigDescription(
            $"Multiplies how long {english} last. 1 is the game's time, 2 doubles it, 0.5 halves it. A category or buff given its own multiplier uses that instead. Buffs already running change too.\n" +
            $"{chinese}的持续时间乘以这个倍数。1 为游戏原本的时间，2 为两倍，0.5 为一半。设置了自己倍数的类别或状态改用自己的倍数。正在生效的状态也会随之改变。",
            new AcceptableValueRange<float>((float)Limits.Min, (float)Limits.Max)));

    private ConfigEntry<float> Category(string key, string english, string chinese) =>
        Config.Bind("TimerCategories", key, (float)Limits.UseAbove, new ConfigDescription(
            $"Multiplies how long {english} last, in place of GlobalGood and GlobalBad. 0 uses the global multipliers. A buff given its own multiplier uses that instead.\n" +
            $"{chinese}的持续时间乘以这个倍数，取代全局倍数。0 为使用全局倍数。设置了自己倍数的状态改用自己的倍数。",
            new AcceptableValueRange<float>((float)Limits.UseAbove, (float)Limits.Max)));

    private sealed record DisplaySettings(ConfigEntry<bool> Weight, ConfigEntry<bool> Stripes, ConfigEntry<bool> Tooltips, ConfigEntry<bool> GameText,
        ConfigEntry<bool> Cures, ConfigEntry<bool> Fitness, ConfigEntry<int> BarSize, ConfigEntry<int> BuffSize, ConfigEntry<int> TooltipSize)
    {
        public ConfigEntryBase[] All => new ConfigEntryBase[] { Weight, Stripes, Tooltips, GameText, Cures, Fitness, BarSize, BuffSize, TooltipSize };
    }

    private sealed record AlertSettings(ConfigEntry<bool> Satiety, ConfigEntry<bool> Morale, ConfigEntry<bool> Stamina, ConfigEntry<bool> Life, ConfigEntry<bool> Fitness,
        ConfigEntry<int> LowAt)
    {
        public ConfigEntryBase[] All => new ConfigEntryBase[] { Satiety, Morale, Stamina, Life, Fitness, LowAt };
    }

    private static Options ReadOptions(DisplaySettings d) => new(d.Weight.Value, d.Stripes.Value, d.Tooltips.Value, d.GameText.Value, d.Cures.Value,
        d.BarSize.Value, d.BuffSize.Value, d.Fitness.Value, d.TooltipSize.Value);

    private static AlertOptions ReadAlerts(AlertSettings a) => new(a.Satiety.Value, a.Morale.Value, a.Stamina.Value, a.Life.Value, a.Fitness.Value,
        System.Math.Clamp(a.LowAt.Value, AlertOptions.MinLowAt, AlertOptions.MaxLowAt));

    private DisplaySettings DisplayEntries()
    {
        var weight = Bind("WeightReadout",
            "Show the weight you carry on the Inventory button, with a bar that marks where Overweight starts.",
            "在「背包」按钮上显示当前负重，并用横条标出「超重」的起点。");
        var stripes = Bind("TimeStripes",
            "Show a stripe under each buff that ends, shrinking as its time runs out. It turns amber in the last 15%.",
            "在每个有时限的状态下方显示计时条，随剩余时间缩短，最后15%变为琥珀色。");
        var tooltips = Bind("ExtendedTooltips",
            "Add details to the stat and buff tooltips: effect values, time left, layers, modifiers, food modifiers and the losses still to come.",
            "在属性和状态的提示中加入详细信息：效果数值、剩余时间、层数、倍率、食物倍率以及即将扣除的数值。");
        var gameText = Bind("StatTooltipGameText",
            "Keep the game's own description at the bottom of the stat tooltips. Only applies when ExtendedTooltips is on.",
            "在属性提示底部保留游戏原有的说明。仅在开启 ExtendedTooltips 时生效。");
        var cures = Bind("CuresInTooltips",
            "List the items that cure or suppress a debuff in its tooltip, with how many of each you have. Only applies when ExtendedTooltips is on.",
            "在负面状态的提示中列出可以治愈或压制它的物品，以及你拥有的数量。仅在开启 ExtendedTooltips 时生效。");
        var fitness = Bind("FitnessBar",
            "Show a Fitness bar under the Life bar, with a hover box that says what Fitness does and how close you are to Weakened.",
            "在「生命」条下方显示健康条，悬停提示说明健康的作用以及距离「亚健康」还有多远。");

        var barSize = Config.Bind("Display", "BarSize", Core.BarSize.Default, new ConfigDescription(
            "How big the Satiety, Morale, Stamina, Life and Fitness bars are drawn, in percent of the game's size, from 50 to 150. 100 is the game's size. Their hover boxes have their own size, BarTooltipSize.\n" +
            "「饱腹」「心情」「体力」「生命」和健康条的显示大小，为游戏原尺寸的百分比，范围 50 到 150。100 为游戏原尺寸。属性条的悬停提示大小由 BarTooltipSize 单独设置。",
            new AcceptableValueRange<int>(Core.BarSize.Min, Core.BarSize.Max)));
        var buffSize = Config.Bind("Display", "BuffSize", Core.BarSize.Default, new ConfigDescription(
            "How big the buffs next to the bars are drawn, in percent of the game's size, from 50 to 150. Their hover boxes grow and shrink with them. At the same number as BarSize they sit in proportion to the bars. 100 is the game's size.\n" +
            "属性条旁状态的显示大小，为游戏原尺寸的百分比，范围 50 到 150。状态的悬停提示会一起缩放。与 BarSize 数值相同时，状态与属性条比例协调。100 为游戏原尺寸。",
            new AcceptableValueRange<int>(Core.BarSize.Min, Core.BarSize.Max)));
        var tooltipSize = Config.Bind("Display", "BarTooltipSize", Core.BarSize.TooltipDefault, new ConfigDescription(
            "How big the hover boxes of the Satiety, Morale, Stamina, Life and Fitness bars are drawn, in percent of the game's size, from 50 to 150, whatever BarSize is. 100 is the game's size.\n" +
            "「饱腹」「心情」「体力」「生命」和健康条悬停提示的显示大小，为游戏原尺寸的百分比，范围 50 到 150，不受 BarSize 影响。100 为游戏原尺寸。",
            new AcceptableValueRange<int>(Core.BarSize.Min, Core.BarSize.Max)));

        return new DisplaySettings(weight, stripes, tooltips, gameText, cures, fitness, barSize, buffSize, tooltipSize);

        ConfigEntry<bool> Bind(string key, string english, string chinese) =>
            Config.Bind("Display", key, true, new ConfigDescription(english + "\n" + chinese));
    }

    private AlertSettings AlertEntries()
    {
        var satiety = Stat("Satiety", "饱腹");
        var morale = Stat("Morale", "心情");
        var stamina = Stat("Stamina", "体力");
        var life = Stat("Life", "生命");
        var fitness = Config.Bind("Alerts", "Fitness", true, new ConfigDescription(
            "Show a warning when Fitness drops below 40, close to Weakened, and again below 30, where you become Weakened.\n" +
            "健康降到40以下（接近「亚健康」）时提醒，降到30以下（陷入「亚健康」）时再次提醒。"));
        var lowAt = Config.Bind("Alerts", "LowAt", AlertOptions.DefaultLowAt, new ConfigDescription(
            "How low Satiety, Morale, Stamina or Life gets before it warns, in percent of the stat's maximum, from 5 to 90. 20 is where the game turns the bar red. A stat warns again only after it climbs 5 back over this.\n" +
            "「饱腹」「心情」「体力」或「生命」降到多少时提醒，为该属性上限的百分比，范围 5 到 90。20 是游戏把属性条变红的位置。属性回升超过这个值 5 以上后才会再次提醒。",
            new AcceptableValueRange<int>(AlertOptions.MinLowAt, AlertOptions.MaxLowAt)));

        return new AlertSettings(satiety, morale, stamina, life, fitness, lowAt);

        ConfigEntry<bool> Stat(string key, string chinese) =>
            Config.Bind("Alerts", key, true, new ConfigDescription(
                $"Show a warning when {key} drops to LowAt percent of its maximum.\n" +
                $"「{chinese}」降到上限的 LowAt 百分比时显示提醒。"));
    }
}
