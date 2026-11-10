using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace VitalSigns.Core;

/// <summary>
/// How the HUD is laid out and what it does beyond the drawing in <see cref="Options"/>. HideFull hides a bar at
/// HideAbove percent or more (100: only a full one), except, with KeepWithBuffs, a bar with buffs beside it.
/// WarnPulse makes a bar pulse at WarnAt percent or below (0: at the warnings' LowAt). Sort and Group order the buffs
/// beside each bar; Favourites go first with a bigger timer, FavouriteSize percent of the badge's. Collapse shrinks the
/// other buffs to dots until the row is hovered. Layout moves and resizes the bars; Tray puts the buffs on another side.
/// Explore draws the bars on the exploration map in a corner (Off for none). Projection shows what the bars will be
/// once the action queue is done. ClickBars lets a click on a bar queue an action for it; Cure lets a click on a debuff
/// queue its cure.
/// </summary>
public sealed record HudOptions(bool HideFull = false, int HideAbove = 100, bool KeepWithBuffs = true, bool WarnPulse = true, int WarnAt = 0,
    string Sort = HudChoices.SortGame, string Group = HudChoices.GroupMixed, IReadOnlyList<int>? Favourites = null, int FavouriteSize = HudChoices.DefaultFavouriteSize,
    bool Collapse = false, HudLayout? Layout = null, string Tray = HudChoices.TrayLeft, string Explore = HudChoices.CornerTopRight, int ExploreSize = 100,
    bool Projection = true, bool ClickBars = false, bool Cure = false);

/// <summary>The values the HUD's choice settings take, and the number the page reads for each.</summary>
public static class HudChoices
{
    public const string SortGame = "Game";
    public const string SortEndingFirst = "EndingFirst";
    public const string SortLongestFirst = "LongestFirst";
    public static readonly string[] Sorts = { SortGame, SortEndingFirst, SortLongestFirst };

    public const string GroupMixed = "Mixed";
    public const string GroupBuffsFirst = "BuffsFirst";
    public const string GroupDebuffsFirst = "DebuffsFirst";
    public static readonly string[] Groups = { GroupMixed, GroupBuffsFirst, GroupDebuffsFirst };

    public const string TrayLeft = "Left";
    public const string TrayRight = "Right";
    public const string TrayAbove = "Above";
    public const string TrayBelow = "Below";
    public static readonly string[] Trays = { TrayLeft, TrayRight, TrayAbove, TrayBelow };

    public const string CornerOff = "Off";
    public const string CornerTopLeft = "TopLeft";
    public const string CornerTopRight = "TopRight";
    public const string CornerBottomLeft = "BottomLeft";
    public const string CornerBottomRight = "BottomRight";
    public static readonly string[] Corners = { CornerOff, CornerTopLeft, CornerTopRight, CornerBottomLeft, CornerBottomRight };

    public const int MinHideAbove = 50;
    public const int MaxHideAbove = 100;
    public const int DefaultFavouriteSize = 130;
    public const int MinFavouriteSize = 100;
    public const int MaxFavouriteSize = 200;
    public const int MinExploreSize = 50;
    public const int MaxExploreSize = 150;

    /// <summary>A choice's place in its list, or 0 (the default) for a value the list does not have.</summary>
    public static int Index(string[] choices, string? value)
    {
        for (var i = 0; i < choices.Length; i++)
        {
            if (string.Equals(choices[i], value, StringComparison.OrdinalIgnoreCase)) return i;
        }

        return 0;
    }

    /// <summary>Buff ids from a setting such as "1300, 200810": anything that is not a positive whole number is skipped.</summary>
    public static IReadOnlyList<int> Ids(string? text)
    {
        var ids = new List<int>();

        foreach (var part in (text ?? "").Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (int.TryParse(part, NumberStyles.None, CultureInfo.InvariantCulture, out var id) && id > 0 && !ids.Contains(id)) ids.Add(id);
        }

        return ids;
    }

    /// <summary>The setting's text with the id added when it was missing and taken out when it was there.</summary>
    public static string Toggle(string? text, int id)
    {
        var ids = new List<int>(Ids(text));

        if (!ids.Remove(id)) ids.Add(id);

        return string.Join(",", ids);
    }
}

/// <summary>
/// Where each bar sits and how big, moved from the game's place: per part, a shift in the HUD's pixels and a size in
/// percent. The parts are the Satiety, Morale, Stamina, Life and Fitness rows, then the whole panel. Kept in the config
/// as "dx,dy,size;..." one part after another; a part left out or written wrong stays where the game puts it.
/// </summary>
public sealed record HudLayout(IReadOnlyList<PartPlace> Parts)
{
    public const int PartCount = 6;
    public const int Panel = 5;
    public const int MinSize = 50;
    public const int MaxSize = 200;
    public const int MaxShift = 4000;

    public static readonly PartPlace Home = new(0, 0, 100);

    public bool IsHome
    {
        get
        {
            foreach (var part in Parts)
            {
                if (part != Home) return false;
            }

            return true;
        }
    }

    /// <summary>Null when every part is at home, so the page needs no layout at all.</summary>
    public static HudLayout? Parse(string? text)
    {
        var parts = new PartPlace[PartCount];
        var pieces = (text ?? "").Split(';');

        for (var i = 0; i < PartCount; i++) parts[i] = i < pieces.Length ? Part(pieces[i]) : Home;

        var layout = new HudLayout(parts);

        return layout.IsHome ? null : layout;
    }

    public static string Format(HudLayout? layout)
    {
        if (layout == null || layout.IsHome) return "";

        var text = new StringBuilder();

        for (var i = 0; i < PartCount; i++)
        {
            var part = i < layout.Parts.Count ? layout.Parts[i] : Home;

            if (i > 0) text.Append(';');

            text.Append(part.X.ToString(CultureInfo.InvariantCulture)).Append(',').Append(part.Y.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(part.Size.ToString(CultureInfo.InvariantCulture));
        }

        return text.ToString();
    }

    private static PartPlace Part(string piece)
    {
        var numbers = piece.Split(',');

        if (numbers.Length != 3) return Home;

        if (!Whole(numbers[0], out var x) || !Whole(numbers[1], out var y) || !Whole(numbers[2], out var size)) return Home;

        return new PartPlace(Math.Clamp(x, -MaxShift, MaxShift), Math.Clamp(y, -MaxShift, MaxShift), Math.Clamp(size, MinSize, MaxSize));
    }

    private static bool Whole(string text, out int value)
    {
        if (double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var number) && double.IsFinite(number))
        {
            value = (int)Math.Round(Math.Clamp(number, -1e6, 1e6));

            return true;
        }

        value = 0;

        return false;
    }
}

/// <summary>One part's shift from the game's place, in the HUD's pixels, and its size in percent.</summary>
public readonly record struct PartPlace(int X, int Y, int Size);

/// <summary>
/// What the HUD shows that changes with the game, beside the reading: each bar's level once the queue is done (0 to 1,
/// in the HUD's row order, null when nothing queued changes it), the debuffs with a cure at hand, and which bars a click
/// has something to do for, with a line for its hover box saying what a click does (or why it can do nothing).
/// </summary>
public sealed record HudReading(IReadOnlyList<double?>? Projected = null, IReadOnlyList<int>? Curable = null, IReadOnlyList<bool>? CanAct = null,
    IReadOnlyList<string>? Tips = null);
