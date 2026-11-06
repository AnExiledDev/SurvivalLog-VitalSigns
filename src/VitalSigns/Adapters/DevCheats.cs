using System.ComponentModel;
using System.Linq;
using System.Text;
using VitalSigns.Core.Acts;

namespace VitalSigns.Adapters;

/// <summary>
/// Test buttons for Cheat Menu's Mod tests tab. Cheat Menu finds this class by its name, so neither mod needs the other.
/// Each method is one button: its DisplayName is the label and its Description the help, as "English\nChinese". What
/// it returns is shown in Cheat Menu's log. Cheat Menu calls them on the main thread.
/// </summary>
public static class DevCheats
{
    [DisplayName("Show the base\n查看家中物品")]
    [Description("Shows what the bar clicks can use: the items in the bag and the home storage, the furniture actions and the beds, and whether you are at home.\n显示点击属性条时可用的东西：背包和家中储物里的物品、家具行动和床，以及你是否在家。")]
    public static string ShowBase()
    {
        if (Clicks() is not { } acts) return "Vital Signs is not running";

        if (acts.IsScanning) return "still looking around the base; turn a bar click on, then try again";

        var view = acts.View;
        var text = new StringBuilder();

        text.Append(view.AtHome ? "at home" : "away from home").Append(", scan ").Append(view.Version).Append('\n');
        text.Append("items: ").Append(Count(view.Items)).Append('\n');

        foreach (var item in view.Items) text.Append("  ").Append(item.Key).Append(' ').Append(item.Name).Append(" x").Append(item.Count).Append(' ').Append(item.State).Append('\n');

        text.Append("furniture actions: ").Append(Count(view.Funcs)).Append('\n');

        foreach (var func in view.Funcs) text.Append("  ").Append(func.Key).Append(' ').Append(func.Name).Append(" (").Append(func.Place).Append(") ").Append(func.State).Append('\n');

        text.Append("beds: ").Append(view.Beds.Count).Append('\n');

        foreach (var bed in view.Beds) text.Append("  ").Append(bed.ConfigId).Append(' ').Append(bed.Name).Append(", up to ").Append(bed.Restore.ToString("0")).Append(" Stamina").Append(bed.SameFloor ? ", same floor" : "").Append('\n');

        return text.ToString().TrimEnd();
    }

    [DisplayName("Show bar clicks\n查看点击属性条")]
    [Description("Shows what a click on each bar would queue now, or why it would queue nothing.\n显示现在点击每个属性条会加入什么，或为什么不会加入。")]
    public static string ShowClicks()
    {
        if (Clicks() is not { } acts) return "Vital Signs is not running";

        var lines = acts.Plans.Select((plan, stat) => ActTexts.StatName(stat, false) + ": " + (plan == null ? "not planned yet" : plan.Why + ", " + plan.Tip));

        return string.Join("\n", lines);
    }

    [DisplayName("Look again\n重新查看")]
    [Description("Reads the base and the action queue again at once, instead of waiting for the next look.\n立即重新读取家中物品和行动队列，不用等下一次。")]
    public static string LookAgain()
    {
        if (Clicks() is not { } acts) return "Vital Signs is not running";

        BaseScan.Refresh();
        QueueReader.Refresh();
        acts.Replan();

        return "the base and the queue are read again at the next frame";
    }

    [DisplayName("Show difficulty\n查看属性难度")]
    [Description("Shows the Difficulty multipliers in use and which character they apply to.\n显示正在使用的属性难度倍数，以及它们作用于哪个角色。")]
    public static string ShowDifficulty()
    {
        var settings = DifficultyHook.Settings;
        var parts = Enumerable.Range(0, 4).Select(stat => $"{ActTexts.StatName(stat, false)} loss x{settings.Drain(stat):0.##}, gain x{settings.Gain(stat):0.##}");
        var hero = DifficultyHook.HeroId;

        return string.Join("\n", parts) + "\n" + (hero == 0 ? "no character yet: load a save" : "applies to character " + hero);
    }

    [DisplayName("Open the layout editor\n打开布局编辑器")]
    [Description("Opens the HUD layout editor, like the 「Edit HUD layout」 button in the pause menu.\n打开界面布局编辑器，与暂停菜单中的「编辑界面布局」按钮相同。")]
    public static string OpenLayoutEditor()
    {
        if (VsTicker.Page is not { } page) return "Vital Signs is not running";

        if (!page.IsOn) return "Vital Signs is turned off";

        page.Edit(true);

        return "asked the HUD to open the layout editor; close Cheat Menu to see it";
    }

    private static HudActs? Clicks() => VsTicker.Page?.Clicks;

    private static string Count(System.Collections.Generic.IReadOnlyList<ActOption> options) =>
        $"{options.Count} ({options.Count(o => o.State == ActState.Ready)} ready, {options.Count(o => o.State == ActState.Busy)} busy, {options.Count(o => o.State == ActState.Missing)} missing)";
}
