using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using ExiledToolkit;
using ExiledToolkit.Game;
using ExiledToolkit.Kit;
using ExiledToolkit.Web;
using HarmonyLib;
using UnityEngine;
using VitalSigns.Adapters;
using VitalSigns.Core;
using VitalSigns.Core.Acts;
using VitalSigns.Timers.Adapters;
using VitalSigns.Timers.Core;

namespace VitalSigns;

[BepInPlugin(Guid, "Vital Signs", Version)]
[BepInDependency(ToolkitGuid, ToolkitVersion)]
[BepInDependency(ModManagerGuid, BepInDependency.DependencyFlags.SoftDependency)]
public sealed class Plugin : BasePlugin
{
    private const string Guid = "com.dev.survivallog.vitalsigns";
    private const string Version = "0.12.3";

    /// <summary>Exiled's Toolkit: its shared helpers, toasts, live settings and typing check all come from it.</summary>
    private const string ToolkitGuid = "com.dev.survivallog.exiledtoolkit";
    private const string ToolkitVersion = ">=0.20.0";
    private const string FitnessBarGuid = "com.dev.survivallog.fitnessbar";
    private const string ModManagerGuid = "com.dev.survivallog.modmanager";
    private const string TimersFileName = "VitalSigns.Timers.json";
    private const string ListTag = "ModManager.Control=List";

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
            "Show more on the HUD: how each stat's gains and losses are scaled, shading for the losses still to come, the Well Fed lines on the Satiety bar, a timer stripe on each buff that ends, buff effects and cures in the tooltips, the weight you carry on the Inventory button and a Fitness bar under the Life bar. Also applies the buff time multipliers and shows the low stat warnings. Off: everything the mod draws goes away, no warnings show and every buff goes back to the game's time.\n" +
            "在屏幕上显示更多信息：各项属性的增减倍率、即将扣除数值的阴影、「饱腹」条上的「饱腹富余」界线、每个有时限的状态上的计时条、提示中的状态效果与解药，「背包」按钮上的当前负重，以及「生命」条下方的健康条。同时应用状态持续时间倍数并显示属性偏低提醒。关闭后本模组显示的内容全部消失，不再提醒，所有状态恢复游戏原本的时间。"));
        var log = new ModLog(Log, "vital-signs.log");

        log.Write($"===== Vital Signs {Version} started" + (enabled.Value ? "" : ": off, VitalSigns/Enabled is false"));

        var page = new VsPage(log.Write, () => OtherFitnessBar.Draws(
            IL2CPPChainloader.Instance.Plugins.TryGetValue(FitnessBarGuid, out var other) ? other.Metadata.Version?.ToString() : null));
        var display = DisplayEntries();
        var alerts = AlertEntries();
        var hud = HudEntries();
        var acts = ActEntries();
        var difficulty = DifficultyEntries();

        ModManagerAnswers.Log = log.Write;
        Holdings.Log = log.Write;
        ModManagerAnswers.SavedBedModes = () => acts.BedModes.Value;
        page.Favourite = id => hud.Favourites.Value = HudChoices.Toggle(hud.Favourites.Value, id);
        page.SaveLayout = saved =>
        {
            hud.Layout.Value = HudLayout.Parse(saved.Parts) is { } layout ? HudLayout.Format(layout) : "";
            hud.Tray.Value = HudChoices.Trays[Math.Clamp(saved.Tray, 0, HudChoices.Trays.Length - 1)];
            hud.Collapse.Value = saved.Collapse;
            log.Write("HUD layout saved");
        };

        LiveSettings.Watch(() => page.Options = ReadOptions(display), display.All);
        LiveSettings.Watch(() => page.Alerts = ReadAlerts(alerts), alerts.All);
        LiveSettings.Watch(() => page.Hud = ReadHud(hud, alerts.LowAt.Value), hud.All.Append(alerts.LowAt).ToArray());
        LiveSettings.Watch(() => page.Acts = ReadActs(acts), acts.All);

        VsTicker.Log = log.Write;
        VsTicker.Page = page;
        VsTicker.Hud = hud;
        VsTicker.Messages = PageInbox.Listen(HudCommand.HudPrefix);
        AddComponent<VsTicker>();

        var settings = StartTimers(enabled.Value, log.Write);
        var wasOn = (bool?)null;

        LiveSettings.Watch(enabled, isOn =>
        {
            page.IsOn = isOn;

            if (settings != null)
            {
                settings.IsOn = isOn;

                // The multipliers watch applies the plan at start; only a change made later needs it again.
                if (wasOn != null && wasOn != isOn) settings.Apply();
            }

            if (wasOn != null && wasOn != isOn) log.Write(isOn ? "turned on" : "turned off: the HUD goes back to the game's, and every buff to its own time");

            wasOn = isOn;
        });

        // Off, every stat changes by the game's numbers again. The hook goes in only once a multiplier is set.
        var difficultyHooks = new Harmony(Guid + ".difficulty");
        var difficultyGate = new DifficultyHookGate(() => DifficultyHook.Apply(difficultyHooks, log.Write));

        AddLayoutButton(page, enabled);

        LiveSettings.Watch(() =>
        {
            var wanted = enabled.Value ? ReadDifficulty(difficulty) : Difficulty.Off;

            difficultyGate.Want(wanted);

            DifficultyHook.Settings = wanted;
        }, difficulty.All.Append(enabled).ToArray());
    }

    /// <summary>
    /// The pause menu's Edit HUD layout button, while the mod is on and a run is loaded: it closes the menu and opens
    /// the editor. EditLayoutKey stays unbound by default, since this button opens it.
    /// </summary>
    private static void AddLayoutButton(VsPage page, ConfigEntry<bool> enabled)
    {
        GameMenu.Add(new GameMenuButton("vitalsigns.editlayout", "Edit HUD layout", GameMenuPlace.PauseBottom, () =>
        {
            GameCore.HotUpdate.ReduxUI.Ac_OutSetting_Close.SendAction();
            page.Edit(true);
        })
        {
            ChineseLabel = "编辑界面布局",
            Order = 300,
            IsShown = () => enabled.Value && GameSingleton.Existing<GameCore.HotUpdate.Battle.Logic.BattleLogicWorld>() != null,
        });
    }

    /// <summary>
    /// The buff time multipliers. If the buff hooks cannot all go in, the HUD part still runs and this returns null.
    /// The hooks go in even with the mod off: they then leave every time as the game's, and turning the mod on needs no
    /// restart.
    /// </summary>
    private Multipliers? StartTimers(bool isOn, System.Action<string> log)
    {
        var good = Global("GlobalGood", "good buffs", "增益状态");
        var bad = Global("GlobalBad", "bad buffs (debuffs)", "负面状态");
        var categories = Categories.Select(c => Category(c.Key, c.English, c.Chinese)).ToList();
        var buffs = Config.Bind("Timers", "Buffs", "", new ConfigDescription(
            FormattableString.Invariant($"Buffs with a multiplier of their own, in place of the category and global ones, as buff id:multiplier split by commas (200810:2). Each multiplier goes from {Limits.Min} to {Limits.Max}. Buffs already running change too.\n") +
            FormattableString.Invariant($"有自己倍数的状态，取代类别倍数和全局倍数，格式为 状态编号:倍数，用逗号分隔（如 200810:2）。每个倍数范围 {Limits.Min} 到 {Limits.Max}。正在生效的状态也会随之改变。"),
            null, ListTag, FormattableString.Invariant($"ModManager.EntryRange={Limits.Min}|{Limits.Max}")));

        MoveBuffFile(buffs, Path.Combine(Paths.ConfigPath, TimersFileName), log);

        var settings = new Multipliers(good, bad, categories, buffs, log);

        // Its own Harmony id: if the buff hooks cannot all go in, they take out only themselves.
        if (!BuffHooks.Apply(new Harmony(Guid + ".buffs"), log)) return null;

        settings.IsOn = isOn;
        LiveSettings.Watch(settings.Apply, settings.Entries.ToArray());

        return settings;
    }

    /// <summary>
    /// Earlier versions kept the per-buff multipliers in their own file. Once, while the Buffs setting is blank, they
    /// move into it, and the file is renamed so it is kept but not read again.
    /// </summary>
    private static void MoveBuffFile(ConfigEntry<string> buffs, string path, System.Action<string> log)
    {
        if (!File.Exists(path)) return;

        try
        {
            var read = OverrideFile.Read(File.ReadAllText(path));

            foreach (var problem in read.Problems) log("per-buff file: " + problem);

            if (buffs.Value.Trim().Length == 0 && read.Buffs.Count > 0) buffs.Value = BuffList.Format(read.Buffs);

            File.Move(path, path + ".old", overwrite: true);
            log($"moved {read.Buffs.Count} per-buff multipliers from {TimersFileName} into Timers Buffs; the file is kept as {TimersFileName}.old");
        }
        catch (Exception ex)
        {
            log($"could not move {TimersFileName} into Timers Buffs, it is left as it was: " + ex.Message);
        }
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
        ConfigEntry<bool> Cures, ConfigEntry<bool> Fitness, ConfigEntry<int> BarSize, ConfigEntry<int> BuffSize, ConfigEntry<int> TooltipSize,
        ConfigEntry<int> HeavyItems, ConfigEntry<bool> Trends, ConfigEntry<bool> MatchHud, ConfigEntry<string> BarLook, ConfigEntry<bool> ColourBlind,
        ConfigEntry<bool> EndingPulse, ConfigEntry<int> EndingMinutes, ConfigEntry<bool> EndingDebuffs)
    {
        public ConfigEntryBase[] All => new ConfigEntryBase[]
        {
            Weight, Stripes, Tooltips, GameText, Cures, Fitness, BarSize, BuffSize, TooltipSize, HeavyItems, Trends, MatchHud, BarLook, ColourBlind, EndingPulse,
            EndingMinutes, EndingDebuffs
        };
    }

    private sealed record AlertSettings(ConfigEntry<bool> Satiety, ConfigEntry<bool> Morale, ConfigEntry<bool> Stamina, ConfigEntry<bool> Life, ConfigEntry<bool> Fitness,
        ConfigEntry<int> LowAt)
    {
        public ConfigEntryBase[] All => new ConfigEntryBase[] { Satiety, Morale, Stamina, Life, Fitness, LowAt };
    }

    /// <summary>The HUD section: what the bars and the buff tray look like and what clicks on them do.</summary>
    internal sealed record HudSettings(ConfigEntry<bool> HideFull, ConfigEntry<int> HideAbove, ConfigEntry<bool> KeepWithBuffs, ConfigEntry<bool> WarnPulse,
        ConfigEntry<string> Sort, ConfigEntry<string> Group, ConfigEntry<string> Favourites, ConfigEntry<int> FavouriteSize, ConfigEntry<bool> Collapse,
        ConfigEntry<string> Layout, ConfigEntry<string> Tray, ConfigEntry<string> Explore, ConfigEntry<int> ExploreSize, ConfigEntry<bool> Projection,
        ConfigEntry<bool> ClickBars, ConfigEntry<bool> Cures, ConfigEntry<KeyCode> EditKey)
    {
        public ConfigEntryBase[] All => new ConfigEntryBase[]
        {
            HideFull, HideAbove, KeepWithBuffs, WarnPulse, Sort, Group, Favourites, FavouriteSize, Collapse, Layout, Tray, Explore, ExploreSize, Projection, ClickBars,
            Cures
        };
    }

    private sealed record ActSettings(ConfigEntry<bool> Satiety, ConfigEntry<bool> Morale, ConfigEntry<bool> Stamina, ConfigEntry<bool> Life, ConfigEntry<bool> UseHome,
        ConfigEntry<bool> AddToQueue, ConfigEntry<bool> Toasts, ConfigEntry<string> FoodGoal, ConfigEntry<int> FoodAbove, ConfigEntry<int> FoodMostItems,
        ConfigEntry<string> FoodSkip, ConfigEntry<bool> FoodAvoidMoraleLoss, ConfigEntry<string> BedModes, ConfigEntry<int> NapBelow, ConfigEntry<string> MoraleMode,
        ConfigEntry<string> Routine, ConfigEntry<bool> MoraleItems, ConfigEntry<bool> AvoidLifeLoss, ConfigEntry<string> LifeItems, ConfigEntry<int> LifeMostItems)
    {
        public ConfigEntryBase[] All => new ConfigEntryBase[]
        {
            Satiety, Morale, Stamina, Life, UseHome, AddToQueue, Toasts, FoodGoal, FoodAbove, FoodMostItems, FoodSkip, FoodAvoidMoraleLoss, BedModes, NapBelow,
            MoraleMode, Routine, MoraleItems, AvoidLifeLoss, LifeItems, LifeMostItems
        };
    }

    private sealed record DifficultySettings(ConfigEntry<float>[] Drain, ConfigEntry<float>[] Gain)
    {
        public ConfigEntryBase[] All => Drain.Concat<ConfigEntryBase>(Gain).ToArray();
    }

    private static HudOptions ReadHud(HudSettings h, int warnAt) => new(h.HideFull.Value, Math.Clamp(h.HideAbove.Value, HudChoices.MinHideAbove, HudChoices.MaxHideAbove),
        h.KeepWithBuffs.Value, h.WarnPulse.Value, Math.Clamp(warnAt, AlertOptions.MinLowAt, AlertOptions.MaxLowAt), h.Sort.Value, h.Group.Value,
        HudChoices.Ids(h.Favourites.Value), Math.Clamp(h.FavouriteSize.Value, HudChoices.MinFavouriteSize, HudChoices.MaxFavouriteSize), h.Collapse.Value,
        HudLayout.Parse(h.Layout.Value), h.Tray.Value, h.Explore.Value, Math.Clamp(h.ExploreSize.Value, HudChoices.MinExploreSize, HudChoices.MaxExploreSize),
        h.Projection.Value, h.ClickBars.Value, h.Cures.Value);

    private static ActOptions ReadActs(ActSettings a) => new(a.Satiety.Value, a.Morale.Value, a.Stamina.Value, a.Life.Value, a.UseHome.Value, a.AddToQueue.Value,
        a.Toasts.Value, a.FoodGoal.Value, Math.Clamp(a.FoodAbove.Value, FoodPlan.MinAbove, FoodPlan.MaxAbove),
        Math.Clamp(a.FoodMostItems.Value, FoodPlan.MinMostItems, FoodPlan.MaxMostItems), ActKeys.Parse(a.FoodSkip.Value), a.FoodAvoidMoraleLoss.Value, a.BedModes.Value,
        Math.Clamp(a.NapBelow.Value, SleepPlan.MinNapBelow, SleepPlan.MaxNapBelow), a.MoraleMode.Value, ActKeys.Parse(a.Routine.Value), a.MoraleItems.Value,
        a.AvoidLifeLoss.Value, LifeKeys(a.LifeItems.Value), Math.Clamp(a.LifeMostItems.Value, LifePlan.MinMostItems, LifePlan.MaxMostItems));

    /// <summary>Blank leaves the choice to the mod (null: every Life item); anything else is the list itself.</summary>
    private static IReadOnlyList<string>? LifeKeys(string text) => text.Trim().Length == 0 ? null : ActKeys.Parse(text);

    private static Difficulty ReadDifficulty(DifficultySettings d) => new(d.Drain.Select(e => (double)e.Value).ToArray(), d.Gain.Select(e => (double)e.Value).ToArray());

    private static Options ReadOptions(DisplaySettings d) => new(d.Weight.Value, d.Stripes.Value, d.Tooltips.Value, d.GameText.Value, d.Cures.Value,
        d.BarSize.Value, d.BuffSize.Value, d.Fitness.Value, d.TooltipSize.Value, Math.Clamp(d.HeavyItems.Value, Heaviest.Min, Heaviest.Max), d.Trends.Value,
        d.MatchHud.Value, d.BarLook.Value, d.ColourBlind.Value, d.EndingPulse.Value, Math.Clamp(d.EndingMinutes.Value, Ending.MinMinutes, Ending.MaxMinutes),
        d.EndingDebuffs.Value);

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
            "Keep the game's description at the bottom of the stat tooltips. Only applies when ExtendedTooltips is on.",
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

        var heavyItems = Config.Bind("Display", "HeaviestItems", Heaviest.Default, new ConfigDescription(
            "How many of the heaviest items in your bag the Inventory button's hover box lists, from 0 to 10, with their weight. Stacks of the same item count as one. 0 lists none. Only applies when WeightReadout is on.\n" +
            "「背包」按钮的悬停提示中列出背包里最重的几种物品及其重量，范围 0 到 10。同一物品的多组算作一种。0 为不列出。仅在开启 WeightReadout 时生效。",
            new AcceptableValueRange<int>(Heaviest.Min, Heaviest.Max)));
        var trends = Bind("TrendArrows",
            "Show an arrow on each stat bar when the stat has risen or fallen over the last game hour.",
            "属性在过去一小时（游戏时间）内上升或下降时，在属性条上显示箭头。");
        var matchHud = Bind("MatchHudStyle",
            "Draw the hover boxes, the multiplier chips and the bar frames in the colours of the HUD style chosen in Exiled's Toolkit (HudStyle). With the classic style they keep this mod's own colours.",
            "按照 Exiled's Toolkit 中选择的界面风格（HudStyle）的配色绘制悬停提示、倍率标签和属性条边框。使用经典风格时，保持本模组自己的配色。");
        var barLook = Config.Bind("Display", "BarLook", BarLook.Game, new ConfigDescription(
            "How the stat bars are drawn. Game: the game's look. Flat: no frame or shadow. Segmented: split into tenths, so you can read the level at a glance.\n" +
            "属性条的样式。Game：游戏原本的样式。Flat：无边框、无阴影。Segmented：分成十格，一眼就能看出数值。",
            new AcceptableValueList<string>(BarLook.Choices)));
        var colourBlind = Config.Bind("Display", "ColourBlind", false, new ConfigDescription(
            "Use colours that stay apart for red-green colour blindness: blue in place of green and orange in place of red, on the bars, the buffs, the multiplier chips and the hover boxes.\n" +
            "使用红绿色盲也能分辨的配色：在属性条、状态、倍率标签和悬停提示中，用蓝色代替绿色，用橙色代替红色。"));

        var endingPulse = Config.Bind("Alerts", "BuffEndingPulse", true, new ConfigDescription(
            "Make a good buff pulse when it is about to end, so you can renew it in time. How soon is BuffEndingMinutes.\n" +
            "增益状态即将结束时闪烁提示，方便及时续上。提前多久由 BuffEndingMinutes 决定。"));
        var endingMinutes = Config.Bind("Alerts", "BuffEndingMinutes", Ending.DefaultMinutes, new ConfigDescription(
            "How many game minutes before a buff ends it starts to pulse, from 5 to 720. 60 is one game hour.\n" +
            "状态结束前多少分钟（游戏时间）开始闪烁，范围 5 到 720。60 为游戏中的一小时。",
            new AcceptableValueRange<int>(Ending.MinMinutes, Ending.MaxMinutes)));
        var endingDebuffs = Config.Bind("Alerts", "BuffEndingDebuffs", false, new ConfigDescription(
            "Make debuffs pulse too when they are about to end. Only applies when BuffEndingPulse is on.\n" +
            "负面状态即将结束时也闪烁提示。仅在开启 BuffEndingPulse 时生效。"));

        return new DisplaySettings(weight, stripes, tooltips, gameText, cures, fitness, barSize, buffSize, tooltipSize, heavyItems, trends, matchHud, barLook,
            colourBlind, endingPulse, endingMinutes, endingDebuffs);

        ConfigEntry<bool> Bind(string key, string english, string chinese) =>
            Config.Bind("Display", key, true, new ConfigDescription(english + "\n" + chinese));
    }

    private HudSettings HudEntries()
    {
        var hideFull = Config.Bind("Hud", "HideFullBars", false, new ConfigDescription(
            "Hide a stat bar while it is full, or above HideAbove, so the HUD only shows what needs you. It comes back as soon as the stat drops.\n" +
            "属性条满了（或高于 HideAbove）时隐藏，界面上只留需要注意的。属性一下降就重新出现。"));
        var hideAbove = Config.Bind("Hud", "HideAbove", HudChoices.MaxHideAbove, new ConfigDescription(
            "Above what level, in percent of the maximum, a bar hides, from 50 to 100. 100 hides only a full bar. Only applies when HideFullBars is on.\n" +
            "属性高于上限的多少百分比时隐藏，范围 50 到 100。100 为只在满时隐藏。仅在开启 HideFullBars 时生效。",
            new AcceptableValueRange<int>(HudChoices.MinHideAbove, HudChoices.MaxHideAbove)));
        var keepWithBuffs = Config.Bind("Hud", "KeepBarsWithBuffs", true, new ConfigDescription(
            "Keep a full bar showing while it has buffs next to it, so they stay where you look for them. Only applies when HideFullBars is on.\n" +
            "属性条旁有状态时，即使满了也保持显示，让状态留在原来的位置。仅在开启 HideFullBars 时生效。"));
        var warnPulse = Config.Bind("Hud", "WarnPulse", true, new ConfigDescription(
            "Make a stat bar pulse once it drops to the warning line (Alerts LowAt), before the game turns it red.\n" +
            "属性降到提醒线（Alerts 中的 LowAt）时，属性条开始闪烁，早于游戏把它变红。"));
        var sort = Config.Bind("Hud", "BuffSort", HudChoices.SortGame, new ConfigDescription(
            "The order of the buffs next to the bars. Game: the game's order. EndingFirst: the one that ends soonest first. LongestFirst: the one with the most time left first. Buffs without a timer go last.\n" +
            "属性条旁状态的顺序。Game：游戏原本的顺序。EndingFirst：最快结束的在前。LongestFirst：剩余时间最长的在前。没有时限的状态排在最后。",
            new AcceptableValueList<string>(HudChoices.Sorts)));
        var group = Config.Bind("Hud", "BuffGroup", HudChoices.GroupMixed, new ConfigDescription(
            "Whether good buffs and debuffs are kept apart. Mixed: together. BuffsFirst: good buffs first. DebuffsFirst: debuffs first.\n" +
            "增益状态和负面状态是否分开。Mixed：混在一起。BuffsFirst：增益在前。DebuffsFirst：负面在前。",
            new AcceptableValueList<string>(HudChoices.Groups)));
        var favourites = Config.Bind("Hud", "FavouriteBuffs", "", new ConfigDescription(
            "Buffs pinned to the front of their row and drawn larger, as buff ids split by commas. Shift-click a buff on the HUD to pin it or unpin it.\n" +
            "固定在所在行最前面并放大显示的状态，填写状态编号，用逗号分隔。在界面上按住 Shift 点击状态即可固定或取消固定。", null, ListTag));
        var favouriteSize = Config.Bind("Hud", "FavouriteSize", HudChoices.DefaultFavouriteSize, new ConfigDescription(
            "How big a pinned buff is drawn, in percent of the other buffs, from 100 to 200.\n" +
            "固定状态的显示大小，为其他状态的百分比，范围 100 到 200。",
            new AcceptableValueRange<int>(HudChoices.MinFavouriteSize, HudChoices.MaxFavouriteSize)));
        var collapse = Config.Bind("Hud", "CollapseBuffs", false, new ConfigDescription(
            "Fold each row's buffs into one badge with their count; hover over it to see them all. Pinned buffs stay out.\n" +
            "把每行的状态收成一个带数量的徽标，悬停即可看到全部。固定的状态仍单独显示。"));
        var layout = Config.Bind("Hud", "Layout", "", new ConfigDescription(
            "Where each bar sits and how big, as the layout editor saves it. Open the editor with the 「Edit HUD layout」 button in the pause menu, or EditLayoutKey. Empty: the game's places.\n" +
            "各属性条的位置和大小，由布局编辑器保存。用暂停菜单中的「编辑界面布局」按钮或 EditLayoutKey 打开编辑器。留空为游戏原本的位置。"));
        var tray = Config.Bind("Hud", "BuffTray", HudChoices.TrayLeft, new ConfigDescription(
            "Which side of the bars the buffs sit on. Left is the game's place. Right, Above and Below move them.\n" +
            "状态显示在属性条的哪一侧。Left 为游戏原本的位置。Right、Above、Below 分别移到右侧、上方、下方。",
            new AcceptableValueList<string>(HudChoices.Trays)));
        var explore = Config.Bind("Hud", "ExploreBars", HudChoices.CornerTopRight, new ConfigDescription(
            "Show small Satiety, Morale, Stamina and Life bars in a corner of the screen while you explore, where the game hides its own. Off hides them.\n" +
            "外出探索时（游戏会隐藏属性条），在屏幕一角显示小号的「饱腹」「心情」「体力」「生命」条。Off 为不显示。",
            new AcceptableValueList<string>(HudChoices.Corners)));
        var exploreSize = Config.Bind("Hud", "ExploreBarsSize", 100, new ConfigDescription(
            "How big the exploring bars are drawn, in percent, from 50 to 150.\n" +
            "探索时属性条的显示大小，为百分比，范围 50 到 150。",
            new AcceptableValueRange<int>(HudChoices.MinExploreSize, HudChoices.MaxExploreSize)));
        var projection = Config.Bind("Hud", "QueueProjection", true, new ConfigDescription(
            "Mark on each bar where the stat will be once the actions in the queue are done.\n" +
            "在属性条上标出行动队列全部完成后属性会到达的位置。"));
        var clickBars = Config.Bind("Hud", "ClickBars", false, new ConfigDescription(
            "Click a stat bar to queue what raises it: eat, do something for Morale, sleep in the best bed, or use a Life item. What each click does is set in the Actions section.\n" +
            "点击属性条，把提升它的行动加入队列：吃东西、做提升心情的事、在最好的床上睡觉，或使用恢复生命的物品。每次点击做什么，在 Actions 部分设置。"));
        var cures = Config.Bind("Hud", "ClickCures", false, new ConfigDescription(
            "Click a debuff to queue its cure: one in the bag first, then one in the home storage. A debuff with a cure at hand is marked.\n" +
            "点击负面状态，把它的解药加入队列：先用背包里的，再用家中储物里的。身边有解药的负面状态会有标记。"));
        var editKey = Config.Bind("Hud", "EditLayoutKey", KeyCode.None, new ConfigDescription(
            "The key that opens and closes the HUD layout editor. None: open it with the 「Edit HUD layout」 button in the pause menu. Ignored while you type in a text box.\n" +
            "打开和关闭界面布局编辑器的按键。None 为不设按键，用暂停菜单中的「编辑界面布局」按钮打开。在文本框中输入时无效。"));

        return new HudSettings(hideFull, hideAbove, keepWithBuffs, warnPulse, sort, group, favourites, favouriteSize, collapse, layout, tray, explore, exploreSize,
            projection, clickBars, cures, editKey);
    }

    private ActSettings ActEntries()
    {
        var satiety = Click("ClickSatiety", "Satiety", "饱腹", "eats food that raises it, as FoodGoal says", "吃能提升它的食物，按 FoodGoal 的设置");
        var morale = Click("ClickMorale", "Morale", "心情", "does something that raises it, as MoraleMode says", "做能提升它的事，按 MoraleMode 的设置");
        var stamina = Click("ClickStamina", "Stamina", "体力", "rests in the best bed at home, as BedModes says", "在家中最好的床上休息，按 BedModes 的设置");
        var life = Click("ClickLife", "Life", "生命", "uses the items in LifeItems", "使用 LifeItems 中的物品");
        var useHome = Flag("UseHomeStorage", true,
            "Let clicks use items from the home storage too while you are at home, not only the bag.",
            "在家时，点击也可以使用家中储物里的物品，而不只是背包里的。");
        var addToQueue = Flag("AddToQueue", true,
            "Add a click's actions after the ones already queued. Off: they replace the queue.",
            "点击的行动排在已有行动之后。关闭后会替换整个队列。");
        var toasts = Flag("Toasts", true,
            "Say in a toast what a click queued, or why it queued nothing.",
            "用提示说明点击加入了什么，或为什么没有加入。");
        var foodGoal = Choice("FoodGoal", FoodPlan.GoalMax, FoodPlan.Goals,
            "How far a click on Satiety eats. Max: to the maximum. WellFed: to Well Fed. AboveWellFed: FoodAbovePercent past Well Fed. Food that would go past the maximum is never eaten.",
            "点击「饱腹」吃到多少。Max：吃到最满。WellFed：吃到「饱食」。AboveWellFed：比「饱食」再高 FoodAbovePercent。会超出上限的食物不会吃。");
        var foodAbove = Whole("FoodAbovePercent", FoodPlan.DefaultAbove, FoodPlan.MinAbove, FoodPlan.MaxAbove,
            "How far past Well Fed AboveWellFed eats, in percent of the maximum, from 0 to 100.",
            "AboveWellFed 比「饱食」再高多少，为上限的百分比，范围 0 到 100。");
        var foodMost = Whole("FoodMostItems", FoodPlan.DefaultMostItems, FoodPlan.MinMostItems, FoodPlan.MaxMostItems,
            "The most food one click on Satiety eats, from 1 to 10.",
            "每次点击「饱腹」最多吃几件食物，范围 1 到 10。");
        var foodSkip = Keys("FoodSkip",
            "Food a click never eats, as i and an item id, or g and a kind of food for all of that kind, split by commas (i1203,g1002).",
            "点击时不吃的食物，填写 i 加物品编号，或 g 加食物种类表示这一类全部，用逗号分隔（如 i1203,g1002）。");
        var foodAvoid = Flag("FoodAvoidMoraleLoss", false,
            "Leave out food that lowers Morale.",
            "不吃会降低心情的食物。");
        var bedModes = Config.Bind("Actions", "BedModes", "", new ConfigDescription(
            "How a click on Stamina uses each bed type, as type:mode split by commas (20001:Sleep). Auto naps when only NapBelow percent or less is missing and sleeps otherwise; Sleep and Nap always do that; Skip never uses that bed. A bed type not listed is Auto.\n" +
            "点击「体力」时如何使用每种床，格式为 类型:方式，用逗号分隔（如 20001:Sleep）。Auto：只缺 NapBelow 百分比或更少时小睡，否则睡觉；Sleep 和 Nap 总是睡觉或小睡；Skip 不用这种床。未列出的床为 Auto。",
            null, ListTag, "ModManager.EntryChoices=" + string.Join("|", SleepPlan.Modes), "ModManager.ChoiceZh=Auto=自动", "ModManager.ChoiceZh=Sleep=睡觉",
            "ModManager.ChoiceZh=Nap=小睡", "ModManager.ChoiceZh=Skip=不用"));
        var napBelow = Whole("NapBelow", SleepPlan.DefaultNapBelow, SleepPlan.MinNapBelow, SleepPlan.MaxNapBelow,
            "With a bed on Auto, nap when this much Stamina or less is missing, in percent of the maximum, from 0 to 100; sleep when more is.",
            "床为 Auto 时，缺少的体力不超过上限的这个百分比就小睡，否则睡觉，范围 0 到 100。");
        var moraleMode = Choice("MoraleMode", MoralePlan.ModeBest, MoralePlan.Modes,
            "What a click on Morale queues. Best: one action, those that raise another stat too first, then the biggest gain; clicking again while it is queued takes the next one. Routine: every action of MoraleRoutine that can be done now, in that order.",
            "点击「心情」加入什么。Best：一个行动，同时提升其他属性的优先，然后是提升最多的；已在队列中时再次点击选下一个。Routine：按顺序加入 MoraleRoutine 中现在能做的全部行动。");
        var routine = Keys("MoraleRoutine",
            "Your Morale routine, in order: f and a furniture action id, i and an item id, or g and a kind of item for the one of that kind you have that gives the most Morale, split by commas (f12,i1203,g3).",
            "你的心情日常安排，按顺序：f 加家具行动编号，i 加物品编号，或 g 加物品种类表示你拥有的这一类中提升心情最多的一个，用逗号分隔（如 f12,i1203,g3）。");
        var moraleItems = Flag("MoraleItems", false,
            "Let Best use items that raise Morale too, not only furniture.",
            "Best 也可以使用提升心情的物品，而不只是家具。");
        var avoidLife = Flag("AvoidLifeLoss", true,
            "Leave out Morale actions that cost Life.",
            "不做会减少生命的心情行动。");
        var lifeItems = Keys("LifeItems",
            "The items a click on Life uses, in order, as i and an item id, or g and a kind of item for every one of that kind you have, the most Life first, split by commas (i1203,g900). Empty: any item that restores Life, smallest first.",
            "点击「生命」时使用的物品，按顺序填写 i 加物品编号，或 g 加物品种类表示你拥有的这一类全部、恢复生命多的先用，用逗号分隔（如 i1203,g900）。留空为使用任何恢复生命的物品，先用小的。");
        var lifeMost = Whole("LifeMostItems", LifePlan.DefaultMostItems, LifePlan.MinMostItems, LifePlan.MaxMostItems,
            "The most items one click on Life uses, from 1 to 10. It stops once Life is full.",
            "每次点击「生命」最多使用几件物品，范围 1 到 10。生命满了就停止。");

        return new ActSettings(satiety, morale, stamina, life, useHome, addToQueue, toasts, foodGoal, foodAbove, foodMost, foodSkip, foodAvoid, bedModes, napBelow,
            moraleMode, routine, moraleItems, avoidLife, lifeItems, lifeMost);

        ConfigEntry<bool> Click(string key, string stat, string chinese, string english, string chineseDoes) =>
            Flag(key, true, $"A click on the {stat} bar {english}.", $"点击「{chinese}」条时{chineseDoes}。");

        ConfigEntry<bool> Flag(string key, bool value, string english, string chinese)
        {
            return Config.Bind("Actions", key, value, new ConfigDescription(english + "\n" + chinese));
        }

        ConfigEntry<int> Whole(string key, int value, int min, int max, string english, string chinese)
        {
            return Config.Bind("Actions", key, value, new ConfigDescription(english + "\n" + chinese, new AcceptableValueRange<int>(min, max)));
        }

        ConfigEntry<string> Choice(string key, string value, string[] choices, string english, string chinese)
        {
            return Config.Bind("Actions", key, value, new ConfigDescription(english + "\n" + chinese, new AcceptableValueList<string>(choices)));
        }

        // A list of i and f keys. Bare item ids, which earlier versions took, are written with their i once at start, and
        // anything that is not a key goes (0.10.0 saved an emptied Life list as "none", which is now just blank).
        ConfigEntry<string> Keys(string key, string english, string chinese)
        {
            var entry = Config.Bind("Actions", key, "", new ConfigDescription(english + "\n" + chinese, null, ListTag));
            var text = entry.Value.Trim();

            if (text.Length > 0 && ActKeys.Join(ActKeys.Parse(text)) is var keys && keys != text) entry.Value = keys;

            return entry;
        }
    }

    private DifficultySettings DifficultyEntries()
    {
        var stats = new[] { ("Satiety", "饱腹"), ("Morale", "心情"), ("Stamina", "体力"), ("Life", "生命") };
        var drain = stats.Select(s => Bind(s.Item1 + "Drain", "every loss of " + s.Item1, $"「{s.Item2}」的每次减少",
            "2 makes it fall twice as fast, 0 stops it falling", "2 为减少速度翻倍，0 为不再减少")).ToArray();
        var gain = stats.Select(s => Bind(s.Item1 + "Gain", "every gain of " + s.Item1, $"「{s.Item2}」的每次增加",
            "2 makes food, rest and the rest raise it twice as much, 0 stops it rising", "2 为食物、休息等的提升翻倍，0 为不再增加")).ToArray();

        return new DifficultySettings(drain, gain);

        ConfigEntry<float> Bind(string key, string english, string chinese, string englishExample, string chineseExample) =>
            Config.Bind("Difficulty", key, (float)Difficulty.Default, new ConfigDescription(
                $"Multiplies {english} you get, from 0 to 5: the slow change over time, food, rest, buffs and furniture alike. 1 is the game's; {englishExample}. Only you, not other characters.\n" +
                $"玩家{chinese}乘以这个倍数，范围 0 到 5，包括随时间的变化、食物、休息、状态和家具。1 为游戏原本的数值；{chineseExample}。只影响你自己，不影响其他角色。",
                new AcceptableValueRange<float>((float)Difficulty.Min, (float)Difficulty.Max)));
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
