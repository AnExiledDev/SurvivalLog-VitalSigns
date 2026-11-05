using VitalSigns.Core;
using Xunit;

namespace VitalSigns.Tests;

/// <summary>
/// The HUD settings on the page: hiding full bars, the warning pulse, clicks on bars and buffs, pinned buffs, the
/// queue's projection, the layout, the layout editor and the bars while exploring.
/// </summary>
public class HudPageTests
{
    private const string Satiety = "page.rows[0]";
    private const string Morale = "page.rows[1]";
    private const string Prefix = "slmod|vitalsigns|hud|";

    private static string Bar(string row) => $"childWith({row}, 'progress-bar-wrapper')";

    private static string Badge(string row, int n) => $"{row}.querySelectorAll('.buff')[{n}]";

    private static string Sent(PageScript page) => page.Text("sent.join('\\n')");

    // ===== Hiding full bars =====

    [Fact]
    public void A_bar_at_or_above_the_set_level_is_hidden_and_one_below_it_is_not()
    {
        var page = PageScript.On("{values:[0.95,0.5,0.9,0.89]}");

        page.Apply(0, Readings.Data(hud: new HudOptions(HideFull: true, HideAbove: 90)));

        Assert.True(page.Is($"{Satiety}.hasAttribute('data-vs-hide')"));
        Assert.False(page.Is($"{Morale}.hasAttribute('data-vs-hide')"));
        Assert.True(page.Is("page.rows[2].hasAttribute('data-vs-hide')"));
        Assert.False(page.Is("page.rows[3].hasAttribute('data-vs-hide')"));
    }

    [Fact]
    public void A_full_bar_with_buffs_stays_when_asked_and_hides_when_not()
    {
        var page = PageScript.On("{values:[1,0.5,0.5,0.5], buffs:[[1300]]}");
        var buffs = new[] { new BuffTime(1300, 100, 100) };

        page.Apply(0, Readings.Data(buffs: buffs, hud: new HudOptions(HideFull: true, HideAbove: 100, KeepWithBuffs: true)));

        Assert.False(page.Is($"{Satiety}.hasAttribute('data-vs-hide')"));

        page.Apply(2, Readings.Data(buffs: buffs, hud: new HudOptions(HideFull: true, HideAbove: 100, KeepWithBuffs: false)));

        Assert.True(page.Is($"{Satiety}.hasAttribute('data-vs-hide')"));
    }

    [Fact]
    public void Turning_hiding_off_shows_the_bar_again()
    {
        var page = PageScript.On("{values:[1,0.5,0.5,0.5]}");

        page.Apply(0, Readings.Data(hud: new HudOptions(HideFull: true)));
        page.Apply(2, Readings.Data(hud: new HudOptions(HideFull: false)));

        Assert.False(page.Is($"{Satiety}.hasAttribute('data-vs-hide')"));
    }

    // ===== Warning pulse =====

    [Fact]
    public void A_bar_at_or_under_the_warning_line_pulses_and_stops_when_the_pulse_is_off()
    {
        var page = PageScript.On("{values:[0.25,0.3,0.31,0.9]}");

        page.Apply(0, Readings.Data(hud: new HudOptions(WarnPulse: true, WarnAt: 30)));

        Assert.True(page.Is($"{Bar(Satiety)}.hasAttribute('data-vs-warn')"));
        Assert.True(page.Is($"{Bar(Morale)}.hasAttribute('data-vs-warn')"));
        Assert.False(page.Is($"{Bar("page.rows[2]")}.hasAttribute('data-vs-warn')"));

        page.Apply(2, Readings.Data(hud: new HudOptions(WarnPulse: false, WarnAt: 30)));

        Assert.False(page.Is($"{Bar(Satiety)}.hasAttribute('data-vs-warn')"));
    }

    // ===== Clicks =====

    [Fact]
    public void A_click_on_a_bar_asks_the_plugin_to_act_for_that_stat()
    {
        var page = PageScript.On();
        var reading = new HudReading(CanAct: new[] { true, false, true, true }, Tips: new[] { "eat to full", "nothing to do", "sleep", "use a bandage" });

        page.Apply(0, Readings.Data(hud: new HudOptions(ClickBars: true), reading: reading));

        Assert.Equal("1", page.Text($"{Bar(Satiety)}.getAttribute('data-vs-act')"));
        Assert.Equal("0", page.Text($"{Bar(Morale)}.getAttribute('data-vs-act')"));

        page.Run($"click({Bar(Satiety)}.querySelector('.progress-fill'));");

        Assert.Equal(Prefix + "act|0", Sent(page));
    }

