using System;
using System.Collections.Generic;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using UnityEngine;
using VitalSigns.Core.Acts;

namespace VitalSigns.Adapters;

/// <summary>
/// Puts a plan's steps in the character's queue the way the game's own suggestion chips do: a furniture action is a
/// walk to the furniture's action spot and then the action's own steps; an item in the bag is its use; an item in home
/// storage is a walk to the storage and then its use. All of it goes in as one batch through the role's action cache
/// (ActionCacheComponent.DoActionCache), which clears the queue first in the Default mode and adds to its end in the
/// Queue mode. The mode is set for the batch and put back after.
/// </summary>
internal static class ActionQueue
{
    /// <summary>
    /// Queues the steps. With append off the queue is replaced, unless what the character is doing now can't be
    /// interrupted, when the steps go on the end instead. Uses already waiting in the queue are taken off a stack's
    /// count first (reserved: item instance id to uses). How many actions went in, 0 when none did.
    /// </summary>
    public static int Queue(IReadOnlyList<PlanStep> steps, bool append, IReadOnlyDictionary<long, int> reserved, Action<string> log)
    {
        var world = GameSingleton.Existing<BattleLogicWorld>();
        var config = GameSingleton.Existing<ConfigManager>();
        var agents = world?._AgentManager;
        var role = agents?.GetLeadingRole();
        var cache = role == null ? null : AgentTools.GetAgentComponent<ActionCacheComponent>(role);

        if (world == null || config == null || agents == null || role == null || cache == null) return 0;

        var mode = cache.Mode;
        var added = 0;

        try
        {
            var replace = !append && world.IsCurrentBehaviorInterruptible(role);

            cache.ClearActionCache();
            cache.InitMode(replace ? ActionMode.Default : ActionMode.Queue);

            var used = new Dictionary<long, int>();

            foreach (var step in steps)
            {
                added += step.Option.Kind == ActKind.Furniture
                    ? AddFurniture(world, config, cache, step.Option)
                    : AddItem(config, agents, cache, step.Option, step.Count, reserved, used);
            }

            if (added == 0)
            {
                cache.ClearActionCache();

                return 0;
            }

            cache.DoActionCache(ActionSourceType.ManualCreate);

            return added;
        }
        catch (Exception ex)
        {
            log($"queueing failed: {ex.Message}");

            try
            {
                cache.ClearActionCache();
            }
            catch (Exception)
            {
                // The cache was only ever this click's own.
            }

            return 0;
        }
        finally
        {
            try
            {
                cache.InitMode(mode);
            }
            catch (Exception)
            {
                // Nothing else to put back.
            }
        }
    }

    private static int AddFurniture(BattleLogicWorld world, ConfigManager config, ActionCacheComponent cache, ActOption option)
    {
        if (option.Target <= 0 || GameCatalog.Func(option.Id) is not { } func) return 0;

        if (world._AgentManager.GetAgent(option.Target) is not { } agent) return 0;

        if (!config._Config_Furniture_Dict.TryGetValue(agent.AgentConfigId, out var row) || row == null) return 0;

        SmartRecommendManager.ResolveFurnitureActionPos(world, option.Target, row, out var pos, out var rotation);

        var added = 0;

        if (row.ActionId > 0)
        {
            cache.AddAction(row.ActionId, option.Target, pos, rotation);
            added++;
        }

        foreach (var actionId in func.ActionIds)
        {
            cache.AddAction(actionId, option.Target, pos, rotation);
            added++;
        }

        return added;
    }

    /// <summary>Uses of an item kind, the bag's stacks first, each stack used as far as its count less what is already queued from it.</summary>
    private static int AddItem(ConfigManager config, AgentManager agents, ActionCacheComponent cache, ActOption option, int uses, IReadOnlyDictionary<long, int> reserved,
        Dictionary<long, int> used)
    {
        if (GameCatalog.Item(option.Id) is not { } kind || uses <= 0) return 0;

        var onlyBag = option.Count == option.InBag;
        var added = 0;

        foreach (var stack in BaseScan.StacksOf(option.Id))
        {
            if (uses == 0) break;

            if (onlyBag && !stack.InBag) break;

            used.TryGetValue(stack.InstanceId, out var already);
            reserved.TryGetValue(stack.InstanceId, out var waiting);

            var left = stack.Count - already - waiting;

            while (left > 0 && uses > 0)
            {
                if (!stack.InBag)
                {
                    if (agents.GetAgent(stack.Owner) is not { } box || !config._Config_Furniture_Dict.TryGetValue(StorageConfigId(box), out var row) || row == null)
                    {
                        break;
                    }

                    if (row.ActionId > 0)
                    {
                        cache.AddAction(row.ActionId, stack.Owner, Vector3.zero, Quaternion.identity);
                        added++;
                    }
                }

                cache.AddAction(kind.UseAction, stack.InstanceId, Vector3.zero, Quaternion.identity);
                added++;
                left--;
                uses--;
                used[stack.InstanceId] = ++already;
            }
        }

        return added;
    }

    /// <summary>
    /// The furniture config row of a storage, as the game reads it when you use an item from one
    /// (OnUseItemFromFurnitureRequest): a placed Furniture's own config id, or a packed BagFurniture's FurnitureConfigId.
    /// </summary>
    private static int StorageConfigId(BaseAgent box) =>
        box.TryCast<BagFurniture>() is { } packed ? packed.FurnitureConfigId : box.AgentConfigId;
}
