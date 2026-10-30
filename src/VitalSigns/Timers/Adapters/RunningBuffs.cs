using System;
using System.Collections.Generic;
using GameCore.HotUpdate.Battle.Logic;
using VitalSigns.Adapters;
using VitalSigns.Timers.Core;

namespace VitalSigns.Timers.Adapters;

/// <summary>
/// Moves the end of every running buff when its multiplier changes, so a new setting also reaches buffs already on.
/// A running buff's time lives in two places: BuffArgs.TimeEndTime (game seconds, what the save and the buff panel
/// read) and its effect's life (real seconds, what ends it). Both are scaled by the same factor. A running buff is taken
/// to have been started with the plan in effect before the change; buffs loaded from a save keep the time they were
/// saved with.
/// </summary>
internal static class RunningBuffs
{
    public static int Rescale(MultiplierPlan from, MultiplierPlan to, Action<string> log)
    {
        var world = GameSingleton.Existing<BattleLogicWorld>();
        var effects = world?._BTEffectManager;
        var clock = world?._GameTimeManager;

        if (effects == null || clock == null) return 0;

        var now = clock.GetTotalSeconds();
        var count = 0;

        foreach (var component in BuffHooks.LiveComponents())
        {
            foreach (var buff in Running(component))
            {
                try
                {
                    if (Rescale(buff, from, to, effects, now)) count++;
                }
                catch (Exception ex)
                {
                    log("could not move one running buff's end, it keeps its time: " + ex.Message);
                }
            }
        }

        return count;
    }

    /// <summary>Gathered first, so nothing holds the game's dictionary open while buffs are changed.</summary>
    private static List<BuffArgs> Running(BuffComponent component)
    {
        var running = new List<BuffArgs>();
        var map = component.buffArgsMap;

        if (map == null) return running;

        foreach (var buff in map.Values)
        {
            if (buff != null) running.Add(buff);
        }

        return running;
    }

    private static bool Rescale(BuffArgs buff, MultiplierPlan from, MultiplierPlan to, BTEffectManager effects, int now)
    {
        var config = buff.config;

        if (config == null || buff.TimeEndTime == -1 || !Scope.IsTimedVisible(config.IsHide, config.BuffDuring)) return false;

        var kind = new BuffKind(config.ID, config.IsGood, config.BuffDisplayType);
        var remaining = (double)buff.TimeEndTime - now;

        if (Scaling.Remaining(remaining, from.For(kind).Value, to.For(kind).Value) is not { } scaled) return false;

        var factor = scaled / remaining;
        var effect = effects.GetBtArgsById(buff.EffectBtArgsID);

        buff.TimeEndTime = (int)Math.Min(int.MaxValue, now + Math.Round(scaled));

        if (effect != null)
        {
            effect.CurLifeDuration = (float)(effect.CurLifeDuration * factor);
            effect.MaxLifeDuration = (float)(effect.MaxLifeDuration * factor);
        }

        return true;
    }
}
