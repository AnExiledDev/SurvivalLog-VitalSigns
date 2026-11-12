using VitalSigns.Core;
using Xunit;

namespace VitalSigns.Tests;

/// <summary>The bar looks, the trend arrows, the pulse on ending buffs and the heaviest items in the weight hover box.</summary>
public class PageLookTests
{
    private const string Satiety = "page.rows[0]";
    private const string Morale = "page.rows[1]";
    private const string Root = "document.documentElement";
    private const string WeightTip = "blockText(page.bag.querySelector('.entry-tip'), 'vs-weight-tip')";
    private const string WeightRows = "childWith(page.bag.querySelector('.entry-tip'), 'vs-weight-tip').children";

    private static readonly CarryWeight Weight = new(12_340, 20_000, 16_000);

    // ===== Look =====

    [Fact]
    public void By_default_the_page_root_asks_for_the_HUD_style_colours_and_the_games_bars()
    {
        var page = PageScript.On();

        page.Apply(0, Readings.Data());

        Assert.True(page.Is($"{Root}.hasAttribute('data-vs-match')"));
        Assert.False(page.Is($"{Root}.hasAttribute('data-vs-cb')"));
        Assert.False(page.Is($"{Root}.hasAttribute('data-vs-bars')"));
    }

    [Fact]
    public void The_bar_look_and_colour_blind_colours_are_named_on_the_root_and_taken_off_when_turned_back()
    {
        var page = PageScript.On();

        page.Apply(0, Readings.Data(options: new Options(BarLook: BarLook.Segmented, ColourBlind: true, MatchHud: false)));

        Assert.Equal("segmented", page.Text($"{Root}.getAttribute('data-vs-bars')"));
        Assert.True(page.Is($"{Root}.hasAttribute('data-vs-cb')"));
        Assert.False(page.Is($"{Root}.hasAttribute('data-vs-match')"));

        page.Apply(2, Readings.Data(options: new Options(BarLook: BarLook.Flat)));

        Assert.Equal("flat", page.Text($"{Root}.getAttribute('data-vs-bars')"));

        page.Apply(4, Readings.Data());

        Assert.False(page.Is($"{Root}.hasAttribute('data-vs-bars')"));
        Assert.False(page.Is($"{Root}.hasAttribute('data-vs-cb')"));
    }

    [Fact]
    public void The_stylesheet_takes_the_Toolkit_colours_only_from_a_look_that_is_not_classic()
    {
        var page = PageScript.On();

        page.Apply(0, Readings.Data());

        var css = page.Text("document.getElementById('vs-style').textContent");

        Assert.Contains("html[data-vs-match][data-etk-look]:not([data-etk-look=\"classic\"]){--vs-panel:var(--etk-panel,rgba(14,16,19,.93))", css);
        Assert.Contains(".vs-chip-up{background:var(--vs-up)", css);
    }

    // ===== Trend arrows =====

    [Fact]
    public void A_stat_that_rose_over_ten_game_minutes_or_more_gets_an_up_arrow()
    {
        var page = PageScript.On("{values:[0.5,0.5,0.5,0.5]}");

        page.Apply(0, Readings.Data());
        page.Run("page.attrList[0].value = 0.6; page.attrList[1].value = 0.4;");
        page.Apply(300);

        Assert.False(page.Is($"{Satiety}.querySelector('.vs-trend')"));

        page.Apply(600);

        Assert.Equal("\u25B2", page.Text($"{Satiety}.querySelector('.vs-trend').textContent"));
        Assert.True(page.Is($"{Satiety}.querySelector('.vs-trend').classList.contains('vs-trend-up')"));
        Assert.True(page.Is($"{Morale}.querySelector('.vs-trend').classList.contains('vs-trend-down')"));
        Assert.Equal("\u25BC", page.Text($"{Morale}.querySelector('.vs-trend').textContent"));
        Assert.False(page.Is("page.rows[2].querySelector('.vs-trend')"));
    }

    [Fact]
    public void The_arrow_sits_inside_the_bar_text_with_the_value()
    {
        var page = PageScript.On("{values:[0.5,0.5,0.5,0.5]}");

        page.Apply(0, Readings.Data());
        page.Run("page.attrList[0].value = 0.9;");
        page.Apply(600);

        Assert.True(page.Is($"{Satiety}.querySelector('.vs-trend').parentElement.classList.contains('progress-text')"));
    }

