using System;
using System.Collections.Generic;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using HarmonyLib;
using Il2CppInterop.Runtime;
using VitalSigns.Timers.Core;

namespace VitalSigns.Timers.Adapters;

/// <summary>
/// Multiplies a buff's time where the game works it out. Every buff goes through BuffComponent.PlayBuff, which takes
/// either the config's time times the talents' ratio (TriggerBuffDuringFix) or a time its caller passed, then adds
/// the talents' flat bonus (TriggerBuffDuringAdd). Each of the three is multiplied, so the whole time is. Loading a save
/// replays every saved buff through PlayBuff with the time it had left, already multiplied, so nothing is touched while
/// LoadGame runs. Static because a Harmony patch has nowhere else to look.
/// </summary>
internal static class BuffHooks
{
    private static volatile MultiplierPlan _plan = MultiplierPlan.Unchanged;
    private static int _loadingDepth;
    private static Action<string> _log = _ => { };
    private static bool _hasReportedFailure;

    /// <summary>Every BuffComponent seen, held weakly so the game can still free them; the running buffs are found through these.</summary>
    private static readonly Dictionary<IntPtr, IntPtr> Seen = new();

    public static MultiplierPlan Plan
    {
        get => _plan;
        set => _plan = value;
    }

    /// <summary>All or nothing: with only some hooks in, saved buffs could be multiplied again on every load.</summary>
    public static bool Apply(Harmony harmony, Action<string> log)
    {
        _log = log;

        try
        {
            var buffs = typeof(BuffComponent);
            var effects = typeof(BTEffectManager);

            harmony.Patch(AccessTools.Method(buffs, nameof(BuffComponent.LoadGame)), prefix: Hook(nameof(BeforeLoad)), finalizer: Hook(nameof(AfterLoad)));
            harmony.Patch(AccessTools.Method(effects, nameof(BTEffectManager.TriggerBuffDuringFix)), postfix: Hook(nameof(AfterRatio)));
            harmony.Patch(AccessTools.Method(effects, nameof(BTEffectManager.TriggerBuffDuringAdd)), postfix: Hook(nameof(AfterBonus)));
            harmony.Patch(AccessTools.Method(buffs, nameof(BuffComponent.PlayBuff)), prefix: Hook(nameof(BeforePlay)));
            log("buff time hooks in");

            return true;
        }
        catch (Exception ex)
        {
            harmony.UnpatchSelf();
            log("could not hook buff times, every buff keeps the game's own time: " + ex.Message);

            return false;
        }
    }

    /// <summary>The BuffComponents the game still has, as fresh wrappers. Ones it freed are dropped.</summary>
    public static List<BuffComponent> LiveComponents()
    {
        var live = new List<BuffComponent>();
        var gone = new List<IntPtr>();

        foreach (var (pointer, handle) in Seen)
        {
            if (IL2CPP.il2cpp_gchandle_get_target(handle) == pointer) live.Add(new BuffComponent(pointer));
            else gone.Add(pointer);
        }

        foreach (var pointer in gone)
        {
            IL2CPP.il2cpp_gchandle_free(Seen[pointer]);
            Seen.Remove(pointer);
        }

        return live;
    }

    private static HarmonyMethod Hook(string name) => new(typeof(BuffHooks), name);

    private static void BeforeLoad(BuffComponent __instance)
    {
        _loadingDepth++;
        Remember(__instance);
    }

    private static Exception? AfterLoad(Exception? __exception)
    {
        if (_loadingDepth > 0) _loadingDepth--;

        return __exception;
    }

    /// <summary>The config's time path: base * (1 + ratio). Only PlayBuff calls this.</summary>
    private static void AfterRatio(Config_Buff __1, ref float __result)
    {
        try
        {
            if (Multiplier(__1) is { } multiplier) __result = Scaling.Ratio(__result, multiplier, __1.BuffDuring);
        }
        catch (Exception ex)
        {
            ReportOnce(ex);
        }
    }

    /// <summary>The talents' flat bonus, added on both paths, and again for each saved buff while a save loads.</summary>
    private static void AfterBonus(Config_Buff __1, ref float __result)
    {
        try
        {
            if (Multiplier(__1) is { } multiplier) __result = Scaling.Bonus(__result, multiplier);
        }
        catch (Exception ex)
        {
            ReportOnce(ex);
        }
    }

    /// <summary>A caller that passes its own time skips the talents' ratio, so that time is multiplied here.</summary>
    private static void BeforePlay(BuffComponent __instance, Config_Buff __1, ref float __2)
    {
        try
        {
            Remember(__instance);

            if (__2 == -1f) return;

            if (Multiplier(__1) is { } multiplier) __2 = Scaling.Passed(__2, multiplier);
        }
        catch (Exception ex)
        {
            ReportOnce(ex);
        }
    }

    /// <summary>Null when this buff keeps the game's time: while a save loads, for hidden or untimed buffs, and at 1x.</summary>
    private static double? Multiplier(Config_Buff? config)
    {
        var plan = _plan;

        if (_loadingDepth > 0 || config == null || !plan.ChangesAnything) return null;

        if (!Scope.IsTimedVisible(config.IsHide, config.BuffDuring)) return null;

        var multiplier = plan.For(new BuffKind(config.ID, config.IsGood, config.BuffDisplayType)).Value;

        return Limits.IsOne(multiplier) ? null : multiplier;
    }

    private static void Remember(BuffComponent? component)
    {
        if (component == null) return;

        var pointer = component.Pointer;

        if (Seen.TryGetValue(pointer, out var handle))
        {
            if (IL2CPP.il2cpp_gchandle_get_target(handle) == pointer) return;

            IL2CPP.il2cpp_gchandle_free(handle);
        }

        Seen[pointer] = IL2CPP.il2cpp_gchandle_new_weakref(pointer, false);
    }

    private static void ReportOnce(Exception ex)
    {
        if (_hasReportedFailure) return;

        _hasReportedFailure = true;
        _log("a buff time hook failed, that buff kept the game's time (reported once): " + ex);
    }
}
