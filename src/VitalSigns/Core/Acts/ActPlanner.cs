using System;
using System.Collections.Generic;

namespace VitalSigns.Core.Acts;

/// <summary>
/// What the base has for the clicks, from one scan: every item with a stat effect (Count in the bag and the reachable
/// home storage, InBag in the bag), every furniture action with a stat effect (Ready at the best spot for it, Busy with
/// the game's reason, Missing when no furniture at home has it), and the beds. Version goes up with each new scan, so a
/// plan made from it can be kept until the next.
/// </summary>
public sealed record BaseView(IReadOnlyList<ActOption> Items, IReadOnlyList<ActOption> Funcs, IReadOnlyList<Bed> Beds, bool AtHome, int Version)
{
    public static readonly BaseView Empty = new(Array.Empty<ActOption>(), Array.Empty<ActOption>(), Array.Empty<Bed>(), false, 0);

    public ActOption? Find(string key)
    {
        var list = key.Length > 0 && key[0] == ActKeys.FurnitureMark ? Funcs : Items;

        foreach (var option in list)
        {
            if (option.Key == key) return option;
        }

        return null;
    }
}

/// <summary>What a click on one bar does: the steps to queue, why there are none, and the line its hover box shows.</summary>
public sealed record BarPlan(int Stat, ActWhy Why, IReadOnlyList<PlanStep> Steps, string Tip)
{
    public bool CanAct => Why == ActWhy.Planned && Steps.Count > 0;

    public static BarPlan Nothing(int stat, ActWhy why, bool isChinese) => new(stat, why, Array.Empty<PlanStep>(), ActTexts.Reason(stat, why, isChinese));
}

/// <summary>
/// Turns the Actions settings, the base and where the bars are into one plan per bar. Pure: the adapters gather the
/// view and queue the steps. With UseHome off only what is in the bag counts.
/// </summary>
public static class ActPlanner
{
    /// <param name="levels">Each bar now and its maximum, in the HUD's row order; with AddToQueue, already moved to where the queue leaves them.</param>
    /// <param name="wellFedFrom">Where Well Fed starts in Satiety points, null when the game has no such buff.</param>
    /// <param name="moraleSkip">The Morale picks already queued since the queue was last empty, so the next click picks something else.</param>
    public static BarPlan Of(int stat, BaseView view, IReadOnlyList<StatLevel> levels, double? wellFedFrom, ActOptions options, ICollection<string> moraleSkip,
        bool isChinese)
    {
        if (!options.IsOn(stat)) return BarPlan.Nothing(stat, ActWhy.Off, isChinese);

        if (stat >= levels.Count || !(levels[stat].Max > 0)) return BarPlan.Nothing(stat, ActWhy.NoPlayer, isChinese);

        var (now, max) = (levels[stat].Value, levels[stat].Max);

        return stat switch
        {
            0 => Food(view, now, max, wellFedFrom, options, isChinese),
            1 => Morale(view, now, max, options, moraleSkip, isChinese),
            2 => Rest(view, now, max, options, isChinese),
            3 => Life(view, now, max, options, isChinese),
            _ => BarPlan.Nothing(stat, ActWhy.Off, isChinese),
        };
    }

    /// <summary>The cure for a debuff: the first of its cure items in the bag, else the first in the home storage.</summary>
    public static ActOption? Cure(IReadOnlyList<int> cureItemIds, BaseView view, bool useHome)
    {
        var cures = new List<ActOption>();

        foreach (var id in cureItemIds)
        {
            if (view.Find(ActKeys.Item(id)) is { } item) cures.Add(Reach(item, useHome));
        }

        return CurePlan.Of(cures);
    }

    /// <summary>The food a Satiety click may eat, in the scan's order: skipped items and kinds and, if asked, food that costs Morale or Life left out.</summary>
    public static List<ActOption> Foods(BaseView view, ActOptions options)
    {
        var skip = options.FoodSkip ?? Array.Empty<string>();
        var foods = new List<ActOption>();

        foreach (var item in view.Items)
        {
            if (!(item.Delta.Satiety > 0) || IsSkipped(skip, item)) continue;

            if (options.FoodAvoidMoraleLoss && item.Delta.Morale < 0) continue;

            if (options.AvoidLifeLoss && item.Delta.Life < 0) continue;

            foods.Add(Reach(item, options.UseHome));
        }

        return foods;
    }

    /// <summary>
    /// The Life items a click uses, in order: the player's list (which may hold nothing), or, with the setting blank
    /// (null here), every Life item smallest first so a big one is kept for a big need. A group in the list stands for
    /// every item of that kind the player has, the most Life first; an item already taken is not taken again.
    /// </summary>
    public static List<ActOption> LifeItems(BaseView view, ActOptions options)
    {
        var items = new List<ActOption>();

        if (options.LifeItems is { } chosen)
        {
            var taken = new HashSet<string>();

            foreach (var key in chosen)
            {
                if (ActKeys.IsGroup(key))
                {
                    var kind = new List<ActOption>();

                    foreach (var item in view.Items)
                    {
                        if (item.Delta.Life > 0 && ActKeys.Holds(key, item) && !taken.Contains(item.Key)) kind.Add(item);
                    }

                    kind.Sort((a, b) => a.Delta.Life != b.Delta.Life ? b.Delta.Life.CompareTo(a.Delta.Life) : string.CompareOrdinal(a.Key, b.Key));

                    foreach (var item in kind)
                    {
                        taken.Add(item.Key);
                        items.Add(Reach(item, options.UseHome));
                    }
                }
                else if (view.Find(key) is { Kind: ActKind.Item } item && item.Delta.Life > 0 && taken.Add(item.Key))
                {
                    items.Add(Reach(item, options.UseHome));
                }
            }

            return items;
        }

        foreach (var item in view.Items)
        {
            if (item.Delta.Life > 0) items.Add(Reach(item, options.UseHome));
        }

        items.Sort((a, b) => a.Delta.Life != b.Delta.Life ? a.Delta.Life.CompareTo(b.Delta.Life) : string.CompareOrdinal(a.Key, b.Key));

        return items;
    }

