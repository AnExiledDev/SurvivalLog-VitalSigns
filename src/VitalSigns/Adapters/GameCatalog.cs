using System;
using System.Collections.Generic;
using ExiledToolkit.Kit;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using VitalSigns.Core;
using VitalSigns.Core.Acts;

namespace VitalSigns.Adapters;

/// <summary>An item kind a click can use: what one use does before the player's modifiers, and what the game needs to queue it.</summary>
internal sealed record CatalogItem(int Id, int SubCategory, int UseAction, Config_Effect Effect, StatDelta Delta);

/// <summary>A furniture action a click can queue: what it does before the player's modifiers, its button text key and icon, and the actions it runs.</summary>
internal sealed record CatalogFunc(int Id, string NameKey, string Icon, StatDelta Delta, int[] ActionIds, bool IsRest);

/// <summary>
/// What the game's tables say every usable item and furniture action does to the four stats, read a slice at a time
/// and kept until the game swaps its tables. Only items whose use is a plain use or a drink count (placing ivy or a
/// trap is an item use too). A furniture action counts when its config previews a stat change, or when it is one of the
/// beds' rests, which the config does not preview: those are worked out from the sleep effect, a step of restore every
/// interval for the action's length.
/// </summary>
internal static class GameCatalog
{
    public const int SleepFunc = 2;
    public const int NapFunc = 4;
    public const int BeautyFunc = 51;
    private static readonly int[] RestFuncs = { SleepFunc, NapFunc, BeautyFunc };

    private const string ItemEffectPrefix = "Action/Item_";
    private const string SleepEffectPrefix = "Action/Sleep_";
    private const int FurnitureActionFunc = 2;

    private static readonly Dictionary<int, CatalogItem> Items = new();
    private static readonly Dictionary<int, CatalogFunc> Funcs = new();
    private static readonly Dictionary<int, int[]> FuncsOfFurniture = new();
    private static readonly Dictionary<int, StatDelta> ByAction = new();
    private static readonly Dictionary<int, string> FuncNames = new();
    private static readonly int[] NoFuncs = Array.Empty<int>();

    private static object? _itemTable;
    private static SlicedJob? _job;
    private static bool _isChinese;

    /// <summary>Whether the whole catalog is read for the tables now loaded. Until then the lists are partial.</summary>
    public static bool IsReady { get; private set; }

    /// <summary>Goes up each time the catalog is read again, so what was built from it can tell it is stale.</summary>
    public static int Version { get; private set; }

    public static IReadOnlyDictionary<int, CatalogItem> AllItems => Items;

    public static IReadOnlyDictionary<int, CatalogFunc> AllFuncs => Funcs;

    /// <summary>
    /// Reads on while the slice lasts. Starts again from the top when the game swaps its tables. False while there is
    /// no config, or the read is not done.
    /// </summary>
    public static bool Advance(ConfigManager config, Func<bool> isSliceSpent)
    {
        var table = config._Config_Item_Dict;

        if (table == null || table.Count == 0) return false;

        if (!ReferenceEquals(table, _itemTable))
        {
            _itemTable = table;
            IsReady = false;
            Items.Clear();
            Funcs.Clear();
            FuncsOfFurniture.Clear();
            ByAction.Clear();
            FuncNames.Clear();
            _job = new SlicedJob(Read(config));
        }

        if (IsReady) return true;

        if (_job != null && _job.Advance(isSliceSpent))
        {
            _job = null;
            IsReady = true;
            Version++;
        }

        return IsReady;
    }

    public static CatalogItem? Item(int id) => Items.TryGetValue(id, out var item) ? item : null;

    public static CatalogFunc? Func(int id) => Funcs.TryGetValue(id, out var func) ? func : null;

    /// <summary>The furniture actions with a stat effect that a kind of furniture offers, in the config's order.</summary>
    public static IReadOnlyList<int> FuncsOf(int furnitureConfigId) => FuncsOfFurniture.TryGetValue(furnitureConfigId, out var funcs) ? funcs : NoFuncs;

    /// <summary>What a queued action does, for the actions the queue holds that are not item uses: a furniture action's first action carries its whole effect.</summary>
    public static bool TryActionDelta(int actionId, out StatDelta delta) => ByAction.TryGetValue(actionId, out delta);

    /// <summary>A furniture action's button text in the game's language, kept per language.</summary>
    public static string FuncName(ConfigManager config, CatalogFunc func, bool isChinese)
    {
        if (isChinese != _isChinese)
        {
            FuncNames.Clear();
            _isChinese = isChinese;
        }

        if (FuncNames.TryGetValue(func.Id, out var name)) return name;

        name = BuffBook.Text(config, func.NameKey, isChinese) ?? "#" + func.Id;
        FuncNames[func.Id] = name;

        return name;
    }

