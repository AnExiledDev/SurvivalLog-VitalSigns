using VitalSigns.Core;
using Xunit;

namespace VitalSigns.Tests;

/// <summary>The stat difficulty's scaling, the saved HUD layout, the pinned buff list and the HUD defaults.</summary>
public class HudSettingsTests
{
    private const int Satiety = 0;
    private const int Morale = 1;

    // ===== Difficulty =====

    [Fact]
    public void A_loss_is_scaled_by_the_drain_multiplier_and_a_gain_by_the_gain_one()
    {
        var difficulty = new Difficulty(new[] { 2.0, 1, 1, 1 }, new[] { 0.5, 1, 1, 1 });

        Assert.Equal(-6f, difficulty.Scale(Satiety, -3f));
        Assert.Equal(2f, difficulty.Scale(Satiety, 4f));
        Assert.Equal(-3f, difficulty.Scale(Morale, -3f));
        Assert.False(difficulty.IsOff);
    }

    [Fact]
    public void Multipliers_are_held_between_zero_and_five_and_a_bad_number_is_one()
    {
        var difficulty = new Difficulty(new[] { 9.0, -1, double.NaN }, new[] { 1.0 });

        Assert.Equal(Difficulty.Max, difficulty.Drain(Satiety));
        Assert.Equal(Difficulty.Min, difficulty.Drain(Morale));
        Assert.Equal(1, difficulty.Drain(2));
        Assert.Equal(1, difficulty.Drain(3));
        Assert.Equal(0f, difficulty.Scale(Morale, -10f));
    }

    [Fact]
    public void All_ones_is_off_and_leaves_every_change_as_the_game_made_it()
    {
        var difficulty = new Difficulty(new[] { 1.0, 1, 1, 1 }, new[] { 1.0, 1, 1, 1 });

        Assert.True(difficulty.IsOff);
        Assert.Equal(-3.25f, difficulty.Scale(Satiety, -3.25f));
        Assert.Equal(7f, Difficulty.Off.Scale(Morale, 7f));
    }

    [Fact]
    public void A_stat_the_difficulty_does_not_know_is_left_alone()
    {
        var difficulty = new Difficulty(new[] { 2.0, 2, 2, 2 }, new[] { 2.0, 2, 2, 2 });

        Assert.Equal(-3f, difficulty.Scale(-1, -3f));
        Assert.Equal(-3f, difficulty.Scale(4, -3f));
    }

    // ===== Layout =====

    [Fact]
    public void A_layout_reads_back_as_it_was_written()
    {
        const string text = "10,-20,150;0,0,100;0,0,100;0,0,100;0,0,100;-30,5,90";

        Assert.Equal(text, HudLayout.Format(HudLayout.Parse(text)));
    }

    [Fact]
    public void A_part_written_wrong_or_left_out_stays_home_and_numbers_are_held_to_their_limits()
    {
        var layout = HudLayout.Parse("junk;5,5;99999,0,999")!;

        Assert.Equal(HudLayout.Home, layout.Parts[0]);
        Assert.Equal(HudLayout.Home, layout.Parts[1]);
        Assert.Equal(new PartPlace(HudLayout.MaxShift, 0, HudLayout.MaxSize), layout.Parts[2]);
        Assert.Equal(HudLayout.Home, layout.Parts[5]);
    }

    [Fact]
    public void A_layout_with_every_part_home_is_none_at_all()
    {
        Assert.Null(HudLayout.Parse("0,0,100;0,0,100"));
        Assert.Null(HudLayout.Parse(""));
        Assert.Equal("", HudLayout.Format(null));
    }

    // ===== Favourites =====

    [Fact]
    public void A_shift_click_adds_a_buff_to_the_pinned_list_and_a_second_takes_it_out()
    {
        var once = HudChoices.Toggle("1300, 200", 500);

        Assert.Equal("1300,200,500", once);
        Assert.Equal("1300,500", HudChoices.Toggle(once, 200));
    }

    [Fact]
    public void Pinned_ids_skip_anything_that_is_not_a_positive_whole_number_and_repeats()
    {
        Assert.Equal(new[] { 12, 7 }, HudChoices.Ids("12; x, -3 7 12 0"));
    }

    // ===== Defaults =====

    [Fact]
    public void Clicks_on_bars_and_debuffs_start_off_so_a_click_queues_nothing_until_the_player_turns_them_on()
    {
        var hud = new HudOptions();

        Assert.False(hud.ClickBars);
        Assert.False(hud.Cure);
        Assert.True(hud.Projection);
        Assert.True(hud.WarnPulse);
    }
}
