using System.Collections.Generic;
using System.Linq;
using VitalSigns.Core;
using VitalSigns.Core.Acts;
using Xunit;

namespace VitalSigns.Tests;

/// <summary>
/// The "Any ..." entries: a list can name a whole kind of item ("Any Cooked Dish") and a click picks the best of that
/// kind from what the player has; the pickers mark what the player has. Fertilizer is not food.
/// </summary>
public class KindsTests
{
    private const int Meds = 2, Meat = 1002, Fruit = 1006;

    private static ActOption Item(int id, StatDelta delta, int shelf, bool isDish = false, int count = 1, ActState state = ActState.Ready) =>
        new(ActKeys.Item(id), ActKind.Item, id, "item" + id, "", delta, state, Count: count, Shelf: shelf, IsDish: isDish);

    private static BaseView View(params ActOption[] items) => new(items, new ActOption[0], new Bed[0], true, 1);

    [Fact]
    public void A_group_key_is_g_and_a_shelf_and_holds_the_items_of_that_kind()
    {
        Assert.Equal(new[] { "g900", "g1002", "i5" }, ActKeys.Parse("g900, G1002, g0, gx, i5, g900"));

        var stew = Item(1, new StatDelta(20, 0, 0, 5), Meat, isDish: true);
        var jerky = Item(2, new StatDelta(10, 0, 0, 0), Meat);
        var apple = Item(3, new StatDelta(8, 0, 0, 0), Fruit);

        Assert.True(ActKeys.Holds(ActKeys.Group(PickerGroups.Dishes), stew));
        Assert.False(ActKeys.Holds(ActKeys.Group(PickerGroups.Dishes), jerky));
        Assert.True(ActKeys.Holds("g1002", jerky));
        Assert.False(ActKeys.Holds("g1002", apple));
        Assert.False(ActKeys.Holds("i2", jerky));
        Assert.False(ActKeys.Holds("g0", new ActOption("f0", ActKind.Furniture, 0, "a", "", StatDelta.None, ActState.Ready)));
    }

    [Fact]
    public void Any_cooked_dish_in_the_Life_list_uses_every_dish_you_have_the_most_Life_first()
    {
        var soup = Item(1, new StatDelta(10, 0, 0, 4), Meat, isDish: true);
        var roast = Item(2, new StatDelta(30, 0, 0, 12), Meat, isDish: true);
        var raw = Item(3, new StatDelta(5, 0, 0, 20), Meat);
        var salad = Item(4, new StatDelta(5, 0, 0, 0), Fruit, isDish: true);
        var bandage = Item(5, new StatDelta(0, 0, 0, 10), Meds);
        var view = View(soup, roast, raw, salad, bandage);

        // The bandage first as listed, then the dishes by Life; the raw meat is no dish and the salad gives no Life.
        Assert.Equal(new[] { "i5", "i2", "i1" }, ActPlanner.LifeItems(view, new ActOptions(LifeItems: ActKeys.Parse("i5,g900"))).Select(item => item.Key));

        // An item named before its kind is not used twice.
        Assert.Equal(new[] { "i1", "i2" }, ActPlanner.LifeItems(view, new ActOptions(LifeItems: ActKeys.Parse("i1,g900"))).Select(item => item.Key));

        // And the plan then takes the biggest dish for a big gap.
        var steps = LifePlan.Of(ActPlanner.LifeItems(view, new ActOptions(LifeItems: ActKeys.Parse("g900"))), 80, 100, 1);

        Assert.Equal("i2", Assert.Single(steps).Option.Key);
    }

    [Fact]
    public void A_kind_in_the_routine_takes_the_one_you_have_that_gives_the_most_Morale()
    {
        var novel = Item(10, new StatDelta(0, 8, 0, 0), 3);
        var comic = Item(11, new StatDelta(0, 15, 0, 0), 3);
        var poems = Item(12, new StatDelta(0, 30, 0, 0), 3, count: 0);
        var diary = Item(13, new StatDelta(0, 40, 0, 0), 3, state: ActState.Busy);
        var guitar = new ActOption("f7", ActKind.Furniture, 7, "guitar", "", new StatDelta(0, 10, 0, 0), ActState.Ready);
        var options = new[] { novel, comic, poems, diary, guitar };

        // The comic beats the novel; the poems are all gone and the diary cannot be used now.
        Assert.Equal(new[] { "f7", "i11" }, MoralePlan.Routine(options, ActKeys.Parse("f7,g3")).Select(step => step.Key));

        // A kind after one of its items takes the next best, and an item its kind already took is not taken again.
        Assert.Equal(new[] { "i11", "i10" }, MoralePlan.Routine(options, ActKeys.Parse("i11,g3")).Select(step => step.Key));
        Assert.Equal(new[] { "i11" }, MoralePlan.Routine(options, ActKeys.Parse("g3,i11")).Select(step => step.Key));
    }

