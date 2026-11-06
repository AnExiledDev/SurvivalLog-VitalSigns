using System;

namespace VitalSigns.Core.Acts;

/// <summary>What a furniture action is as a rest: a full sleep, a nap, a beauty sleep, or no rest at all.</summary>
public enum RestKind
{
    None,
    Sleep,
    Nap,
    Beauty,
}

public static class RestKinds
{
    /// <summary>
    /// A rest by the effect its action runs, not by its id: each bed has its own Sleep and Nap actions (the plain bed
    /// 2 and 4, the Wooden Bed 1708 and 1709, the Iron Single Bed 1800 and 1801), all on the same few sleep effects.
    /// "Action/Sleep_1Hour" is a nap, "Action/Sleep_6Hour_NoSpeed" the beauty sleep, any other "Action/Sleep_" a sleep.
    /// </summary>
    public static RestKind Of(string? effectName)
    {
        if (effectName == null || !effectName.StartsWith("Action/Sleep_", StringComparison.Ordinal)) return RestKind.None;

        if (effectName.StartsWith("Action/Sleep_1Hour", StringComparison.Ordinal)) return RestKind.Nap;

        return effectName.StartsWith("Action/Sleep_6Hour_NoSpeed", StringComparison.Ordinal) ? RestKind.Beauty : RestKind.Sleep;
    }
}
