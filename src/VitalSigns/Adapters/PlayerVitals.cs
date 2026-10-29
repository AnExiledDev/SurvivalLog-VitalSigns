using System;
using System.Collections.Generic;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using VitalSigns.Core;

namespace VitalSigns.Adapters;

/// <summary>
/// Reads the player as the HUD needs it: how each stat's changes are scaled, the running buffs that end, the game
/// clock, carried weight, and the Well Fed line. Read only; nothing the game keeps is changed. Null before a player
/// exists.
/// </summary>
internal static class PlayerVitals
{
    /// <summary>The HUD's rows, top to bottom. The game calls Life "Vitality".</summary>
    private static readonly (AttrName Gain, AttrName Loss, AttrName NaturalGain, AttrName NaturalLoss)[] Rows =
    {
        (AttrName.SatietyAdd_Ratio, AttrName.SatietyDel_Ratio, AttrName.SatietyAutoAdd_Ratio, AttrName.SatietyAutoDel_Ratio),
        (AttrName.MoraleAdd_Ratio, AttrName.MoraleDel_Ratio, AttrName.MoraleAutoAdd_Ratio, AttrName.MoraleAutoDel_Ratio),
        (AttrName.StaminaAdd_Ratio, AttrName.StaminaDel_Ratio, AttrName.StaminaAutoAdd_Ratio, AttrName.StaminaAutoDel_Ratio),
        (AttrName.VitalityAdd_Ratio, AttrName.VitalityDel_Ratio, AttrName.VitalityAutoAdd_Ratio, AttrName.VitalityAutoDel_Ratio)
    };

    public static Vitals? Read()
    {
        var world = GameSingleton.Existing<BattleLogicWorld>();
        var role = world?._AgentManager?.GetLeadingRole();
        var clock = world?._GameTimeManager;
        var attributes = role == null ? null : AgentTools.GetAgentComponent<AttributeComponent>(role);

        if (role == null || clock == null || attributes?.AttrDict == null) return null;

        return new Vitals(Stats(attributes), Buffs(AgentTools.GetAgentComponent<BuffComponent>(role)), clock.GetTotalSeconds(),
            Weight(AgentTools.GetAgentComponent<BagComponent>(role)), WellFed(attributes));
    }

    /// <summary>The game clock alone, for the checks that send nothing else.</summary>
    public static int Now() => GameSingleton.Existing<BattleLogicWorld>()?._GameTimeManager?.GetTotalSeconds() ?? 0;

    private static List<StatRatios> Stats(AttributeComponent attributes)
    {
        var stats = new List<StatRatios>(Rows.Length);

        foreach (var row in Rows)
        {
            stats.Add(new StatRatios(Ratio(attributes, row.Gain), Ratio(attributes, row.Loss), Ratio(attributes, row.NaturalGain), Ratio(attributes, row.NaturalLoss)));
        }

        return stats;
    }

    /// <summary>The game's float total is already divided down to a plain ratio and held to the stat's limits.</summary>
    private static double Ratio(AttributeComponent attributes, AttrName name) =>
        attributes.AttrDict.ContainsKey(name) ? attributes.GetTotalValue_Float(name) : 0;

    /// <summary>Buffs the HUD shows and that end: hidden buffs and buffs that last until removed have no timer to draw.</summary>
    private static List<BuffTime> Buffs(BuffComponent? component)
    {
        var buffs = new List<BuffTime>();
        var running = component?.buffArgsMap;

        if (running == null) return buffs;

        foreach (var buff in running.Values)
        {
            var config = buff?.config;

            if (buff == null || config == null || config.IsHide || buff.TimeEndTime == -1) continue;

            buffs.Add(new BuffTime(config.ID, buff.TimeEndTime, (int)Math.Max(0, Math.Round(config.BuffDuring))));
        }

        buffs.Sort((a, b) => a.ConfigId != b.ConfigId ? a.ConfigId.CompareTo(b.ConfigId) : a.EndsAt.CompareTo(b.EndsAt));

        return buffs;
    }

    private static CarryWeight? Weight(BagComponent? bag)
    {
        if (bag == null) return null;

        var max = bag.GetMaxBurden();

        return max > 0 ? new CarryWeight(bag.GetBagWeight(), max, bag.GetOverweightThreshold()) : null;
    }

    private static WellFedLine? WellFed(AttributeComponent attributes)
    {
        if (GameTexts.WellFedRule() is not { } rule || !attributes.AttrDict.ContainsKey(AttrName.MaxSatiety)) return null;

        return new WellFedLine(rule.From, rule.Until, attributes.GetTotalValue_Int(AttrName.MaxSatiety));
    }
}
