using VitalSigns.Timers.Core;
using Xunit;

namespace VitalSigns.Tests.Timers;

/// <summary>The Timers tab showed Chinese names in an English game: the game had not yet filled its English table when
/// the list was read, so the English slot was empty and the page fell back to Chinese.</summary>
public class BuffTextTests
{
    [Fact]
    public void An_english_game_gets_english_text_before_the_english_table_is_filled()
    {
        var text = BuffText.Pick(isChinese: false, current: "Short Boost", english: null, chinese: "短期激励", local: "短期激励");

        Assert.Equal("Short Boost", text.En);
        Assert.Equal("短期激励", text.Zh);
    }

    [Fact]
    public void A_chinese_game_gets_chinese_text_before_the_chinese_table_is_filled()
    {
        var text = BuffText.Pick(isChinese: true, current: "短期激励", english: null, chinese: null, local: null);

        Assert.Equal("短期激励", text.Zh);
        Assert.Equal("", text.En);
    }

    [Fact]
    public void The_current_language_never_fills_the_other_languages_slot()
    {
        var text = BuffText.Pick(isChinese: false, current: "Short Boost", english: null, chinese: null, local: "短期激励");

        Assert.Equal("短期激励", text.Zh);
    }

    [Fact]
    public void Both_tables_filled_are_used_as_they_are()
    {
        var text = BuffText.Pick(isChinese: false, current: "Short Boost", english: "Short Boost", chinese: "短期激励", local: "x");

        Assert.Equal(("Short Boost", "短期激励"), text);
    }
}
