using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using VitalSigns.Core.Acts;

namespace VitalSigns.Core;

/// <summary>
/// How the pickers file what they list: items under the backpack's own categories (Meds, Books, Household...), food under
/// the game's food kinds (Staple Food, Meat, Fruit...), so a list of two thousand dishes splits into shelves a player
/// knows. Meds come first, food last, then by the game's own order.
/// </summary>
public static class PickerGroups
{
    public const int Food = 1;

    /// <summary>Where an item sorts: its category's number, or food's kind after every other category.</summary>
    public static int Rank(int category, int subCategory) => category != Food ? category : 1000 + (subCategory > 0 ? subCategory : 999);

    private static readonly (string En, string Zh)[] BuffKinds =
    {
        ("Calm", "平静"), ("Food and sickness", "饮食与疾病"), ("Mood", "心态"), ("Energy and sleep", "精力与睡眠"),
        ("Exercise and weather", "运动与天气"), ("Health and injury", "健康与伤病"), ("Exploration", "探索"),
    };

    public static readonly (string En, string Zh) Talents = ("Talents", "天赋");

    /// <summary>
    /// A buff's shelf: the kind the game sorts it under (its BuffDisplayType; the names are ours, as on the Timers
    /// settings), or Talents for the few hundred a talent gives, which would otherwise bury Exploration.
    /// </summary>
    public static (int Rank, string En, string Zh) Buff(int displayType, bool isTalent)
    {
        if (isTalent) return (BuffKinds.Length + 1, Talents.En, Talents.Zh);

        return displayType >= 0 && displayType < BuffKinds.Length ? (displayType, BuffKinds[displayType].En, BuffKinds[displayType].Zh) : (BuffKinds.Length, "Other", "其他");
    }

    /// <summary>The game's text key for the shelf name: the food kind for food that has one, else the category.</summary>
    public static string NameKey(int category, int subCategory) =>
        category == Food && subCategory > 0
            ? "ConstantText_Text_ItemSubCategory" + subCategory.ToString(CultureInfo.InvariantCulture)
            : "ConstantText_Text_ItemType" + category.ToString(CultureInfo.InvariantCulture);
}

/// <summary>A kind of bed the game has: what it is called, whether it sleeps or naps, and whether a package makes it.</summary>
public sealed record BedKind(int ConfigId, double Sleep, double Nap, bool IsBuilt);

public static class BedChoices
{
    /// <summary>
    /// The beds worth a mode: those a package builds, those in the player's base now, and those already given a mode.
    /// The game has a dozen more, set in places a player only visits (a hospital bed, a tent, story beds); they stay out.
    /// Beds in the base come first.
    /// </summary>
    public static IReadOnlyList<BedKind> Pick(IEnumerable<BedKind> all, ISet<int> inBase, ISet<int> saved) =>
        all.Where(bed => (bed.Sleep > 0 || bed.Nap > 0) && (bed.IsBuilt || inBase.Contains(bed.ConfigId) || saved.Contains(bed.ConfigId)))
            .OrderBy(bed => inBase.Contains(bed.ConfigId) ? 0 : bed.IsBuilt ? 1 : 2)
            .ThenBy(bed => bed.ConfigId)
            .ToList();

    /// <summary>What a bed does: "Sleep: Stamina +60. Nap: Stamina +12."</summary>
    public static (string En, string Zh) Note(BedKind bed)
    {
        var en = new List<string>();
        var zh = new List<string>();

        if (bed.Sleep > 0)
        {
            en.Add("Sleep: " + PickerLines.Delta(new StatDelta(0, 0, bed.Sleep, 0)).En + ".");
            zh.Add("睡觉：" + PickerLines.Delta(new StatDelta(0, 0, bed.Sleep, 0)).Zh + "。");
        }

        if (bed.Nap > 0)
        {
            en.Add("Nap: " + PickerLines.Delta(new StatDelta(0, 0, bed.Nap, 0)).En + ".");
            zh.Add("小睡：" + PickerLines.Delta(new StatDelta(0, 0, bed.Nap, 0)).Zh + "。");
        }

        return (string.Join(" ", en), string.Join("", zh));
    }
}
