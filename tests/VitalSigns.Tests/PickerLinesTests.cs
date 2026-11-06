using VitalSigns.Core;
using VitalSigns.Core.Acts;
using Xunit;

namespace VitalSigns.Tests;

/// <summary>The lines the Mod Manager's pickers read for this mod's lists: one entry per line, eight parts split by "|".</summary>
public class PickerLinesTests
{
    [Fact]
    public void Each_entry_is_one_line_and_a_bar_or_line_break_in_a_name_cannot_split_it()
    {
        var text = PickerLines.Of(new[]
        {
            new PickerEntry("i12", "Beans | tinned", "罐头\n豆", "Res/beans.png", "Items", "物品", "Satiety +20", "饱腹 +20"),
            new PickerEntry("40", "Hungry", "饥饿"),
        });

        Assert.Equal("i12|Beans / tinned|罐头 豆|Res/beans.png|Items|物品|Satiety +20|饱腹 +20||\n40|Hungry|饥饿|||||||", text);
    }

    [Fact]
    public void A_note_names_only_the_stats_that_change_with_their_sign()
    {
        var (en, zh) = PickerLines.Delta(new StatDelta(20, -5.5, 0, 0.01));

        Assert.Equal("Satiety +20, Morale -5.5", en);
        Assert.Equal("饱腹 +20，心情 -5.5", zh);
    }

    [Theory]
    [InlineData(100000, "1 d 3 h", "1天 3小时")]
    [InlineData(1800, "30 m", "30分钟")]
    [InlineData(20, "0 m", "0分钟")]
    public void A_buffs_time_shows_at_most_two_parts(double seconds, string en, string zh)
    {
        Assert.Equal((en, zh), PickerLines.Duration(seconds));
    }
}