    /// <summary>The arrow is the last game hour only: a stat that rose and then held for an hour shows none.</summary>
    [Fact]
    public void Readings_older_than_an_hour_are_dropped()
    {
        var page = PageScript.On("{values:[0.2,0.5,0.5,0.5]}");

        page.Apply(0, Readings.Data());
        page.Run("page.attrList[0].value = 0.8;");
        page.Apply(3700);

        Assert.True(page.Is($"{Satiety}.querySelector('.vs-trend')"));

        page.Apply(7400);

        Assert.False(page.Is($"{Satiety}.querySelector('.vs-trend')"));
    }

    [Fact]
    public void A_change_too_small_to_see_is_no_change()
    {
        var page = PageScript.On("{values:[0.5,0.5,0.5,0.5]}");

        page.Apply(0, Readings.Data());
        page.Run("page.attrList[0].value = 0.503;");
        page.Apply(900);

        Assert.False(page.Is($"{Satiety}.querySelector('.vs-trend')"));
    }

    [Fact]
    public void A_clock_that_went_back_starts_the_readings_again()
    {
        var page = PageScript.On("{values:[0.5,0.5,0.5,0.5]}");

        page.Apply(5000, Readings.Data());
        page.Run("page.attrList[0].value = 0.9;");
        page.Apply(100);
        page.Apply(400);

        Assert.False(page.Is($"{Satiety}.querySelector('.vs-trend')"));
    }

    [Fact]
    public void With_trend_arrows_off_the_arrows_go()
    {
        var page = PageScript.On("{values:[0.5,0.5,0.5,0.5]}");

        page.Apply(0, Readings.Data());
        page.Run("page.attrList[0].value = 0.9;");
        page.Apply(600);
        page.Apply(602, Readings.Data(options: new Options(Trends: false)));

        Assert.Equal(0, page.Number("document.querySelectorAll('.vs-trend').length"));
    }

    // ===== Ending pulse =====

    private static Details GoodOnly(params int[] ids) => Readings.DetailsOf(good: ids);

    [Theory]
    [InlineData(3000, true)]
    [InlineData(3600, true)]
    [InlineData(4000, false)]
    public void A_good_buff_pulses_once_its_time_left_reaches_the_set_minutes(int left, bool isPulsing)
    {
        var page = PageScript.On("{buffs:[[1300]]}");

        page.Apply(1000, Readings.Data(buffs: new[] { new BuffTime(1300, 1000 + left, 20_000) }, details: GoodOnly(1300)));

        Assert.Equal(isPulsing, page.Is($"{Satiety}.querySelectorAll('.buff')[0].hasAttribute('data-vs-ending')"));
    }

    [Fact]
    public void A_debuff_pulses_only_with_debuffs_turned_on()
    {
        var page = PageScript.On("{buffs:[[1300]]}");
        var buffs = new[] { new BuffTime(1300, 1500, 20_000) };

        page.Apply(1000, Readings.Data(buffs: buffs, details: GoodOnly()));

        Assert.False(page.Is($"{Satiety}.querySelectorAll('.buff')[0].hasAttribute('data-vs-ending')"));

        page.Apply(1002, Readings.Data(buffs: buffs, details: GoodOnly(), options: new Options(EndingDebuffs: true)));

        Assert.True(page.Is($"{Satiety}.querySelectorAll('.buff')[0].hasAttribute('data-vs-ending')"));
    }

    [Fact]
    public void A_buff_without_an_end_or_with_the_pulse_off_does_not_pulse()
    {
        var page = PageScript.On("{buffs:[[1300, 7]]}");
        var buffs = new[] { new BuffTime(1300, 1500, 20_000), new BuffTime(7, -1, 0) };

        page.Apply(1000, Readings.Data(buffs: buffs, details: GoodOnly(1300, 7)));

        Assert.False(page.Is($"{Satiety}.querySelectorAll('.buff')[1].hasAttribute('data-vs-ending')"));

        page.Apply(1002, Readings.Data(buffs: buffs, details: GoodOnly(1300, 7), options: new Options(EndingPulse: false)));

        Assert.False(page.Is($"{Satiety}.querySelectorAll('.buff')[0].hasAttribute('data-vs-ending')"));
    }

    [Fact]
    public void The_pulse_starts_with_the_clock_alone()
    {
        var page = PageScript.On("{buffs:[[1300]]}");

        page.Apply(0, Readings.Data(buffs: new[] { new BuffTime(1300, 4000, 20_000) }, details: GoodOnly(1300)));

        Assert.False(page.Is($"{Satiety}.querySelectorAll('.buff')[0].hasAttribute('data-vs-ending')"));

        page.Apply(500);

        Assert.True(page.Is($"{Satiety}.querySelectorAll('.buff')[0].hasAttribute('data-vs-ending')"));
    }

