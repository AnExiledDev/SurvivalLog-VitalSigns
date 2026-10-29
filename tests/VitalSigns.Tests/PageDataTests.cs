using VitalSigns.Core;
using Xunit;

namespace VitalSigns.Tests;

public class PageDataTests
{
    [Fact]
    public void A_reading_carries_every_part_the_page_draws()
    {
        var data = Readings.Data(
            Readings.Stats(satiety: new StatRatios(0.25, -0.5, 0, 0.1), life: new StatRatios(0, 0, 1, 0)),
            new[] { new BuffTime(1300, 90000, 21600), new BuffTime(4, 95000, 0) },
            new CarryWeight(12345, 20000, 16000),
            new WellFedLine(80, 60, 100));

        Assert.Equal("{zh:false,stats:[[0.25,-0.5,0,0.1],[0,0,0,0],[0,0,0,0],[0,0,1,0]],buffs:[[1300,90000,21600],[4,95000,0]],"
            + "weight:[12345,20000,16000],wellFed:[80,60,100],names:{wellFed:\"Well Fed\",overweight:\"Overweight\"}}", data);
    }

    [Fact]
    public void With_no_player_the_reading_is_null()
    {
        Assert.Equal("null", PageData.Of(null, Readings.English, false));
    }

    [Fact]
    public void Parts_the_game_does_not_have_go_as_null()
    {
        var data = Readings.Data();

        Assert.Contains("weight:null,wellFed:null", data);
        Assert.Contains("buffs:[]", data);
    }

    /// <summary>Float noise in a ratio must not make an unchanged reading look new, or it is sent every check.</summary>
    [Fact]
    public void Ratios_are_rounded_so_noise_does_not_change_the_reading()
    {
        var a = Readings.Data(Readings.Stats(satiety: new StatRatios(0.30000001, 0, 0, 0)));
        var b = Readings.Data(Readings.Stats(satiety: new StatRatios(0.29999999, 0, 0, 0)));

        Assert.Equal(a, b);
        Assert.Contains("[0.3,0,0,0]", a);
    }

    [Fact]
    public void A_tiny_negative_ratio_is_written_as_zero_and_a_broken_one_as_zero()
    {
        var data = Readings.Data(Readings.Stats(satiety: new StatRatios(-0.00001, double.NaN, double.PositiveInfinity, 0)));

        Assert.Contains("stats:[[0,0,0,0]", data);
    }

    [Fact]
    public void Names_are_quoted_so_any_text_reaches_the_page_whole()
    {
        var data = Readings.Data(names: new GameNames("Well \"Fed\"", "超重"), isChinese: true);

        Assert.StartsWith("{zh:true,", data);
        Assert.Contains("names:{wellFed:\"Well \\\"Fed\\\"\",overweight:\"超重\"}", data);
    }
}