    /// <summary>What a Best click on Morale chooses from: every furniture action, and the items too when MoraleItems is on.</summary>
    public static List<ActOption> MoraleOptions(BaseView view, ActOptions options)
    {
        var all = new List<ActOption>(view.Funcs);

        if (!options.MoraleItems) return all;

        foreach (var item in view.Items)
        {
            if (item.Delta.Morale > 0) all.Add(Reach(item, options.UseHome));
        }

        return all;
    }

    private static BarPlan Food(BaseView view, double now, double max, double? wellFedFrom, ActOptions options, bool isChinese)
    {
        var (steps, outcome) = FoodPlan.Of(Foods(view, options), now, max, wellFedFrom, options.FoodGoal, options.FoodAbove, options.FoodMostItems);

        return outcome switch
        {
            FoodPlan.Outcome.Planned => Planned(0, steps, isChinese),
            FoodPlan.Outcome.Full => BarPlan.Nothing(0, ActWhy.Full, isChinese),
            FoodPlan.Outcome.AtGoal => BarPlan.Nothing(0, ActWhy.AtGoal, isChinese),
            FoodPlan.Outcome.TooBig => BarPlan.Nothing(0, ActWhy.TooBig, isChinese),
            _ => BarPlan.Nothing(0, ActWhy.NoFood, isChinese),
        };
    }

    private static BarPlan Morale(BaseView view, double now, double max, ActOptions options, ICollection<string> skip, bool isChinese)
    {
        if (!(max - now > 1e-6)) return BarPlan.Nothing(1, ActWhy.Full, isChinese);

        if (HudChoices.Index(MoralePlan.Modes, options.MoraleMode) == 1)
        {
            var routine = options.Routine ?? Array.Empty<string>();

            if (routine.Count == 0) return BarPlan.Nothing(1, ActWhy.RoutineEmpty, isChinese);

            var all = new List<ActOption>(view.Funcs);

            foreach (var item in view.Items) all.Add(Reach(item, options.UseHome));

            var picked = MoralePlan.Routine(all, routine);

            if (picked.Count == 0) return BarPlan.Nothing(1, ActWhy.RoutineNotReady, isChinese);

            var steps = new List<PlanStep>(picked.Count);

            foreach (var option in picked) steps.Add(new PlanStep(option, 1));

            return Planned(1, steps, isChinese);
        }

        var choices = MoraleOptions(view, options);

        // Once every action has been picked since the queue emptied, a click starts down the list again.
        var next = MoralePlan.Next(choices, skip, options.AvoidLifeLoss) ?? (skip.Count > 0 ? MoralePlan.Next(choices, Array.Empty<string>(), options.AvoidLifeLoss) : null);

        return next == null ? BarPlan.Nothing(1, ActWhy.NoMorale, isChinese) : Planned(1, new[] { new PlanStep(next, 1) }, isChinese);
    }

    private static BarPlan Rest(BaseView view, double now, double max, ActOptions options, bool isChinese)
    {
        var (rest, bed, outcome) = SleepPlan.Of(view.Beds, now, max, SleepPlan.ParseModes(options.BedModes), options.NapBelow);

        return outcome switch
        {
            SleepPlan.Outcome.Planned when rest != null && bed != null => Planned(2, new[] { new PlanStep(rest with { Place = bed.Name, Target = bed.FurnitureId }, 1) }, isChinese),
            SleepPlan.Outcome.Full => BarPlan.Nothing(2, ActWhy.Full, isChinese),
            SleepPlan.Outcome.NotReady => BarPlan.Nothing(2, ActWhy.BedNotReady, isChinese),
            _ => BarPlan.Nothing(2, ActWhy.NoBed, isChinese),
        };
    }

    private static BarPlan Life(BaseView view, double now, double max, ActOptions options, bool isChinese)
    {
        if (!(max - now > 1e-6)) return BarPlan.Nothing(3, ActWhy.Full, isChinese);

        var steps = LifePlan.Of(LifeItems(view, options), now, max, options.LifeMostItems);

        return steps.Count == 0 ? BarPlan.Nothing(3, ActWhy.NoLifeItems, isChinese) : Planned(3, steps, isChinese);
    }

    private static BarPlan Planned(int stat, IReadOnlyList<PlanStep> steps, bool isChinese) => new(stat, ActWhy.Planned, steps, ActTexts.Planned(stat, steps, isChinese));

    /// <summary>With UseHome off, only the bag's own count of an item counts.</summary>
    private static ActOption Reach(ActOption item, bool useHome) => useHome || item.Count == item.InBag ? item : item with { Count = item.InBag };

    /// <summary>Whether a food is on the skip list itself or of a kind that is.</summary>
    private static bool IsSkipped(IReadOnlyList<string> keys, ActOption item)
    {
        foreach (var k in keys)
        {
            if (k == item.Key || ActKeys.Holds(k, item)) return true;
        }

        return false;
    }
}
