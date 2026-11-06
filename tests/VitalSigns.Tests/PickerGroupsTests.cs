using System.Collections.Generic;
using System.Linq;
using VitalSigns.Core;
using VitalSigns.Core.Acts;
using Xunit;

namespace VitalSigns.Tests;

/// <summary>
/// The picker lists on the Mods screen: beds are the real ones (the Wooden Bed and Iron Single Bed have their own Sleep and
/// Nap actions, so they are known by their effect), and items are filed under the game's categories, not one heap.
/// </summary>
public class PickerGroupsTests
{
    [Theory]
    [InlineData("Action/Sleep_6Hour", RestKind.Sleep)]
    [InlineData("Action/Sleep_1Hour", RestKind.Nap)]
    [InlineData("Action/Sleep_6Hour_NoSpeed", RestKind.Beauty)]
    [InlineData("Action/Furniture_Bar_Default", RestKind.None)]
    [InlineData("Action/Item_Eat", RestKind.None)]
    [InlineData(null, RestKind.None)]
    public void A_rest_is_known_by_the_effect_its_action_runs(string? effect, RestKind kind) => Assert.Equal(kind, RestKinds.Of(effect));

    [Fact]
    public void Meds_and_books_come_before_food_and_food_files_under_its_kind()
    {
        const int meds = 2, books = 3, meat = 2, fruit = 6;

        var order = new[] { (PickerGroups.Food, fruit), (books, 0), (PickerGroups.Food, 0), (PickerGroups.Food, meat), (meds, 0) }
            .OrderBy(g => PickerGroups.Rank(g.Item1, g.Item2)).ToList();

        Assert.Equal(new[] { (meds, 0), (books, 0), (PickerGroups.Food, meat), (PickerGroups.Food, fruit), (PickerGroups.Food, 0) }, order);
        Assert.Equal("ConstantText_Text_ItemSubCategory2", PickerGroups.NameKey(PickerGroups.Food, meat));
        Assert.Equal("ConstantText_Text_ItemType1", PickerGroups.NameKey(PickerGroups.Food, 0));
        Assert.Equal("ConstantText_Text_ItemType3", PickerGroups.NameKey(books, 7));
    }

    [Fact]
    public void Buffs_file_by_the_kind_the_game_gives_them_and_talent_buffs_last_on_their_own_shelf()
    {
        // 6 is where the game files the few hundred base-spot and perk buffs talents give, next to the real exploration ones.
        Assert.Equal((1, "Food and sickness", "饮食与疾病"), PickerGroups.Buff(1, false));
        Assert.Equal((6, "Exploration", "探索"), PickerGroups.Buff(6, false));
        Assert.Equal("Talents", PickerGroups.Buff(6, true).En);
        Assert.True(PickerGroups.Buff(6, true).Rank > PickerGroups.Buff(9, false).Rank);
        Assert.Equal("Other", PickerGroups.Buff(9, false).En);
    }

    // The game's beds: 2 the plain bed in a base, 20000 and 20001 built from packages, 9122 and 80088 beds in places the
    // player only visits, 20002 the sofa, whose nap is a plain action and not a rest.
    private static readonly BedKind[] Game =
    {
        new(2, 60, 12, false),
        new(9122, 0, 30, false),
        new(20000, 66, 13.2, true),
        new(20001, 75, 15, true),
        new(20002, 0, 0, true),
        new(80088, 60, 0, false),
    };

    [Fact]
    public void The_beds_listed_are_those_built_those_in_the_base_and_those_given_a_mode()
    {
        var picked = BedChoices.Pick(Game, new HashSet<int> { 2, 20001 }, new HashSet<int> { 9122 }).Select(bed => bed.ConfigId);

        // The base's first, then what packages build, then one only the saved modes name. No sofa, no tent.
        Assert.Equal(new[] { 2, 20001, 20000, 9122 }, picked);
    }

    [Fact]
    public void Away_from_the_base_the_built_beds_still_show()
    {
        var picked = BedChoices.Pick(Game, new HashSet<int>(), new HashSet<int>()).Select(bed => bed.ConfigId);

        Assert.Equal(new[] { 20000, 20001 }, picked);
    }

    [Fact]
    public void A_bed_says_what_its_sleep_and_nap_restore()
    {
        Assert.Equal(("Sleep: Stamina +75. Nap: Stamina +15.", "睡觉：体力 +75。小睡：体力 +15。"), BedChoices.Note(Game[3]));
        Assert.Equal(("Nap: Stamina +30.", "小睡：体力 +30。"), BedChoices.Note(Game[1]));
    }
}
