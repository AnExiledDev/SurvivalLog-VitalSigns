using System;

namespace VitalSigns.Timers.Core;

/// <summary>
/// The range a multiplier may take and the longest time a buff may be given. The game keeps a buff's end as whole game
/// seconds in an int, which runs out after about 68 years of game time, so these stay far below it.
/// </summary>
public static class Limits
{
    public const double Min = 0.1;
    public const double Max = 100;
    public const double Default = 1;

    /// <summary>A category or buff set to this has no multiplier of its own and uses the one above it.</summary>
    public const double UseAbove = 0;

    public const double SecondsPerDay = 86400;

    /// <summary>The cap on each part of a duration: the base time and the talents' flat bonus. Both together stay under 1,000 game days.</summary>
    public const double MaxPartSeconds = 500 * SecondsPerDay;

    /// <summary>A multiplier this close to 1 changes nothing and is left out.</summary>
    private const double Same = 1e-9;

    /// <summary>A global multiplier: anything not a number reads as 1, anything outside the range is moved to its nearest end.</summary>
    public static double Global(double value, out bool wasChanged)
    {
        var limited = double.IsNaN(value) ? Default : Math.Clamp(value, Min, Max);

        wasChanged = !limited.Equals(value);

        return limited;
    }

    /// <summary>A category or buff multiplier: 0 or less, or not a number, means "use the one above"; the rest is held to the range.</summary>
    public static double? Override(double value, out bool wasChanged)
    {
        if (value == UseAbove)
        {
            wasChanged = false;

            return null;
        }

        if (double.IsNaN(value) || value < 0)
        {
            wasChanged = true;

            return null;
        }

        var limited = Math.Clamp(value, Min, Max);

        wasChanged = !limited.Equals(value);

        return limited;
    }

    public static bool IsOne(double multiplier) => Math.Abs(multiplier - 1) < Same;
}

/// <summary>Which buffs the mod touches: the ones the player sees that have a timer. Hidden and untimed buffs keep the game's own times.</summary>
public static class Scope
{
    public static bool IsTimedVisible(bool isHidden, float baseSeconds) => !isHidden && baseSeconds > 0 && !float.IsInfinity(baseSeconds);
}
