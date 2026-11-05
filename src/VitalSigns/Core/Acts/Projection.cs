using System;
using System.Collections.Generic;

namespace VitalSigns.Core.Acts;

/// <summary>
/// Where each bar will be once the queued actions are done: the bar now plus what every queued action does, held between
/// empty and full, as a share of the bar. A stat no queued action changes has no projection (null), so the HUD draws
/// nothing for it. The maximum is raised first by any MaxMorale or MaxStamina the actions give.
/// </summary>
public static class Projection
{
    /// <summary>Changes smaller than this (in stat points) are not drawn.</summary>
    public const double Least = 0.5;

    public static IReadOnlyList<double?>? Of(IReadOnlyList<(double Now, double Max)> stats, IReadOnlyList<StatDelta> queued)
    {
        if (queued.Count == 0) return null;

        var total = StatDelta.None;

        foreach (var delta in queued) total = total.Plus(delta);

        var shares = new double?[StatDelta.Stats];
        var any = false;

        for (var i = 0; i < StatDelta.Stats && i < stats.Count; i++)
        {
            var change = total[i];

            if (Math.Abs(change) < Least) continue;

            var max = stats[i].Max + (i == 1 ? total.MaxMorale : i == 2 ? total.MaxStamina : 0);

            if (!(max > 0)) continue;

            shares[i] = Math.Clamp((stats[i].Now + change) / max, 0, 1);
            any = true;
        }

        return any ? shares : null;
    }
}
