using System.Collections.Generic;

namespace VitalSigns.Core.Acts;

/// <summary>
/// Turns a scan's base, which holds what the tables say, into what a click would get: every item and action scaled by
/// the player's modifiers, and the item uses already waiting in the queue taken off the counts, the bag's first since
/// the queue uses those first. An item with nothing left is dropped.
/// </summary>
public static class ViewScale
{
    /// <param name="foodOf">Each item kind's food type; a kind not in it has none.</param>
    /// <param name="reserved">Uses waiting in the queue per item kind.</param>
    public static BaseView Of(BaseView raw, StatScale scale, IReadOnlyDictionary<int, int> foodOf, IReadOnlyDictionary<int, int> reserved)
    {
        var items = new List<ActOption>(raw.Items.Count);

        foreach (var item in raw.Items)
        {
            reserved.TryGetValue(item.Id, out var waiting);

            var count = item.Count - waiting;

            if (count <= 0) continue;

            var inBag = item.InBag - System.Math.Min(waiting, item.InBag);

            items.Add(item with { Delta = scale.Item(item.Delta, foodOf.TryGetValue(item.Id, out var food) ? food : 0), Count = count, InBag = inBag });
        }

        var funcs = new List<ActOption>(raw.Funcs.Count);

        foreach (var func in raw.Funcs) funcs.Add(func with { Delta = scale.Action(func.Delta) });

        var beds = new List<Bed>(raw.Beds.Count);

        foreach (var bed in raw.Beds)
        {
            beds.Add(bed with { Sleep = Scaled(bed.Sleep, scale), Nap = Scaled(bed.Nap, scale), Beauty = Scaled(bed.Beauty, scale) });
        }

        return new BaseView(items, funcs, beds, raw.AtHome, raw.Version);
    }

    private static ActOption? Scaled(ActOption? option, StatScale scale) => option == null ? null : option with { Delta = scale.Action(option.Delta) };
}
