using System.Collections.Generic;
using System.Linq;
using VitalSigns.Core.Acts;
using Xunit;

namespace VitalSigns.Tests;

public class ActsTests
{
    private static ActOption Food(int id, double satiety, int count, int inBag = 0, double morale = 0) =>
        new(ActKeys.Item(id), ActKind.Item, id, "food" + id, "", new StatDelta(satiety, morale, 0, 0), ActState.Ready, Count: count, InBag: inBag);

    private static ActOption Act(int id, StatDelta delta, ActState state = ActState.Ready) =>
        new(ActKeys.Furniture(id), ActKind.Furniture, id, "act" + id, "", delta, state);

    private static Dictionary<string, int> Eaten(IReadOnlyList<PlanStep> steps) => steps.ToDictionary(step => step.Option.Key, step => step.Count);

    [Fact]
    public void Max_gets_as_close_to_full_as_the_food_allows_without_going_over()
    {
        // 70 missing: 40 + 30 fills it exactly, where the greedy biggest-first 40 + 25 would stop at 65.
        var foods = new[] { Food(1, 40, 1), Food(2, 30, 1), Food(3, 25, 2) };

        var (steps, outcome) = FoodPlan.Of(foods, 30, 100, null, FoodPlan.GoalMax, 10, 4);

        Assert.Equal(FoodPlan.Outcome.Planned, outcome);
        Assert.Equal(new Dictionary<string, int> { ["i1"] = 1, ["i2"] = 1 }, Eaten(steps));
    }

    [Fact]
    public void Food_bigger_than_the_room_left_is_never_eaten()
    {
        var (steps, outcome) = FoodPlan.Of(new[] { Food(1, 50, 3) }, 70, 100, null, FoodPlan.GoalMax, 10, 4);

        Assert.Empty(steps);
        Assert.Equal(FoodPlan.Outcome.TooBig, outcome);
    }

    [Fact]
    public void Well_fed_eats_the_smallest_total_that_reaches_the_line()
    {
        // Well Fed from 80 with 50 now: 30 needed. 35 is the smallest total that gets there (not 40 + 25 = 65).
        var foods = new[] { Food(1, 40, 1), Food(2, 35, 1), Food(3, 25, 1) };

        var (steps, _) = FoodPlan.Of(foods, 50, 100, 80, FoodPlan.GoalWellFed, 10, 4);

        Assert.Equal(new Dictionary<string, int> { ["i2"] = 1 }, Eaten(steps));
    }

    [Fact]
    public void Above_well_fed_aims_the_set_percent_past_the_line()
    {
        // Line 60 + 20% of 100 = 80; from 50 that is 30, and only 10 + 10 + 10 reaches it.
        var (steps, _) = FoodPlan.Of(new[] { Food(1, 10, 5) }, 50, 100, 60, FoodPlan.GoalAboveWellFed, 20, 5);

        Assert.Equal(new Dictionary<string, int> { ["i1"] = 3 }, Eaten(steps));
    }

    [Fact]
    public void Already_at_the_goal_eats_nothing()
    {
        var (steps, outcome) = FoodPlan.Of(new[] { Food(1, 10, 5) }, 85, 100, 80, FoodPlan.GoalWellFed, 10, 4);

        Assert.Empty(steps);
        Assert.Equal(FoodPlan.Outcome.AtGoal, outcome);
    }

    [Fact]
    public void Equal_totals_take_fewer_items_then_the_bag_then_more_morale()
    {
        var fewer = FoodPlan.Of(new[] { Food(1, 10, 2), Food(2, 20, 1) }, 80, 100, null, FoodPlan.GoalMax, 10, 4).Steps;
        var bag = FoodPlan.Of(new[] { Food(1, 20, 1), Food(2, 20, 1, inBag: 1) }, 80, 100, null, FoodPlan.GoalMax, 10, 4).Steps;
        var morale = FoodPlan.Of(new[] { Food(1, 20, 1), Food(2, 20, 1, morale: 5) }, 80, 100, null, FoodPlan.GoalMax, 10, 4).Steps;

        Assert.Equal(new Dictionary<string, int> { ["i2"] = 1 }, Eaten(fewer));
        Assert.Equal(new Dictionary<string, int> { ["i2"] = 1 }, Eaten(bag));
        Assert.Equal(new Dictionary<string, int> { ["i2"] = 1 }, Eaten(morale));
    }

    [Fact]
    public void No_more_than_the_set_number_of_items_is_eaten()
    {
        var (steps, _) = FoodPlan.Of(new[] { Food(1, 5, 20) }, 0, 100, null, FoodPlan.GoalMax, 10, 3);

        Assert.Equal(3, steps.Sum(step => step.Count));
    }

