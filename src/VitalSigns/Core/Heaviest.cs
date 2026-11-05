using System.Collections.Generic;

namespace VitalSigns.Core;

/// <summary>One kind of item in the bag, for the Inventory button's hover box: its name, the weight of every stack of it in grams, and how many there are.</summary>
public readonly record struct HeavyItem(string Name, int Grams, int Count);

/// <summary>A stack in the bag as the game keeps it: the item id, the stack's weight in grams and its count.</summary>
public readonly record struct BagStack(int Id, int Grams, int Count);

/// <summary>The heaviest kinds of item in the bag: stacks of one item added together, heaviest first.</summary>
public static class Heaviest
{
    public const int Default = 5;
    public const int Min = 0;
    public const int Max = 10;

    /// <summary>At most <paramref name="most"/> kinds, heaviest first; ties go by item id so the list does not shuffle. Weightless items are left out.</summary>
    public static List<BagStack> Of(IReadOnlyList<BagStack> stacks, int most)
    {
        var kinds = new List<BagStack>();

        if (most <= 0) return kinds;

        foreach (var stack in stacks)
        {
            var at = kinds.FindIndex(kind => kind.Id == stack.Id);

            if (at < 0) kinds.Add(stack);
            else kinds[at] = new BagStack(stack.Id, kinds[at].Grams + stack.Grams, kinds[at].Count + stack.Count);
        }

        kinds.RemoveAll(kind => kind.Grams <= 0);
        kinds.Sort((a, b) => a.Grams != b.Grams ? b.Grams.CompareTo(a.Grams) : a.Id.CompareTo(b.Id));

        if (kinds.Count > most) kinds.RemoveRange(most, kinds.Count - most);

        return kinds;
    }
}