    [Fact]
    public void With_bar_clicks_off_a_click_on_a_bar_sends_nothing()
    {
        var page = PageScript.On();
        var reading = new HudReading(CanAct: new[] { true, true, true, true }, Tips: new[] { "a", "b", "c", "d" });

        page.Apply(0, Readings.Data(hud: new HudOptions(ClickBars: false), reading: reading));
        page.Run($"click({Bar(Satiety)});");

        Assert.False(page.Is($"{Bar(Satiety)}.hasAttribute('data-vs-act')"));
        Assert.Equal("", Sent(page));
    }

    [Fact]
    public void A_click_on_a_curable_debuff_asks_for_its_cure_and_a_shift_click_pins_it()
    {
        var page = PageScript.On("{buffs:[[1300,1400]]}");
        var buffs = new[] { new BuffTime(1300, 100, 100), new BuffTime(1400, 100, 100) };

        page.Apply(0, Readings.Data(buffs: buffs, hud: new HudOptions(Cure: true), reading: new HudReading(Curable: new[] { 1300 })));

        Assert.True(page.Is($"{Badge(Satiety, 0)}.hasAttribute('data-vs-cure')"));
        Assert.False(page.Is($"{Badge(Satiety, 1)}.hasAttribute('data-vs-cure')"));

        page.Run($"click({Badge(Satiety, 0)}); click({Badge(Satiety, 1)}); click({Badge(Satiety, 1)}, true);");

        Assert.Equal(Prefix + "cure|1300\n" + Prefix + "fav|1400", Sent(page));
    }

    [Fact]
    public void With_cures_off_a_debuff_is_not_marked_and_a_click_on_it_sends_nothing()
    {
        var page = PageScript.On("{buffs:[[1300]]}");

        page.Apply(0, Readings.Data(buffs: new[] { new BuffTime(1300, 100, 100) }, hud: new HudOptions(Cure: false), reading: new HudReading(Curable: new[] { 1300 })));
        page.Run($"click({Badge(Satiety, 0)});");

        Assert.False(page.Is($"{Badge(Satiety, 0)}.hasAttribute('data-vs-cure')"));
        Assert.Equal("", Sent(page));
    }

    // ===== Buff tray =====

    [Fact]
    public void A_pinned_buff_sits_next_to_the_bar_and_shows_its_time_left()
    {
        var page = PageScript.On("{buffs:[[10,20,30]]}");
        var buffs = new[] { new BuffTime(10, 100, 100), new BuffTime(20, 100, 100), new BuffTime(30, 3600, 3600) };

        page.Apply(0, Readings.Data(buffs: buffs, hud: new HudOptions(Favourites: new[] { 30 })));

        // A tray left of the bar is drawn right to left: the highest order is the one by the bar.
        Assert.Equal("2", page.Text($"{Badge(Satiety, 2)}.style.order"));
        Assert.True(page.Is($"{Badge(Satiety, 2)}.hasAttribute('data-vs-fav')"));
        Assert.True(page.Is($"childWith({Badge(Satiety, 2)}, 'vs-ftime')"));
        Assert.False(page.Is($"childWith({Badge(Satiety, 0)}, 'vs-ftime')"));
    }

    [Fact]
    public void Ending_first_puts_the_buff_with_least_time_by_the_bar_and_the_game_order_comes_back_when_turned_off()
    {
        var page = PageScript.On("{buffs:[[10,20]]}");
        var buffs = new[] { new BuffTime(10, 900, 1000), new BuffTime(20, 200, 1000) };

        page.Apply(0, Readings.Data(buffs: buffs, hud: new HudOptions(Sort: HudChoices.SortEndingFirst)));

        Assert.Equal("1", page.Text($"{Badge(Satiety, 1)}.style.order"));
        Assert.Equal("0", page.Text($"{Badge(Satiety, 0)}.style.order"));

        page.Apply(2, Readings.Data(buffs: buffs, hud: new HudOptions()));

        Assert.Equal("", page.Text($"{Badge(Satiety, 0)}.style.order || ''"));
        Assert.Equal("", page.Text($"{Badge(Satiety, 1)}.style.order || ''"));
    }

    // ===== Projection =====

