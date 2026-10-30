using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Configuration;
using VitalSigns.Timers.Core;

namespace VitalSigns.Timers.Adapters;

/// <summary>
/// Where the multipliers live: the global and category ones in the .cfg, the per-buff ones in their own JSON file.
/// Applying them hands the hooks a new plan and moves the end of buffs already running.
/// </summary>
internal sealed class Multipliers
{
    private readonly ConfigEntry<float> _good;
    private readonly ConfigEntry<float> _bad;
    private readonly IReadOnlyList<ConfigEntry<float>> _categories;
    private readonly string _overridesPath;
    private readonly Action<string> _log;
    private IReadOnlyDictionary<int, double> _buffs;

    public Multipliers(ConfigEntry<float> good, ConfigEntry<float> bad, IReadOnlyList<ConfigEntry<float>> categories, string overridesPath, Action<string> log)
    {
        _good = good;
        _bad = bad;
        _categories = categories;
        _overridesPath = overridesPath;
        _log = log;
        (_buffs, FileProblems) = ReadFile();
    }

    /// <summary>What was wrong in the per-buff file when it was last read, for the tab to show.</summary>
    public IReadOnlyList<string> FileProblems { get; private set; }

    public IEnumerable<ConfigEntryBase> Entries => new ConfigEntryBase[] { _good, _bad }.Concat(_categories);

    public MultiplierPlan Current() => new(_good.Value, _bad.Value, _categories.Select(c => (double?)c.Value).ToList(), _buffs);

    public void Apply()
    {
        var from = BuffHooks.Plan;
        var to = Current();

        BuffHooks.Plan = to;

        var moved = RunningBuffs.Rescale(from, to, _log);

        _log($"multipliers: good {to.Good:0.###}, bad {to.Bad:0.###}, {to.Categories.Count(c => c != null)} categories and {to.Buffs.Count} buffs set; {moved} running buffs rescaled");
    }

    /// <summary>The file is written first: a failed write leaves every setting as it was.</summary>
    public void Save(SaveRequest.Accepted save)
    {
        WriteFile(save.Buffs);
        _buffs = new Dictionary<int, double>(save.Buffs);
        FileProblems = Array.Empty<string>();
        _good.Value = (float)save.Good;
        _bad.Value = (float)save.Bad;

        for (var i = 0; i < _categories.Count && i < save.Categories.Count; i++) _categories[i].Value = (float)(save.Categories[i] ?? Limits.UseAbove);

        Apply();
    }

    private (IReadOnlyDictionary<int, double>, IReadOnlyList<string>) ReadFile()
    {
        if (!File.Exists(_overridesPath)) return (new Dictionary<int, double>(), Array.Empty<string>());

        try
        {
            var read = OverrideFile.Read(File.ReadAllText(_overridesPath));

            foreach (var problem in read.Problems) _log("per-buff file: " + problem);

            return (read.Buffs, read.Problems);
        }
        catch (Exception ex)
        {
            _log("could not read the per-buff file, no buff has its own multiplier: " + ex.Message);

            return (new Dictionary<int, double>(), new[] { ex.Message });
        }
    }

    private void WriteFile(IReadOnlyDictionary<int, double> buffs)
    {
        if (File.Exists(_overridesPath)) File.Copy(_overridesPath, _overridesPath + ".bak", overwrite: true);

        File.WriteAllText(_overridesPath, OverrideFile.Write(buffs));
    }
}
