using System;
using System.Collections.Generic;

namespace VitalSigns.Core;

/// <summary>A stat's current value and its maximum, as the game keeps them.</summary>
public readonly record struct StatLevel(double Value, double Max);

/// <summary>Which stats warn, and the percent of its maximum at which Satiety, Morale, Stamina or Life counts as low.</summary>
public sealed record AlertOptions(bool Satiety = true, bool Morale = true, bool Stamina = true, bool Life = true, bool Fitness = true, int LowAt = AlertOptions.DefaultLowAt)
{
    /// <summary>Where the game turns a bar red and makes it pulse.</summary>
    public const int DefaultLowAt = 20;

    public const int MinLowAt = 5;
    public const int MaxLowAt = 90;

    public bool IsOn(AlertStat stat) => stat switch
    {
        AlertStat.Satiety => Satiety,
        AlertStat.Morale => Morale,
        AlertStat.Stamina => Stamina,
        AlertStat.Life => Life,
        _ => Fitness
    };
}

/// <summary>The HUD's rows in order, then Fitness.</summary>
public enum AlertStat
{
    Satiety,
    Morale,
    Stamina,
    Life,
    Fitness
}

/// <summary>Low is a stat at or under its LowAt; NearWeakened and Weakened are Fitness under 40 and under 30.</summary>
public enum AlertLevel
{
    Low,
    NearWeakened,
    Weakened
}

/// <summary>One warning to show: the stat, how bad, and its value and maximum when it crossed.</summary>
public readonly record struct Alert(AlertStat Stat, AlertLevel Level, int Value, int Max);

/// <summary>
/// Warns once when a stat drops past a line, not every reading it stays there. A stat must climb a little back over
/// the line before it can warn again, so a value that wobbles on the line does not warn over and over. The first
/// reading after there was no player only notes where each stat is: loading a save is not a drop.
/// </summary>
public sealed class AlertWatch
{
    /// <summary>Weakened starts below this Fitness; the page draws the same line.</summary>
    public const int WeakenedBelow = 30;

    /// <summary>Fitness below this is close to Weakened; the page turns the bar amber here.</summary>
    public const int NearWeakenedBelow = 40;

    /// <summary>How many percent of its maximum a stat climbs back over its line before it can warn again.</summary>
    public const double StatRearm = 5;

    /// <summary>How many Fitness points it climbs back over a line before that line can warn again.</summary>
    public const int FitnessRearm = 2;

    private const int Stats = 5;

    // Per stat, how far past its lines it is: 0 none, 1 the first line, 2 the second (only Fitness has one).
    private readonly int[] _zones = new int[Stats];
    private bool _hasReading;

    /// <summary>Forget where the stats were, as after no player: the next reading only notes them.</summary>
    public void Reset() => _hasReading = false;

    /// <param name="levels">Satiety, Morale, Stamina and Life; null when there is no player.</param>
    public IReadOnlyList<Alert> Step(IReadOnlyList<StatLevel>? levels, FitnessReading? fitness, AlertOptions options)
    {
        if (levels == null)
        {
            Reset();

            return Array.Empty<Alert>();
        }

        var alerts = new List<Alert>();
        var isFirst = !_hasReading;
        _hasReading = true;

        for (var i = 0; i < levels.Count && i < (int)AlertStat.Fitness; i++)
        {
            var level = levels[i];

            if (level.Max <= 0) continue;

            var percent = level.Value / level.Max * 100;

            if (Move(i, StatZone(percent, options.LowAt), StatZone(percent - StatRearm, options.LowAt), isFirst) && options.IsOn((AlertStat)i))
            {
                alerts.Add(new Alert((AlertStat)i, AlertLevel.Low, (int)Math.Round(level.Value), (int)Math.Round(level.Max)));
            }
        }

        if (fitness is { Max: > 0 } fit)
        {
            var zone = FitnessZone(fit.Value);

            if (Move((int)AlertStat.Fitness, zone, FitnessZone(fit.Value - FitnessRearm), isFirst) && options.Fitness)
            {
                alerts.Add(new Alert(AlertStat.Fitness, zone == 2 ? AlertLevel.Weakened : AlertLevel.NearWeakened, fit.Value, fit.Max));
            }
        }

        return alerts;
    }

    /// <summary>True when the stat went past a line it was not past before. It only comes back once it clears the margin.</summary>
    private bool Move(int stat, int zone, int rearmedZone, bool isFirst)
    {
        var was = _zones[stat];

        if (isFirst)
        {
            _zones[stat] = zone;

            return false;
        }

        if (zone > was)
        {
            _zones[stat] = zone;

            return true;
        }

        if (rearmedZone < was) _zones[stat] = rearmedZone;

        return false;
    }

    private static int StatZone(double percent, int lowAt) => percent <= lowAt ? 1 : 0;

    private static int FitnessZone(int value) => value < WeakenedBelow ? 2 : value < NearWeakenedBelow ? 1 : 0;
}
