using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using VitalSigns.Core.Acts;

namespace VitalSigns.Core;

/// <summary>One entry a picker on the Mods screen offers: its id as the setting writes it, both names, and how it shows.</summary>
public sealed record PickerEntry(string Id, string NameEn, string NameZh, string? Icon = null, string GroupEn = "", string GroupZh = "", string NoteEn = "", string NoteZh = "");

/// <summary>
/// The entries the Mod Manager's pickers read from ModManagerAnswers.Entries: one per line,
/// "id|English|Chinese|icon|group|group in Chinese|note|note in Chinese". A "|" or line break inside a text is
/// swapped for a plain character, so one name can never split a line.
/// </summary>
public static class PickerLines
{
    public static string Of(IEnumerable<PickerEntry> entries) => string.Join("\n", entries.Select(Line));

    private static string Line(PickerEntry e) =>
        string.Join("|", new[] { e.Id, e.NameEn, e.NameZh, e.Icon ?? "", e.GroupEn, e.GroupZh, e.NoteEn, e.NoteZh }.Select(Clean));

    private static string Clean(string text) => text.Replace('|', '/').Replace('\r', ' ').Replace('\n', ' ').Trim();

    private static readonly (string En, string Zh)[] StatNames = { ("Satiety", "饱腹"), ("Morale", "心情"), ("Stamina", "体力"), ("Life", "生命") };

    /// <summary>What one use does to the four stats, before the player's modifiers: "Satiety +20, Morale -5".</summary>
    public static (string En, string Zh) Delta(StatDelta delta)
    {
        var values = new[] { delta.Satiety, delta.Morale, delta.Stamina, delta.Life };
        var en = new List<string>();
        var zh = new List<string>();

        for (var i = 0; i < values.Length; i++)
        {
            if (Math.Abs(values[i]) < 0.05) continue;

            var number = (values[i] > 0 ? "+" : "") + values[i].ToString("0.#", CultureInfo.InvariantCulture);

            en.Add(StatNames[i].En + " " + number);
            zh.Add(StatNames[i].Zh + " " + number);
        }

        return (string.Join(", ", en), string.Join("，", zh));
    }

    /// <summary>A buff's base time in game time, at most two parts: "1 d 4 h", "30 m".</summary>
    public static (string En, string Zh) Duration(double seconds)
    {
        var s = (long)Math.Max(0, Math.Round(seconds));
        var parts = new List<(long Count, string En, string Zh)>
        {
            (s / 86400, "d", "天"), (s % 86400 / 3600, "h", "小时"), (s % 3600 / 60, "m", "分钟"),
        };
        var shown = parts.Where(part => part.Count > 0).Take(2).ToList();

        if (shown.Count == 0) shown.Add((0, "m", "分钟"));

        return (string.Join(" ", shown.Select(part => part.Count + " " + part.En)), string.Join(" ", shown.Select(part => part.Count + part.Zh)));
    }
}
