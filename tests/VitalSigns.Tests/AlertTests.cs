using VitalSigns.Core;
using Xunit;

namespace VitalSigns.Tests;

/// <summary>The warnings must come once per drop, never on loading a save and never over and over while a stat sits on its line.</summary>
public class AlertTests
{
    private static readonly AlertOptions All = new();

    private static StatLevel[] Levels(double satiety = 80, double morale = 80, double stamina = 80, double life = 80) =>
        new[] { new StatLevel(satiety, 100), new StatLevel(morale, 100), new StatLevel(stamina, 100), new StatLevel(life, 100) };

    private static FitnessReading Fit(int value) => new(value, 100);

    [Fact]
    public void The_first_reading_only_notes_where_the_stats_are()
    {
        var watch = new AlertWatch();

        Assert.Empty(watch.Step(Levels(satiety: 10), Fit(20), All));
        Assert.Empty(watch.Step(Levels(satiety: 9), Fit(19), All));
    }

    [Fact]
    public void A_stat_that_drops_to_its_line_warns_once()
    {
        var watch = new AlertWatch();

        watch.Step(Levels(), Fit(80), All);

        var alerts = watch.Step(Levels(stamina: 20), Fit(80), All);

        Assert.Equal(new[] { new Alert(AlertStat.Stamina, AlertLevel.Low, 20, 100) }, alerts);
        Assert.Empty(watch.Step(Levels(stamina: 12), Fit(80), All));
    }

    [Fact]
    public void Just_above_the_line_does_not_warn()
    {
        var watch = new AlertWatch();

        watch.Step(Levels(), Fit(80), All);

        Assert.Empty(watch.Step(Levels(morale: 20.5), Fit(80), All));
    }

    [Fact]
    public void A_stat_wobbling_on_its_line_warns_again_only_after_climbing_5_back_over_it()
    {
        var watch = new AlertWatch();

        watch.Step(Levels(), Fit(80), All);
        Assert.Single(watch.Step(Levels(satiety: 19), Fit(80), All));

        Assert.Empty(watch.Step(Levels(satiety: 24), Fit(80), All));
        Assert.Empty(watch.Step(Levels(satiety: 19), Fit(80), All));

        Assert.Empty(watch.Step(Levels(satiety: 25.5), Fit(80), All));
        Assert.Single(watch.Step(Levels(satiety: 19), Fit(80), All));
    }

    [Fact]
    public void The_line_is_a_percent_of_the_stat_s_own_maximum()
    {
        var watch = new AlertWatch();
        var levels = new[] { new StatLevel(150, 150), new StatLevel(80, 100), new StatLevel(80, 100), new StatLevel(80, 100) };

        watch.Step(levels, null, All);
        levels[0] = new StatLevel(31, 150);
        Assert.Empty(watch.Step(levels, null, All));

        levels[0] = new StatLevel(30, 150);
        Assert.Equal(new[] { new Alert(AlertStat.Satiety, AlertLevel.Low, 30, 150) }, watch.Step(levels, null, All));
    }

    [Fact]
    public void LowAt_moves_the_line()
    {
        var watch = new AlertWatch();
        var options = new AlertOptions(LowAt: 50);

        watch.Step(Levels(), Fit(80), options);

        Assert.Single(watch.Step(Levels(life: 50), Fit(80), options));
    }

    [Fact]
    public void A_stat_turned_off_never_warns_and_the_others_still_do()
    {
        var watch = new AlertWatch();
        var options = new AlertOptions(Morale: false);

        watch.Step(Levels(), Fit(80), options);

        var alerts = watch.Step(Levels(morale: 5, life: 5), Fit(80), options);

        Assert.Equal(new[] { new Alert(AlertStat.Life, AlertLevel.Low, 5, 100) }, alerts);
    }

    [Fact]
    public void Fitness_warns_below_40_and_again_below_30()
    {
        var watch = new AlertWatch();

        watch.Step(Levels(), Fit(45), All);

        Assert.Empty(watch.Step(Levels(), Fit(40), All));
        Assert.Equal(new[] { new Alert(AlertStat.Fitness, AlertLevel.NearWeakened, 39, 100) }, watch.Step(Levels(), Fit(39), All));
        Assert.Empty(watch.Step(Levels(), Fit(30), All));
        Assert.Equal(new[] { new Alert(AlertStat.Fitness, AlertLevel.Weakened, 29, 100) }, watch.Step(Levels(), Fit(29), All));
        Assert.Empty(watch.Step(Levels(), Fit(25), All));
    }

