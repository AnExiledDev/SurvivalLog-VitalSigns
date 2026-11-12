using VitalSigns.Core.Acts;
using Xunit;

namespace VitalSigns.Tests;

public class RefusedClicksTests
{
    [Fact]
    public void A_refused_click_logs_the_bar_the_reason_and_the_base_it_planned_from()
    {
        var line = new RefusedClicks().Line(0, ActWhy.NoFood, 12, 3, 1);

        Assert.Equal("click on Satiety: nothing to queue (NoFood), planned from 12 items, 3 furniture actions, 1 beds", line);
    }

    [Fact]
    public void Clicking_the_same_bar_again_for_the_same_reason_logs_nothing()
    {
        var refused = new RefusedClicks();

        refused.Line(2, ActWhy.NoBed, 5, 2, 0);

        Assert.Null(refused.Line(2, ActWhy.NoBed, 5, 2, 0));
    }

    [Fact]
    public void A_new_reason_another_bar_or_a_changed_base_logs_again()
    {
        var refused = new RefusedClicks();

        refused.Line(2, ActWhy.NoBed, 5, 2, 0);

        Assert.NotNull(refused.Line(2, ActWhy.BedNotReady, 5, 2, 0));
        Assert.NotNull(refused.Line(1, ActWhy.NoBed, 5, 2, 0));
        Assert.NotNull(refused.Line(2, ActWhy.NoBed, 6, 2, 0));
        Assert.NotNull(refused.Line(2, ActWhy.NoBed, 5, 3, 0));
        Assert.NotNull(refused.Line(2, ActWhy.NoBed, 5, 2, 1));
    }

    [Fact]
    public void After_a_clear_the_same_click_logs_again()
    {
        var refused = new RefusedClicks();

        refused.Line(3, ActWhy.NoLifeItems, 0, 0, 0);
        refused.Clear();

        Assert.NotNull(refused.Line(3, ActWhy.NoLifeItems, 0, 0, 0));
    }
}
