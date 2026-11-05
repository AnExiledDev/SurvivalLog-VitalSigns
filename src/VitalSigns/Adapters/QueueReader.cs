using System;
using System.Collections.Generic;
using GameCore.HotUpdate.Battle.Logic;
using VitalSigns.Core.Acts;

namespace VitalSigns.Adapters;

/// <summary>
/// What the character's queue will do to the stats: each waiting action's effect, read from the queue the game keeps
/// for the character (ActionManager's action source). An item use counts what that one item does; a furniture action
/// counts what the game previews for it. The action being done now is left out: part of it is already on the bars.
/// Also counts the item uses waiting per item, so a click does not plan to eat what is already queued. Read at most
/// twice a second.
/// </summary>
internal static class QueueReader
{
    private const double EverySeconds = 0.5;

    private static readonly List<(StatDelta Delta, int Food)> Deltas = new();
    private static readonly Dictionary<long, int> ItemUses = new();
    private static readonly Dictionary<int, int> KindUses = new();
    private static double _nextAt;
    private static int _signature;
    private static Action<string>? _log;
    private static bool _reported;

    /// <summary>The waiting actions' effects before the player's modifiers, in queue order, with the food type of an item use (-1 for anything else).</summary>
    public static IReadOnlyList<(StatDelta Delta, int Food)> Queued => Deltas;

    /// <summary>Uses waiting in the queue per item instance id.</summary>
    public static IReadOnlyDictionary<long, int> Reserved => ItemUses;

    /// <summary>Uses waiting in the queue per item kind.</summary>
    public static IReadOnlyDictionary<int, int> ReservedKinds => KindUses;

    /// <summary>Whether nothing waits in the queue and nothing is being done.</summary>
    public static bool IsIdle { get; private set; } = true;

    /// <summary>Changes whenever what the queue holds changes, for the plans kept from it.</summary>
    public static int Signature => _signature;

    public static void SetLog(Action<string> log) => _log = log;

    /// <summary>Reads the queue again at once at the next tick, after a click added to it.</summary>
    public static void Refresh() => _nextAt = 0;

    public static void Tick(double now)
    {
        if (now < _nextAt) return;

        _nextAt = now + EverySeconds;

        try
        {
            Read();
        }
        catch (Exception ex)
        {
            Deltas.Clear();
            ItemUses.Clear();
            KindUses.Clear();

            if (!_reported)
            {
                _reported = true;
                _log?.Invoke($"queue read failed: {ex.Message}");
            }
        }
    }

    private static void Read()
    {
        var world = GameSingleton.Existing<BattleLogicWorld>();
        var role = world?._AgentManager?.GetLeadingRole();
        var source = role == null ? null : world!._ActionManager?.GetActionSource(role.InstanceId);
        var list = source?._ActionList_k__BackingField;
        var current = source?._CurrentAction_k__BackingField;
        var items = world?._ItemManager;
        var hash = 17;

        Deltas.Clear();
        ItemUses.Clear();
        KindUses.Clear();

        IsIdle = current == null && (list == null || list.Count == 0);

        for (var i = 0; list != null && i < list.Count; i++)
        {
            var action = list[i];

            if (action == null || current != null && action.Pointer == current.Pointer) continue;

            var actionId = action._ActionId_k__BackingField;
            var target = action._TargetId_k__BackingField;

            hash = hash * 31 + actionId;
            hash = hash * 31 + target.GetHashCode();

            var item = target > 0 ? items?.GetItemData(target) : null;

            if (item != null && GameCatalog.Item(item._ItemConfigId_k__BackingField) is { } kind && kind.UseAction == actionId)
            {
                Deltas.Add((GameCatalog.ItemDelta(kind, item), kind.SubCategory));
                ItemUses[target] = (ItemUses.TryGetValue(target, out var n) ? n : 0) + 1;
                KindUses[kind.Id] = (KindUses.TryGetValue(kind.Id, out var k) ? k : 0) + 1;

                continue;
            }

            if (GameCatalog.TryActionDelta(actionId, out var delta)) Deltas.Add((delta, -1));
        }

        _signature = hash;
    }
}
