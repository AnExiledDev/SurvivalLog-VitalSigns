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
        var coming = new IReadOnlyList<LossLine>[] { new[] { new LossLine(1300, 10, 100) }, Array.Empty<LossLine>(), Array.Empty<LossLine>(), Array.Empty<LossLine>() };

        page.Apply(0, Readings.Data(Readings.Stats(satiety: new StatRatios(0.5, 0.2, 0, 0)), new[] { new BuffTime(1300, 100, 100) },
            new CarryWeight(5000, 20000, 16000), WellFed, details: Readings.DetailsOf(new[] { Readings.Rule(1300) }, coming), options: new Options(GameText: false)));
        page.Run($"hover(buffTip({Satiety}, 0))");
        Assert.True(page.Is("document.querySelectorAll('.vs-bt').length && document.querySelectorAll('.vs-loss').length && document.querySelectorAll('.vs-no-game-text').length"));

        Assert.True(page.Number("document.querySelectorAll('.vs-chip').length") > 0);
        Assert.Equal(PageAnswer.NoPlayer, page.Apply(10, PageData.NoPlayer));

        foreach (var drawn in new[] { "vs-chip", "vs-mark", "vs-loss", "vs-st", "vs-timer", "vs-bt", "vs-weight", "vs-wbar", "vs-weight-tip", "vs-no-game-text" })
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

        Assert.Equal("x1.5", page.Text("childWith(maskOf(page.rows[3]), 'vs-chip').textContent"));
        Assert.Equal(2, page.Number("maskOf(page.rows[3]).querySelectorAll('.vs-mark').length"));
        Assert.False(page.Is("childWith(maskOf(page.rows[0]), 'vs-chip')"));
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
        Assert.False(page.Is($"tipOf({Satiety}).classList.contains('vs-no-game-text')"));

        page.Apply(2, Readings.Data(Readings.Stats(satiety: new StatRatios(0.5, 0, 0, 0)), options: new Options(GameText: false)));

        Assert.True(page.Is($"tipOf({Satiety}).classList.contains('vs-no-game-text')"));
    }

    [Fact]
    public void With_tooltips_off_the_bars_keep_their_chip_and_shading_but_no_lines()
    {
        var page = PageScript.On("{values:[0.5,0.5,0.5,0.5],buffs:[[1111]]}");

        page.Apply(0, Readings.Data(Readings.Stats(satiety: new StatRatios(0.5, 0, 0, 0)), new[] { new BuffTime(1111, 3600, 7200) },
            details: Readings.DetailsOf(new[] { Cut }, Coming(0, new LossLine(1111, 10, 900)), Items), options: new Options(Tooltips: false)));
        page.Run($"hover(buffTip({Satiety}, 0))");

        Assert.True(page.Is($"childWith(maskOf({Satiety}), 'vs-chip')"));
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

        Assert.Equal("Life every 2h -4\nSatiety gain +100%\nLayers: 2\n1h left\n\nCure with:\nGauze x2\nHerbs x1\nBandage x0\nSuppress with:\nMorphine x0",
            page.Text($"blockText(buffTip({Life}, 0), 'vs-bt')"));
        Assert.Equal("50.00%", page.Text($"childWith(buffTip({Life}, 0), 'vs-bt').querySelector('.vs-tfill').style.width"));
        Assert.True(page.Is($"childWith(buffTip({Life}, 0), 'vs-bt').querySelectorAll('.vs-item')[2].classList.contains('vs-none')"));
        Assert.False(page.Is($"childWith(buffTip({Life}, 0), 'vs-bt').querySelectorAll('.vs-item')[0].classList.contains('vs-none')"));
        Assert.Equal("Icons/gauze.png", page.Text($"childWith(buffTip({Life}, 0), 'vs-bt').querySelectorAll('.vs-item')[0].children[0].attrs.src"));
        Assert.True(page.Is($"childWith(buffTip({Life}, 0), 'bt-body')"));
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
}
