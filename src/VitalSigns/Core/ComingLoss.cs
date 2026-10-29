using System;
using System.Collections.Generic;

namespace VitalSigns.Core;

/// <summary>What a running buff will still take from one stat before it ends: the amount, and the game second of the last loss.</summary>
public readonly record struct LossLine(int BuffId, double Amount, int LastAt);

/// <summary>
/// The losses timed buffs still have coming, per stat. A buff counts while it runs, has an end and is not suppressed.
/// It loses its Every amount at each interval still ahead and its AtEnd amount when it ends, each times its layers.
/// Interval losses of a cold debuff are cut by cold resistance. Each loss is scaled as the game scales it, by
/// (1 + the stat's loss ratio at that moment), and the ratio a buff adds is taken off once that buff has ended.
/// Gains, natural drain and buffs with no end are left out.
/// </summary>
public static class ComingLoss
{
    public static IReadOnlyList<IReadOnlyList<LossLine>> Of(int now, IReadOnlyList<BuffTime> running, IReadOnlyDictionary<int, BuffRule> rules,
        double coldFactor, IReadOnlyList<double> lossRatioNow)
    {
        var active = new List<(BuffTime Buff, BuffRule Rule)>();

        foreach (var buff in running)
        {
            if (!buff.IsSuppressed && buff.EndsAt > now && rules.TryGetValue(buff.ConfigId, out var rule)) active.Add((buff, rule));
        }

        var lines = new IReadOnlyList<LossLine>[BuffRule.Stats];

        for (var stat = 0; stat < BuffRule.Stats; stat++)
        {
            var statLines = new List<LossLine>();
            var ratioNow = stat < lossRatioNow.Count ? lossRatioNow[stat] : 0;

            foreach (var (buff, rule) in active)
            {
                var layers = Math.Max(1, buff.Layers);
                var amount = 0.0;

                if (rule.Interval > 0 && rule.Every[stat] < 0)
                {
                    var each = -rule.Every[stat] * layers * (rule.IsCold ? coldFactor : 1);
                    var ticks = (int)Math.Floor((buff.EndsAt - now) / rule.Interval);

                    // The interval losses fall on the buff's end and every interval before it.
                    for (var k = 0; k < ticks; k++) amount += each * Scale(buff.EndsAt - k * rule.Interval);
                }

                if (rule.AtEnd[stat] < 0) amount += -rule.AtEnd[stat] * layers * Scale(buff.EndsAt);

                if (amount > 0.0001) statLines.Add(new LossLine(rule.Id, amount, buff.EndsAt));
            }

            lines[stat] = statLines;

            double Scale(double at)
            {
                var ratio = ratioNow;

                foreach (var (buff, rule) in active)
                {
                    if (buff.EndsAt < at) ratio -= rule.Loss[stat] * Math.Max(1, buff.Layers);
                }

                return Math.Max(0, 1 + ratio);
            }
        }

        return lines;
    }
}
