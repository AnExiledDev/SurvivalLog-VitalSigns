using VitalSigns.Timers.Core;
using Xunit;

namespace VitalSigns.Tests.Timers;

public class ScalingTests
{
    private const float Hour = 3600f;

    [Theory]
    [InlineData(0f, 2, 2 * Hour)]
    [InlineData(0.5f, 2, 3 * Hour)]
    [InlineData(0f, 0.5, 0.5 * Hour)]
    [InlineData(-0.25f, 4, 3 * Hour)]
    public void The_ratio_makes_the_config_path_come_out_multiplied(float talentRatio, double multiplier, double expectedSeconds)
    {
        var ratio = Scaling.Ratio(talentRatio, multiplier, Hour);

        Assert.Equal(expectedSeconds, Hour * (1 + (double)ratio), 1);
    }

    [Fact]
    public void A_multiplier_of_one_leaves_the_ratio_alone()
    {
        Assert.Equal(0.3f, Scaling.Ratio(0.3f, 1, Hour));
    }

    [Theory]
    [InlineData(-1f)]
    [InlineData(-2f)]
    [InlineData(float.NaN)]
    public void A_ratio_that_already_zeroes_or_breaks_the_time_is_left_to_the_game(float talentRatio)
    {
        Assert.Equal(talentRatio, Scaling.Ratio(talentRatio, 5, Hour));
    }

    [Fact]
    public void The_ratio_never_takes_a_long_buff_past_the_cap()
    {
        var baseSeconds = 30f * 86400f;
        var ratio = Scaling.Ratio(1f, 100, baseSeconds);

        Assert.True(baseSeconds * (1 + (double)ratio) <= Limits.MaxPartSeconds * 1.0001);
    }

    [Fact]
    public void The_bonus_is_multiplied_and_capped_both_ways()
    {
        Assert.Equal(1200f, Scaling.Bonus(600f, 2));
        Assert.Equal(-1200f, Scaling.Bonus(-600f, 2));
        Assert.Equal((float)Limits.MaxPartSeconds, Scaling.Bonus(float.MaxValue / 1000, 100));
        Assert.Equal((float)-Limits.MaxPartSeconds, Scaling.Bonus(-float.MaxValue / 1000, 100));
        Assert.Equal(600f, Scaling.Bonus(600f, 1));
    }

    [Theory]
    [InlineData(-1f)]
    [InlineData(0f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void A_passed_time_that_is_not_a_real_time_is_never_touched(float seconds)
    {
        Assert.Equal(seconds, Scaling.Passed(seconds, 3));
    }

    [Fact]
    public void A_passed_time_is_multiplied_and_capped()
    {
        Assert.Equal(2 * Hour, Scaling.Passed(Hour, 2));
        Assert.Equal((float)Limits.MaxPartSeconds, Scaling.Passed(400f * 86400f, 100));
    }

    [Fact]
    public void A_running_buff_keeps_its_share_of_the_new_time()
    {
        // Half an hour left of a buff at 1x, moved to 3x: one and a half hours left.
        Assert.Equal(5400, Scaling.Remaining(1800, 1, 3)!.Value, 6);
        Assert.Equal(900, Scaling.Remaining(1800, 2, 1)!.Value, 6);
    }

    [Theory]
    [InlineData(0, 1, 2)]
    [InlineData(-5, 1, 2)]
    [InlineData(double.NaN, 1, 2)]
    [InlineData(100, 0, 2)]
    [InlineData(100, 2, 0)]
    [InlineData(100, 2, 2)]
    public void A_running_buff_with_nothing_to_change_is_left_alone(double remaining, double from, double to)
    {
        Assert.Null(Scaling.Remaining(remaining, from, to));
    }

    [Fact]
    public void A_running_buff_is_never_moved_past_both_caps_together()
    {
        Assert.Equal(2 * Limits.MaxPartSeconds, Scaling.Remaining(Limits.MaxPartSeconds, 0.1, 100));
    }

    [Fact]
    public void Total_is_base_after_talents_plus_bonus()
    {
        Assert.Equal(3 * Hour + 60, Scaling.Total(Hour, 2, 60), 3);
    }
}
