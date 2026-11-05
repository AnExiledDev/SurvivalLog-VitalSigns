using VitalSigns.Core;
using Xunit;

namespace VitalSigns.Tests;

public class HeaviestTests
{
    [Fact]
    public void Stacks_of_one_item_are_added_together_and_listed_heaviest_first()
    {
        var bag = new[] { new BagStack(10, 300, 3), new BagStack(20, 1000, 1), new BagStack(10, 900, 9), new BagStack(30, 50, 5) };

        var heaviest = Heaviest.Of(bag, 5);

        Assert.Equal(new[] { new BagStack(10, 1200, 12), new BagStack(20, 1000, 1), new BagStack(30, 50, 5) }, heaviest);
    }

    [Fact]
    public void Only_the_set_number_of_kinds_is_kept()
    {
        var bag = new[] { new BagStack(1, 100, 1), new BagStack(2, 400, 1), new BagStack(3, 300, 1), new BagStack(4, 200, 1) };

        Assert.Equal(new[] { 2, 3 }, Heaviest.Of(bag, 2).ConvertAll(kind => kind.Id));
    }

    /// <summary>The list is rebuilt whenever the bag changes; equal weights must keep their places or the box reshuffles.</summary>
    [Fact]
    public void Equal_weights_go_by_item_id_whatever_order_the_bag_has()
    {
        var bag = new[] { new BagStack(9, 500, 1), new BagStack(3, 500, 1), new BagStack(5, 500, 1) };

        Assert.Equal(new[] { 3, 5, 9 }, Heaviest.Of(bag, 5).ConvertAll(kind => kind.Id));
    }

    [Fact]
    public void Weightless_items_are_left_out()
    {
        var bag = new[] { new BagStack(1, 0, 4), new BagStack(2, 10, 1) };

        Assert.Equal(new[] { 2 }, Heaviest.Of(bag, 5).ConvertAll(kind => kind.Id));
    }

    [Fact]
    public void Zero_lists_nothing()
    {
        Assert.Empty(Heaviest.Of(new[] { new BagStack(1, 100, 1) }, 0));
    }
}