    [Fact]
    public void The_queue_projection_shades_from_the_level_to_where_the_queue_leaves_it()
    {
        var page = PageScript.On("{values:[0.5,0.6,0.5,0.5]}");
        var reading = new HudReading(Projected: new double?[] { 0.8, 0.4, null, 0.5 });

        page.Apply(0, Readings.Data(hud: new HudOptions(Projection: true), reading: reading));

        var up = $"childWith(maskOf({Satiety}), 'vs-proj')";
        var down = $"childWith(maskOf({Morale}), 'vs-proj')";

        Assert.Equal("50.00%", page.Text($"{up}.style.left"));
        Assert.Equal("30.00%", page.Text($"{up}.style.width"));
        Assert.False(page.Is($"{up}.classList.contains('vs-proj-down')"));
        Assert.True(page.Is($"{down}.classList.contains('vs-proj-down')"));
        Assert.Equal("40.00%", page.Text($"{down}.style.left"));
        Assert.False(page.Is("childWith(maskOf(page.rows[2]), 'vs-proj')"));
        Assert.False(page.Is("childWith(maskOf(page.rows[3]), 'vs-proj')"));

        page.Apply(2, Readings.Data(hud: new HudOptions(Projection: false), reading: reading));

        Assert.False(page.Is(up));
    }

    // ===== Layout =====

    [Fact]
    public void A_saved_layout_moves_and_sizes_its_parts_and_going_back_home_puts_them_back()
    {
        var page = PageScript.On();
        var layout = HudLayout.Parse("10,20,150;0,0,100;0,0,100;0,0,100;0,0,100;-30,5,100");

        page.Apply(0, Readings.Data(hud: new HudOptions(Layout: layout, Tray: HudChoices.TrayRight)));

        Assert.Equal("translate(10px,20px) scale(1.5)", page.Text($"{Satiety}.style.transform"));
        Assert.True(page.Is($"{Satiety}.hasAttribute('data-vs-moved')"));
        Assert.False(page.Is($"{Morale}.hasAttribute('data-vs-moved')"));
        Assert.Equal("translate(-30px,5px) scale(1)", page.Text("page.panel.style.transform"));
        Assert.Equal("right", page.Text("page.panel.getAttribute('data-vs-tray')"));

        page.Apply(2, Readings.Data(hud: new HudOptions()));

        Assert.Equal("", page.Text($"{Satiety}.style.transform || ''"));
        Assert.False(page.Is($"{Satiety}.hasAttribute('data-vs-moved')"));
        Assert.False(page.Is("page.panel.hasAttribute('data-vs-tray')"));
    }

    [Fact]
    public void The_editor_shows_every_bar_and_save_sends_the_layout_and_closes()
    {
        var page = PageScript.On("{values:[1,0.5,0.5,0.5]}");

        page.Apply(0, Readings.Data(hud: new HudOptions(HideFull: true)));

        Assert.True(page.Is($"{Satiety}.hasAttribute('data-vs-hide')"));
        Assert.Equal("ok", page.Text("window.__slvitals.edit(true)"));
        Assert.True(page.Is("document.getElementById('vs-edit-bar')"));
        Assert.False(page.Is($"{Satiety}.hasAttribute('data-vs-hide')"));
        Assert.True(page.Is("page.panel.hasAttribute('data-vs-editing')"));

        page.Run("click(document.getElementById('vs-edit-bar').querySelector('.vs-tray'));");
        page.Run("click(document.getElementById('vs-edit-bar').querySelector('.vs-save'));");

        Assert.Equal(Prefix + "layout||1|0", Sent(page));
        Assert.False(page.Is("document.getElementById('vs-edit-bar')"));
        Assert.False(page.Is("document.getElementById('vs-edit')"));
        Assert.True(page.Is($"{Satiety}.hasAttribute('data-vs-hide')"));
        Assert.Equal("right", page.Text("page.panel.getAttribute('data-vs-tray')"));
    }

    [Fact]
    public void Cancel_closes_the_editor_without_a_layout_and_tells_the_plugin()
    {
        var page = PageScript.On();

        page.Apply(0, Readings.Data(hud: new HudOptions()));
        page.Text("window.__slvitals.edit(true)");
        page.Run("click(document.getElementById('vs-edit-bar').querySelector('.vs-tray'));");
        page.Run("click(document.getElementById('vs-edit-bar').querySelector('.vs-cancel'));");

        Assert.Equal(Prefix + "edit|0", Sent(page));
        Assert.False(page.Is("document.getElementById('vs-edit-bar')"));
        Assert.False(page.Is("page.panel.hasAttribute('data-vs-tray')"));
    }

    [Fact]
    public void Bar_clicks_send_nothing_while_the_editor_is_open()
    {
        var page = PageScript.On();
        var reading = new HudReading(CanAct: new[] { true, true, true, true }, Tips: new[] { "a", "b", "c", "d" });

        page.Apply(0, Readings.Data(hud: new HudOptions(), reading: reading));
        page.Text("window.__slvitals.edit(true)");
        page.Run($"click({Bar(Satiety)});");

        Assert.Equal("", Sent(page));
    }

    // ===== Bars while exploring =====