    /// <summary>What one use of an item does: its effect's start and end values, the end read for this one item (a cooked dish keeps its own).</summary>
    public static StatDelta ItemDelta(CatalogItem item, ItemData? instance)
    {
        if (instance == null) return item.Delta;

        var e = item.Effect;

        return new StatDelta(
            e.ItemSatietyStart + ItemValueDisplayHelper.GetEffectiveEffectEnd(instance, e, AttrName.Satiety),
            e.ItemMoraleStart + ItemValueDisplayHelper.GetEffectiveEffectEnd(instance, e, AttrName.Morale),
            e.ItemStaminaStart + ItemValueDisplayHelper.GetEffectiveEffectEnd(instance, e, AttrName.Stamina),
            e.ItemVitalityStart + ItemValueDisplayHelper.GetEffectiveEffectEnd(instance, e, AttrName.Vitality));
    }

    private static IEnumerable<bool> Read(ConfigManager config)
    {
        var actions = config._Config_Action_Dict;
        var effects = config._Config_Effect_Dict;

        if (actions == null || effects == null) yield break;

        var read = 0;

        foreach (var row in config._Config_Item_Dict.Values)
        {
            if (++read % 64 == 0) yield return true;

            if (row == null || row.CantUse || row.UseAction <= 0) continue;

            if (!actions.TryGetValue(row.UseAction, out var action) || action == null) continue;

            if (!effects.TryGetValue(action.EffectConfigID, out var effect) || effect == null) continue;

            if (effect.ConfigName == null || !effect.ConfigName.StartsWith(ItemEffectPrefix, StringComparison.Ordinal)) continue;

            var delta = new StatDelta(effect.ItemSatietyStart + effect.ItemSatietyEnd, effect.ItemMoraleStart + effect.ItemMoraleEnd,
                effect.ItemStaminaStart + effect.ItemStaminaEnd, effect.ItemVitalityStart + effect.ItemVitalityEnd);

            if (delta.IsNone) continue;

            Items[row.ID] = new CatalogItem(row.ID, row.SubCategory, row.UseAction, effect, delta);
        }

        yield return true;

        var funcRows = config._Config_FurnitureFunc_Dict;

        if (funcRows == null) yield break;

        foreach (var row in funcRows.Values)
        {
            if (++read % 64 == 0) yield return true;

            if (row == null || row.FuncType != FurnitureActionFunc || row.ActionIds == null || row.ActionIds.Count == 0) continue;

            var isRest = Array.IndexOf(RestFuncs, row.ID) >= 0;
            var delta = isRest ? RestDelta(actions, effects, row.ActionIds[0]) : StatDelta.Parse(row.PreviewAttrDelta);

            if (delta.IsNone) continue;

            var ids = new int[row.ActionIds.Count];

            for (var i = 0; i < ids.Length; i++) ids[i] = row.ActionIds[i];

            Funcs[row.ID] = new CatalogFunc(row.ID, row.BtnName ?? "", row.BtnIcon ?? "", delta, ids, isRest);

            ByAction.TryAdd(ids[0], delta);
        }

        yield return true;

        var furniture = config._Config_Furniture_Dict;

        if (furniture == null) yield break;

        var found = new List<int>();

        foreach (var row in furniture.Values)
        {
            if (++read % 64 == 0) yield return true;

            if (row?.FurnitureFunc == null) continue;

            found.Clear();

            for (var i = 0; i < row.FurnitureFunc.Count; i++)
            {
                var id = row.FurnitureFunc[i];

                if (Funcs.ContainsKey(id) && !found.Contains(id)) found.Add(id);
            }

            if (found.Count > 0) FuncsOfFurniture[row.ID] = found.ToArray();
        }

        // A furniture action no kind of furniture offers is left out: the tables keep tests and retired actions.
        var offered = new HashSet<int>();

        foreach (var list in FuncsOfFurniture.Values) offered.UnionWith(list);

        var unused = new List<int>();

        foreach (var id in Funcs.Keys)
        {
            if (!offered.Contains(id)) unused.Add(id);
        }

        foreach (var id in unused) Funcs.Remove(id);
    }

    /// <summary>A rest's Stamina: one step of the sleep effect every interval, for the length of the action.</summary>
    private static StatDelta RestDelta(Il2CppSystem.Collections.Generic.Dictionary<int, Config_Action> actions,
        Il2CppSystem.Collections.Generic.Dictionary<int, Config_Effect> effects, int actionId)
    {
        if (!actions.TryGetValue(actionId, out var action) || action == null) return StatDelta.None;

        if (!effects.TryGetValue(action.EffectConfigID, out var effect) || effect == null) return StatDelta.None;

        if (effect.ConfigName == null || !effect.ConfigName.StartsWith(SleepEffectPrefix, StringComparison.Ordinal) || effect.IntValue0 <= 0) return StatDelta.None;

        return new StatDelta(0, 0, Math.Floor(action.During / effect.IntValue0) * effect.FloatValue0, 0);
    }
}
