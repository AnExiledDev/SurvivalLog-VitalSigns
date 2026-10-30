using VitalSigns.Core;
using Xunit;

namespace VitalSigns.Tests;

public class HeldDataTests
{
    private const string Reading = "{zh:false}";
    private const string Other = "{zh:true}";

    [Fact]
    public void A_reading_the_page_took_is_not_sent_again()
    {
        var held = new HeldData();

        Assert.Equal(Reading, held.ToSend(Reading));
        held.Answered(PageAnswer.Ok);

        Assert.Null(held.ToSend(Reading));
    }

    [Fact]
    public void A_changed_reading_is_sent()
    {
        var held = new HeldData();

        held.ToSend(Reading);
        held.Answered(PageAnswer.Ok);

        Assert.Equal(Other, held.ToSend(Other));
    }

    /// <summary>The script still keeps the reading when it has nothing to draw it on, so these count as taken.</summary>
    [Theory]
    [InlineData(PageAnswer.NoBar)]
    [InlineData(PageAnswer.NoPlayer)]
    public void A_reading_answered_with_nothing_to_draw_on_counts_as_taken(string answer)
    {
        var held = new HeldData();

        held.ToSend(Reading);
        held.Answered(answer);

        Assert.Null(held.ToSend(Reading));
    }

    [Theory]
    [InlineData(PageAnswer.Missing)]
    [InlineData(PageAnswer.NoPage)]
    [InlineData(PageAnswer.NoWebView)]
    [InlineData(PageAnswer.NoAnswer)]
    [InlineData("run failed: boom")]
    [InlineData("error: TypeError")]
    public void Any_other_answer_means_the_page_may_hold_nothing(string answer)
    {
        var held = new HeldData();

        held.ToSend(Reading);
        held.Answered(PageAnswer.Ok);
        held.ToSend(Other);
        held.Answered(answer);

        Assert.Equal(Reading, held.ToSend(Reading));
    }

    /// <summary>A call given up on may or may not have reached the page, so the page may hold either reading.</summary>
    [Fact]
    public void A_call_never_answered_leaves_nothing_known_held()
    {
        var held = new HeldData();

        held.ToSend(Reading);
        held.Answered(PageAnswer.Ok);
        held.ToSend(Other);

        Assert.Equal(Reading, held.ToSend(Reading));
    }

    [Fact]
    public void An_install_always_carries_the_reading()
    {
        var held = new HeldData();

        held.ToSend(Reading);
        held.Answered(PageAnswer.Ok);

        Assert.Equal(Reading, held.ToInstall(Reading));
    }

    [Fact]
    public void The_page_holds_a_reading_only_once_it_answered_for_it()
    {
        var held = new HeldData();

        held.ToSend(Reading);
        Assert.False(held.Holds(Reading));

        held.Answered(PageAnswer.Ok);
        Assert.True(held.Holds(Reading));
        Assert.False(held.Holds(Other));
    }
}
