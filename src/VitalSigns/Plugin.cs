using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using VitalSigns.Adapters;
using VitalSigns.Core;

namespace VitalSigns;

[BepInPlugin(Guid, "Vital Signs", Version)]
public sealed class Plugin : BasePlugin
{
    private const string Guid = "com.dev.survivallog.vitalsigns";
    private const string Version = "0.4.0";

    public override void Load()
    {
        var isEnabled = Config.Bind("VitalSigns", "Enabled", true, new ConfigDescription(
            "Show more on the HUD: how each stat's gains and losses are scaled, shading for the losses still to come, the Well Fed lines on the Satiety bar, a timer stripe on each buff that ends, buff effects and cures in the tooltips, and the weight you carry on the Inventory button. Off: the mod does nothing. Takes effect at the next game start.\n" +
            "在屏幕上显示更多信息：各项属性的增减倍率、即将扣除数值的阴影、「饱腹」条上的「饱腹富余」界线、每个有时限的状态上的计时条、提示中的状态效果与解药，以及「背包」按钮上的当前负重。关闭后本模组不做任何改动。下次启动游戏时生效。", null, "ModManager.Restart")).Value;
        var log = new VsLog(Log, Path.Combine(Paths.BepInExRootPath, "vital-signs.log"));

        if (!isEnabled)
        {
            log.Write($"===== Vital Signs {Version} started: off, VitalSigns/Enabled is false");

            return;
        }

        log.Write($"===== Vital Signs {Version} started");

        VsTicker.Log = log.Write;
        VsTicker.Page = new VsPage(log.Write, ReadOptions());

        AddComponent<VsTicker>();
    }

    private Options ReadOptions()
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

        var barSize = Config.Bind("Display", "BarSize", BarSize.Default, new ConfigDescription(
            "How big the Satiety, Morale, Stamina and Life bars are drawn, in percent of the game's size, from 50 to 150. Their hover boxes grow and shrink with them. 100 is the game's size.\n" +
            "「饱腹」「心情」「体力」「生命」属性条的显示大小，为游戏原尺寸的百分比，范围 50 到 150。属性条的悬停提示会一起缩放。100 为游戏原尺寸。",
            new AcceptableValueRange<int>(BarSize.Min, BarSize.Max), "ModManager.Restart")).Value;
        var buffSize = Config.Bind("Display", "BuffSize", BarSize.Default, new ConfigDescription(
            "How big the buffs next to the bars are drawn, in percent of the game's size, from 50 to 150. Their hover boxes grow and shrink with them. At the same number as BarSize they sit in proportion to the bars. 100 is the game's size.\n" +
            "属性条旁状态的显示大小，为游戏原尺寸的百分比，范围 50 到 150。状态的悬停提示会一起缩放。与 BarSize 数值相同时，状态与属性条比例协调。100 为游戏原尺寸。",
            new AcceptableValueRange<int>(BarSize.Min, BarSize.Max), "ModManager.Restart")).Value;

        return new Options(weight, stripes, tooltips, gameText, cures, barSize, buffSize);

        bool Bind(string key, string english, string chinese) =>
            Config.Bind("Display", key, true, new ConfigDescription(english + "\n" + chinese, null, "ModManager.Restart")).Value;
    }
}
