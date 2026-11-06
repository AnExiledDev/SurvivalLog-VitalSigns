using System.Collections.Generic;
using System.Linq;
using VitalSigns.Core;
using VitalSigns.Core.Acts;
using Xunit;

namespace VitalSigns.Tests;

/// <summary>The scaling behind what a click counts on: food types, the stat difficulty, uses already queued, and the Best ranking.</summary>
public class PlanScaleTests
{
    [Fact]
    public void Food_gains_take_the_food_type_ratio_then_the_stat_gain_ratio_and_losses_the_loss_ratio()
    {
        var ratios = new[] { new StatRatios(0.5, 0, 0, 0), new StatRatios(0, 1, 0, 0), new StatRatios(0, 0, 0, 0), new StatRatios(0, 0, 0, 0) };
        var scale = new StatScale(ratios, new[] { new FoodMod(0, 3, 0.2) }, 0.25);

        var meat = scale.Item(new StatDelta(10, -2, 0, 4), 3);

        Assert.Equal(10 * 1.2 * 1.5, meat.Satiety, 6);
        Assert.Equal(-4, meat.Morale, 6);
        Assert.Equal(5, meat.Life, 6);

        // Another food type gets no food ratio, only the stat's.
        Assert.Equal(15, scale.Item(new StatDelta(10, 0, 0, 0), 1).Satiety, 6);
    }

    [Fact]
    public void The_stat_difficulty_scales_what_a_plan_counts_on_as_it_scales_the_change_in_game()
    {
        // Satiety gains twice, Morale falls three times as fast, Life gains nothing: a plan must count on that, or a
        // click to the maximum eats twice what fits.
        var difficulty = new Difficulty(new[] { 1.0, 3, 1, 1 }, new[] { 2.0, 1, 1, 0 });
        var scale = StatScale.None with { Difficulty = difficulty };

        var meal = scale.Item(new StatDelta(10, -2, 0, 4), 3);

        Assert.Equal(20, meal.Satiety, 6);
        Assert.Equal(-6, meal.Morale, 6);
        Assert.Equal(0, meal.Life, 6);

        var bread = new ActOption(ActKeys.Item(5), ActKind.Item, 5, "Bread", "", new StatDelta(10, 0, 0, 0), ActState.Ready, Count: 1, InBag: 1);
        var view = ViewScale.Of(new BaseView(new[] { bread }, new ActOption[0], new Bed[0], true, 1), scale, new Dictionary<int, int>(), new Dictionary<int, int>());

        Assert.Equal(20, Assert.Single(view.Items).Delta.Satiety, 6);
    }

    [Fact]
    public void Uses_waiting_in_the_queue_come_off_the_bag_first_and_an_item_with_none_left_drops_out()
    {
        var bread = new ActOption(ActKeys.Item(5), ActKind.Item, 5, "Bread", "", new StatDelta(10, 0, 0, 0), ActState.Ready, Count: 3, InBag: 2);
        var tea = new ActOption(ActKeys.Item(6), ActKind.Item, 6, "Tea", "", new StatDelta(0, 5, 0, 0), ActState.Ready, Count: 1, InBag: 1);
        var raw = new BaseView(new[] { bread, tea }, new ActOption[0], new Bed[0], true, 1);

        var view = ViewScale.Of(raw, StatScale.None, new Dictionary<int, int>(), new Dictionary<int, int> { [5] = 1, [6] = 1 });

        var left = Assert.Single(view.Items);
        Assert.Equal("i5", left.Key);
        Assert.Equal(2, left.Count);
        Assert.Equal(1, left.InBag);
    }

    [Fact]
    public void Best_morale_ranks_the_ones_raising_another_stat_first_and_puts_life_costs_last()
    {
        var plain = new ActOption(ActKeys.Furniture(1), ActKind.Furniture, 1, "Read", "", new StatDelta(0, 20, 0, 0), ActState.Ready);
        var both = new ActOption(ActKeys.Furniture(2), ActKind.Furniture, 2, "Snack", "", new StatDelta(5, 8, 0, 0), ActState.Ready);
        var hurts = new ActOption(ActKeys.Furniture(3), ActKind.Furniture, 3, "Brawl", "", new StatDelta(0, 30, 0, -5), ActState.Ready);

        var ranked = MoralePlan.Ranked(new[] { plain, both, hurts }, avoidLifeLoss: true).Select(o => o.Key).ToList();

        Assert.Equal(new[] { "f2", "f1", "f3" }, ranked);
    }

    [Fact]
    public void The_hud_messages_parse_into_the_commands_the_plugin_handles()
    {
        Assert.Equal(new HudCommand.Edit(true), HudCommand.Parse(HudCommand.HudPrefix + "edit|1"));
        Assert.Equal(new HudCommand.Layout("abc", 2, true), HudCommand.Parse(HudCommand.HudPrefix + "layout|abc|2|1"));
        Assert.IsType<HudCommand.Unknown>(HudCommand.Parse(HudCommand.HudPrefix + "layout|abc|9|1"));
        Assert.IsType<HudCommand.Unknown>(HudCommand.Parse("slmod|vitalsigns|actions|load"));
    }
}
