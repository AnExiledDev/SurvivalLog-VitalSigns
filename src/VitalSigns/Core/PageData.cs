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

    /// <summary>Effect kinds as the page names them.</summary>
    private const string Kinds = "esnmglvfo";

    /// <param name="hasOtherFitnessBar">The Fitness Bar mod is installed too: the Fitness reading carries a third value, 1, and the bar's hover box says so.</param>
    public static string Of(Vitals? vitals, GameNames names, bool isChinese, Options? options = null, bool hasOtherFitnessBar = false)
    {
        if (vitals == null) return NoPlayer;

        options ??= new Options();

        var data = new StringBuilder(1024);

        data.Append("{zh:").Append(isChinese ? "true" : "false");
        data.Append(",stats:[");
        AppendList(data, vitals.Stats, (text, stat) => text.Append('[').Append(Ratio(stat.Gain)).Append(',').Append(Ratio(stat.Loss)).Append(',')
            .Append(Ratio(stat.NaturalGain)).Append(',').Append(Ratio(stat.NaturalLoss)).Append(']'));
        data.Append("],buffs:[");
        AppendList(data, vitals.Buffs, (text, buff) => text.Append('[').Append(Whole(buff.ConfigId)).Append(',').Append(Whole(buff.EndsAt)).Append(',')
            .Append(Whole(buff.Duration)).Append(',').Append(Whole(System.Math.Max(1, buff.Layers))).Append(',').Append(buff.IsSuppressed ? '1' : '0').Append(']'));
        data.Append("],weight:");
        data.Append(vitals.Weight is { } weight ? $"[{Whole(weight.Carried)},{Whole(weight.Max)},{Whole(weight.Overweight)}]" : "null");
        data.Append(",wellFed:");
        data.Append(vitals.WellFed is { } wellFed ? $"[{Whole(wellFed.From)},{Whole(wellFed.Until)},{Whole(wellFed.Max)}]" : "null");
        data.Append(",names:{wellFed:").Append(PageCalls.Quote(names.WellFed)).Append(",overweight:").Append(PageCalls.Quote(names.Overweight)).Append('}');
        data.Append(",opt:[").Append(Flag(options.Weight)).Append(',').Append(Flag(options.Stripes)).Append(',').Append(Flag(options.Tooltips)).Append(',')
            .Append(Flag(options.GameText)).Append(',').Append(Flag(options.Cures)).Append(']');
        data.Append(",zoom:").Append(Ratio(BarSize.Zoom(options.BarSize))).Append(",buffZoom:").Append(Ratio(BarSize.Zoom(options.BuffSize)))
            .Append(",tipZoom:").Append(Ratio(BarSize.Zoom(options.TooltipSize)));
        data.Append(",fit:");
        data.Append(options.Fitness && vitals.Fitness is { } fitness ? $"[{Whole(fitness.Value)},{Whole(fitness.Max)}{(hasOtherFitnessBar ? ",1" : "")}]" : "null");

        if (vitals.Details is { } details) AppendDetails(data, details);

        return data.Append('}').ToString();
    }

    /// <summary>
    /// max: each stat's maximum. coming: per stat, [buff, amount, last loss second]. info: per buff id, [name, is good,
    /// interval in hours, effect lines [kind, stat or food, value for one layer], cure item ids, suppressor item ids,
    /// is suppressible]. items: per item id, [name, icon, count]. by: the running suppressors. foods: food type names.
    /// food: the player's own food modifiers, [stat, food type, ratio].
    /// </summary>
    private static void AppendDetails(StringBuilder data, Details details)
    {
        data.Append(",max:[");
        AppendList(data, details.Max, (text, max) => text.Append(Ratio(max)));
        data.Append("],coming:[");
        AppendList(data, details.Coming, (text, stat) =>
        {
            text.Append('[');
            AppendList(text, stat, (line, loss) => line.Append('[').Append(Whole(loss.BuffId)).Append(',').Append(Amount(loss.Amount)).Append(',')
                .Append(Whole(loss.LastAt)).Append(']'));
            text.Append(']');
        });
        data.Append("],info:{");
        AppendList(data, details.Rules, (text, rule) =>
        {
            text.Append(Whole(rule.Id)).Append(":[").Append(PageCalls.Quote(rule.Name)).Append(',').Append(Flag(rule.IsGood)).Append(',')
                .Append(Ratio(rule.Interval / 3600)).Append(",[");
            AppendList(text, EffectLines.Of(rule, details.ColdFactor), (line, fx) => line.Append("[\"").Append(Kinds[(int)fx.Kind]).Append("\",")
                .Append(Whole(fx.Index)).Append(',').Append(Ratio(fx.Value)).Append(']'));
            text.Append("],[");
            AppendList(text, rule.Cures, (line, id) => line.Append(Whole(id)));
            text.Append("],[");
            AppendList(text, rule.Suppressors, (line, id) => line.Append(Whole(id)));
            text.Append("],").Append(Flag(rule.IsSuppressible)).Append(']');
        });
        data.Append("},items:{");
        AppendList(data, details.Items, (text, item) => text.Append(Whole(item.Id)).Append(":[").Append(PageCalls.Quote(item.Name)).Append(',')
            .Append(PageCalls.Quote(item.Icon)).Append(',').Append(Whole(item.Count)).Append(']'));
        data.Append("},by:").Append(PageCalls.Quote(details.SuppressedBy)).Append(",foods:[");
        AppendList(data, details.Foods, (text, name) => text.Append(PageCalls.Quote(name)));
        data.Append("],food:[");
        AppendList(data, details.FoodMods, (text, mod) => text.Append('[').Append(Whole(mod.Stat)).Append(',').Append(Whole(mod.Food)).Append(',')
            .Append(Ratio(mod.Ratio)).Append(']'));
        data.Append(']');

        if (details.Sources == null) return;

        data.Append(",src:[");
        AppendList(data, details.Sources, (text, stat) =>
        {
            text.Append('[');
            AppendList(text, stat, (line, source) => line.Append('[').Append(PageCalls.Quote(source.Name)).Append(',').Append(Ratio(source.Ratio)).Append(']'));
            text.Append(']');
        });
        data.Append(']');
    }

    private static char Flag(bool value) => value ? '1' : '0';

    /// <summary>Losses show to one place; two keep the shading smooth without float noise.</summary>
    private static string Amount(double amount) => double.IsFinite(amount) ? (System.Math.Round(amount, 2) + 0.0).ToString("0.##", CultureInfo.InvariantCulture) : "0";

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