    // ===== Heaviest items =====

    [Fact]
    public void The_weight_hover_box_lists_the_heaviest_items_after_the_room_left()
    {
        var page = PageScript.On();

        page.Apply(0, Readings.Data(weight: Weight, heavy: new[] { new HeavyItem("Iron bar", 4000, 2), new HeavyItem("Rope", 500, 1) }));

        Assert.Equal("Carrying 12.4 / 20.0 kg\nRoom before Overweight 3.6 kg\nHeaviest items\nIron bar x2 4.0 kg\nRope 0.5 kg", page.Text(WeightTip));
    }

    /// <summary>The heading reads as a heading and each item is a name on the left with its weight on the right, so the weights line up.</summary>
    [Fact]
    public void Each_heavy_item_puts_its_name_left_and_its_weight_right_under_a_heading()
    {
        var page = PageScript.On();

        page.Apply(0, Readings.Data(weight: Weight, heavy: new[] { new HeavyItem("Iron bar", 4000, 2), new HeavyItem("Rope", 500, 1) }));

        Assert.True(page.Is($"{WeightRows}[2].classList.contains('vs-head')"));
        Assert.True(page.Is($"{WeightRows}[3].classList.contains('vs-kv') && {WeightRows}[4].classList.contains('vs-kv')"));
        Assert.Equal("Iron bar x2", page.Text($"childWith({WeightRows}[3], 'vs-k').textContent"));
        Assert.Equal("4.0 kg", page.Text($"childWith({WeightRows}[3], 'vs-v').textContent"));
    }

    [Fact]
    public void Past_Overweight_the_heaviest_items_follow_the_Overweight_line()
    {
        var page = PageScript.On();

        page.Apply(0, Readings.Data(weight: new CarryWeight(17_050, 20_000, 16_000), heavy: new[] { new HeavyItem("Iron bar", 4000, 2) }));

        Assert.Equal("Carrying 17.1 / 20.0 kg\nOverweight 1.1 kg over\nHeaviest items\nIron bar x2 4.0 kg", page.Text(WeightTip));
    }

    [Fact]
    public void The_heaviest_items_are_listed_in_Chinese_in_a_Chinese_game()
    {
        var page = PageScript.On();

        page.Apply(0, Readings.Data(weight: Weight, isChinese: true, names: new GameNames("饱腹富余", "超重"), heavy: new[] { new HeavyItem("铁锭", 4000, 2) }));

        Assert.EndsWith("\n最重的物品\n铁锭 x2 4.0 千克", page.Text(WeightTip));
    }

    [Fact]
    public void With_no_heaviest_items_the_hover_box_has_no_heading()
    {
        var page = PageScript.On();

        page.Apply(0, Readings.Data(weight: Weight, heavy: System.Array.Empty<HeavyItem>()));

        Assert.Equal("Carrying 12.4 / 20.0 kg\nRoom before Overweight 3.6 kg", page.Text(WeightTip));
    }

    // ===== Taken away =====

    [Fact]
    public void With_no_player_the_arrows_the_pulse_and_the_root_marks_are_taken_away()
    {
        var page = PageScript.On("{buffs:[[1300]],values:[0.5,0.5,0.5,0.5]}");
        var data = Readings.Data(buffs: new[] { new BuffTime(1300, 1500, 20_000) }, details: GoodOnly(1300),
            options: new Options(BarLook: BarLook.Segmented, ColourBlind: true));

        page.Apply(0, data);
        page.Run("page.attrList[0].value = 0.9;");
        page.Apply(600);

        Assert.True(page.Is($"{Satiety}.querySelector('.vs-trend') && {Satiety}.querySelectorAll('.buff')[0].hasAttribute('data-vs-ending')"));

        Assert.Equal(PageAnswer.NoPlayer, page.Apply(602, PageData.NoPlayer));

        Assert.Equal(0, page.Number("document.querySelectorAll('.vs-trend').length"));
        Assert.False(page.Is($"{Satiety}.querySelectorAll('.buff')[0].hasAttribute('data-vs-ending')"));
        Assert.False(page.Is($"{Root}.hasAttribute('data-vs-bars') || {Root}.hasAttribute('data-vs-cb') || {Root}.hasAttribute('data-vs-match')"));

        // The readings went with the player: the next one starts the hour again.
        page.Apply(604, data);

        Assert.False(page.Is($"{Satiety}.querySelector('.vs-trend')"));
    }
}