    [Fact]
    public void Food_that_is_not_ready_is_left_alone()
    {
        var blocked = Food(1, 30, 1) with { State = ActState.Busy };

        var (steps, outcome) = FoodPlan.Of(new[] { blocked }, 0, 100, null, FoodPlan.GoalMax, 10, 4);

        Assert.Empty(steps);
        Assert.Equal(FoodPlan.Outcome.NoFood, outcome);
    }

    private static Bed BedOf(long id, int type, bool beauty = false, double restore = 0, bool sameFloor = true, ActState sleepState = ActState.Ready) =>
        new(id, type, "bed" + type, Act(2, new StatDelta(0, 0, 50, 0), sleepState) with { Target = id }, Act(4, new StatDelta(0, 0, 10, 0)) with { Target = id },
            beauty ? Act(51, new StatDelta(0, 0, 60, 0)) with { Target = id } : null, restore, sameFloor);

    private static readonly IReadOnlyDictionary<int, string> NoModes = new Dictionary<int, string>();

    [Fact]
    public void A_big_gap_sleeps_in_the_bed_with_beauty_sleep()
    {
        var (rest, bed, _) = SleepPlan.Of(new[] { BedOf(1, 320, restore: 5), BedOf(2, 9076, beauty: true) }, 20, 100, NoModes, 25);

        Assert.Equal(2, bed!.FurnitureId);
        Assert.Equal("f51", rest!.Key);
    }

    [Fact]
    public void A_small_gap_naps_in_auto_mode()
    {
        var (rest, _, _) = SleepPlan.Of(new[] { BedOf(1, 320) }, 80, 100, NoModes, 25);

        Assert.Equal("f4", rest!.Key);
    }

    [Fact]
    public void A_bed_type_set_to_skip_is_never_used_and_one_set_to_sleep_always_sleeps()
    {
        var modes = SleepPlan.ParseModes("9076:Skip,320:Sleep");

        var (rest, bed, _) = SleepPlan.Of(new[] { BedOf(1, 320), BedOf(2, 9076, beauty: true) }, 90, 100, modes, 25);

        Assert.Equal(1, bed!.FurnitureId);
        Assert.Equal("f2", rest!.Key);
    }

    [Fact]
    public void When_the_wanted_rest_is_not_ready_the_other_is_used()
    {
        var (rest, _, outcome) = SleepPlan.Of(new[] { BedOf(1, 320, sleepState: ActState.Busy) }, 10, 100, NoModes, 25);

        Assert.Equal(SleepPlan.Outcome.Planned, outcome);
        Assert.Equal("f4", rest!.Key);
    }

    [Fact]
    public void Ties_go_to_the_bed_with_more_restore_then_the_same_floor()
    {
        var (_, byRestore, _) = SleepPlan.Of(new[] { BedOf(1, 320, restore: 1), BedOf(2, 371, restore: 3) }, 0, 100, NoModes, 25);
        var (_, byFloor, _) = SleepPlan.Of(new[] { BedOf(1, 320, sameFloor: false), BedOf(2, 371) }, 0, 100, NoModes, 25);

        Assert.Equal(2, byRestore!.FurnitureId);
        Assert.Equal(2, byFloor!.FurnitureId);
    }

    [Fact]
    public void Full_stamina_rests_nowhere()
    {
        Assert.Equal(SleepPlan.Outcome.Full, SleepPlan.Of(new[] { BedOf(1, 320) }, 100, 100, NoModes, 25).Outcome);
    }

    [Fact]
    public void Setting_a_bed_back_to_auto_takes_it_out_of_the_setting()
    {
        Assert.Equal("320:Nap,9076:Sleep", SleepPlan.SetMode("9076:Sleep", 320, "Nap"));
        Assert.Equal("320:Nap", SleepPlan.SetMode("320:Nap,9076:Sleep", 9076, "Auto"));
        Assert.Empty(SleepPlan.ParseModes("320:Lie,x:Nap"));
    }

    [Fact]
    public void Best_morale_takes_actions_that_raise_other_stats_first_then_the_biggest_gain()
    {
        var options = new[] { Act(1, new StatDelta(0, 30, 0, 0)), Act(2, new StatDelta(0, 5, 10, 0)), Act(3, new StatDelta(0, 12, 0, 0)) };

        Assert.Equal("f2", MoralePlan.Next(options, new HashSet<string>(), true)!.Key);
        Assert.Equal("f1", MoralePlan.Next(options, new HashSet<string> { "f2" }, true)!.Key);
        Assert.Equal("f3", MoralePlan.Next(options, new HashSet<string> { "f2", "f1" }, true)!.Key);
    }

