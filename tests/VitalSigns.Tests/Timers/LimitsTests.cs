using VitalSigns.Timers.Core;
using Xunit;

namespace VitalSigns.Tests.Timers;

public class LimitsTests
{
    [Theory]
    [InlineData(1, 1, false)]
    [InlineData(0.05, 0.1, true)]
    [InlineData(250, 100, true)]
    [InlineData(-3, 0.1, true)]
    [InlineData(double.PositiveInfinity, 100, true)]
    public void A_global_multiplier_is_held_to_the_range(double value, double expected, bool changed)
    {
        Assert.Equal(expected, Limits.Global(value, out var wasChanged));
        Assert.Equal(changed, wasChanged);
    }

    [Fact]
    public void A_global_multiplier_that_is_not_a_number_reads_as_the_games_time()
    {
        Assert.Equal(1, Limits.Global(double.NaN, out var wasChanged));
        Assert.True(wasChanged);
    }

    [Fact]
    public void Zero_on_a_category_or_buff_means_use_the_one_above_and_is_no_change()
    {
        Assert.Null(Limits.Override(0, out var wasChanged));
        Assert.False(wasChanged);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    public void A_negative_or_broken_override_is_dropped_and_reported(double value)
    {
        Assert.Null(Limits.Override(value, out var wasChanged));
        Assert.True(wasChanged);
    }

    [Theory]
    [InlineData(0.01, 0.1)]
    [InlineData(1000, 100)]
    [InlineData(2.5, 2.5)]
    public void An_override_is_held_to_the_range(double value, double expected)
    {
        Assert.Equal(expected, Limits.Override(value, out _));
    }

    [Theory]
    [InlineData(false, 3600f, true)]
    [InlineData(true, 3600f, false)]
    [InlineData(false, 0f, false)]
    [InlineData(false, -1f, false)]
    [InlineData(false, float.PositiveInfinity, false)]
    public void Only_visible_buffs_with_a_timer_are_touched(bool isHidden, float seconds, bool expected)
    {
        Assert.Equal(expected, Scope.IsTimedVisible(isHidden, seconds));
    }

    [Fact]
    public void The_longest_buff_stays_far_below_the_games_int_seconds()
    {
        // The game keeps the end time as int seconds; base and bonus at their caps, from a late game clock, still fit.
        var lateClock = 50.0 * 365 * Limits.SecondsPerDay;

        Assert.True(lateClock + 2 * Limits.MaxPartSeconds < int.MaxValue);
    }
}
