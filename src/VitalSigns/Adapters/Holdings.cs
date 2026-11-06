using System;
using System.Collections.Generic;
using System.Diagnostics;
using ExiledToolkit.Game;
using ExiledToolkit.Kit;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;

namespace VitalSigns.Adapters;

/// <summary>
/// What the player has, for the pickers' marks: every item anywhere the game counts as theirs (the bag, every container
/// at home, the vehicle's boxes), wherever the player is, and the kinds of furniture placed at home. Read only, when the
/// Mods screen asks, and kept for a second since it asks once per list.
/// </summary>
internal static class Holdings
{
    private const double KeepMs = 1000;

    private static readonly Stopwatch Age = new();
    private static (IReadOnlyDictionary<int, int> Items, ISet<int> Furniture) _kept = (new Dictionary<int, int>(), new HashSet<int>());
    private static bool _reported;

    internal static Action<string> Log { get; set; } = _ => { };

    public static (IReadOnlyDictionary<int, int> Items, ISet<int> Furniture) Now()
    {
        if (Age.IsRunning && Age.Elapsed.TotalMilliseconds < KeepMs) return _kept;

        _kept = Read();
        Age.Restart();

        return _kept;
    }

    private static (IReadOnlyDictionary<int, int> Items, ISet<int> Furniture) Read()
    {
        var items = new Dictionary<int, int>();
        var furniture = new HashSet<int>();

        try
        {
            var world = GameSingleton.Existing<BattleLogicWorld>();
            var agents = world?._AgentManager;
            var role = agents?.GetLeadingRole();

            if (world == null || agents == null || role == null || world._GameTimeManager?.WorldStateType != WorldStateType.PostDisaster) return (items, furniture);

            var list = world._ItemManager?.GetAllAvailableItems(role.InstanceId);

            for (var i = 0; list != null && i < list.Count; i++)
            {
                var item = list[i];

                if (item == null || item._ItemCount_k__BackingField <= 0) continue;

                var id = item._ItemConfigId_k__BackingField;

                items[id] = (items.TryGetValue(id, out var count) ? count : 0) + item._ItemCount_k__BackingField;
            }

            var placed = agents.GetAllFurnitures();
            var tags = world._TagManager;

            for (var i = 0; placed != null && i < placed.Count; i++)
            {
                if (placed[i] is { } piece && BaseScan.IsShownAtHome(tags, agents, piece)) furniture.Add(piece._AgentConfigId_k__BackingField);
            }
        }
        catch (Exception ex)
        {
            if (!_reported) Log("reading what you have failed: " + ex.Message);

            _reported = true;
        }

        return (items, furniture);
    }
}
