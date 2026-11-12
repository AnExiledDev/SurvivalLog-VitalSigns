using System;
using System.Collections.Generic;

namespace VitalSigns.Core;

/// <summary>
/// Reads Satiety, Morale, Stamina and Life with their maximums, by the game's attribute ids. The game keeps each
/// maximum as a whole number, and its float read of a whole-number attribute returns 0
/// (AttributeComponent.GetTotalValue_Float logs a warning to Player.log and gives 0), so maximums must go through the
/// int read. LeadingRole.SyncAttr2UI reads them the same way.
/// </summary>
public static class StatReads
{
    /// <summary>Satiety, Morale, Stamina, Vitality (the game's name for Life).</summary>
    public static readonly int[] ValueIds = { 1, 2, 3, 5 };

    /// <summary>MaxSatiety, MaxMorale, MaxStamina, MaxVitality.</summary>
    public static readonly int[] MaxIds = { 101, 102, 103, 105 };

    /// <summary>Each stat's value and maximum. A stat the player lacks reads 0 of 0, which never warns or plans.</summary>
    public static List<StatLevel> Levels(Func<int, bool> has, Func<int, double> readFloat, Func<int, int> readInt)
    {
        var levels = new List<StatLevel>(ValueIds.Length);

        for (var i = 0; i < ValueIds.Length; i++)
        {
            levels.Add(has(ValueIds[i]) && has(MaxIds[i]) ? new StatLevel(readFloat(ValueIds[i]), readInt(MaxIds[i])) : new StatLevel(0, 0));
        }

        return levels;
    }

    /// <summary>Each stat's maximum, or <paramref name="missing"/> for a stat the player lacks.</summary>
    public static double[] Maxes(Func<int, bool> has, Func<int, int> readInt, double missing)
    {
        var max = new double[MaxIds.Length];

        for (var i = 0; i < max.Length; i++) max[i] = has(MaxIds[i]) ? readInt(MaxIds[i]) : missing;

        return max;
    }
}
