using System.Collections.Generic;
using VitalSigns.Timers.Core;
using Xunit;

namespace VitalSigns.Tests.Timers;

public class MultiplierPlanTests
{
    private static readonly double?[] NoCategories = new double?[MultiplierPlan.CategoryCount];

    private static double?[] Categories(int index, double value)
    {
        var categories = new double?[MultiplierPlan.CategoryCount];

        categories[index] = value;

        return categories;
    }

    [Fact]
    public void Good_and_bad_buffs_take_their_own_global_multiplier()
    {
        var plan = new MultiplierPlan(2, 0.5, NoCategories, new Dictionary<int, double>());

        Assert.Equal(new Resolved(2, Source.Global), plan.For(new BuffKind(1, true, 3)));
        Assert.Equal(new Resolved(0.5, Source.Global), plan.For(new BuffKind(2, false, 3)));
    }

    [Fact]
    public void A_category_multiplier_beats_the_global_one_for_good_and_bad_alike()
    {
        var plan = new MultiplierPlan(2, 0.5, Categories(4, 3), new Dictionary<int, double>());

        Assert.Equal(new Resolved(3, Source.Category), plan.For(new BuffKind(1, true, 4)));
        Assert.Equal(new Resolved(3, Source.Category), plan.For(new BuffKind(2, false, 4)));
        Assert.Equal(new Resolved(2, Source.Global), plan.For(new BuffKind(3, true, 5)));
    }

    [Fact]
    public void A_buffs_own_multiplier_beats_its_category()
    {
        var plan = new MultiplierPlan(2, 0.5, Categories(4, 3), new Dictionary<int, double> { [7] = 10 });

        Assert.Equal(new Resolved(10, Source.Buff), plan.For(new BuffKind(7, true, 4)));
    }

    [Fact]
    public void A_category_set_to_zero_uses_the_global_multiplier()
    {
        var plan = new MultiplierPlan(2, 0.5, Categories(4, 0), new Dictionary<int, double>());

        Assert.Null(plan.Categories[4]);
        Assert.Equal(Source.Global, plan.For(new BuffKind(1, true, 4)).Source);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(7)]
    [InlineData(99)]
    public void A_category_the_game_does_not_have_falls_back_to_global(int category)
    {
        var plan = new MultiplierPlan(2, 0.5, Categories(0, 3), new Dictionary<int, double>());

        Assert.Equal(new Resolved(2, Source.Global), plan.For(new BuffKind(1, true, category)));
    }

    [Fact]
    public void Values_outside_the_range_are_held_to_it_when_the_plan_is_made()
    {
        var plan = new MultiplierPlan(500, 0.001, Categories(1, 1000), new Dictionary<int, double> { [5] = 0.0001, [6] = 0, [8] = -2 });

        Assert.Equal(Limits.Max, plan.Good);
        Assert.Equal(Limits.Min, plan.Bad);
        Assert.Equal(Limits.Max, plan.Categories[1]);
        Assert.Equal(Limits.Min, plan.Buffs[5]);
        Assert.False(plan.Buffs.ContainsKey(6));
        Assert.False(plan.Buffs.ContainsKey(8));
    }

    [Fact]
    public void A_plan_of_ones_changes_nothing_and_any_other_value_does()
    {
        Assert.False(MultiplierPlan.Unchanged.ChangesAnything);
        Assert.False(new MultiplierPlan(1, 1, Categories(2, 1), new Dictionary<int, double> { [3] = 1 }).ChangesAnything);
        Assert.True(new MultiplierPlan(1, 1, Categories(2, 1.5), new Dictionary<int, double>()).ChangesAnything);
        Assert.True(new MultiplierPlan(1, 1, NoCategories, new Dictionary<int, double> { [3] = 0.5 }).ChangesAnything);
        Assert.True(new MultiplierPlan(1, 2, NoCategories, new Dictionary<int, double>()).ChangesAnything);
    }

    [Fact]
    public void WithBuffs_keeps_the_rest_and_replaces_only_the_buffs()
    {
        var plan = new MultiplierPlan(2, 0.5, Categories(4, 3), new Dictionary<int, double> { [7] = 10 }).WithBuffs(new Dictionary<int, double> { [9] = 4 });

        Assert.Equal(2, plan.Good);
        Assert.Equal(3, plan.Categories[4]);
        Assert.False(plan.Buffs.ContainsKey(7));
        Assert.Equal(4, plan.Buffs[9]);
    }
}
