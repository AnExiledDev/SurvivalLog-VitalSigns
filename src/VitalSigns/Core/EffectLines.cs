using System.Collections.Generic;

namespace VitalSigns.Core;

/// <summary>What a buff line says: which value, for which stat or food type, and how much for one layer.</summary>
public enum EffectKind
{
    /// <summary>Change each interval.</summary>
    Every,
    Start,
    End,
    Max,
    Gain,
    Loss,
    Speed,
    SatietyFood,
    MoraleFood
}

/// <summary>One line of a buff tooltip. Index is the stat (0 to 3, HUD order) or the food type (1 to 11); Value is for one layer.</summary>
public readonly record struct EffectLine(EffectKind Kind, int Index, double Value);

/// <summary>The lines a buff tooltip shows, in a fixed order, leaving out every value that is zero.</summary>
public static class EffectLines
{
    public static IReadOnlyList<EffectLine> Of(BuffRule rule, double coldFactor)
    {
        var lines = new List<EffectLine>();

        if (rule.Interval > 0) Add(lines, EffectKind.Every, rule.Every, rule.IsCold ? coldFactor : 1);

        Add(lines, EffectKind.Start, rule.AtStart, 1);
        Add(lines, EffectKind.End, rule.AtEnd, 1);
        Add(lines, EffectKind.Max, rule.Max, 1);
        Add(lines, EffectKind.Gain, rule.Gain, 1);
        Add(lines, EffectKind.Loss, rule.Loss, 1);

        if (rule.Speed != 0) lines.Add(new EffectLine(EffectKind.Speed, 0, rule.Speed));

        AddFood(lines, EffectKind.SatietyFood, rule.SatietyFood);
        AddFood(lines, EffectKind.MoraleFood, rule.MoraleFood);

        return lines;
    }

    private static void Add(List<EffectLine> lines, EffectKind kind, IReadOnlyList<double> values, double scale)
    {
        for (var stat = 0; stat < values.Count; stat++)
        {
            if (values[stat] != 0) lines.Add(new EffectLine(kind, stat, values[stat] * scale));
        }
    }

    private static void AddFood(List<EffectLine> lines, EffectKind kind, IReadOnlyList<double> values)
    {
        for (var i = 0; i < values.Count; i++)
        {
            if (values[i] != 0) lines.Add(new EffectLine(kind, i + 1, values[i]));
        }
    }
}
