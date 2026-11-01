using System;
using System.Collections.Generic;
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
        Assert.Equal("x1.5", page.Text($"childWith(chipOf({Satiety}), 'vs-cx').textContent"));
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
        var coming = new IReadOnlyList<LossLine>[] { new[] { new LossLine(1300, 10, 100) }, Array.Empty<LossLine>(), Array.Empty<LossLine>(), Array.Empty<LossLine>() };

        page.Apply(0, Readings.Data(Readings.Stats(satiety: new StatRatios(0.5, 0.2, 0, 0)), new[] { new BuffTime(1300, 100, 100) },
            new CarryWeight(5000, 20000, 16000), WellFed, details: Readings.DetailsOf(new[] { Readings.Rule(1300) }, coming), options: new Options(GameText: false)));
        page.Run($"hover(buffTip({Satiety}, 0))");
        Assert.True(page.Is("document.querySelectorAll('.vs-bt').length && document.querySelectorAll('.vs-loss').length && markedTips().length >= 2"));

        Assert.True(page.Number("document.querySelectorAll('.vs-chip').length") > 0);
        Assert.Equal(PageAnswer.NoPlayer, page.Apply(10, PageData.NoPlayer));

        foreach (var drawn in new[] { "vs-chip", "vs-mark", "vs-loss", "vs-st", "vs-timer", "vs-bt", "vs-weight", "vs-wbar", "vs-weight-tip", "vs-btime", "vs-bd" })
        {
            Assert.Equal(0, page.Number($"document.querySelectorAll('.{drawn}').length"));
        }

        Assert.Equal(0, page.Number("markedTips().length"));

        Assert.False(page.Is("document.getElementById('vs-zoom')"));
    }

    // ===== Stat bars =====

    [Fact]
    public void A_scaled_gain_shows_its_multiplier_on_the_bar_and_an_unscaled_one_shows_nothing()
    {
        var page = PageScript.On();

        page.Apply(0, Readings.Data(Readings.Stats(satiety: new StatRatios(0.25, 0, 0, 0), morale: new StatRatios(-0.5, 0, 0, 0), life: new StatRatios(0, 0.3, 0, 0))));

        Assert.Equal("x1.25", page.Text($"childWith(chipOf({Satiety}), 'vs-cx').textContent"));
        Assert.True(page.Is($"chipOf({Satiety}).classList.contains('vs-chip-up')"));
        Assert.Equal("x0.5", page.Text($"childWith(chipOf({Morale}), 'vs-cx').textContent"));
        Assert.True(page.Is($"chipOf({Morale}).classList.contains('vs-chip-down')"));
        Assert.False(page.Is($"chipOf({Life})"));
        // Its own chip left of the bar, so the bar's name and value keep the whole bar.
        Assert.Equal(page.Number($"{Satiety}.children.indexOf({Satiety}.querySelector('.progress-bar-wrapper'))") - 1,
            page.Number($"{Satiety}.children.indexOf(chipOf({Satiety}))"));
        Assert.Equal(0, page.Number($"maskOf({Satiety}).querySelectorAll('.vs-chip').length"));
    }

    [Fact]
    public void The_chip_goes_back_beside_the_bar_when_the_game_puts_a_node_in_between()
    {
        var page = PageScript.On();
        var data = Readings.Data(Readings.Stats(satiety: new StatRatios(0.25, 0, 0, 0)));

        page.Apply(0, data);
        page.Run($"{Satiety}.insertBefore(new FakeElement('div', 'game-node'), {Satiety}.querySelector('.progress-bar-wrapper'))");
        page.Apply(2, data);

        Assert.Equal(page.Number($"{Satiety}.children.indexOf({Satiety}.querySelector('.progress-bar-wrapper'))") - 1,
            page.Number($"{Satiety}.children.indexOf(chipOf({Satiety}))"));
        Assert.Equal(1, page.Number($"{Satiety}.querySelectorAll('.vs-chip').length"));
    }

    // ===== The multiplier's hover box =====

    private static IReadOnlyList<IReadOnlyList<GainSource>> SatietySources(params GainSource[] satiety) =>
        new IReadOnlyList<GainSource>[] { satiety, Array.Empty<GainSource>(), Array.Empty<GainSource>(), Array.Empty<GainSource>() };

    /// <summary>The buffs' shares add to 0.2 of the 0.4 gain; the game's other 0.2 (gear, traits) is its own line, so the
    /// lines always add up to the chip.</summary>
    [Fact]
    public void The_multiplier_hover_box_lists_each_buffs_share_and_the_rest_as_other()
    {
        var page = PageScript.On();
        var sources = SatietySources(new GainSource("Quick Jog", 0.3), new GainSource("Cold", -0.1));

        page.Apply(0, Readings.Data(Readings.Stats(satiety: new StatRatios(0.4, 0, 0, 0)), details: Readings.DetailsOf(sources: sources)));

        var lines = page.Text($"blockText(chipOf({Satiety}), 'vs-ct')").Split('\n');

        Assert.EndsWith("gain x1.4", lines[0]);
        Assert.Equal(new[] { "Quick Jog +30%", "Cold -10%", "Other +20%" }, lines[1..]);
        Assert.True(page.Is($"childWith(chipOf({Satiety}), 'vs-ct').classList.contains('vs-ct-up')"));
        Assert.True(page.Is($"childWith(childWith(chipOf({Satiety}), 'vs-ct'), 'vs-kv').classList.contains('vs-good')"));
        Assert.True(page.Is($"chipOf({Satiety}).hasAttribute('data-interactive')"));
    }

    [Fact]
    public void When_the_buffs_account_for_the_whole_multiplier_there_is_no_other_line()
    {
        var page = PageScript.On();
        var sources = SatietySources(new GainSource("Hungry", -0.5));

        page.Apply(0, Readings.Data(Readings.Stats(satiety: new StatRatios(-0.5, 0, 0, 0)), details: Readings.DetailsOf(sources: sources)));

        var lines = page.Text($"blockText(chipOf({Satiety}), 'vs-ct')").Split('\n');

        Assert.Equal(new[] { "Hungry -50%" }, lines[1..]);
        Assert.True(page.Is($"childWith(chipOf({Satiety}), 'vs-ct').classList.contains('vs-ct-down')"));
    }

    [Fact]
    public void With_hover_boxes_off_the_chip_has_no_box_and_lets_the_mouse_through()
    {
        var page = PageScript.On();
        var data = Readings.Data(Readings.Stats(satiety: new StatRatios(0.4, 0, 0, 0)), details: Readings.DetailsOf(sources: SatietySources(new GainSource("Quick Jog", 0.4))));

        page.Apply(0, data);
        page.Apply(2, Readings.Data(Readings.Stats(satiety: new StatRatios(0.4, 0, 0, 0)), options: new Options(Tooltips: false)));

        Assert.False(page.Is($"childWith(chipOf({Satiety}), 'vs-ct')"));
        Assert.False(page.Is($"chipOf({Satiety}).hasAttribute('data-interactive')"));
        Assert.Equal("x1.4", page.Text($"childWith(chipOf({Satiety}), 'vs-cx').textContent"));
    }

    [Fact]
    public void The_multiplier_hover_box_moves_up_onto_the_window_like_the_others()
    {
        var page = PageScript.On();

        page.Apply(0, Readings.Data(Readings.Stats(life: new StatRatios(0.4, 0, 0, 0))));
        page.Run($"placeBox(childWith(chipOf({Life}), 'vs-ct'), 500, 300, 2, 700)");
        page.Run($"hover(chipOf({Life}))");

        Assert.Equal("-53px", page.Text($"childWith(chipOf({Life}), 'vs-ct').style.marginTop"));
    }

    // ===== Hover boxes kept on screen =====

    /// <summary>A box running 106 window pixels past the bottom edge (6 pixels kept clear) at zoom 2 moves up 53 of its own
    /// pixels, and its arrow moves back down the same, so it still points at the bar.</summary>
    [Fact]
    public void A_hover_box_past_the_bottom_of_the_window_moves_up_onto_it_and_its_arrow_back_to_the_bar()
    {
        var page = PageScript.On();

        page.Apply(0, Readings.Data());
        page.Run($"placeBox(tipOf({Life}), 500, 300, 2, 700)");
        page.Run($"hover(maskOf({Life}))");

        Assert.Equal("-53px", page.Text($"tipOf({Life}).style.marginTop"));
        Assert.Equal("53px", page.Text($"tipOf({Life}).style.marginBottom"));
        Assert.Equal("53px", page.Text($"tipOf({Life}).style['--vs-arrow']"));

        // Measured again where it now sits, it stays put.
        page.Run($"placeBox(tipOf({Life}), 500, 300, 2, 700)");
        page.Apply(2, Readings.Data());

        Assert.Equal("-53px", page.Text($"tipOf({Life}).style.marginTop"));
    }

    [Fact]
    public void A_hover_box_that_fits_is_not_moved_and_one_that_grew_back_onto_the_window_moves_back()
    {
        var page = PageScript.On();

        page.Apply(0, Readings.Data());
        page.Run($"placeBox(tipOf({Satiety}), 100, 200, 1, 700)");
        page.Run($"hover(tipOf({Satiety}))");

        Assert.Equal("", page.Text($"tipOf({Satiety}).style.marginTop || ''"));

        page.Run($"placeBox(tipOf({Satiety}), 600, 200, 1, 700)");
        page.Apply(2, Readings.Data());

        Assert.Equal("-106px", page.Text($"tipOf({Satiety}).style.marginTop"));

        page.Run($"placeBox(tipOf({Satiety}), 100, 200, 1, 700)");
        page.Apply(4, Readings.Data());

        Assert.Equal("", page.Text($"tipOf({Satiety}).style.marginTop"));
    }

    /// <summary>Taller than the window, the box keeps its top on screen; its arrow stays inside the box.</summary>
    [Fact]
    public void A_hover_box_taller_than_the_window_keeps_its_top_on_screen_and_its_arrow_inside()
    {
        var page = PageScript.On("{buffs:[[1300]]}");

        page.Apply(0, Readings.Data(buffs: new[] { new BuffTime(1300, 100, 100) }, details: Readings.DetailsOf(new[] { Readings.Rule(1300) })));
        page.Run($"placeBox(buffTip({Satiety}, 0), 300, 800, 1, 700)");
        page.Run($"hover({Satiety}.querySelectorAll('.buff')[0])");

        Assert.Equal("-294px", page.Text($"buffTip({Satiety}, 0).style.marginTop"));
        Assert.Equal("294px", page.Text($"buffTip({Satiety}, 0).style['--vs-arrow']"));

        // A short box moved more than half its height would put its arrow past its edge, so the arrow stops inside.
        page.Run($"placeBox(buffTip({Satiety}, 0), 650, 200, 1, 700)");
        page.Apply(2, Readings.Data(buffs: new[] { new BuffTime(1300, 98, 100) }, details: Readings.DetailsOf(new[] { Readings.Rule(1300) })));

        Assert.Equal("-156px", page.Text($"buffTip({Satiety}, 0).style.marginTop"));
        Assert.Equal("88px", page.Text($"buffTip({Satiety}, 0).style['--vs-arrow']"));
    }

    [Fact]
    public void With_no_player_a_moved_hover_box_goes_back_to_the_game_place()
    {
        var page = PageScript.On();

        page.Apply(0, Readings.Data());
        page.Run($"placeBox(tipOf({Life}), 500, 300, 1, 700)");
        page.Run($"hover(maskOf({Life}))");
        page.Apply(2, PageData.NoPlayer);

        Assert.Equal("", page.Text($"tipOf({Life}).style.marginTop"));
        Assert.Equal("", page.Text($"tipOf({Life}).style.marginBottom"));
        Assert.False(page.Is($"tipOf({Life}).style['--vs-arrow']"));
    }

    [Fact]
    public void A_gain_that_goes_back_to_normal_takes_its_chip_away()
    {
        var page = PageScript.On();

        page.Apply(0, Readings.Data(Readings.Stats(satiety: new StatRatios(0.25, 0, 0, 0))));
        page.Apply(2, Readings.Data());

        Assert.False(page.Is($"chipOf({Satiety})"));
    }

    [Fact]
    public void The_hover_box_lists_every_scaled_change_and_leaves_out_the_unscaled_ones()
    {
        var page = PageScript.On();

        page.Apply(0, Readings.Data(Readings.Stats(stamina: new StatRatios(0.2, -0.25, 0, 1))));

        Assert.Equal("Gains +20%\nLosses -25%\nNatural drain +100%", page.Text("blockText(tipOf(page.rows[2]), 'vs-st')"));
        Assert.False(page.Is($"childWith(tipOf({Morale}), 'vs-st')"));
    }

    [Fact]
    public void The_Satiety_bar_gets_the_Well_Fed_lines_and_its_hover_box_says_where_they_are()
    {
        var page = PageScript.On();

        page.Apply(0, Readings.Data(wellFed: new WellFedLine(80, 60, 120)));

        Assert.Equal("66.67%", page.Text($"childWith(maskOf({Satiety}), 'vs-mark-from').style.left"));
        Assert.Equal("50.00%", page.Text($"childWith(maskOf({Satiety}), 'vs-mark-until').style.left"));
        Assert.Equal("Well Fed at 80 or more, lost below 60.", page.Text($"blockText(tipOf({Satiety}), 'vs-st')"));
        Assert.Equal(0, page.Number($"maskOf({Morale}).querySelectorAll('.vs-mark').length"));
    }

    [Fact]
    public void The_Satiety_bar_is_found_by_its_mark_wherever_it_is()
    {
        var page = PageScript.On("{icons:['\\u2764','\\uD83E\\uDDE0','\\u26A1','\\uD83C\\uDF56']}");

        page.Apply(0, Readings.Data(Readings.Stats(satiety: new StatRatios(0.5, 0, 0, 0)), wellFed: WellFed));

        Assert.Equal("x1.5", page.Text("childWith(chipOf(page.rows[3]), 'vs-cx').textContent"));
        Assert.Equal(2, page.Number("maskOf(page.rows[3]).querySelectorAll('.vs-mark').length"));
        Assert.False(page.Is("chipOf(page.rows[0])"));
    }

    [Fact]
    public void A_Chinese_game_gets_Chinese_lines()
    {
        var page = PageScript.On();

        page.Apply(0, Readings.Data(Readings.Stats(satiety: new StatRatios(0.5, 0, 0, 0)), wellFed: WellFed, isChinese: true, names: new GameNames("饱腹富余", "超重")));

        Assert.Equal("增加 +50%\n饱腹达到80获得「饱腹富余」，低于60失去。", page.Text($"blockText(tipOf({Satiety}), 'vs-st')"));
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

    // ===== 0.2.0: shading, tooltips, options =====

    private static IReadOnlyList<LossLine>[] Coming(int stat, params LossLine[] lines)
    {
        var coming = new IReadOnlyList<LossLine>[] { Array.Empty<LossLine>(), Array.Empty<LossLine>(), Array.Empty<LossLine>(), Array.Empty<LossLine>() };

        coming[stat] = lines;

        return coming;
    }

    private static readonly BuffRule Cut = Readings.Rule(1111, "Cut", interval: 7200, every: new double[] { 0, 0, 0, -2 }, gain: new double[] { 0.5, 0, 0, 0 },
        cures: new[] { 2400, 2401, 2402 }, suppressors: new[] { 2916 }, isSuppressible: true);

    private static readonly ItemCount[] Items =
    {
        new(2400, "Bandage", "Icons/bandage.png", 0), new(2401, "Gauze", "Icons/gauze.png", 2), new(2402, "Herbs", "", 1), new(2916, "Morphine", "Icons/morphine.png", 0)
    };

    [Fact]
    public void The_loss_still_coming_is_shaded_over_the_end_of_the_filled_bar()
    {
        var page = PageScript.On("{values:[0.5,0.5,0.5,0.5]}");

        page.Apply(0, Readings.Data(details: Readings.DetailsOf(new[] { Cut }, Coming(0, new LossLine(1111, 15, 900), new LossLine(1111, 5, 1800)))));

        Assert.Equal("30.00%", page.Text($"childWith(maskOf({Satiety}), 'vs-loss').style.left"));
        Assert.Equal("20.00%", page.Text($"childWith(maskOf({Satiety}), 'vs-loss').style.width"));
        Assert.True(page.Is($"childWith(maskOf({Satiety}), 'vs-loss').parentElement.children.indexOf(childWith(maskOf({Satiety}), 'progress-text')) === "
            + $"maskOf({Satiety}).children.indexOf(childWith(maskOf({Satiety}), 'vs-loss')) + 1"));
        Assert.False(page.Is($"childWith(maskOf({Morale}), 'vs-loss')"));
    }

    [Fact]
    public void A_loss_bigger_than_what_is_left_shades_only_down_to_zero()
    {
        var page = PageScript.On("{values:[0.1,0.5,0.5,0.5]}");

        page.Apply(0, Readings.Data(details: Readings.DetailsOf(coming: Coming(0, new LossLine(1111, 50, 900)))));

        Assert.Equal("0.00%", page.Text($"childWith(maskOf({Satiety}), 'vs-loss').style.left"));
        Assert.Equal("10.00%", page.Text($"childWith(maskOf({Satiety}), 'vs-loss').style.width"));
    }

    [Fact]
    public void The_stat_tooltip_shows_the_value_food_changes_and_each_loss_still_coming()
    {
        var page = PageScript.On("{values:[0.6,0.5,0.5,0.5]}");

        page.Apply(0, Readings.Data(Readings.Stats(satiety: new StatRatios(0, 0.1, 0, 0)), wellFed: WellFed,
            details: Readings.DetailsOf(new[] { Cut }, Coming(0, new LossLine(1111, 6.666, 7500)), foods: new[] { "Meat", "Fruit" },
                foodMods: new[] { new FoodMod(0, 2, -0.25), new FoodMod(1, 1, 0.5) }, max: new double[] { 120, 100, 100, 100 })));

        Assert.Equal("Value 72 / 120\nLosses +10%\nFruit -25%\nCut: -6.7 over 2h 5m\nWell Fed at 80 or more, lost below 60.",
            page.Text($"blockText(tipOf({Satiety}), 'vs-st')"));
        Assert.Contains("Meat +50%", page.Text($"blockText(tipOf({Morale}), 'vs-st')"));
        Assert.True(page.Is($"childWith(tipOf({Satiety}), 'vs-st').classList.contains('vs-bad') || childWith(tipOf({Satiety}), 'vs-st').children[2].classList.contains('vs-bad')"));
    }

    [Fact]
    public void Our_stat_lines_go_above_the_game_text_which_the_player_can_hide()
    {
        var page = PageScript.On();

        page.Apply(0, Readings.Data(Readings.Stats(satiety: new StatRatios(0.5, 0, 0, 0))));

        Assert.True(page.Is($"tipOf({Satiety}).children.indexOf(childWith(tipOf({Satiety}), 'vs-st')) < tipOf({Satiety}).children.indexOf(tipOf({Satiety}).querySelectorAll('.st-body')[1])"));
        Assert.False(page.Is($"tipOf({Satiety}).hasAttribute('data-vs-no-game-text')"));

        page.Apply(2, Readings.Data(Readings.Stats(satiety: new StatRatios(0.5, 0, 0, 0)), options: new Options(GameText: false)));

        Assert.True(page.Is($"tipOf({Satiety}).hasAttribute('data-vs-no-game-text')"));
    }

    [Fact]
    public void With_tooltips_off_the_bars_keep_their_chip_and_shading_but_no_lines()
    {
        var page = PageScript.On("{values:[0.5,0.5,0.5,0.5],buffs:[[1111]]}");

        page.Apply(0, Readings.Data(Readings.Stats(satiety: new StatRatios(0.5, 0, 0, 0)), new[] { new BuffTime(1111, 3600, 7200) },
            details: Readings.DetailsOf(new[] { Cut }, Coming(0, new LossLine(1111, 10, 900)), Items), options: new Options(Tooltips: false)));
        page.Run($"hover(buffTip({Satiety}, 0))");

        Assert.True(page.Is($"chipOf({Satiety})"));
        Assert.True(page.Is($"childWith(maskOf({Satiety}), 'vs-loss')"));
        Assert.Equal(0, page.Number("document.querySelectorAll('.vs-st').length + document.querySelectorAll('.vs-bt').length"));
    }

    [Fact]
    public void The_Life_row_shows_a_heart()
    {
        var page = PageScript.On();

        page.Run($"maskOf({Life}).querySelector('.attr-name').children[0].textContent = '\\uD83D\\uDEE1';");
        page.Apply(0, Readings.Data());

        Assert.Equal("\u2764", page.Text($"maskOf({Life}).querySelector('.attr-name').children[0].textContent"));
        Assert.Equal("\uD83C\uDF56", page.Text($"maskOf({Satiety}).querySelector('.attr-name').children[0].textContent"));
    }

    [Fact]
    public void A_hovered_buff_shows_its_effects_for_every_layer_its_time_and_its_cures_owned_first()
    {
        var page = PageScript.On("{buffs:[[], [], [], [1111]]}");

        page.Apply(0, Readings.Data(buffs: new[] { new BuffTime(1111, 3600, 7200, 2) }, details: Readings.DetailsOf(new[] { Cut }, items: Items)));
        page.Run($"hover(buffTip({Life}, 0).querySelector('.bt-body'))");

        // The first row is the time bar, which has no text; the time itself sits on the header line.
        Assert.Equal("\nLife every 2h -4\nSatiety gain +100%\nLayers 2\nCure with\nGauze x2\nHerbs x1\nBandage x0\nSuppress with\nMorphine x0",
            page.Text($"blockText(buffTip({Life}, 0), 'vs-bt')"));
        Assert.Equal("50.00%", page.Text($"childWith(buffTip({Life}, 0), 'vs-bt').querySelector('.vs-tfill').style.width"));
        Assert.True(page.Is($"childWith(buffTip({Life}, 0), 'vs-bt').querySelectorAll('.vs-item')[2].classList.contains('vs-none')"));
        Assert.False(page.Is($"childWith(buffTip({Life}, 0), 'vs-bt').querySelectorAll('.vs-item')[0].classList.contains('vs-none')"));
        Assert.Equal("Icons/gauze.png", page.Text($"childWith(buffTip({Life}, 0), 'vs-bt').querySelectorAll('.vs-item')[0].children[0].attrs.src"));
        Assert.True(page.Is($"childWith(buffTip({Life}, 0), 'bt-body')"));
    }

    [Fact]
    public void A_hovered_buff_shows_the_game_text_a_sentence_to_a_line_in_place_of_the_game_block()
    {
        var page = PageScript.On("{buffs:[[], [], [], [1111]]}");

        page.Run($"childWith(buffTip({Life}, 0), 'bt-body').textContent = 'Raises the chance of illness by 1.5 times.  Spreads to others! Lasts a while?'");
        page.Apply(0, Readings.Data(buffs: new[] { new BuffTime(1111, 3600, 7200) }, details: Readings.DetailsOf(new[] { Cut })));
        page.Run($"hover(buffTip({Life}, 0))");

        Assert.Equal("Raises the chance of illness by 1.5 times.\nSpreads to others!\nLasts a while?", page.Text($"blockText(buffTip({Life}, 0), 'vs-bd')"));
        // The game's own block stays, hidden while ours shows, so Vue can keep writing to it.
        Assert.True(page.Is($"childWith(buffTip({Life}, 0), 'bt-body')"));

        page.Run($"childWith(buffTip({Life}, 0), 'bt-body').textContent = '\u4f20\u67d3\u3002\u6301\u7eed\u4e24\u5929\u3002'");
        page.Apply(2, Readings.Data(buffs: new[] { new BuffTime(1111, 3598, 7200) }, details: Readings.DetailsOf(new[] { Cut })));

        Assert.Equal("\u4f20\u67d3\u3002\n\u6301\u7eed\u4e24\u5929\u3002", page.Text($"blockText(buffTip({Life}, 0), 'vs-bd')"));
    }

    [Fact]
    public void A_buff_with_no_details_keeps_the_game_text_block()
    {
        var page = PageScript.On("{buffs:[[], [], [], [1111]]}");

        page.Apply(0, Readings.Data(buffs: new[] { new BuffTime(1111, 3600, 7200) }));
        page.Run($"hover(buffTip({Life}, 0))");

        Assert.False(page.Is($"childWith(buffTip({Life}, 0), 'vs-bd')"));
        Assert.False(page.Is($"buffTip({Life}, 0).hasAttribute('data-vs-tip')"));
    }

    [Fact]
    public void A_timed_buff_shows_its_time_on_the_header_line_in_place_of_the_game_timer()
    {
        var page = PageScript.On("{buffs:[[], [], [], [1111]]}");

        page.Apply(0, Readings.Data(buffs: new[] { new BuffTime(1111, 3600, 7200) }, details: Readings.DetailsOf(new[] { Cut })));
        page.Run($"hover(buffTip({Life}, 0))");

        Assert.Equal("1h", page.Text($"childWith(buffTip({Life}, 0), 'vs-btime').textContent"));
        Assert.True(page.Is($"buffTip({Life}, 0).hasAttribute('data-vs-tip') && buffTip({Life}, 0).hasAttribute('data-vs-timed')"));
        Assert.True(page.Is($"childWith(buffTip({Life}, 0), 'vs-bt').classList.contains('vs-bt-timed')"));
    }

    [Fact]
    public void An_untimed_buff_keeps_the_game_timer_and_gets_no_time_of_ours()
    {
        var page = PageScript.On("{buffs:[[], [], [], [1111]]}");

        page.Apply(0, Readings.Data(buffs: new[] { new BuffTime(1111, -1, 0) }, details: Readings.DetailsOf(new[] { Cut })));
        page.Run($"hover(buffTip({Life}, 0))");

        Assert.True(page.Is($"buffTip({Life}, 0).hasAttribute('data-vs-tip')"));
        Assert.False(page.Is($"buffTip({Life}, 0).hasAttribute('data-vs-timed')"));
        Assert.False(page.Is($"childWith(buffTip({Life}, 0), 'vs-btime')"));
    }

    [Fact]
    public void The_new_look_survives_the_game_showing_its_tooltip()
    {
        var page = PageScript.On("{buffs:[[], [], [], [1111]]}");

        page.Apply(0, Readings.Data(buffs: new[] { new BuffTime(1111, 3600, 7200) }, details: Readings.DetailsOf(new[] { Cut })));
        page.Run($"hover(buffTip({Life}, 0))");
        // The game shows the box right after the hover, writing its class again; no call from the plugin comes between.
        page.Run($"vueRewritesClass(buffTip({Life}, 0), 'buff-tooltip bt-pos bt-show')");

        Assert.True(page.Is($"buffTip({Life}, 0).hasAttribute('data-vs-tip') && buffTip({Life}, 0).hasAttribute('data-vs-timed')"));
        Assert.Equal("1h", page.Text($"childWith(buffTip({Life}, 0), 'vs-btime').textContent"));
    }

    [Fact]
    public void The_stat_tooltip_keeps_the_game_text_hidden_when_the_game_rewrites_its_class()
    {
        var page = PageScript.On();

        page.Apply(0, Readings.Data(Readings.Stats(satiety: new StatRatios(0.5, 0, 0, 0)), options: new Options(GameText: false)));
        page.Run($"vueRewritesClass(tipOf({Satiety}), 'status-tooltip last-row-fix')");

        Assert.True(page.Is($"tipOf({Satiety}).hasAttribute('data-vs-no-game-text')"));
    }

    [Fact]
    public void The_new_tooltip_look_hangs_on_marks_the_game_never_rewrites()
    {
        var page = PageScript.On();

        page.Apply(0, Readings.Data());

        var css = page.Text("document.getElementById('vs-style').textContent");

        Assert.Contains(".buff-tooltip[data-vs-tip]{", css);
        Assert.Contains("[data-vs-tip][data-vs-timed] .bt-timer{display:none}", css);
        Assert.Contains(".status-tooltip[data-vs-no-game-text] > .st-body", css);
        Assert.DoesNotContain(".vs-tip", css);
        Assert.DoesNotContain(".vs-timed", css);
        Assert.DoesNotContain(".vs-no-game-text", css);
    }

    [Fact]
    public void Moving_off_a_tooltip_takes_our_marks_off_it()
    {
        var page = PageScript.On("{buffs:[[1111, 1111]]}");

        page.Apply(0, Readings.Data(buffs: new[] { new BuffTime(1111, 3600, 7200), new BuffTime(1111, -1, 0) }, details: Readings.DetailsOf(new[] { Cut })));
        page.Run($"hover(buffTip({Satiety}, 0))");
        page.Run($"hover(buffTip({Satiety}, 1))");

        Assert.False(page.Is($"buffTip({Satiety}, 0).hasAttribute('data-vs-tip') || buffTip({Satiety}, 0).hasAttribute('data-vs-timed')"));
        Assert.False(page.Is($"childWith(buffTip({Satiety}, 0), 'vs-btime')"));
    }

    [Fact]
    public void A_suppressed_buff_says_what_holds_it_back_instead_of_its_effects()
    {
        var page = PageScript.On("{buffs:[[], [], [], [1111]]}");

        page.Apply(0, Readings.Data(buffs: new[] { new BuffTime(1111, -1, 0, 1, true) }, details: Readings.DetailsOf(new[] { Cut }, suppressedBy: "Morphine")));
        page.Run($"hover(buffTip({Life}, 0))");

        Assert.Equal("Suppressed by Morphine", page.Text($"blockText(buffTip({Life}, 0), 'vs-bt')"));
    }

    [Fact]
    public void A_good_buff_and_cures_turned_off_list_no_items()
    {
        var page = PageScript.On("{buffs:[[], [], [], [1111, 7]]}");
        var good = Readings.Rule(7, "Rested", isGood: true, atStart: new double[] { 0, 5, 0, 0 }, cures: new[] { 2400 });

        page.Apply(0, Readings.Data(buffs: new[] { new BuffTime(1111, -1, 0), new BuffTime(7, -1, 0) }, details: Readings.DetailsOf(new[] { Cut, good }, items: Items)));
        page.Run($"hover(buffTip({Life}, 1))");
        Assert.Equal("Morale when it starts +5", page.Text($"blockText(buffTip({Life}, 1), 'vs-bt')"));

        page.Apply(2, Readings.Data(buffs: new[] { new BuffTime(1111, -1, 0), new BuffTime(7, -1, 0) }, details: Readings.DetailsOf(new[] { Cut, good }, items: Items),
            options: new Options(Cures: false)));
        page.Run($"hover(buffTip({Life}, 0))");
        Assert.Equal("Life every 2h -2\nSatiety gain +50%", page.Text($"blockText(buffTip({Life}, 0), 'vs-bt')"));
    }

    [Fact]
    public void Moving_to_another_buff_takes_the_block_out_of_the_first_tooltip()
    {
        var page = PageScript.On("{buffs:[[1111, 1111]]}");

        page.Apply(0, Readings.Data(buffs: new[] { new BuffTime(1111, -1, 0), new BuffTime(1111, -1, 0) }, details: Readings.DetailsOf(new[] { Cut })));
        page.Run($"hover(buffTip({Satiety}, 0))");
        page.Run($"hover(buffTip({Satiety}, 1))");

        Assert.False(page.Is($"childWith(buffTip({Satiety}, 0), 'vs-bt')"));
        Assert.True(page.Is($"childWith(buffTip({Satiety}, 1), 'vs-bt')"));
    }

    [Fact]
    public void The_hovered_tooltip_follows_a_new_reading()
    {
        var page = PageScript.On("{buffs:[[], [], [], [1111]]}");

        page.Apply(0, Readings.Data(buffs: new[] { new BuffTime(1111, -1, 0, 1) }, details: Readings.DetailsOf(new[] { Cut })));
        page.Run($"hover(buffTip({Life}, 0))");
        page.Apply(2, Readings.Data(buffs: new[] { new BuffTime(1111, -1, 0, 3) }, details: Readings.DetailsOf(new[] { Cut })));

        Assert.StartsWith("Life every 2h -6", page.Text($"blockText(buffTip({Life}, 0), 'vs-bt')"));
    }

    [Fact]
    public void Stat_names_in_buff_lines_are_the_game_own()
    {
        var page = PageScript.On("{buffs:[[], [], [], [1111]],names:['Hunger','Mood','Energy','Health']}");

        page.Apply(0, Readings.Data(buffs: new[] { new BuffTime(1111, -1, 0) }, details: Readings.DetailsOf(new[] { Cut })));
        page.Run($"hover(buffTip({Life}, 0))");

        Assert.StartsWith("Health every 2h -2\nHunger gain +50%", page.Text($"blockText(buffTip({Life}, 0), 'vs-bt')"));
    }

    [Fact]
    public void A_stripe_turns_amber_when_little_time_is_left()
    {
        var page = PageScript.On("{buffs:[[1300, 1300]]}");

        page.Apply(0, Readings.Data(buffs: new[] { new BuffTime(1300, 100, 1000), new BuffTime(1300, 900, 1000) }));

        Assert.True(page.Is($"stripeOn({Satiety}, 0).classList.contains('vs-timer-low')"));
        Assert.False(page.Is($"stripeOn({Satiety}, 1).classList.contains('vs-timer-low')"));
    }

    [Fact]
    public void A_buff_with_no_end_and_stripes_turned_off_draw_no_stripe()
    {
        var page = PageScript.On("{buffs:[[1300, 5]]}");

        page.Apply(0, Readings.Data(buffs: new[] { new BuffTime(1300, -1, 0), new BuffTime(5, 500, 1000) }));
        Assert.False(page.Is($"stripeOn({Satiety}, 0)"));
        Assert.True(page.Is($"stripeOn({Satiety}, 1)"));

        page.Apply(2, Readings.Data(buffs: new[] { new BuffTime(1300, -1, 0), new BuffTime(5, 500, 1000) }, options: new Options(Stripes: false)));
        Assert.Equal(0, page.Number("document.querySelectorAll('.vs-timer').length"));
    }

    [Fact]
    public void The_weight_bar_fills_to_the_load_with_a_gap_at_Overweight()
    {
        var page = PageScript.On();

        page.Apply(0, Readings.Data(weight: new CarryWeight(12_340, 20_000, 16_000)));

        Assert.Equal("61.70%", page.Text("childWith(childWith(page.bag, 'vs-wbar'), 'vs-wfill').style.width"));
        Assert.Equal("80.00%", page.Text("childWith(childWith(page.bag, 'vs-wbar'), 'vs-wgap').style.left"));

        page.Apply(2, Readings.Data(weight: new CarryWeight(25_000, 20_000, 16_000)));

        Assert.Equal("100.00%", page.Text("childWith(childWith(page.bag, 'vs-wbar'), 'vs-wfill').style.width"));
        Assert.True(page.Is("childWith(page.bag, 'vs-wbar').classList.contains('vs-over')"));
    }

    [Fact]
    public void With_the_weight_readout_off_the_button_is_left_alone()
    {
        var page = PageScript.On();

        page.Apply(0, Readings.Data(weight: new CarryWeight(12_340, 20_000, 16_000)));
        page.Apply(2, Readings.Data(weight: new CarryWeight(12_340, 20_000, 16_000), options: new Options(Weight: false)));

        Assert.Equal(0, page.Number("document.querySelectorAll('.vs-weight').length + document.querySelectorAll('.vs-wbar').length + document.querySelectorAll('.vs-weight-tip').length"));
    }

    // ===== Bar size =====

    // The multiplier chip sits beside the bar, so it is zoomed with it.
    private const string BarRule = ".status-panel > .status-row > .progress-bar-wrapper,.status-panel > .vs-fit-row > .progress-bar-wrapper,.status-panel > .status-row > .vs-chip{zoom:";
    private const string BuffRule = ".status-panel > .status-row > .buff-container{zoom:";
    private const string TipRule = ".status-panel .progress-bar-wrapper > .status-tooltip,.status-panel .vs-chip > .vs-ct{zoom:";

    /// <summary>The hover boxes sit inside the zoomed bars and zoom multiplies, so at the 80% bars they get 1/0.8 to show at
    /// the game's own size.</summary>
    [Fact]
    public void Bars_and_buffs_start_at_the_same_size_and_the_hover_boxes_at_the_game_size()
    {
        var page = PageScript.On();

        page.Apply(0, Readings.Data());

        Assert.Equal(BarRule + "0.8}" + BuffRule + "0.8}" + TipRule + "1.25}", page.Text("document.getElementById('vs-zoom').textContent"));
    }

    [Fact]
    public void The_bar_size_no_longer_scales_the_buffs_or_the_hover_boxes()
    {
        var page = PageScript.On();

        page.Apply(0, Readings.Data(options: new Options(BarSize: 120)));

        Assert.Equal(BarRule + "1.2}" + BuffRule + "0.8}" + TipRule + "0.8333}", page.Text("document.getElementById('vs-zoom').textContent"));
    }

    [Fact]
    public void The_buff_size_scales_only_the_buffs()
    {
        var page = PageScript.On();

        page.Apply(0, Readings.Data(options: new Options(BuffSize: 130)));

        Assert.Equal(BarRule + "0.8}" + BuffRule + "1.3}" + TipRule + "1.25}", page.Text("document.getElementById('vs-zoom').textContent"));
    }

    [Fact]
    public void The_hover_box_size_is_its_own_whatever_the_bar_size()
    {
        var page = PageScript.On();

        page.Apply(0, Readings.Data(options: new Options(BarSize: 100, BuffSize: 100, TooltipSize: 120)));

        Assert.Equal(TipRule + "1.2}", page.Text("document.getElementById('vs-zoom').textContent"));

        page.Apply(2, Readings.Data(options: new Options(BarSize: 60, BuffSize: 100, TooltipSize: 60)));

        Assert.Equal(BarRule + "0.6}", page.Text("document.getElementById('vs-zoom').textContent"));
    }

    [Fact]
    public void At_the_game_size_nothing_is_zoomed()
    {
        var page = PageScript.On();

        page.Apply(0, Readings.Data(options: new Options(BarSize: 100, BuffSize: 100)));

        Assert.Equal("", page.Text("document.getElementById('vs-zoom').textContent"));

        page.Apply(2, Readings.Data(options: new Options(BarSize: 100, BuffSize: 60)));

        Assert.Equal(BuffRule + "0.6}", page.Text("document.getElementById('vs-zoom').textContent"));
    }

    // ===== Fitness =====

    private const string FitRow = "childWith(page.panel, 'vs-fit-row')";
    private const string FitBar = FitRow + ".children[0]";
    private const string FitTip = "childWith(" + FitBar + ", 'status-tooltip')";

    private static string Fit(int value, int max = 100, Options? options = null, bool isChinese = false) =>
        Readings.Data(fitness: new FitnessReading(value, max), options: options, isChinese: isChinese);

    [Fact]
    public void Fitness_gets_a_bar_of_the_game_s_own_build_after_the_stat_rows()
    {
        var page = PageScript.On();

        page.Apply(0, Fit(72));

        Assert.Equal(5, page.Number("page.panel.children.length"));
        Assert.True(page.Is($"page.panel.children[4] === {FitRow}"));
        Assert.True(page.Is($"{FitBar}.classList.contains('progress-bar-wrapper') && {FitBar}.hasAttribute('data-interactive')"));
        Assert.Equal("Fitness", page.Text($"{FitBar}.querySelector('.vs-fit-name').textContent"));
        Assert.Equal("72", page.Text($"{FitBar}.querySelector('.vs-fit-value').textContent"));
        Assert.Equal("scaleX(0.7200)", page.Text($"{FitBar}.querySelector('.progress-fill').style.transform"));
        Assert.Equal("72/100", page.Text($"{FitTip}.querySelector('.st-value').textContent"));
        Assert.Contains("Below 30 you become Weakened", page.Text($"childWith({FitTip}, 'vs-fit-about').textContent"));
    }

    [Fact]
    public void Fitness_is_green_when_fine_amber_near_Weakened_and_red_below_30()
    {
        var page = PageScript.On();
        var fill = $"{FitBar}.querySelector('.progress-fill')";

        page.Apply(0, Fit(40));
        Assert.True(page.Is($"{fill}.classList.contains('bar-green') && !{fill}.classList.contains('bar-yellow') && !{FitBar}.classList.contains('is-low')"));
        Assert.Equal("10 above 30, where Weakened starts.", page.Text($"blockText({FitTip}, 'vs-st')"));

        page.Apply(2, Fit(39));
        Assert.True(page.Is($"{fill}.classList.contains('bar-yellow') && !{fill}.classList.contains('bar-green') && !{FitBar}.classList.contains('is-low')"));
        Assert.Equal("Close to Weakened: 10 more lost and it drops below 30.", page.Text($"blockText({FitTip}, 'vs-st')"));
        Assert.True(page.Is($"childWith({FitTip}, 'vs-st').children[0].classList.contains('vs-warn')"));

        page.Apply(4, Fit(29));
        Assert.True(page.Is($"{FitBar}.classList.contains('is-low') && !{fill}.classList.contains('bar-yellow') && !{fill}.classList.contains('bar-green')"));
        Assert.Equal("Below 30, so you are Weakened.", page.Text($"blockText({FitTip}, 'vs-st')"));
        Assert.True(page.Is($"childWith({FitTip}, 'vs-st').children[0].classList.contains('vs-bad')"));
    }

    [Fact]
    public void The_Fitness_state_line_sits_above_the_description_like_the_other_bars()
    {
        var page = PageScript.On();

        page.Apply(0, Fit(50));

        Assert.True(page.Is($"{FitTip}.children.indexOf(childWith({FitTip}, 'vs-st')) === {FitTip}.children.indexOf(childWith({FitTip}, 'vs-fit-about')) - 1"));
    }

    [Fact]
    public void The_Fitness_row_is_drawn_once_and_kept_last()
    {
        var page = PageScript.On();

        page.Apply(0, Fit(60));
        page.Run("page.panel.appendChild(new FakeElement('div', 'status-row'))");
        page.Apply(2, Fit(61));

        Assert.Equal(1, page.Number("document.querySelectorAll('.vs-fit-row').length"));
        Assert.True(page.Is($"page.panel.children[page.panel.children.length - 1] === {FitRow}"));
        Assert.Equal("61", page.Text($"{FitBar}.querySelector('.vs-fit-value').textContent"));
    }

    [Fact]
    public void The_panel_is_marked_while_the_Fitness_row_is_in_it()
    {
        var page = PageScript.On();

        page.Apply(0, Fit(60));
        Assert.True(page.Is("page.panel.hasAttribute('data-vs-fit')"));

        page.Apply(2, Readings.Data());
        Assert.False(page.Is("page.panel.hasAttribute('data-vs-fit')"));
        Assert.Equal(0, page.Number("document.querySelectorAll('.vs-fit-row').length"));
    }

    [Fact]
    public void With_no_player_the_Fitness_row_is_taken_away()
    {
        var page = PageScript.On();

        page.Apply(0, Fit(60));
        page.Apply(2, PageData.NoPlayer);

        Assert.Equal(0, page.Number("document.querySelectorAll('.vs-fit-row').length"));
        Assert.False(page.Is("page.panel.hasAttribute('data-vs-fit')"));
    }

    [Fact]
    public void With_the_Fitness_bar_off_no_row_is_drawn()
    {
        var page = PageScript.On();

        page.Apply(0, Fit(60, options: new Options(Fitness: false)));

        Assert.Equal(0, page.Number("document.querySelectorAll('.vs-fit-row').length"));
    }

    [Fact]
    public void With_the_game_text_off_the_Fitness_box_keeps_only_its_state_line()
    {
        var page = PageScript.On();

        page.Apply(0, Fit(60, options: new Options(GameText: false)));

        Assert.True(page.Is($"{FitTip}.hasAttribute('data-vs-no-game-text')"));
        Assert.Equal("30 above 30, where Weakened starts.", page.Text($"blockText({FitTip}, 'vs-st')"));
    }

    [Fact]
    public void A_Chinese_game_gets_the_Fitness_text_in_Chinese()
    {
        var page = PageScript.On();

        page.Apply(0, Fit(35, isChinese: true));

        Assert.Equal("健康", page.Text($"{FitBar}.querySelector('.vs-fit-name').textContent"));
        Assert.Equal("接近「亚健康」：再降低 6 点就会低于30。", page.Text($"blockText({FitTip}, 'vs-st')"));
    }

    [Fact]
    public void With_the_Fitness_Bar_mod_also_in_the_Fitness_box_says_to_remove_it()
    {
        var page = PageScript.On();

        page.Apply(0, Readings.Data(fitness: new FitnessReading(60, 100), hasOtherFitnessBar: true));

        Assert.Contains("Remove FitnessBar.dll", page.Text($"blockText({FitTip}, 'vs-fit-dup')"));
        Assert.True(page.Is($"childWith({FitTip}, 'vs-fit-dup').children[0].classList.contains('vs-warn')"));
    }

    [Fact]
    public void The_Fitness_Bar_warning_shows_with_the_extra_tooltips_and_game_text_off()
    {
        var page = PageScript.On();

        page.Apply(0, Readings.Data(fitness: new FitnessReading(60, 100), options: new Options(Tooltips: false, GameText: false), hasOtherFitnessBar: true));

        Assert.Contains("Remove FitnessBar.dll", page.Text($"blockText({FitTip}, 'vs-fit-dup')"));
    }

    [Fact]
    public void Without_the_Fitness_Bar_mod_there_is_no_warning()
    {
        var page = PageScript.On();

        page.Apply(0, Fit(60));

        Assert.False(page.Is($"childWith({FitTip}, 'vs-fit-dup')"));
    }

    // ===== Settings changed while playing =====

    private static string Everything(Options options) => Readings.Data(Readings.Stats(satiety: new StatRatios(0.5, 0.2, 0, 0)), new[] { new BuffTime(1300, 100, 100) },
        new CarryWeight(5000, 20000, 16000), WellFed, details: Readings.DetailsOf(new[] { Readings.Rule(1300) }), options: options, fitness: new FitnessReading(60, 100));

    [Theory]
    [InlineData("Weight", "vs-weight,vs-wbar,vs-weight-tip")]
    [InlineData("Stripes", "vs-timer")]
    [InlineData("Tooltips", "vs-st,vs-bt")]
    [InlineData("Fitness", "vs-fit-row")]
    public void A_display_setting_turned_off_takes_away_what_it_drew(string setting, string classes)
    {
        var page = PageScript.On("{buffs:[[1300]]}");
        var off = setting switch
        {
            "Weight" => new Options(Weight: false),
            "Stripes" => new Options(Stripes: false),
            "Tooltips" => new Options(Tooltips: false),
            _ => new Options(Fitness: false)
        };

        page.Apply(0, Everything(new Options()));
        page.Run($"hover(buffTip({Satiety}, 0))");

        foreach (var drawn in classes.Split(','))
        {
            Assert.True(page.Number($"document.querySelectorAll('.{drawn}').length") > 0, drawn + " drawn while on");
        }

        page.Apply(2, Everything(off));
        page.Run($"hover(buffTip({Satiety}, 0))");

        foreach (var drawn in classes.Split(','))
        {
            Assert.Equal(0, page.Number($"document.querySelectorAll('.{drawn}').length"));
        }

        page.Apply(4, Everything(new Options()));
        page.Run($"hover(buffTip({Satiety}, 0))");

        foreach (var drawn in classes.Split(','))
        {
            Assert.True(page.Number($"document.querySelectorAll('.{drawn}').length") > 0, drawn + " back when on again");
        }
    }

    [Fact]
    public void A_size_changed_while_playing_redraws_at_the_new_size()
    {
        var page = PageScript.On();

        page.Apply(0, Readings.Data(options: new Options(BarSize: 120)));
        Assert.StartsWith(BarRule + "1.2}", page.Text("document.getElementById('vs-zoom').textContent"));

        page.Apply(2, Readings.Data(options: new Options(BarSize: 100, BuffSize: 100, TooltipSize: 100)));
        Assert.Equal("", page.Text("document.getElementById('vs-zoom').textContent"));
    }
}
