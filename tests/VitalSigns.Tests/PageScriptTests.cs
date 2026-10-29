using VitalSigns.Core;
using Xunit;

namespace VitalSigns.Tests;

public class PageScriptTests
{
    private const string Satiety = "page.rows[0]";
    private const string Morale = "page.rows[1]";
    private const string Life = "page.rows[3]";

    private static readonly WellFedLine WellFed = new(80, 60, 100);

    // ===== Answers =====

    [Fact]
    public void Before_any_reading_the_script_asks_for_one()
    {
        Assert.Equal(PageAnswer.Missing, PageScript.On().Apply(0));
    }

    [Fact]
    public void A_check_with_only_the_clock_draws_from_the_reading_it_holds()
    {
        var page = PageScript.On();

        page.Apply(0, Readings.Data(Readings.Stats(satiety: new StatRatios(0.5, 0, 0, 0))));

        Assert.Equal(PageAnswer.Ok, page.Apply(2));
        Assert.Equal("x1.5", page.Text($"childWith(maskOf({Satiety}), 'vs-chip').textContent"));
    }

    [Fact]
    public void With_no_stat_bars_on_the_page_the_answer_says_so()
    {
        Assert.Equal(PageAnswer.NoBar, PageScript.On("{icons:[]}").Apply(0, Readings.Data()));
    }

    [Fact]
    public void With_no_player_everything_drawn_is_taken_away()
    {
        var page = PageScript.On("{buffs:[[1300]]}");

        page.Apply(0, Readings.Data(Readings.Stats(satiety: new StatRatios(0.5, 0.2, 0, 0)), new[] { new BuffTime(1300, 100, 100) },
            new CarryWeight(5000, 20000, 16000), WellFed));

        Assert.True(page.Number("document.querySelectorAll('.vs-chip').length") > 0);
        Assert.Equal(PageAnswer.NoPlayer, page.Apply(10, PageData.NoPlayer));

        foreach (var drawn in new[] { "vs-chip", "vs-mark", "vs-lines", "vs-timer", "vs-weight", "vs-weight-tip" })
        {
            Assert.Equal(0, page.Number($"document.querySelectorAll('.{drawn}').length"));
        }
    }

    // ===== Stat bars =====

    [Fact]
    public void A_scaled_gain_shows_its_multiplier_on_the_bar_and_an_unscaled_one_shows_nothing()
    {
        var page = PageScript.On();

        page.Apply(0, Readings.Data(Readings.Stats(satiety: new StatRatios(0.25, 0, 0, 0), morale: new StatRatios(-0.5, 0, 0, 0), life: new StatRatios(0, 0.3, 0, 0))));

        Assert.Equal("x1.25", page.Text($"childWith(maskOf({Satiety}), 'vs-chip').textContent"));
        Assert.True(page.Is($"childWith(maskOf({Satiety}), 'vs-chip').classList.contains('vs-chip-up')"));
        Assert.Equal("x0.5", page.Text($"childWith(maskOf({Morale}), 'vs-chip').textContent"));
        Assert.True(page.Is($"childWith(maskOf({Morale}), 'vs-chip').classList.contains('vs-chip-down')"));
        Assert.False(page.Is($"childWith(maskOf({Life}), 'vs-chip')"));
    }

    [Fact]
    public void A_gain_that_goes_back_to_normal_takes_its_chip_away()
    {
        var page = PageScript.On();

        page.Apply(0, Readings.Data(Readings.Stats(satiety: new StatRatios(0.25, 0, 0, 0))));
        page.Apply(2, Readings.Data());

        Assert.False(page.Is($"childWith(maskOf({Satiety}), 'vs-chip')"));
    }

    [Fact]
    public void The_hover_box_lists_every_scaled_change_and_leaves_out_the_unscaled_ones()
    {
        var page = PageScript.On();

        page.Apply(0, Readings.Data(Readings.Stats(stamina: new StatRatios(0.2, -0.25, 0, 1))));

        Assert.Equal("Gains x1.2\nLosses x0.75\nNatural drain x2", page.Text("childWith(tipOf(page.rows[2]), 'vs-lines').textContent"));
        Assert.False(page.Is($"childWith(tipOf({Morale}), 'vs-lines')"));
    }

    [Fact]
    public void The_Satiety_bar_gets_the_Well_Fed_lines_and_its_hover_box_says_where_they_are()
    {
        var page = PageScript.On();

        page.Apply(0, Readings.Data(wellFed: new WellFedLine(80, 60, 120)));

        Assert.Equal("66.67%", page.Text($"childWith(maskOf({Satiety}), 'vs-mark-from').style.left"));
        Assert.Equal("50.00%", page.Text($"childWith(maskOf({Satiety}), 'vs-mark-until').style.left"));
        Assert.Equal("Well Fed at 80 or more, lost below 60.", page.Text($"childWith(tipOf({Satiety}), 'vs-lines').textContent"));
        Assert.Equal(0, page.Number($"maskOf({Morale}).querySelectorAll('.vs-mark').length"));
    }

