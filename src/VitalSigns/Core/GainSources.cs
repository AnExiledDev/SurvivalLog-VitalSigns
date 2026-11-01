using System;
using System.Collections.Generic;

namespace VitalSigns.Core;

/// <summary>
/// Puts the buffs' shares of each stat's gain ratio in the order the hover box lists them: one line per name (layers and
/// copies of a buff added together), the biggest change first, and nothing that rounds to no change.
/// </summary>
public static class GainSources
{
    /// <summary>Below half a percent a line would read +0%.</summary>
    private const double Smallest = 0.005;

    public static IReadOnlyList<IReadOnlyList<GainSource>> Of(IEnumerable<(int Stat, string Name, double Ratio)> parts, int stats)
    {
        var lists = new List<GainSource>[stats];

        for (var i = 0; i < stats; i++) lists[i] = new List<GainSource>();

        foreach (var (stat, name, ratio) in parts)
        {
            if (stat < 0 || stat >= stats || !double.IsFinite(ratio)) continue;

            var list = lists[stat];
            var at = list.FindIndex(source => source.Name == name);

            if (at >= 0) list[at] = list[at] with { Ratio = list[at].Ratio + ratio };
            else list.Add(new GainSource(name, ratio));
        }

        foreach (var list in lists)
        {
            list.RemoveAll(source => Math.Abs(source.Ratio) < Smallest);
            list.Sort((a, b) => Math.Abs(b.Ratio).CompareTo(Math.Abs(a.Ratio)));
        }

        return lists;
    }
}
