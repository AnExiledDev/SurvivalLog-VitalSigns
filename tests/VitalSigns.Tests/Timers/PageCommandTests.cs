using VitalSigns.Timers.Core;
using Xunit;

namespace VitalSigns.Tests.Timers;

public class PageCommandTests
{
    [Fact]
    public void Only_messages_with_our_prefix_are_ours()
    {
        Assert.True(PageCommand.IsOurs("slmod|vitalsigns|timers|load"));
        Assert.False(PageCommand.IsOurs("slmod|newloopplus|load"));
        Assert.False(PageCommand.IsOurs(null));
    }

    [Fact]
    public void Each_verb_parses_to_its_command()
    {
        Assert.IsType<PageCommand.Load>(PageCommand.Parse("slmod|vitalsigns|timers|load"));
        Assert.IsType<PageCommand.NoScript>(PageCommand.Parse("slmod|vitalsigns|timers|noscript"));
        Assert.Equal(new PageCommand.Save("{\"a\":\"x|y\"}"), PageCommand.Parse("slmod|vitalsigns|timers|save|{\"a\":\"x|y\"}"));
    }

    [Theory]
    [InlineData("slmod|vitalsigns|timers|delete")]
    [InlineData("slmod|vitalsigns|timers|")]
    [InlineData("hello")]
    public void Anything_else_is_unknown(string message)
    {
        Assert.Equal(new PageCommand.Unknown(message), PageCommand.Parse(message));
    }
}
