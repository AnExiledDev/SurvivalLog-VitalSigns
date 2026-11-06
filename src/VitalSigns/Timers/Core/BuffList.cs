using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace VitalSigns.Timers.Core;

/// <summary>
/// The Timers Buffs setting: each buff with its own multiplier, written "id:multiplier" split by commas
/// ("200810:2,1043:0.5"). An entry that is not a buff id and a number is skipped on its own, a multiplier is held to
/// the range, and 0 or less leaves that buff to the multipliers above it.
/// </summary>
public static class BuffList
{
    public static IReadOnlyDictionary<int, double> Parse(string? text)
    {
        var buffs = new Dictionary<int, double>();

        foreach (var part in (text ?? "").Split(','))
        {
            var pair = part.Split(':');

            if (pair.Length != 2 || !int.TryParse(pair[0].Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var id) || id <= 0) continue;

            if (!double.TryParse(pair[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value)) continue;

            if (Limits.Override(value, out _) is { } kept) buffs[id] = kept;
        }

        return buffs;
    }

    /// <summary>The setting's text, buffs in id order.</summary>
    public static string Format(IReadOnlyDictionary<int, double> buffs) =>
        string.Join(",", buffs.OrderBy(pair => pair.Key).Select(pair => pair.Key.ToString(CultureInfo.InvariantCulture) + ":" + Math.Round(pair.Value, 4).ToString(CultureInfo.InvariantCulture)));
}