    [Fact]
    public void While_the_travel_map_is_open_the_bars_show_in_the_set_corner_and_go_when_it_closes()
    {
        var page = PageScript.On("{values:[0.1,0.4,0.9,0.6]}");

        page.Run("var root = buildTop(true);");
        page.Apply(0, Readings.Data(hud: new HudOptions(Explore: HudChoices.CornerBottomLeft, ExploreSize: 120)));

        var strip = "root.document.getElementById('vs-explore')";

        Assert.Equal("bl", page.Text($"{strip}.getAttribute('data-corner')"));
        Assert.Equal("1.2", page.Text($"{strip}.style.zoom"));
        Assert.Equal(4, page.Number($"{strip}.children.length"));
        Assert.True(page.Is($"{strip}.children[0].classList.contains('vse-low')"));
        Assert.True(page.Is($"{strip}.children[1].classList.contains('vse-mid')"));
        Assert.Equal("90.00%", page.Text($"{strip}.children[2].children[0].style.width"));
        Assert.True(page.Is("root.document.getElementById('vs-explore-style')"));

        page.Run("root.frame.style.display = 'none';");
        page.Apply(2, Readings.Data(hud: new HudOptions(Explore: HudChoices.CornerBottomLeft)));

        Assert.False(page.Is(strip));
    }

    [Fact]
    public void With_the_explore_bars_off_nothing_is_drawn_on_the_travel_map()
    {
        var page = PageScript.On();

        page.Run("var root = buildTop(true);");
        page.Apply(0, Readings.Data(hud: new HudOptions(Explore: HudChoices.CornerOff)));

        Assert.False(page.Is("root.document.getElementById('vs-explore')"));
    }

    // ===== Off =====

    [Fact]
    public void With_no_player_the_HUD_marks_the_order_and_the_explore_bars_are_taken_away()
    {
        var page = PageScript.On("{values:[1,0.1,0.5,0.5], buffs:[[10,20]]}");
        var buffs = new[] { new BuffTime(10, 100, 100), new BuffTime(20, 100, 100) };
        var layout = HudLayout.Parse("10,20,150;0,0,100;0,0,100;0,0,100;0,0,100;0,0,100");

        page.Run("var root = buildTop(true);");
        page.Apply(0, Readings.Data(buffs: buffs, hud: new HudOptions(HideFull: true, WarnAt: 20, Favourites: new[] { 20 }, Layout: layout, Explore: HudChoices.CornerTopRight),
            reading: new HudReading(CanAct: new[] { true, true, true, true }, Tips: new[] { "a", "b", "c", "d" })));
        page.Apply(2, PageData.NoPlayer);

        Assert.False(page.Is($"{Satiety}.hasAttribute('data-vs-hide')"));
        Assert.False(page.Is($"{Satiety}.hasAttribute('data-vs-moved')"));
        Assert.Equal("", page.Text($"{Satiety}.style.transform || ''"));
        Assert.False(page.Is($"{Bar(Morale)}.hasAttribute('data-vs-warn')"));
        Assert.False(page.Is($"{Bar(Satiety)}.hasAttribute('data-vs-act')"));
        Assert.False(page.Is($"{Badge(Satiety, 1)}.hasAttribute('data-vs-fav')"));
        Assert.Equal("", page.Text($"{Badge(Satiety, 1)}.style.order || ''"));
        Assert.False(page.Is("root.document.getElementById('vs-explore')"));
    }

    // ===== Data =====

    [Fact]
    public void The_reading_carries_no_hud_object_when_the_plugin_sends_none()
    {
        Assert.DoesNotContain("hud:", Readings.Data());
    }

    [Fact]
    public void Settings_that_are_off_are_sent_as_null_so_the_page_draws_nothing_for_them()
    {
        var data = Readings.Data(hud: new HudOptions(HideFull: false, WarnPulse: false, Explore: HudChoices.CornerOff, Projection: false, ClickBars: false),
            reading: new HudReading(Projected: new double?[] { 0.5, 0.5, 0.5, 0.5 }, CanAct: new[] { true, true, true, true }));

        Assert.Contains("hud:{hide:null,warn:0,", data);
        Assert.Contains(",exp:null,proj:null,", data);
        Assert.Contains(",act:null}", data);
    }

    [Fact]
    public void Numbers_out_of_range_are_held_to_their_limits_before_the_page_sees_them()
    {
        var data = Readings.Data(hud: new HudOptions(HideFull: true, HideAbove: 500, WarnAt: 140));

        Assert.Contains($"hide:[{HudChoices.MaxHideAbove},1]", data);
        Assert.Contains("warn:100,", data);
    }
}
