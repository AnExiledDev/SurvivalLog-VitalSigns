using System;
using System.Collections.Generic;
using System.Globalization;

namespace VitalSigns.Core.Acts;

/// <summary>
/// A bed at home and the rests it offers now: a full sleep, a nap and the beauty sleep only some beds have. An option
/// is null when that bed type has no such action. ConfigId is the bed type, which the Sleep Planner keys its modes by.
/// </summary>
public sealed record Bed(long FurnitureId, int ConfigId, string Name, ActOption? Sleep, ActOption? Nap, ActOption? Beauty, double Restore, bool SameFloor);

/// <summary>
/// What a click on Stamina does: the best rest in the best bed. Each bed type has a mode. Auto naps when only a little
/// stamina is missing (NapBelow percent of the maximum or less) and sleeps otherwise; Sleep and Nap always do that; Skip
/// leaves the bed type out. A sleep is a beauty sleep when the bed has one ready. When the wanted rest is not ready the
/// other one is used. Beds are ranked by: the wanted rest, then a beauty sleep, then the bed's restore bonus, then the
/// same floor as the player.
/// </summary>
public static class SleepPlan
{
    public const string ModeAuto = "Auto";
    public const string ModeSleep = "Sleep";
    public const string ModeNap = "Nap";
    public const string ModeSkip = "Skip";
    public static readonly string[] Modes = { ModeAuto, ModeSleep, ModeNap, ModeSkip };

    public const int DefaultNapBelow = 25;
    public const int MinNapBelow = 0;
    public const int MaxNapBelow = 100;

    public enum Outcome
    {
        Planned,
        Full,
        NoBed,
        NotReady,
    }

    public static (ActOption? Choice, Bed? Bed, Outcome Outcome) Of(IReadOnlyList<Bed> beds, double stamina, double max, IReadOnlyDictionary<int, string> modes,
        int napBelow)
    {
        var missing = max - stamina;

        if (!(missing > 1e-6)) return (null, null, Outcome.Full);

        var napIsEnough = missing <= Math.Clamp(napBelow, MinNapBelow, MaxNapBelow) / 100.0 * max + 1e-6;
        ActOption? bestRest = null;
        Bed? bestBed = null;
        var bestScore = (Wanted: false, Beauty: false, Restore: 0.0, Floor: false);
        var anyBed = false;

        foreach (var bed in beds)
        {
            var mode = ModeOf(modes, bed.ConfigId);

            if (mode == ModeSkip) continue;

            if (bed.Sleep == null && bed.Nap == null && bed.Beauty == null) continue;

            anyBed = true;

            var wantNap = mode == ModeNap || (mode == ModeAuto && napIsEnough);
            var sleep = Ready(bed.Beauty) ?? Ready(bed.Sleep);
            var nap = Ready(bed.Nap);
            var rest = wantNap ? nap ?? sleep : sleep ?? nap;

            if (rest == null) continue;

            var score = (Wanted: wantNap ? rest == nap : rest == sleep, Beauty: rest == bed.Beauty, bed.Restore, Floor: bed.SameFloor);

            if (bestRest != null && !IsBetter(score, bestScore)) continue;

            bestRest = rest;
            bestBed = bed;
            bestScore = score;
        }

        if (bestRest != null) return (bestRest, bestBed, Outcome.Planned);

        return (null, null, anyBed ? Outcome.NotReady : Outcome.NoBed);
    }

    private static ActOption? Ready(ActOption? option) => option is { IsReady: true } ? option : null;

    private static bool IsBetter((bool Wanted, bool Beauty, double Restore, bool Floor) a, (bool Wanted, bool Beauty, double Restore, bool Floor) b)
    {
        if (a.Wanted != b.Wanted) return a.Wanted;

        if (a.Beauty != b.Beauty) return a.Beauty;

        if (Math.Abs(a.Restore - b.Restore) > 1e-9) return a.Restore > b.Restore;

        return a.Floor && !b.Floor;
    }

    public static string ModeOf(IReadOnlyDictionary<int, string> modes, int bedType) =>
        modes.TryGetValue(bedType, out var mode) ? Modes[HudChoices.Index(Modes, mode)] : ModeAuto;

    /// <summary>The per-bed modes in a setting such as "9076:Sleep,320:Nap". A bed type not listed is Auto.</summary>
    public static IReadOnlyDictionary<int, string> ParseModes(string? text)
    {
        var modes = new Dictionary<int, string>();

        foreach (var part in (text ?? "").Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var colon = part.IndexOf(':');

            if (colon <= 0 || !int.TryParse(part.Substring(0, colon), NumberStyles.None, CultureInfo.InvariantCulture, out var id) || id <= 0) continue;

            var mode = part.Substring(colon + 1).Trim();
            var index = HudChoices.Index(Modes, mode);

            if (!string.Equals(Modes[index], mode, StringComparison.OrdinalIgnoreCase)) continue;

            modes[id] = Modes[index];
        }

        return modes;
    }

    /// <summary>The setting with one bed type's mode changed. Auto is the default, so it is written by leaving the bed type out.</summary>
    public static string SetMode(string? text, int bedType, string mode)
    {
        var modes = new SortedDictionary<int, string>();

        foreach (var pair in ParseModes(text)) modes[pair.Key] = pair.Value;

        var normal = Modes[HudChoices.Index(Modes, mode)];

        if (normal == ModeAuto) modes.Remove(bedType);
        else modes[bedType] = normal;

        var parts = new List<string>();

        foreach (var pair in modes) parts.Add(pair.Key.ToString(CultureInfo.InvariantCulture) + ":" + pair.Value);

        return string.Join(",", parts);
    }
}
