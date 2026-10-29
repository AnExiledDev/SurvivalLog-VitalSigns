using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace VitalSigns.Core;

/// <summary>
/// A reading as the page script takes it: a JavaScript object literal, or null when there is no player. Built fresh
/// every check; the plugin compares it with the last one the page took and only sends it again when it differs, so
/// the page is not redrawn every couple of seconds for nothing. The game clock goes separately, every check.
/// </summary>
public static class PageData
{
    public const string NoPlayer = "null";

    public static string Of(Vitals? vitals, GameNames names, bool isChinese)
    {
        if (vitals == null) return NoPlayer;

        var data = new StringBuilder(256);

        data.Append("{zh:").Append(isChinese ? "true" : "false");
        data.Append(",stats:[");
        AppendList(data, vitals.Stats, (text, stat) => text.Append('[').Append(Ratio(stat.Gain)).Append(',').Append(Ratio(stat.Loss)).Append(',')
            .Append(Ratio(stat.NaturalGain)).Append(',').Append(Ratio(stat.NaturalLoss)).Append(']'));
        data.Append("],buffs:[");
        AppendList(data, vitals.Buffs, (text, buff) => text.Append('[').Append(Whole(buff.ConfigId)).Append(',').Append(Whole(buff.EndsAt)).Append(',')
            .Append(Whole(buff.Duration)).Append(']'));
        data.Append("],weight:");
        data.Append(vitals.Weight is { } weight ? $"[{Whole(weight.Carried)},{Whole(weight.Max)},{Whole(weight.Overweight)}]" : "null");
        data.Append(",wellFed:");
        data.Append(vitals.WellFed is { } wellFed ? $"[{Whole(wellFed.From)},{Whole(wellFed.Until)},{Whole(wellFed.Max)}]" : "null");
        data.Append(",names:{wellFed:").Append(PageCalls.Quote(names.WellFed)).Append(",overweight:").Append(PageCalls.Quote(names.Overweight)).Append("}}");

        return data.ToString();
    }

    private static void AppendList<T>(StringBuilder data, IReadOnlyList<T> items, System.Action<StringBuilder, T> append)
    {
        for (var i = 0; i < items.Count; i++)
        {
            if (i > 0) data.Append(',');

            append(data, items[i]);
        }
    }

    /// <summary>Four places is finer than anything the page shows (it rounds to two), and keeps float noise out of the comparison. Adding 0.0 turns a rounded -0 into 0.</summary>
    private static string Ratio(double ratio) => double.IsFinite(ratio) ? (System.Math.Round(ratio, 4) + 0.0).ToString("0.####", CultureInfo.InvariantCulture) : "0";

    private static string Whole(int value) => value.ToString(CultureInfo.InvariantCulture);
}
