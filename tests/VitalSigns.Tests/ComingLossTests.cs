using VitalSigns.Core;
using Xunit;

namespace VitalSigns.Tests;

public class ComingLossTests
{
    private const int Life = 3;

    private static readonly double[] NoRatio = { 0, 0, 0, 0 };

    private static readonly BuffRule Bleed = Readings.Rule(1, "Bleed", interval: 3600, every: new double[] { 0, 0, 0, -2 }, atEnd: new double[] { 0, 0, 0, -1 });

    private static IReadOnlyList<IReadOnlyList<LossLine>> Of(int now, IEnumerable<BuffTime> running, IEnumerable<BuffRule> rules, double cold = 1, double[]? ratio = null) =>
        ComingLoss.Of(now, running.ToList(), rules.ToDictionary(r => r.Id), cold, ratio ?? NoRatio);

    [Fact]
    public void A_buff_loses_at_its_end_and_every_interval_before_it_plus_its_end_loss()
    {
        var lines = Of(0, new[] { new BuffTime(1, 7200, 7200) }, new[] { Bleed });

        Assert.Equal(new[] { new LossLine(1, 5, 7200) }, lines[Life]);
        Assert.Empty(lines[0]);
    }

    /// <summary>Only whole intervals still ahead count: 5000 seconds left at one per hour is one more tick, not 1.4.</summary>
    [Fact]
    public void A_part_interval_left_is_not_a_tick()
    {
        var lines = Of(0, new[] { new BuffTime(1, 5000, 7200) }, new[] { Bleed });

        Assert.Equal(3, lines[Life][0].Amount, 6);
    }

    [Fact]
    public void Layers_multiply_every_loss()
    {
        var lines = Of(0, new[] { new BuffTime(1, 7200, 7200, 3) }, new[] { Bleed });

        Assert.Equal(15, lines[Life][0].Amount, 6);
    }

    [Fact]
    public void Cold_resistance_cuts_only_the_interval_losses_of_a_cold_debuff()
    {
        var cold = Bleed with { IsCold = true };

        Assert.Equal(3, Of(0, new[] { new BuffTime(1, 7200, 7200) }, new[] { cold }, cold: 0.5)[Life][0].Amount, 6);
        Assert.Equal(5, Of(0, new[] { new BuffTime(1, 7200, 7200) }, new[] { Bleed }, cold: 0.5)[Life][0].Amount, 6);
    }

    [Fact]
    public void Each_loss_is_scaled_by_the_loss_ratio_of_the_moment()
    {
        var lines = Of(0, new[] { new BuffTime(1, 7200, 7200) }, new[] { Bleed }, ratio: new double[] { 0, 0, 0, 0.5 });

        Assert.Equal(7.5, lines[Life][0].Amount, 6);
    }

    /// <summary>A buff raising the loss ratio scales the losses that fall while it runs, and stops once it has ended.</summary>
    [Fact]
    public void A_loss_ratio_from_another_buff_stops_counting_once_that_buff_ends()
    {
        var frail = Readings.Rule(2, "Frail", loss: new double[] { 0, 0, 0, 0.5 });
        var running = new[] { new BuffTime(1, 7200, 7200), new BuffTime(2, 5000, 5000) };
        var lines = Of(0, running, new[] { Bleed, frail }, ratio: new double[] { 0, 0, 0, 0.5 });

        // Tick at 3600 while Frail runs: 2 x 1.5. Tick at 7200 and the end loss after it ended: 2 + 1.
        Assert.Equal(6, lines[Life][0].Amount, 6);
    }

    [Fact]
    public void Suppressed_untimed_ended_and_unknown_buffs_and_gains_are_left_out()
    {
        var heal = Readings.Rule(3, "Heal", interval: 3600, every: new double[] { 0, 0, 0, 2 });
        var running = new[]
        {
            new BuffTime(1, 7200, 7200, 1, true), new BuffTime(1, -1, 0), new BuffTime(1, 0, 7200), new BuffTime(9, 7200, 7200), new BuffTime(3, 7200, 7200)
        };

        var lines = Of(0, running, new[] { Bleed, heal });

        Assert.All(lines, Assert.Empty);
    }
}
