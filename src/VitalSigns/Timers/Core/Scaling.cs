using System;

namespace VitalSigns.Timers.Core;

/// <summary>
/// The arithmetic on the game's three numbers. The game makes a buff's time as base * (1 + talent ratio) + talent bonus,
/// or as a time the caller passes + talent bonus. Each part is multiplied here, so the whole time comes out multiplied.
/// Everything is worked in double and turned back into the game's float once, and each part is held under
/// <see cref="Limits.MaxPartSeconds"/>.
/// </summary>
public static class Scaling
{
    /// <summary>
    /// The talent ratio that makes base * (1 + ratio) come out multiplied. A ratio that already zeroes or flips the time
    /// is the game's own business and is left alone.
    /// </summary>
    public static float Ratio(float ratio, double multiplier, float baseSeconds)
    {
        if (!IsUsable(ratio) || Limits.IsOne(multiplier)) return ratio;

        double factor = 1 + (double)ratio;

        if (factor <= 0) return ratio;

        var scaled = factor * multiplier;

        if (baseSeconds > 0 && baseSeconds * scaled > Limits.MaxPartSeconds) scaled = Limits.MaxPartSeconds / baseSeconds;

        return (float)(scaled - 1);
    }

    /// <summary>The talents' flat bonus (or cut) in game seconds, multiplied and held within the cap either way.</summary>
    public static float Bonus(float seconds, double multiplier)
    {
        if (!IsUsable(seconds) || Limits.IsOne(multiplier)) return seconds;

        return (float)Math.Clamp(seconds * multiplier, -Limits.MaxPartSeconds, Limits.MaxPartSeconds);
    }

    /// <summary>A time a caller passed straight in. -1 means "the config's own time" and is never touched.</summary>
    public static float Passed(float seconds, double multiplier)
    {
        if (!IsUsable(seconds) || seconds <= 0 || Limits.IsOne(multiplier)) return seconds;

        return (float)Math.Min(seconds * multiplier, Limits.MaxPartSeconds);
    }

    /// <summary>
    /// The new remaining time of a running buff when its multiplier goes from one value to another. Null when nothing
    /// should change.
    /// </summary>
    public static double? Remaining(double remainingSeconds, double from, double to)
    {
        if (double.IsNaN(remainingSeconds) || remainingSeconds <= 0 || from <= 0 || to <= 0) return null;

        var ratio = to / from;

        if (Limits.IsOne(ratio)) return null;

        return Math.Min(remainingSeconds * ratio, 2 * Limits.MaxPartSeconds);
    }

    /// <summary>The longest a buff could last with these parts: the base after talents, then the flat bonus.</summary>
    public static double Total(double baseSeconds, double ratio, double bonus) => baseSeconds * (1 + ratio) + bonus;

    private static bool IsUsable(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}