    [Fact]
    public void The_Satiety_bar_is_found_by_its_mark_wherever_it_is()
    {
        var page = PageScript.On("{icons:['\\u2764','\\uD83E\\uDDE0','\\u26A1','\\uD83C\\uDF56']}");

        page.Apply(0, Readings.Data(Readings.Stats(satiety: new StatRatios(0.5, 0, 0, 0)), wellFed: WellFed));

        Assert.Equal("x1.5", page.Text("childWith(maskOf(page.rows[3]), 'vs-chip').textContent"));
        Assert.Equal(2, page.Number("maskOf(page.rows[3]).querySelectorAll('.vs-mark').length"));
        Assert.False(page.Is("childWith(maskOf(page.rows[0]), 'vs-chip')"));
    }

    [Fact]
    public void A_Chinese_game_gets_Chinese_lines()
    {
        var page = PageScript.On();

        page.Apply(0, Readings.Data(Readings.Stats(satiety: new StatRatios(0.5, 0, 0, 0)), wellFed: WellFed, isChinese: true, names: new GameNames("饱腹富余", "超重")));

        Assert.Equal("增加 x1.5\n饱腹达到80获得「饱腹富余」，低于60失去。", page.Text($"childWith(tipOf({Satiety}), 'vs-lines').textContent"));
    }

    // ===== Buff timers =====

    [Fact]
    public void A_buff_with_an_end_gets_a_stripe_for_the_part_still_to_run()
    {
        var page = PageScript.On("{buffs:[[], [1300, 77]]}");

        page.Apply(1000, Readings.Data(buffs: new[] { new BuffTime(1300, 1000 + 1500, 2000) }));

        Assert.Equal("scaleX(0.7500)", page.Text($"stripeOn({Morale}, 0).style.transform"));
        Assert.False(page.Is($"stripeOn({Morale}, 1)"));
    }

    [Fact]
    public void The_stripe_moves_with_the_clock_alone()
    {
        var page = PageScript.On("{buffs:[[1300]]}");

        page.Apply(0, Readings.Data(buffs: new[] { new BuffTime(1300, 1000, 1000) }));
        page.Apply(500);

        Assert.Equal("scaleX(0.5000)", page.Text($"stripeOn({Satiety}, 0).style.transform"));
    }

    /// <summary>A buff added on top of a running one can last longer than its own length; the stripe starts full, not past it.</summary>
    [Fact]
    public void A_buff_with_more_time_left_than_its_length_starts_full()
    {
        var page = PageScript.On("{buffs:[[1300]]}");

        page.Apply(0, Readings.Data(buffs: new[] { new BuffTime(1300, 3000, 1000) }));
        Assert.Equal("scaleX(1.0000)", page.Text($"stripeOn({Satiety}, 0).style.transform"));

        page.Apply(1500);
        Assert.Equal("scaleX(0.5000)", page.Text($"stripeOn({Satiety}, 0).style.transform"));
    }

    /// <summary>The length learned from a long first run is dropped when the buff is given again, or a fresh one would start part empty.</summary>
    [Fact]
    public void A_buff_given_again_refills_its_stripe()
    {
        var page = PageScript.On("{buffs:[[1300]]}");

        page.Apply(0, Readings.Data(buffs: new[] { new BuffTime(1300, 3000, 1000) }));
        page.Apply(2500);
        page.Apply(2500, Readings.Data(buffs: new[] { new BuffTime(1300, 3500, 1000) }));

        Assert.Equal("scaleX(1.0000)", page.Text($"stripeOn({Satiety}, 0).style.transform"));
    }

    /// <summary>Two buffs of one kind are matched in end order, the way the game numbers their badges.</summary>
    [Fact]
    public void Two_buffs_of_one_kind_each_get_their_own_stripe()
    {
        var page = PageScript.On("{buffs:[[5, 5]]}");

        page.Apply(0, Readings.Data(buffs: new[] { new BuffTime(5, 250, 1000), new BuffTime(5, 1000, 1000) }));

        Assert.Equal("scaleX(0.2500)", page.Text($"stripeOn({Satiety}, 0).style.transform"));
        Assert.Equal("scaleX(1.0000)", page.Text($"stripeOn({Satiety}, 1).style.transform"));
    }

