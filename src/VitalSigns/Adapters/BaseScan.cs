using System;
using System.Collections.Generic;
using System.Diagnostics;
using ExiledToolkit.Game;
using ExiledToolkit.Kit;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using VitalSigns.Core;
using VitalSigns.Core.Acts;

namespace VitalSigns.Adapters;

/// <summary>One stack of an item a click can use: where it is, how many, and whether it is in the bag.</summary>
internal readonly record struct ItemStack(long InstanceId, long Owner, int Count, bool InBag);

/// <summary>
/// Looks around the base for what the clicks can use: the items with a stat effect in the bag and the home storage the
/// game lets the player take from, and every furniture action with a stat effect at home, with the game's own answer
/// to whether it can be done now (the same button list the furniture's menu shows). The look is spread over frames, a
/// slice of about two milliseconds each, and taken again every few seconds; a click asks for a fresh one at once. Read
/// only. Away from home only the bag counts, and furniture actions show as not at home.
/// </summary>
internal static class BaseScan
{
    private const double SliceMs = 2;
    private const double EverySeconds = 8;

    private static readonly Stopwatch Slice = new();
    private static readonly Dictionary<int, List<ItemStack>> Stacks = new();
    private static readonly Func<bool> IsSliceSpent = () => Slice.Elapsed.TotalMilliseconds >= SliceMs;

    private static SlicedJob? _job;
    private static double _nextAt;
    private static bool _isChinese;
    private static int _version;
    private static Action<string>? _log;
    private static readonly HashSet<string> Reported = new();

    // What the running scan has gathered so far; swapped in whole when it ends.
    private static Dictionary<int, List<ItemStack>> _found = new();

    public static BaseView View { get; private set; } = BaseView.Empty;

    /// <summary>The stacks of an item kind from the last scan, the bag's first.</summary>
    public static IReadOnlyList<ItemStack> StacksOf(int configId) => Stacks.TryGetValue(configId, out var list) ? list : Array.Empty<ItemStack>();

    public static void SetLog(Action<string> log) => _log = log;

    /// <summary>Asks for a new look at the next tick, after a click changed what the base has or will have.</summary>
    public static void Refresh() => _nextAt = 0;

    /// <summary>Drops the last look, when the clicks are turned off.</summary>
    public static void Clear()
    {
        _job = null;
        _nextAt = 0;
        Stacks.Clear();
        View = BaseView.Empty;
    }

    /// <summary>Runs one slice of the catalog read or the scan. Call every frame while a feature that needs it is on.</summary>
    public static void Tick(double now, bool isChinese)
    {
        var config = GameSingleton.Existing<ConfigManager>();

        if (config == null) return;

        Slice.Restart();

        try
        {
            if (!GameCatalog.Advance(config, IsSliceSpent)) return;

            if (isChinese != _isChinese)
            {
                _isChinese = isChinese;
                _nextAt = 0;
            }

            if (_job == null)
            {
                if (now < _nextAt) return;

                _nextAt = now + EverySeconds;
                _job = new SlicedJob(Scan(config, isChinese));
            }

            if (_job.Advance(IsSliceSpent)) _job = null;
        }
        catch (Exception ex)
        {
            _job = null;
            Report("scan", ex);
        }
        finally
        {
            Slice.Stop();
        }
    }

