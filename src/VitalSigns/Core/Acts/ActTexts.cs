using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace VitalSigns.Core.Acts;

/// <summary>Why a bar's click has nothing to queue, or Planned when it has.</summary>
public enum ActWhy
{
    Planned,
    Off,
    Scanning,
    NoPlayer,
    Full,
    AtGoal,
    NoFood,
    TooBig,
    NoMorale,
    RoutineEmpty,
    RoutineNotReady,
    NoBed,
    BedNotReady,
    NoLifeItems,
    QueueBusy,
}

/// <summary>
/// The words a bar's hover box and the toasts use for what a click does. The HUD puts "Click: " in front of a tip;
/// the toasts use the tip as their body. Names come from the game in its own language.
/// </summary>
public static class ActTexts
{
    public const string Source = "Vital Signs";

    /// <summary>What a click on the bar does now, or why it does nothing.</summary>
    public static string Tip(int stat, ActWhy why, IReadOnlyList<PlanStep> steps, bool isChinese) =>
        why == ActWhy.Planned ? Planned(stat, steps, isChinese) : Reason(stat, why, isChinese);

    public static string Planned(int stat, IReadOnlyList<PlanStep> steps, bool isChinese)
    {
        var list = List(steps, isChinese);

        return stat switch
        {
            0 => isChinese ? "吃" + list : "eat " + list,
            3 => isChinese ? "使用" + list : "use " + list,
            _ => list,
        };
    }

    public static string Reason(int stat, ActWhy why, bool isChinese)
    {
        var name = StatName(stat, isChinese);

        return why switch
        {
            ActWhy.Off => isChinese ? "已在 Vital Signs 的 Actions 设置中关闭" : "turned off in the Vital Signs Actions settings",
            ActWhy.Scanning => isChinese ? "正在查看家中物品..." : "looking around the base...",
            ActWhy.NoPlayer => isChinese ? "现在无法行动" : "nothing can be done right now",
            ActWhy.Full => isChinese ? "「" + name + "」已满" : name + " is full",
            ActWhy.AtGoal => isChinese ? "已达到目标" : "already at your goal",
            ActWhy.NoFood => isChinese ? "身边没有可吃的食物" : "no food at hand",
            ActWhy.TooBig => isChinese ? "现有食物都会超出上限" : "every food at hand would go past the maximum",
            ActWhy.NoMorale => isChinese ? "现在没有能提升心情的行动" : "nothing at hand raises Morale now",
            ActWhy.RoutineEmpty => isChinese ? "请先在 Vital Signs 的 MoraleRoutine 设置中安排日常" : "set up a routine in the Vital Signs MoraleRoutine setting first",
            ActWhy.RoutineNotReady => isChinese ? "日常安排中的行动现在都不可用" : "no action of your routine can be done now",
            ActWhy.NoBed => isChinese ? "家中没有可用的床" : "no bed at home to use",
            ActWhy.BedNotReady => isChinese ? "现在没有床可以使用" : "no bed can be used right now",
            ActWhy.NoLifeItems => isChinese ? "身边没有选定的恢复生命物品" : "none of your Life items at hand",
            ActWhy.QueueBusy => isChinese ? "当前行动无法打断" : "the current action cannot be interrupted",
            _ => "",
        };
    }

    /// <summary>"2x Rice, Bread" or "米饭 x2、面包"; a furniture action has its furniture after it: "Sleep (Wooden Bed)".</summary>
    public static string List(IReadOnlyList<PlanStep> steps, bool isChinese)
    {
        var text = new StringBuilder();

        for (var i = 0; i < steps.Count; i++)
        {
            if (i > 0) text.Append(isChinese ? "、" : ", ");

            var count = steps[i].Count;
            var option = steps[i].Option;
            var name = option.Place.Length > 0 ? option.Name + " (" + option.Place + ")" : option.Name;

            if (count <= 1) text.Append(name);
            else if (isChinese) text.Append(name).Append(" x").Append(count.ToString(CultureInfo.InvariantCulture));
            else text.Append(count.ToString(CultureInfo.InvariantCulture)).Append("x ").Append(name);
        }

        return text.ToString();
    }

    public static string Cure(string item, string buff, bool isChinese) => isChinese ? "使用" + item + "治疗「" + buff + "」" : "use " + item + " to cure " + buff;

    public static string NoCure(string buff, bool isChinese) => isChinese ? "身边没有能治疗「" + buff + "」的物品" : "nothing at hand cures " + buff;

    public static string QueuedTitle(bool isChinese) => isChinese ? "已加入行动队列" : "Queued";

    public static string NothingTitle(int stat, bool isChinese) =>
        stat < 0 ? isChinese ? "无法治疗" : "No cure queued" : isChinese ? "「" + StatName(stat, isChinese) + "」无事可做" : "Nothing to do for " + StatName(stat, isChinese);

    public static string Failed(bool isChinese) => isChinese ? "游戏没有接受这个行动" : "the game did not take the action";

    /// <summary>Why a furniture action can't be done while the player is out exploring.</summary>
    public static string Away(bool isChinese) => isChinese ? "不在家中" : "not at home";

    /// <summary>Why a furniture action or item the player chose is greyed out: the base has none.</summary>
    public static string Missing(bool isChinese) => isChinese ? "家中没有" : "none at home";

    public static string StatName(int stat, bool isChinese) => stat switch
    {
        0 => isChinese ? "饱腹" : "Satiety",
        1 => isChinese ? "心情" : "Morale",
        2 => isChinese ? "体力" : "Stamina",
        3 => isChinese ? "生命" : "Life",
        _ => "",
    };
}
