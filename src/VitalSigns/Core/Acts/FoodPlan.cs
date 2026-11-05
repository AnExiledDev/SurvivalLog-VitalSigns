using System;
using System.Linq;
using System.Collections.Generic;

namespace VitalSigns.Core.Acts;

/// <summary>One thing to queue, and how many times: uses of an item, or 1 for a furniture action.</summary>
public readonly record struct PlanStep(ActOption Option, int Count);

/// <summary>
/// What a click on Satiety eats. Max: as close to the bar's maximum as the food allows without going over. WellFed:
/// just enough to reach Well Fed (the smallest total that gets there). AboveWellFed: just enough to reach Above percent
/// of the maximum past the Well Fed line. Never past the maximum, at most MostItems items. When totals tie it takes
/// fewer items, then more from the bag, then the food that leaves Morale highest.
/// </summary>
public static class FoodPlan
{
    public const string GoalMax = "Max";
    public const string GoalWellFed = "WellFed";
    public const string GoalAboveWellFed = "AboveWellFed";
    public static readonly string[] Goals = { GoalMax, GoalWellFed, GoalAboveWellFed };

    public const int DefaultAbove = 10;
    public const int MinAbove = 0;
    public const int MaxAbove = 100;
    public const int DefaultMostItems = 4;
    public const int MinMostItems = 1;
    public const int MaxMostItems = 10;

    /// <summary>Satiety is measured in whole points here; a bar bigger than this is held to it so a plan stays cheap.</summary>
    private const int MostRoom = 1000;

    /// <summary>Why a plan came back empty, or Planned.</summary>
    public enum Outcome
    {
        Planned,
        Full,
        AtGoal,
        NoFood,
        TooBig,
    }

    public static (IReadOnlyList<PlanStep> Steps, Outcome Outcome) Of(IReadOnlyList<ActOption> foods, double now, double max, double? wellFedFrom, string goal,
        int above, int mostItems)
    {
        var room = (int)Math.Floor(Math.Min(max - now, MostRoom) + 1e-6);

        if (room < 1) return (Array.Empty<PlanStep>(), Outcome.Full);

        var need = Need(now, max, wellFedFrom, goal, above, room);

        if (need == 0) return (Array.Empty<PlanStep>(), Outcome.AtGoal);

        var units = Units(foods, room, Math.Clamp(mostItems, MinMostItems, MaxMostItems));

        if (units.Count == 0) return (Array.Empty<PlanStep>(), foods.Any(f => f.IsReady && f.Count > 0 && f.Delta.Satiety > 0) ? Outcome.TooBig : Outcome.NoFood);

        var best = Fill(units, foods.Count, room, Math.Clamp(mostItems, MinMostItems, MaxMostItems));
        var pick = Pick(best, need, room);

        if (pick == null) return (Array.Empty<PlanStep>(), Outcome.TooBig);

        var steps = new List<PlanStep>();

        for (var i = 0; i < foods.Count; i++)
        {
            if (pick.Counts[i] > 0) steps.Add(new PlanStep(foods[i], pick.Counts[i]));
        }

        return (steps, Outcome.Planned);
    }

    /// <summary>The total the plan has to reach (at most the room), -1 for "as much as fits", or 0 when the goal is already met.</summary>
    private static int Need(double now, double max, double? wellFedFrom, string goal, int above, int room)
    {
        if (wellFedFrom is not { } from || HudChoices.Index(Goals, goal) == 0) return -1;

        var target = from;

        if (string.Equals(goal, GoalAboveWellFed, StringComparison.OrdinalIgnoreCase)) target += Math.Clamp(above, MinAbove, MaxAbove) / 100.0 * max;

        var need = (int)Math.Ceiling(Math.Min(target, max) - now - 1e-6);

        return need <= 0 ? 0 : Math.Min(need, room);
    }

    private readonly record struct Unit(int Food, int Gain, bool InBag, double Morale);

    private static List<Unit> Units(IReadOnlyList<ActOption> foods, int room, int mostItems)
    {
        var units = new List<Unit>();

        for (var i = 0; i < foods.Count; i++)
        {
            var food = foods[i];

            if (!food.IsReady || food.Count <= 0 || !(food.Delta.Satiety > 0)) continue;

            var gain = Math.Max(1, (int)Math.Round(food.Delta.Satiety));

            if (gain > room) continue;

            var count = Math.Min(Math.Min(food.Count, mostItems), room / gain);

            for (var n = 0; n < count; n++) units.Add(new Unit(i, gain, n < food.InBag, food.Delta.Morale));
        }

        return units;
    }

    private sealed class Entry
    {
        public int Items;
        public int Bag;
        public double Morale;
        public int[] Counts = Array.Empty<int>();

        /// <summary>Whether a way to eat with these totals beats <paramref name="other"/>: fewer items, then more from the bag, then more Morale.</summary>
        public static bool Beats(int items, int bag, double morale, Entry? other)
        {
            if (other == null) return true;

            if (items != other.Items) return items < other.Items;

            if (bag != other.Bag) return bag > other.Bag;

            return morale > other.Morale + 1e-9;
        }
    }

    /// <summary>For every whole total from 0 to the room, the best way to eat exactly that much, or null for none.</summary>
    private static Entry?[] Fill(List<Unit> units, int foods, int room, int mostItems)
    {
        var best = new Entry?[room + 1];

        best[0] = new Entry { Counts = new int[foods] };

        foreach (var unit in units)
        {
            for (var s = room; s >= unit.Gain; s--)
            {
                var from = best[s - unit.Gain];

                if (from == null || from.Items >= mostItems) continue;

                var items = from.Items + 1;
                var bag = from.Bag + (unit.InBag ? 1 : 0);
                var morale = from.Morale + unit.Morale;

                if (!Entry.Beats(items, bag, morale, best[s])) continue;

                var made = new Entry { Items = items, Bag = bag, Morale = morale, Counts = (int[])from.Counts.Clone() };

                made.Counts[unit.Food]++;
                best[s] = made;
            }
        }

        return best;
    }

    private static Entry? Pick(Entry?[] best, int need, int room)
    {
        if (need > 0)
        {
            for (var s = need; s <= room; s++)
            {
                if (best[s] != null) return best[s];
            }
        }

        // Max, or a goal the food cannot reach: as much as fits.
        for (var s = room; s > 0; s--)
        {
            if (best[s] != null) return best[s];
        }

        return null;
    }
}
