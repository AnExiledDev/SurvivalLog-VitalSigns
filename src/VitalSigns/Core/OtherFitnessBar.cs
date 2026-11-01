using System;

namespace VitalSigns.Core;

/// <summary>Whether the separate Fitness Bar mod draws a second Fitness bar beside this mod's own.</summary>
public static class OtherFitnessBar
{
    /// <summary>From 0.1.2 Fitness Bar turns itself off when Vital Signs is installed.</summary>
    public static readonly Version StepsAsideFrom = new(0, 1, 2);

    /// <summary>
    /// True only for a Fitness Bar old enough to still draw its bar. Null means it is not installed. A version that does
    /// not read as one is taken as new, so no warning shows for a bar that is not there.
    /// </summary>
    public static bool Draws(string? installedVersion)
    {
        if (installedVersion == null) return false;

        var core = installedVersion.Split('-', '+')[0];

        return Version.TryParse(core, out var version) && version < StepsAsideFrom;
    }
}
