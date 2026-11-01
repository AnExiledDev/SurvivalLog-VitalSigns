using VitalSigns.Core;
using Xunit;

namespace VitalSigns.Tests;

public class GainSourcesTests
{
    [Fact]
    public void Two_copies_of_a_buff_are_one_line_and_the_biggest_change_comes_first()
    {
        var sources = GainSources.Of(new[] { (0, "Cold", -0.1), (0, "Quick Jog", 0.2), (0, "Quick Jog", 0.2), (2, "Tired", -0.3) }, 4);

        Assert.Equal(new[] { new GainSource("Quick Jog", 0.4), new GainSource("Cold", -0.1) }, sources[0]);
        Assert.Empty(sources[1]);
        Assert.Equal(new[] { new GainSource("Tired", -0.3) }, sources[2]);
    }

    [Fact]
    public void A_share_that_rounds_to_no_change_and_a_stat_out_of_range_are_left_out()
    {
        var sources = GainSources.Of(new[] { (0, "Tiny", 0.004), (0, "Even", 0.2), (0, "Even", -0.2), (4, "Nowhere", 0.5), (-1, "Nowhere", 0.5) }, 4);

        Assert.All(sources, Assert.Empty);
    }
}
