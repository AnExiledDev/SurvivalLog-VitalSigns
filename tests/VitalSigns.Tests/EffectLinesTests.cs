using VitalSigns.Core;
using Xunit;

namespace VitalSigns.Tests;

public class EffectLinesTests
{
    [Fact]
    public void Lines_come_in_a_fixed_order_and_leave_out_zeros()
    {
        var food = new double[BuffRule.FoodTypes];

        food[2] = 0.25;

        var rule = Readings.Rule(1, interval: 3600, every: new double[] { 0, 0, 0, -2 }, atStart: new double[] { 0, 5, 0, 0 }, atEnd: new double[] { -1, 0, 0, 0 },
            max: new double[] { 0, 0, -10, 0 }, gain: new double[] { 0.5, 0, 0, 0 }, loss: new double[] { 0, 0, 0, 0.2 }, speed: -0.1, moraleFood: food);

        Assert.Equal(new[]
        {
            new EffectLine(EffectKind.Every, 3, -2), new EffectLine(EffectKind.Start, 1, 5), new EffectLine(EffectKind.End, 0, -1), new EffectLine(EffectKind.Max, 2, -10),
            new EffectLine(EffectKind.Gain, 0, 0.5), new EffectLine(EffectKind.Loss, 3, 0.2), new EffectLine(EffectKind.Speed, 0, -0.1), new EffectLine(EffectKind.MoraleFood, 3, 0.25)
        }, EffectLines.Of(rule, 1));
    }

    [Fact]
    public void Without_an_interval_there_is_no_every_line()
    {
        var rule = Readings.Rule(1, every: new double[] { 0, 0, 0, -2 });

        Assert.Empty(EffectLines.Of(rule, 1));
    }

    [Fact]
    public void Cold_resistance_cuts_the_every_line_of_a_cold_debuff_only()
    {
        var rule = Readings.Rule(1, interval: 3600, every: new double[] { 0, 0, 0, -2 }, atEnd: new double[] { 0, 0, 0, -4 }, isCold: true);

        Assert.Equal(new[] { new EffectLine(EffectKind.Every, 3, -1), new EffectLine(EffectKind.End, 3, -4) }, EffectLines.Of(rule, 0.5));
    }
}
