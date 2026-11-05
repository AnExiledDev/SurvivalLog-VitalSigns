using System;
using System.Threading;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using HarmonyLib;
using VitalSigns.Core;

namespace VitalSigns.Adapters;

/// <summary>
/// The stat difficulty. Every change to a stat goes through AttributeComponent.CostAttrValue (the 8-argument overload;
/// the 6-argument one forwards to it): the slow drain and recovery over time (AttrRecoveryComponent.UpdateCd), food,
/// rest, buffs and furniture alike. A rise is positive, a fall negative (the method's own check at its start). The
/// prefix scales the amount before the game applies its own ratios, for the player alone: every other character costs
/// one compare. Its first 14 bytes hold no relative jump or call. The method runs for every character's every stat
/// change, so the hook goes in only once a multiplier is set off 1, and then stays in; with every multiplier back at 1
/// it returns at once. A failure is logged once and the hook goes quiet: a stat change must never break because of this
/// mod.
/// </summary>
internal static class DifficultyHook
{
    private static Difficulty _settings = Difficulty.Off;
    private static long _heroId;
    private static int _isQuiet;
    private static int _tried;
    private static Action<string> _log = _ => { };

    public static Difficulty Settings
    {
        get => Volatile.Read(ref _settings);
        set => Volatile.Write(ref _settings, value);
    }

    /// <summary>The player's id, set by the ticker each frame; 0 before a player exists.</summary>
    public static long HeroId
    {
        get => Interlocked.Read(ref _heroId);
        set => Interlocked.Exchange(ref _heroId, value);
    }

    /// <summary>Puts the hook in the first time it is called; later calls do nothing.</summary>
    public static void Apply(Harmony harmony, Action<string> log)
    {
        if (Interlocked.Exchange(ref _tried, 1) != 0) return;

        _log = log;

        try
        {
            var target = AccessTools.Method(typeof(AttributeComponent), nameof(AttributeComponent.CostAttrValue),
                             new[] { typeof(AttrName), typeof(float), typeof(long), typeof(EffectArgsType), typeof(bool), typeof(bool), typeof(VitalityChangeSourceType), typeof(int) })
                         ?? throw new MissingMethodException(nameof(AttributeComponent) + "." + nameof(AttributeComponent.CostAttrValue));

            harmony.Patch(target, prefix: new HarmonyMethod(typeof(DifficultyHook), nameof(Before)));
            log("stat difficulty hooked");
        }
        catch (Exception ex)
        {
            log("could not hook stat changes, the Difficulty settings do nothing: " + ex.Message);
        }
    }

    private static void Before(AttributeComponent __instance, AttrName attrName, ref float value)
    {
        var settings = Settings;

        if (settings.IsOff || Volatile.Read(ref _isQuiet) != 0) return;

        try
        {
            var hero = HeroId;

            if (hero == 0 || __instance.ownerInstanceId != hero) return;

            var stat = attrName switch
            {
                AttrName.Satiety => 0,
                AttrName.Morale => 1,
                AttrName.Stamina => 2,
                AttrName.Vitality => 3,
                _ => -1,
            };

            if (stat >= 0) value = settings.Scale(stat, value);
        }
        catch (Exception ex)
        {
            if (Interlocked.Exchange(ref _isQuiet, 1) == 0) _log("the Difficulty hook failed and is off until the game restarts: " + ex);
        }
    }
}