    [Fact]
    public void Fitness_falling_straight_past_both_lines_warns_Weakened_alone()
    {
        var watch = new AlertWatch();

        watch.Step(Levels(), Fit(41), All);

        Assert.Equal(new[] { new Alert(AlertStat.Fitness, AlertLevel.Weakened, 28, 100) }, watch.Step(Levels(), Fit(28), All));
    }

    [Fact]
    public void Fitness_warns_Weakened_again_only_after_climbing_2_back_over_30()
    {
        var watch = new AlertWatch();

        watch.Step(Levels(), Fit(35), All);
        Assert.Single(watch.Step(Levels(), Fit(29), All));

        Assert.Empty(watch.Step(Levels(), Fit(31), All));
        Assert.Empty(watch.Step(Levels(), Fit(29), All));

        Assert.Empty(watch.Step(Levels(), Fit(32), All));
        Assert.Single(watch.Step(Levels(), Fit(29), All));
    }

    [Fact]
    public void With_the_Fitness_warning_off_Fitness_never_warns()
    {
        var watch = new AlertWatch();
        var options = new AlertOptions(Fitness: false);

        watch.Step(Levels(), Fit(50), options);

        Assert.Empty(watch.Step(Levels(), Fit(20), options));
    }

    [Fact]
    public void After_no_player_the_next_reading_only_notes_again()
    {
        var watch = new AlertWatch();

        watch.Step(Levels(), Fit(80), All);
        watch.Step(null, null, All);

        Assert.Empty(watch.Step(Levels(satiety: 5), Fit(20), All));
    }

    [Fact]
    public void A_reset_makes_the_next_reading_only_note()
    {
        var watch = new AlertWatch();

        watch.Step(Levels(), Fit(80), All);
        watch.Reset();

        Assert.Empty(watch.Step(Levels(satiety: 5), Fit(80), All));
    }

    [Fact]
    public void A_stat_with_no_maximum_never_warns()
    {
        var watch = new AlertWatch();
        var levels = new[] { new StatLevel(0, 0), new StatLevel(80, 100), new StatLevel(80, 100), new StatLevel(80, 100) };

        watch.Step(levels, null, All);

        Assert.Empty(watch.Step(levels, null, All));
    }

    // ===== Words =====

    [Fact]
    public void A_low_stat_names_the_stat_and_its_value()
    {
        var notice = AlertText.Of(new Alert(AlertStat.Satiety, AlertLevel.Low, 18, 100), isChinese: false);

        Assert.Equal(new AlertNotice("Satiety is low", "Satiety is down to 18/100.", false, "vitalsigns.alert.Satiety"), notice);
    }

    [Fact]
    public void Weakened_is_the_severe_one()
    {
        Assert.True(AlertText.Of(new Alert(AlertStat.Fitness, AlertLevel.Weakened, 29, 100), false).IsSevere);
        Assert.False(AlertText.Of(new Alert(AlertStat.Fitness, AlertLevel.NearWeakened, 39, 100), false).IsSevere);
        Assert.Equal("Fitness close to Weakened", AlertText.Of(new Alert(AlertStat.Fitness, AlertLevel.NearWeakened, 39, 100), false).Title);
    }

    [Fact]
    public void Each_stat_has_its_own_key_so_a_new_warning_replaces_its_old_one()
    {
        var life = AlertText.Of(new Alert(AlertStat.Life, AlertLevel.Low, 10, 100), false).Key;
        var near = AlertText.Of(new Alert(AlertStat.Fitness, AlertLevel.NearWeakened, 39, 100), false).Key;
        var weak = AlertText.Of(new Alert(AlertStat.Fitness, AlertLevel.Weakened, 29, 100), false).Key;

        Assert.NotEqual(life, near);
        Assert.Equal(near, weak);
    }

    [Fact]
    public void A_Chinese_game_gets_the_warnings_in_Chinese()
    {
        Assert.Equal(new AlertNotice("「体力」偏低", "「体力」只剩 15/100。", false, "vitalsigns.alert.Stamina"),
            AlertText.Of(new Alert(AlertStat.Stamina, AlertLevel.Low, 15, 100), isChinese: true));
        Assert.Equal("健康低于30", AlertText.Of(new Alert(AlertStat.Fitness, AlertLevel.Weakened, 29, 100), true).Title);
    }
}
