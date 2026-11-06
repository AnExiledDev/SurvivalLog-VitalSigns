using System;
using System.Collections.Generic;
using System.Globalization;

namespace VitalSigns.Core.Acts;

/// <summary>Whether something a click can queue can be done now: Ready, Busy (there, but not now: a daily limit, a cooldown, a lock) or Missing (no such furniture or item at home).</summary>
public enum ActState
{
    Ready,
    Busy,
    Missing,
}

/// <summary>An item kind or a furniture action, the two things a click on a bar queues.</summary>
public enum ActKind
{
    Item,
    Furniture,
}

/// <summary>
/// One thing a click can queue, as the base has it now. Key is "i" and the item's config id, or "f" and the furniture
/// action's id. Delta is what one use does, already scaled by the player's modifiers. Count is how many of the item
/// there are in the bag and the home storage together, InBag how many of them are in the bag. Target is the furniture a
/// furniture action is done at and Place its name. Why says, in the game's language, why it is not Ready. Shelf is an
/// item's shelf in the pickers (<see cref="PickerGroups.Rank"/>) and IsDish whether a cooking recipe makes it, which the
/// group keys ("g" and a shelf) go by.
/// </summary>
public sealed record ActOption(string Key, ActKind Kind, int Id, string Name, string Icon, StatDelta Delta, ActState State, string Why = "", int Count = 0,
    int InBag = 0, long Target = 0, string Place = "", int Shelf = 0, bool IsDish = false)
{
    public bool IsReady => State == ActState.Ready;
}

/// <summary>
/// The keys the settings keep lists of actions in: "f205,i2139,g1002". A group key, "g" and a shelf, stands for every
/// item of that kind the player has, such as every meat dish (g1002) or every cooked dish (g900).
/// </summary>
public static class ActKeys
{
    public const char ItemMark = 'i';
    public const char FurnitureMark = 'f';
    public const char GroupMark = 'g';

    public static string Item(int configId) => ItemMark + configId.ToString(CultureInfo.InvariantCulture);

    public static string Furniture(int funcId) => FurnitureMark + funcId.ToString(CultureInfo.InvariantCulture);

    public static string Group(int shelf) => GroupMark + shelf.ToString(CultureInfo.InvariantCulture);

    public static bool IsGroup(string key) => key.Length > 1 && key[0] == GroupMark;

    /// <summary>Whether an item is of the kind a group key names: on its shelf, or, for the dishes' key, made by a cooking recipe.</summary>
    public static bool Holds(string groupKey, ActOption option)
    {
        if (option.Kind != ActKind.Item || !IsGroup(groupKey)) return false;

        var shelf = Number(groupKey);

        return shelf == PickerGroups.Dishes ? option.IsDish : option.Shelf == shelf;
    }

    /// <summary>The keys in a setting, in order, each once. A bare number is read as an item, so a list of item ids works too.</summary>
    public static IReadOnlyList<string> Parse(string? text)
    {
        var keys = new List<string>();

        foreach (var part in (text ?? "").Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var key = Normal(part);

            if (key != null && !keys.Contains(key)) keys.Add(key);
        }

        return keys;
    }

    /// <summary>The item config ids among a setting's keys, in order.</summary>
    public static IReadOnlyList<int> Items(string? text)
    {
        var ids = new List<int>();

        foreach (var key in Parse(text))
        {
            if (key[0] == ItemMark) ids.Add(Number(key));
        }

        return ids;
    }

    public static string Join(IEnumerable<string> keys) => string.Join(",", keys);

    /// <summary>The setting with the key added at the end when it was missing, and taken out when it was there.</summary>
    public static string Toggle(string? text, string key)
    {
        var keys = new List<string>(Parse(text));
        var normal = Normal(key);

        if (normal == null) return Join(keys);

        if (!keys.Remove(normal)) keys.Add(normal);

        return Join(keys);
    }

    /// <summary>The setting with the key moved one place earlier (by -1) or later (by 1); unchanged at either end.</summary>
    public static string Move(string? text, string key, int by)
    {
        var keys = new List<string>(Parse(text));
        var at = keys.IndexOf(Normal(key) ?? "");
        var to = at + Math.Sign(by);

        if (at < 0 || to < 0 || to >= keys.Count) return Join(keys);

        (keys[at], keys[to]) = (keys[to], keys[at]);

        return Join(keys);
    }

    public static int Number(string key) => int.Parse(key.Substring(1), NumberStyles.None, CultureInfo.InvariantCulture);

    private static string? Normal(string part)
    {
        var text = part.Trim();

        if (text.Length == 0) return null;

        var mark = char.ToLowerInvariant(text[0]);
        var digits = mark is ItemMark or FurnitureMark or GroupMark ? text.Substring(1) : text;

        if (!int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var id) || id <= 0) return null;

        return (mark is FurnitureMark or GroupMark ? mark : ItemMark) + id.ToString(CultureInfo.InvariantCulture);
    }
}