    private static IEnumerable<bool> Scan(ConfigManager config, bool isChinese)
    {
        var world = GameSingleton.Existing<BattleLogicWorld>();
        var agents = world?._AgentManager;
        var role = agents?.GetLeadingRole();

        if (world == null || agents == null || role == null || world._GameTimeManager?.WorldStateType != WorldStateType.PostDisaster)
        {
            Publish(new List<ActOption>(), new List<ActOption>(), new List<Bed>(), false, new Dictionary<int, List<ItemStack>>());

            yield break;
        }

        var roleId = role.InstanceId;
        var atHome = IsAtHome(agents);

        _found = new Dictionary<int, List<ItemStack>>();

        var items = world._ItemManager;
        var list = items == null ? null : atHome ? items.GetAllAvailableItems(roleId) : items.GetItemDataList(roleId);
        var deltas = new Dictionary<int, StatDelta>();

        yield return true;

        if (list != null)
        {
            for (var i = 0; i < list.Count; i++)
            {
                if (i % 32 == 31) yield return true;

                var item = list[i];

                if (item == null || item._ItemCount_k__BackingField <= 0) continue;

                var configId = item._ItemConfigId_k__BackingField;

                if (GameCatalog.Item(configId) is not { } kind) continue;

                var owner = item._OwnerId_k__BackingField;
                var inBag = owner == roleId;

                if (!inBag && ItemManager.IsExcludedFromCostPool(owner)) continue;

                if (!_found.TryGetValue(configId, out var stacks)) _found[configId] = stacks = new List<ItemStack>();

                var stack = new ItemStack(item.InstanceId, owner, item._ItemCount_k__BackingField, inBag);

                // The bag's stacks go first: a click eats from the bag before it walks to a shelf.
                if (inBag)
                {
                    var at = 0;

                    while (at < stacks.Count && stacks[at].InBag) at++;

                    stacks.Insert(at, stack);

                    if (at == 0) deltas[configId] = GameCatalog.ItemDelta(kind, item);
                }
                else
                {
                    stacks.Add(stack);

                    if (stacks.Count == 1) deltas[configId] = GameCatalog.ItemDelta(kind, item);
                }
            }
        }

        var itemOptions = new List<ActOption>(_found.Count);

        foreach (var (configId, stacks) in _found)
        {
            var count = 0;
            var inBag = 0;

            foreach (var stack in stacks)
            {
                count += stack.Count;

                if (stack.InBag) inBag += stack.Count;
            }

            var (name, icon) = BuffBook.Item(config, configId, isChinese);

            itemOptions.Add(new ActOption(ActKeys.Item(configId), ActKind.Item, configId, name, icon, deltas[configId], ActState.Ready, "", count, inBag));
        }

        itemOptions.Sort((a, b) => a.Id.CompareTo(b.Id));

        yield return true;

        var best = new Dictionary<int, (ActOption Option, int Rank)>();
        var beds = new List<Bed>();

        if (atHome)
        {
            var roleFloor = SafeFloor(agents, roleId);
            var furniture = agents.GetAllFurnitures();
            var tags = world._TagManager;

            for (var i = 0; furniture != null && i < furniture.Count; i++)
            {
                var piece = furniture[i];

                if (piece == null) continue;

                var funcs = GameCatalog.FuncsOf(piece._AgentConfigId_k__BackingField);

                if (funcs.Count == 0 || !IsShownAtHome(tags, agents, piece)) continue;

                if (!config._Config_Furniture_Dict.TryGetValue(piece._AgentConfigId_k__BackingField, out var row) || row == null) continue;

                ReadFurniture(config, agents, piece, row, funcs, roleFloor, isChinese, best, beds);

                yield return false;
            }
        }

        var funcOptions = new List<ActOption>();
        var away = ActTexts.Away(isChinese);
        var missing = ActTexts.Missing(isChinese);

        foreach (var (id, func) in GameCatalog.AllFuncs)
        {
            if (func.IsRest) continue;

            if (best.TryGetValue(id, out var found))
            {
                funcOptions.Add(found.Option);

                continue;
            }

            funcOptions.Add(new ActOption(ActKeys.Furniture(id), ActKind.Furniture, id, GameCatalog.FuncName(config, func, isChinese), func.Icon, func.Delta,
                atHome ? ActState.Missing : ActState.Busy, atHome ? missing : away));
        }

        funcOptions.Sort((a, b) => a.Id.CompareTo(b.Id));

        Publish(itemOptions, funcOptions, beds, atHome, _found);
    }

