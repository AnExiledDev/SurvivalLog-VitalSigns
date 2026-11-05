using System;
using System.Collections.Generic;

namespace VitalSigns.Core.Acts;

/// <summary>
/// What a click on Morale queues. Best: one action per click, the ones that also raise another stat (or a maximum)
/// first, then the biggest Morale gain; a click while earlier picks are still queued takes the next one down the list
/// instead of the same one again. Routine: every ready action of the player's own list, in that order.
/// </summary>
public static class MoralePlan
{
    public const string ModeBest = "Best";
    public const string ModeRoutine = "Routine";
    public static readonly string[] Modes = { ModeBest, ModeRoutine };

    /// <summary>
    /// The next best action that is not in <paramref name="skip"/> (the keys already picked since the queue was last
    /// empty). Actions that cost Life are left out when <paramref name="avoidLifeLoss"/> is on. Null when there is none.
    /// </summary>
    public static ActOption? Next(IReadOnlyList<ActOption> options, ICollection<string> skip, bool avoidLifeLoss)
    {
        ActOption? best = null;

        foreach (var option in options)
        {
            if (!IsCandidate(option, avoidLifeLoss) || skip.Contains(option.Key)) continue;

            if (best == null || IsBetter(option, best)) best = option;
        }

        return best;
    }

    /// <summary>The ready actions of the routine, in the routine's order; an action listed but not ready now is left out.</summary>
    public static IReadOnlyList<ActOption> Routine(IReadOnlyList<ActOption> options, IReadOnlyList<string> routine)
    {
        var steps = new List<ActOption>();

        foreach (var key in routine)
        {
            foreach (var option in options)
            {
                if (option.Key != key) continue;

                if (option.IsReady && (option.Kind == ActKind.Furniture || option.Count > 0)) steps.Add(option);

                break;
            }
        }

        return steps;
    }

    /// <summary>Every action that raises Morale, in the order Best clicks go down them: the ones that can be picked now first, each part in Best's order.</summary>
    public static List<ActOption> Ranked(IReadOnlyList<ActOption> options, bool avoidLifeLoss)
    {
        var ranked = new List<ActOption>();

        foreach (var option in options)
        {
            if (option.Delta.Morale > 0) ranked.Add(option);
        }

        ranked.Sort((a, b) =>
        {
            var aPick = IsCandidate(a, avoidLifeLoss);
            var bPick = IsCandidate(b, avoidLifeLoss);

            if (aPick != bPick) return aPick ? -1 : 1;

            if (a.Key == b.Key) return 0;

            return IsBetter(a, b) ? -1 : 1;
        });

        return ranked;
    }

    /// <summary>Whether an action can be a Best pick: ready, there, raises Morale and, if asked, costs no Life.</summary>
    public static bool IsCandidate(ActOption option, bool avoidLifeLoss) =>
        option.IsReady && option.Delta.Morale > 0 && (option.Kind == ActKind.Furniture || option.Count > 0) && !(avoidLifeLoss && option.Delta.Life < 0);

    private static bool IsBetter(ActOption a, ActOption b)
    {
        var aOther = a.Delta.RaisesOther(1);
        var bOther = b.Delta.RaisesOther(1);

        if (aOther != bOther) return aOther;

        if (Math.Abs(a.Delta.Morale - b.Delta.Morale) > 1e-9) return a.Delta.Morale > b.Delta.Morale;

        return string.CompareOrdinal(a.Key, b.Key) < 0;
    }
}

/// <summary>
/// What a click on Life uses: the player's Life items in their order, one at a time, while Life is still short of its
/// maximum, up to MostItems. Every use is counted as healing what the item says, so a big item is not used for a small gap
/// only when an earlier item in the list closes it.
/// </summary>
public static class LifePlan
{
    public const int DefaultMostItems = 1;
    public const int MinMostItems = 1;
    public const int MaxMostItems = 10;

    public static IReadOnlyList<PlanStep> Of(IReadOnlyList<ActOption> items, double life, double max, int mostItems)
    {
        var steps = new List<PlanStep>();
        var missing = max - life;
        var left = Math.Clamp(mostItems, MinMostItems, MaxMostItems);

        foreach (var item in items)
        {
            if (!(missing > 1e-6) || left == 0) break;

            if (!item.IsReady || item.Count <= 0 || !(item.Delta.Life > 0)) continue;

            var uses = 0;

            while (uses < item.Count && left > 0 && missing > 1e-6)
            {
                uses++;
                left--;
                missing -= item.Delta.Life;
            }

            steps.Add(new PlanStep(item, uses));
        }

        return steps;
    }
}

/// <summary>
/// What a click on a debuff's cure uses: one of the cures the buff lists, the first one in the bag if any is there,
/// otherwise the first one in the home storage. Null when there is none.
/// </summary>
public static class CurePlan
{
    public static ActOption? Of(IReadOnlyList<ActOption> cures)
    {
        foreach (var cure in cures)
        {
            if (cure.IsReady && cure.InBag > 0) return cure;
        }

        foreach (var cure in cures)
        {
            if (cure.IsReady && cure.Count > 0) return cure;
        }

        return null;
    }
}
