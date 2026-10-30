using VitalSigns.Core;
using Xunit;

namespace VitalSigns.Tests;

public class BarSizeTests
{
    [Fact]
    public void The_default_is_eighty_percent_of_the_game_size()
    {
        Assert.Equal(80, BarSize.Default);
        Assert.Equal(0.8, BarSize.Zoom(BarSize.Default));
        Assert.Equal(BarSize.Default, new Options().BarSize);
    }

    [Fact]
    public void Buffs_default_to_the_same_size_as_the_bars()
    {
        Assert.Equal(80, new Options().BuffSize);
        Assert.Equal(new Options().BarSize, new Options().BuffSize);
    }

    /// <summary>The hover boxes keep the game's own size by default: at the 80% bar size their text was too small.</summary>
    [Fact]
    public void Hover_boxes_default_to_the_game_size_whatever_the_bar_size()
    {
        Assert.Equal(100, new Options().TooltipSize);
        Assert.Equal(100, new Options(BarSize: 60).TooltipSize);
    }

    [Fact]
    public void A_size_inside_the_range_is_used_as_it_is()
    {
        Assert.Equal(0.5, BarSize.Zoom(50));
        Assert.Equal(1.0, BarSize.Zoom(100));
        Assert.Equal(1.5, BarSize.Zoom(150));
    }

    [Fact]
    public void A_size_outside_the_range_is_held_to_its_nearest_end()
    {
        Assert.Equal(0.5, BarSize.Zoom(30));
        Assert.Equal(0.5, BarSize.Zoom(-5));
        Assert.Equal(1.5, BarSize.Zoom(200));
    }
}
