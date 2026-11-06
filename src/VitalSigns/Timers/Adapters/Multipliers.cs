using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using VitalSigns.Timers.Core;

namespace VitalSigns.Timers.Adapters;

/// <summary>
/// The multipliers, all in the .cfg: global, per category, and per buff (the Buffs setting). Applying them hands the
/// hooks a new plan and moves the end of buffs already running.
/// </summary>
internal sealed class Multipliers
{
    private readonly ConfigEntry<float> _good;
    private readonly ConfigEntry<float> _bad;
    private readonly IReadOnlyList<ConfigEntry<float>> _categories;
    private readonly ConfigEntry<string> _buffs;
    private readonly Action<string> _log;

    public Multipliers(ConfigEntry<float> good, ConfigEntry<float> bad, IReadOnlyList<ConfigEntry<float>> categories, ConfigEntry<string> buffs, Action<string> log)
    {
        _good = good;
        _bad = bad;
        _categories = categories;
        _buffs = buffs;
        _log = log;
    }

    public IEnumerable<ConfigEntryBase> Entries => new ConfigEntryBase[] { _good, _bad }.Concat(_categories).Append(_buffs);

    /// <summary>VitalSigns/Enabled: off, the plan leaves every buff's time as the game's, whatever is set.</summary>
    public bool IsOn { get; set; } = true;

    public MultiplierPlan Current() => IsOn ? new(_good.Value, _bad.Value, _categories.Select(c => (double?)c.Value).ToList(), BuffList.Parse(_buffs.Value)) : MultiplierPlan.Unchanged;

    public void Apply()
    {
        var from = BuffHooks.Plan;
        var to = Current();

        BuffHooks.Plan = to;

        var moved = RunningBuffs.Rescale(from, to, _log);

        _log($"multipliers: good {to.Good:0.###}, bad {to.Bad:0.###}, {to.Categories.Count(c => c != null)} categories and {to.Buffs.Count} buffs set; {moved} running buffs rescaled");
    }
}