    /// <summary>The game keeps a leaving badge in place for its animation; it is not in attrList, so it is passed over.</summary>
    [Fact]
    public void A_badge_on_its_way_out_is_passed_over()
    {
        var page = PageScript.On("{buffs:[[1300]]}");

        page.Run($"var gone = addBadge({Satiety}.querySelector('.buff-container'), 9); gone.classList.add('bf-leave-active');"
            + $"{Satiety}.querySelector('.buff-container').children.unshift({Satiety}.querySelector('.buff-container').children.pop());");
        page.Apply(0, Readings.Data(buffs: new[] { new BuffTime(1300, 500, 1000) }));

        Assert.False(page.Is("childWith(gone, 'vs-timer')"));
        Assert.Equal("scaleX(0.5000)", page.Text($"stripeOn({Satiety}, 1).style.transform"));
    }

    [Fact]
    public void Badges_that_do_not_line_up_with_the_list_wait_for_the_next_check()
    {
        var page = PageScript.On("{buffs:[[1300]]}");

        page.Run($"addBadge({Satiety}.querySelector('.buff-container'), 9);");
        page.Apply(0, Readings.Data(buffs: new[] { new BuffTime(1300, 500, 1000) }));

        Assert.Equal(0, page.Number("document.querySelectorAll('.vs-timer').length"));
    }

    [Fact]
    public void A_buff_that_no_longer_ends_loses_its_stripe()
    {
        var page = PageScript.On("{buffs:[[1300]]}");

        page.Apply(0, Readings.Data(buffs: new[] { new BuffTime(1300, 500, 1000) }));
        page.Apply(2, Readings.Data());

        Assert.False(page.Is($"stripeOn({Satiety}, 0)"));
    }

    // ===== Carried weight =====

    [Fact]
    public void The_Inventory_button_shows_the_weight_and_its_hover_box_the_room_left()
    {
        var page = PageScript.On();

        page.Apply(0, Readings.Data(weight: new CarryWeight(12_340, 20_000, 16_000)));

        Assert.Equal("12.4", page.Text("childWith(page.bag, 'vs-weight').textContent"));
        Assert.Equal("Carrying 12.4 / 20.0 kg\nOverweight above 16.0 kg\n3.6 kg left before Overweight",
            page.Text("childWith(page.bag.querySelector('.entry-tip'), 'vs-weight-tip').textContent"));
        Assert.False(page.Is("childWith(page.phone, 'vs-weight')"));
        Assert.False(page.Is("childWith(page.bag, 'vs-weight').classList.contains('vs-near')"));
    }

    [Theory]
    [InlineData(14_400, "vs-near")]
    [InlineData(16_001, "vs-over")]
    public void The_weight_turns_amber_near_Overweight_and_red_past_it(int carried, string state)
    {
        var page = PageScript.On();

        page.Apply(0, Readings.Data(weight: new CarryWeight(carried, 20_000, 16_000)));

        Assert.True(page.Is($"childWith(page.bag, 'vs-weight').classList.contains('{state}')"));
    }

    [Fact]
    public void Past_Overweight_the_hover_box_says_by_how_much()
    {
        var page = PageScript.On();

        page.Apply(0, Readings.Data(weight: new CarryWeight(17_050, 20_000, 16_000)));

        Assert.EndsWith("Overweight: 1.1 kg over", page.Text("childWith(page.bag.querySelector('.entry-tip'), 'vs-weight-tip').textContent"));
    }

    [Fact]
    public void The_Inventory_button_is_found_by_the_game_word_for_it()
    {
        var page = PageScript.On("{bagName:'背包'}");

        page.Apply(0, Readings.Data(weight: new CarryWeight(5000, 20_000, 16_000), isChinese: true, names: new GameNames("饱腹富余", "超重")));

        Assert.Equal("5.0", page.Text("childWith(page.bag, 'vs-weight').textContent"));
        Assert.Equal("负重 5.0 / 20.0 千克\n超过 16.0 千克即「超重」\n距离「超重」还有 11.0 千克",
            page.Text("childWith(page.bag.querySelector('.entry-tip'), 'vs-weight-tip').textContent"));
    }

    [Fact]
    public void Without_a_weight_reading_the_button_is_left_as_the_game_draws_it()
    {
        var page = PageScript.On();

        page.Apply(0, Readings.Data(weight: new CarryWeight(5000, 20_000, 16_000)));
        page.Apply(2, Readings.Data());

        Assert.False(page.Is("childWith(page.bag, 'vs-weight')"));
        Assert.False(page.Is("childWith(page.bag.querySelector('.entry-tip'), 'vs-weight-tip')"));
    }

    [Fact]
    public void The_weight_still_shows_when_the_stat_bars_are_gone()
    {
        var page = PageScript.On("{icons:[]}");

        Assert.Equal(PageAnswer.NoBar, page.Apply(0, Readings.Data(weight: new CarryWeight(5000, 20_000, 16_000))));
        Assert.Equal("5.0", page.Text("childWith(page.bag, 'vs-weight').textContent"));
    }
}