    /// <summary>
    /// One piece of furniture's actions with a stat effect, as its menu would show them now: a visible button is ready
    /// or, when locked, busy with the game's reason; a hidden one is left out. For each action the best piece is kept:
    /// ready before busy, then one on the player's floor.
    /// </summary>
    private static void ReadFurniture(ConfigManager config, AgentManager agents, Furniture piece, Config_Furniture row, IReadOnlyList<int> funcs, int roleFloor,
        bool isChinese, Dictionary<int, (ActOption Option, int Rank)> best, List<Bed> beds)
    {
        var buttons = piece.BuildBtnDataList(row, -1);

        if (buttons == null) return;

        var id = piece._InstanceId_k__BackingField;
        var sameFloor = roleFloor != 0 && SafeFloor(agents, id) == roleFloor;
        var place = BuffBook.Text(config, row.Name, isChinese) ?? "#" + row.ID;
        ActOption? sleep = null, nap = null, beauty = null;

        for (var b = 0; b < buttons.Count; b++)
        {
            var button = buttons[b];

            if (button == null || button.BtnType != BattleSelectBtnType.FurnitureFunc || !button.IsVisible) continue;

            var funcId = button.FurnitureFuncId;

            if (!Contains(funcs, funcId) || GameCatalog.Func(funcId) is not { } func) continue;

            var state = button.IsLocked ? ActState.Busy : ActState.Ready;
            var name = string.IsNullOrWhiteSpace(button.BtnName) ? GameCatalog.FuncName(config, func, isChinese) : button.BtnName.Trim();
            var why = state == ActState.Busy ? (button.LockTipText ?? "").Trim() : "";
            var option = new ActOption(ActKeys.Furniture(funcId), ActKind.Furniture, funcId, name, func.Icon, func.Delta, state, why, 0, 0, id, place);

            if (func.IsRest)
            {
                if (funcId == GameCatalog.SleepFunc) sleep = option;
                else if (funcId == GameCatalog.NapFunc) nap = option;
                else beauty = option;

                continue;
            }

            var rank = (state == ActState.Ready ? 2 : 0) + (sameFloor ? 1 : 0);

            if (!best.TryGetValue(funcId, out var known) || rank > known.Rank) best[funcId] = (option, rank);
        }

        if (sleep != null || nap != null || beauty != null)
        {
            var restore = Math.Max(beauty?.Delta.Stamina ?? 0, sleep?.Delta.Stamina ?? 0);

            beds.Add(new Bed(id, row.ID, place, sleep, nap, beauty, restore, sameFloor));
        }
    }

    private static void Publish(List<ActOption> items, List<ActOption> funcs, List<Bed> beds, bool atHome, Dictionary<int, List<ItemStack>> stacks)
    {
        Stacks.Clear();

        foreach (var (id, list) in stacks) Stacks[id] = list;

        View = new BaseView(items, funcs, beds, atHome, ++_version);
    }

    /// <summary>The home filter the plant HUD uses: placed, on the home map, on an unlocked floor and in an unlocked area.</summary>
    private static bool IsShownAtHome(TagManager? tags, AgentManager agents, Furniture furniture)
    {
        var map = furniture._MapConfigId_k__BackingField;

        return furniture._IsShow_k__BackingField && agents.IsHomeMap(map) && FloorTagConstants.IsMapPointUnlocked(tags, map) && AreaUnlockConstants.IsFurnitureAreaUnlocked(furniture);
    }

    /// <summary>The furniture is on the home map, so a failed check counts as away and only the bag is used.</summary>
    public static bool IsAtHome(AgentManager agents)
    {
        try
        {
            return agents.IsHomeMap(ExploreSearchStateManager.GetCurrentMapId());
        }
        catch (Exception ex)
        {
            Report("home check", ex);

            return false;
        }
    }

    private static int SafeFloor(AgentManager agents, long id)
    {
        try
        {
            return agents.GetAgentFloor(id);
        }
        catch (Exception)
        {
            return 0;
        }
    }

    private static bool Contains(IReadOnlyList<int> list, int id)
    {
        for (var i = 0; i < list.Count; i++)
        {
            if (list[i] == id) return true;
        }

        return false;
    }

    private static void Report(string what, Exception ex)
    {
        if (Reported.Add(what + ex.GetType().Name)) _log?.Invoke($"base {what} failed: {ex.Message}");
    }
}