    [Fact]
    public void Morale_actions_that_cost_life_are_skipped_when_asked()
    {
        var bath = Act(208, new StatDelta(0, 30, 0, -12));
        var yoga = Act(35, new StatDelta(0, 12, 0, 0));

        Assert.Equal("f35", MoralePlan.Next(new[] { bath, yoga }, new HashSet<string>(), true)!.Key);
        Assert.Equal("f208", MoralePlan.Next(new[] { bath, yoga }, new HashSet<string>(), false)!.Key);
    }

    [Fact]
    public void The_routine_queues_its_ready_actions_in_its_own_order()
    {
        var options = new[] { Act(1, new StatDelta(0, 5, 0, 0)), Act(2, new StatDelta(0, 5, 0, 0), ActState.Busy), Act(3, new StatDelta(0, 5, 0, 0)) };

        var steps = MoralePlan.Routine(options, ActKeys.Parse("f3,f2,f1,f9"));

        Assert.Equal(new[] { "f3", "f1" }, steps.Select(step => step.Key));
    }

    [Fact]
    public void Life_items_are_used_in_order_until_the_gap_closes()
    {
        var bandage = new ActOption("i5", ActKind.Item, 5, "b", "", new StatDelta(0, 0, 0, 10), ActState.Ready, Count: 5);
        var kit = new ActOption("i6", ActKind.Item, 6, "k", "", new StatDelta(0, 0, 0, 40), ActState.Ready, Count: 1);

        var steps = LifePlan.Of(new[] { bandage, kit }, 75, 100, 5);

        Assert.Equal(new Dictionary<string, int> { ["i5"] = 3 }, Eaten(steps));
        Assert.Single(LifePlan.Of(new[] { bandage, kit }, 0, 100, 1));
        Assert.Empty(LifePlan.Of(new[] { bandage }, 100, 100, 5));
    }

    [Fact]
    public void A_blank_Life_list_uses_every_Life_item_smallest_first_and_a_set_one_only_its_own()
    {
        var kit = new ActOption("i6", ActKind.Item, 6, "k", "", new StatDelta(0, 0, 0, 40), ActState.Ready, Count: 1);
        var bandage = new ActOption("i5", ActKind.Item, 5, "b", "", new StatDelta(0, 0, 0, 10), ActState.Ready, Count: 5);
        var view = new BaseView(new[] { kit, bandage }, new ActOption[0], new Bed[0], true, 1);

        Assert.Equal(new[] { "i5", "i6" }, ActPlanner.LifeItems(view, new ActOptions(LifeItems: null)).Select(item => item.Key));
        Assert.Equal(new[] { "i6" }, ActPlanner.LifeItems(view, new ActOptions(LifeItems: ActKeys.Parse("i6"))).Select(item => item.Key));
    }

    [Fact]
    public void A_cure_in_the_bag_is_used_before_one_in_storage()
    {
        var stored = new ActOption("i1", ActKind.Item, 1, "a", "", StatDelta.None, ActState.Ready, Count: 2);
        var carried = new ActOption("i2", ActKind.Item, 2, "b", "", StatDelta.None, ActState.Ready, Count: 1, InBag: 1);

        Assert.Equal("i2", CurePlan.Of(new[] { stored, carried })!.Key);
        Assert.Equal("i1", CurePlan.Of(new[] { stored })!.Key);
        Assert.Null(CurePlan.Of(new[] { stored with { Count = 0 } }));
    }

    [Fact]
    public void Keys_parse_bare_numbers_as_items_and_toggle_and_move()
    {
        Assert.Equal(new[] { "i12", "f5" }, ActKeys.Parse("12, F5, i12, x, f0"));
        Assert.Equal("f5", ActKeys.Toggle("i12,f5", "i12"));
        Assert.Equal("f5,i12", ActKeys.Move("i12,f5", "f5", -1));
        Assert.Equal(new[] { 12 }, ActKeys.Items("i12,f5"));
    }

    [Fact]
    public void A_furniture_preview_reads_into_stats()
    {
        Assert.Equal(new StatDelta(-10, 5, 0, -2, 2, 0), StatDelta.Parse("Satiety:-10;Morale:+5;MaxMorale:+2;Vitality:-2;Odd:3;Morale:x"));
    }

    [Fact]
    public void The_projection_adds_up_the_queue_and_holds_the_bar_between_empty_and_full()
    {
        var stats = new[] { (50.0, 100.0), (90.0, 100.0), (20.0, 100.0), (60.0, 100.0) };
        var queue = new[] { new StatDelta(30, 15, 0, 0), new StatDelta(10, 0, 0, 0) };

        var shares = Projection.Of(stats, queue)!;

        Assert.Equal(0.9, shares[0]!.Value, 6);
        Assert.Equal(1.0, shares[1]!.Value, 6);
        Assert.Null(shares[2]);
        Assert.Null(Projection.Of(stats, new StatDelta[0]));
    }
}
