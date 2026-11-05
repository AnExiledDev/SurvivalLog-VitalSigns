using System;
using System.Collections.Generic;

namespace VitalSigns.Core;

/// <summary>
/// The stat difficulty: per stat (Satiety, Morale, Stamina, Life), how much every loss and every gain the player gets
/// is multiplied by. 1 leaves the game's own number. Applies to all of it: the slow drain over time, food, rest,
/// buffs and furniture alike.
/// </summary>
public sealed class Difficulty
{
    public const double Default = 1;
    public const double Min = 0;
    public const double Max = 5;

    public static readonly Difficulty Off = new(new[] { 1.0, 1, 1, 1 }, new[] { 1.0, 1, 1, 1 });

    private readonly double[] _drain;
    private readonly double[] _gain;

    public Difficulty(IReadOnlyList<double> drain, IReadOnlyList<double> gain)
    {
        _drain = Held(drain);
        _gain = Held(gain);

        IsOff = Array.TrueForAll(_drain, IsOne) && Array.TrueForAll(_gain, IsOne);
    }

    /// <summary>True when every multiplier is 1, so the hook can return at once.</summary>
    public bool IsOff { get; }

    public double Drain(int stat) => stat >= 0 && stat < _drain.Length ? _drain[stat] : Default;

    public double Gain(int stat) => stat >= 0 && stat < _gain.Length ? _gain[stat] : Default;

    /// <summary>A change to a stat as the game is about to apply it, scaled: a rise by the gain multiplier, a fall by the drain one.</summary>
    public float Scale(int stat, float change)
    {
        if (stat < 0 || stat >= _drain.Length || change == 0 || !float.IsFinite(change)) return change;

        var factor = change > 0 ? _gain[stat] : _drain[stat];

        return IsOne(factor) ? change : (float)(change * factor);
    }

    private static double[] Held(IReadOnlyList<double> values)
    {
        var held = new double[4];

        for (var i = 0; i < held.Length; i++)
        {
            var value = i < values.Count ? values[i] : Default;

            held[i] = double.IsFinite(value) ? Math.Clamp(value, Min, Max) : Default;
        }

        return held;
    }

    private static bool IsOne(double value) => Math.Abs(value - 1) < 1e-9;
}
