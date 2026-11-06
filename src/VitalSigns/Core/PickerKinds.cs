using System.Collections.Generic;
using System.Linq;
using VitalSigns.Core.Acts;

namespace VitalSigns.Core;

/// <summary>One item a picker lists: its entry, its shelf, whether a cooking recipe makes it, and how many the player has anywhere.</summary>
public sealed record ItemPick(PickerEntry Entry, int Shelf, bool IsDish, int Held);

/// <summary>
/// The item lists the pickers offer: first one "Any ..." entry per kind of item in the list (each food kind, Meds,
/// Books, cooked dishes), a group key that stands for every item of that kind the player has; then the items, each
/// marked with how many the player has, the ones they have first on each shelf.
/// </summary>
public static class PickerKinds
{
    public static readonly (string En, string Zh) Group = ("Any of a kind", "任意一类");
    public static readonly (string En, string Zh) DishesName = ("Cooked Dish", "料理");

    /// <param name="items">The items in the list, in their shelves' order.</param>
    /// <param name="shelfNames">Each shelf's name, by its rank.</param>
    /// <param name="note">What an "Any ..." entry does in this list.</param>
    public static List<PickerEntry> Of(IReadOnlyList<ItemPick> items, IReadOnlyDictionary<int, (string En, string Zh)> shelfNames, (string En, string Zh) note)
    {
        var entries = new List<PickerEntry>();
        var shelves = items.Select(item => item.Shelf).Distinct().ToList();

        if (items.Any(item => item.IsDish)) shelves.Insert(0, PickerGroups.Dishes);

        foreach (var shelf in shelves)
        {
            var kind = items.Where(item => shelf == PickerGroups.Dishes ? item.IsDish : item.Shelf == shelf).ToList();
            var (en, zh) = shelf == PickerGroups.Dishes ? DishesName : shelfNames.TryGetValue(shelf, out var named) ? named : ("#" + shelf, "#" + shelf);
            var held = kind.Count(item => item.Held > 0);
            var have = held > 0 ? PickerLines.HaveKinds(held) : ("", "");
            var icon = (kind.FirstOrDefault(item => item.Held > 0 && item.Entry.Icon != null) ?? kind.FirstOrDefault(item => item.Entry.Icon != null))?.Entry.Icon;

            entries.Add(new PickerEntry(ActKeys.Group(shelf), "Any " + en, "任意" + zh, icon, Group.En, Group.Zh, note.En, note.Zh, have.Item1, have.Item2));
        }

        // Within a shelf, what the player has comes first; the shelves and the rest keep their order.
        var order = shelves.Where(shelf => shelf != PickerGroups.Dishes).Select((shelf, at) => (shelf, at)).ToDictionary(pair => pair.shelf, pair => pair.at);

        entries.AddRange(items.Select((item, at) => (item, at))
            .OrderBy(pair => order[pair.item.Shelf]).ThenBy(pair => pair.item.Held > 0 ? 0 : 1).ThenBy(pair => pair.at)
            .Select(pair => Marked(pair.item)));

        return entries;
    }

    private static PickerEntry Marked(ItemPick item)
    {
        if (item.Held <= 0) return item.Entry;

        var (en, zh) = PickerLines.Have(item.Held);

        return item.Entry with { HaveEn = en, HaveZh = zh };
    }
}