    [Fact]
    public void A_kind_on_the_skip_list_keeps_a_click_from_eating_any_of_it()
    {
        var jerky = Item(2, new StatDelta(10, 0, 0, 0), Meat);
        var apple = Item(3, new StatDelta(8, 0, 0, 0), Fruit);
        var pear = Item(4, new StatDelta(8, 0, 0, 0), Fruit);

        var foods = ActPlanner.Foods(View(jerky, apple, pear), new ActOptions(FoodSkip: ActKeys.Parse("g1002,i4")));

        Assert.Equal(new[] { "i3" }, foods.Select(item => item.Key));
    }

    [Fact]
    public void Only_food_counts_for_Satiety_so_fertilizer_is_never_eaten()
    {
        const int fertilizer = 11;

        Assert.Equal(StatDelta.None, PickerGroups.ItemDelta(fertilizer, new StatDelta(30, 0, 0, 0)));
        Assert.Equal(new StatDelta(0, 0, 0, 5), PickerGroups.ItemDelta(Meds, new StatDelta(2, 0, 0, 5)));
        Assert.Equal(new StatDelta(30, 2, 0, 0), PickerGroups.ItemDelta(PickerGroups.Food, new StatDelta(30, 2, 0, 0)));
    }

    private static ItemPick Pick(int id, int shelf, bool isDish, int held, string? icon = null) =>
        new(new PickerEntry(ActKeys.Item(id), "item" + id, "物品" + id, icon, "shelf" + shelf, "货架" + shelf, "Life +1", "生命 +1"), shelf, isDish, held);

    [Fact]
    public void The_picker_offers_each_kind_first_and_marks_what_you_have_putting_it_first_on_its_shelf()
    {
        var items = new[]
        {
            Pick(1, Meds, false, 0, "bandage.png"), Pick(2, Meds, false, 4, "kit.png"),
            Pick(3, Meat, true, 0, "stew.png"), Pick(4, Meat, false, 2), Pick(5, Meat, true, 1, "roast.png"),
        };
        var names = new Dictionary<int, (string En, string Zh)> { [Meds] = ("Meds", "药品"), [Meat] = ("Meat", "肉类") };

        var entries = PickerKinds.Of(items, names, ("Every one you have.", "你拥有的全部。"));

        Assert.Equal(new[] { "g900", "g2", "g1002", "i2", "i1", "i4", "i5", "i3" }, entries.Select(entry => entry.Id));

        var dishes = entries[0];

        Assert.Equal(("Any Cooked Dish", "任意料理"), (dishes.NameEn, dishes.NameZh));
        Assert.Equal(("Any of a kind", "Every one you have."), (dishes.GroupEn, dishes.NoteEn));
        Assert.Equal(("You have 1 kind", "你有 1 种"), (dishes.HaveEn, dishes.HaveZh));
        Assert.Equal("roast.png", dishes.Icon);
        Assert.Equal(("Any Meds", "You have 1 kind", "kit.png"), (entries[1].NameEn, entries[1].HaveEn, entries[1].Icon));
        Assert.Equal("You have 2 kinds", entries[2].HaveEn);
        Assert.Equal(("You have 4", "你有 4 个"), (entries[3].HaveEn, entries[3].HaveZh));
        Assert.Equal("", entries[4].HaveEn);
    }

    [Fact]
    public void A_kind_you_have_none_of_says_nothing_and_a_line_carries_what_you_have_last()
    {
        var entries = PickerKinds.Of(new[] { Pick(1, Meds, false, 0) }, new Dictionary<int, (string En, string Zh)>(), ("n", "n"));

        Assert.Equal(new[] { "g2", "i1" }, entries.Select(entry => entry.Id));
        Assert.Equal(("Any #2", ""), (entries[0].NameEn, entries[0].HaveEn));
        Assert.Equal("i1|item1|物品1||shelf2|货架2|Life +1|生命 +1|You have 3|你有 3 个",
            PickerLines.Of(new[] { entries[1] with { HaveEn = "You have 3", HaveZh = "你有 3 个" } }));
    }
}
