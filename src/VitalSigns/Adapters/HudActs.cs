using System;
using System.Collections.Generic;
using ExiledToolkit.Game;
using GameCore.HotUpdate;
using VitalSigns.Core;
using VitalSigns.Core.Acts;

namespace VitalSigns.Adapters;

/// <summary>
/// The clicks on the HUD: keeps a plan per bar from the base scan, the player's modifiers and the queue, turns it into
/// what the HUD shows (which bars a click can act for, the line in each bar's hover box, the debuffs with a cure at
/// hand, where the bars go once the queue is done), and queues a plan when its bar is clicked. Plans are made again at
/// most twice a second, and only while a feature that needs them is on. Main thread only.
/// </summary>
internal sealed class HudActs
{
    private const double PlanEverySeconds = 0.5;

    private static readonly IReadOnlyDictionary<int, int> NoneWaiting = new Dictionary<int, int>();
    private static readonly IReadOnlyDictionary<long, int> NoneReserved = new Dictionary<long, int>();

    private readonly Action<string> _log;
    private readonly HashSet<string> _moraleSkip = new();
    private readonly BarPlan[] _plans = new BarPlan[StatDelta.Stats];
    private double _nextPlanAt;
    private (int, int, int) _loggedCounts = (-1, -1, -1);

    public HudActs(Action<string> log)
    {
        _log = log;
        BaseScan.SetLog(log);
        QueueReader.SetLog(log);
    }

    /// <summary>What the HUD shows from the plans, or null while nothing that needs them is on.</summary>
    public HudReading? Reading { get; private set; }

    /// <summary>The base as a click sees it: scaled by the player's modifiers, with queued uses taken off. Empty until the first scan.</summary>
    public BaseView View { get; private set; } = BaseView.Empty;

    /// <summary>The last plan of each bar, in the HUD's row order; null before the first.</summary>
    public IReadOnlyList<BarPlan?> Plans => _plans;

    /// <summary>Whether the catalog and the first scan of the base are still being read.</summary>
    public bool IsScanning => !GameCatalog.IsReady || BaseScan.View.Version == 0;

    /// <summary>
    /// One frame's step. The scan and the queue are read while the clicks, the cures or the queue's projection are on;
    /// with none of them, the last scan is dropped.
    /// </summary>
    public void Tick(double now, Vitals? vitals, HudOptions hud, ActOptions acts)
    {
        var needsScan = hud.ClickBars && acts.AnyOn || hud.Cure;
        var needsQueue = needsScan || hud.Projection;

        if (!needsQueue)
        {
            if (Reading != null || View.Version != 0) Drop();

            return;
        }

        var isChinese = GameLanguage.IsChinese();

        if (needsScan) BaseScan.Tick(now, isChinese);
        else if (View.Version != 0) BaseScan.Clear();

        QueueReader.Tick(now);

        if (now < _nextPlanAt) return;

        _nextPlanAt = now + PlanEverySeconds;
        Plan(vitals, hud, acts, needsScan, isChinese);
    }

    /// <summary>A click on a bar: plans it again from what the game has right now and queues the plan. Says how it went in a toast, if asked to.</summary>
    public void Act(int stat, double now, Vitals? vitals, HudOptions hud, ActOptions acts)
    {
        if (!hud.ClickBars || stat < 0 || stat >= StatDelta.Stats) return;

        var isChinese = GameLanguage.IsChinese();

        QueueReader.Refresh();
        QueueReader.Tick(now);
        Plan(vitals, hud, acts, true, isChinese);

        var plan = _plans[stat] ?? BarPlan.Nothing(stat, ActWhy.Scanning, isChinese);

        if (!plan.CanAct)
        {
            Toast(acts, ActTexts.NothingTitle(stat, isChinese), plan.Tip, plan.Why is ActWhy.Off or ActWhy.Full or ActWhy.AtGoal ? Kind.Info : Kind.Warning, "act" + stat);

            return;
        }

        var added = ActionQueue.Queue(plan.Steps, acts.AddToQueue, acts.AddToQueue ? QueueReader.Reserved : NoneReserved, _log);

        if (added == 0)
        {
            Toast(acts, ActTexts.NothingTitle(stat, isChinese), ActTexts.Failed(isChinese), Kind.Error, "act" + stat);

            return;
        }

        if (stat == 1)
        {
            foreach (var step in plan.Steps) _moraleSkip.Add(step.Option.Key);
        }

        _log($"click on {ActTexts.StatName(stat, false)}: queued {ActTexts.List(plan.Steps, false)} ({added} actions)");
        Toast(acts, ActTexts.QueuedTitle(isChinese), ActTexts.StatName(stat, isChinese) + ": " + plan.Tip, Kind.Success, "act" + stat);
        AfterQueue();
    }

    /// <summary>A click on a debuff: queues its first cure in the bag, else the first in the home storage.</summary>
    public void Cure(int buffId, double now, Vitals? vitals, HudOptions hud, ActOptions acts)
    {
        if (!hud.Cure) return;

        var isChinese = GameLanguage.IsChinese();
        var config = GameSingleton.Existing<ConfigManager>();
        var rule = config == null ? null : BuffBook.Rule(config, buffId, isChinese);

        if (rule == null || rule.IsGood) return;

        QueueReader.Refresh();
        QueueReader.Tick(now);
        Plan(vitals, hud, acts, true, isChinese);

        var cure = IsScanning ? null : ActPlanner.Cure(rule.Cures, View, acts.UseHome);

        if (cure == null)
        {
            Toast(acts, ActTexts.NothingTitle(-1, isChinese), ActTexts.NoCure(rule.Name, isChinese), Kind.Warning, "cure" + buffId);

            return;
        }

        var steps = new[] { new PlanStep(cure, 1) };
        var added = ActionQueue.Queue(steps, acts.AddToQueue, acts.AddToQueue ? QueueReader.Reserved : NoneReserved, _log);

        if (added == 0)
        {
            Toast(acts, ActTexts.NothingTitle(-1, isChinese), ActTexts.Failed(isChinese), Kind.Error, "cure" + buffId);

            return;
        }

        _log($"click on buff {buffId}: queued {cure.Key} to cure it");
        Toast(acts, ActTexts.QueuedTitle(isChinese), ActTexts.Cure(cure.Name, rule.Name, isChinese), Kind.Success, "cure" + buffId);
        AfterQueue();
    }

    /// <summary>Plans again at the next frame, after a setting changed.</summary>
    public void Replan() => _nextPlanAt = 0;

    private void AfterQueue()
    {
        BaseScan.Refresh();
        QueueReader.Refresh();
        _nextPlanAt = 0;
    }

    private void Drop()
    {
        BaseScan.Clear();
        Reading = null;
        View = BaseView.Empty;
        _moraleSkip.Clear();
        Array.Clear(_plans, 0, _plans.Length);
    }

    private void Plan(Vitals? vitals, HudOptions hud, ActOptions acts, bool withScan, bool isChinese)
    {
        if (QueueReader.IsIdle) _moraleSkip.Clear();

        var levels = PlayerVitals.ReadLevels() ?? vitals?.Levels;
        var scale = (PlayerVitals.ReadScale() ?? StatScale.None) with { Difficulty = DifficultyHook.Settings };
        var queued = Queued(scale);
        var scanning = IsScanning;

        View = withScan && !scanning ? ViewScale.Of(BaseScan.View, scale, FoodTypes(BaseScan.View), acts.AddToQueue ? QueueReader.ReservedKinds : NoneWaiting) : BaseView.Empty;

        var planLevels = levels == null ? Array.Empty<StatLevel>() : acts.AddToQueue ? After(levels, queued) : levels;
        var wellFedFrom = vitals?.WellFed is { } line ? line.From : (double?)null;
        var canAct = new bool[StatDelta.Stats];
        var tips = new string[StatDelta.Stats];

        for (var stat = 0; stat < StatDelta.Stats; stat++)
        {
            var plan = !acts.IsOn(stat) ? BarPlan.Nothing(stat, ActWhy.Off, isChinese)
                : scanning ? BarPlan.Nothing(stat, ActWhy.Scanning, isChinese)
                : ActPlanner.Of(stat, View, planLevels, wellFedFrom, acts, _moraleSkip, isChinese);

            _plans[stat] = plan;
            canAct[stat] = plan.CanAct;
            tips[stat] = plan.Tip;
        }

        // Logged whenever the counts change, so the log shows the scan inside a save and not only the empty one at the menu.
        var counts = (View.Items.Count, View.Funcs.Count, View.Beds.Count);

        if (!scanning && counts != _loggedCounts)
        {
            _loggedCounts = counts;
            _log($"base scan ready: {counts.Item1} items, {counts.Item2} furniture actions, {counts.Item3} beds");
        }

        var projected = hud.Projection && levels != null ? Projection.Of(Pairs(levels), queued) : null;
        var curable = hud.Cure && !scanning ? Curable(vitals, acts, isChinese) : null;

        Reading = new HudReading(projected, curable, hud.ClickBars && acts.AnyOn ? canAct : null, hud.ClickBars && acts.AnyOn ? tips : null);
    }

    /// <summary>What every waiting action does once the player's modifiers are applied.</summary>
    private static List<StatDelta> Queued(StatScale scale)
    {
        var queued = new List<StatDelta>(QueueReader.Queued.Count);

        foreach (var (delta, food) in QueueReader.Queued) queued.Add(food >= 0 ? scale.Item(delta, food) : scale.Action(delta));

        return queued;
    }

    /// <summary>Where the bars will be once the queue is done, held between empty and the (raised) maximum.</summary>
    private static IReadOnlyList<StatLevel> After(IReadOnlyList<StatLevel> levels, List<StatDelta> queued)
    {
        if (queued.Count == 0) return levels;

        var total = StatDelta.None;

        foreach (var delta in queued) total = total.Plus(delta);

        var after = new StatLevel[levels.Count];

        for (var i = 0; i < levels.Count; i++)
        {
            var max = levels[i].Max + (i == 1 ? total.MaxMorale : i == 2 ? total.MaxStamina : 0);

            after[i] = levels[i].Max > 0 ? new StatLevel(Math.Clamp(levels[i].Value + total[i], 0, Math.Max(0, max)), max) : levels[i];
        }

        return after;
    }

    private static List<(double Now, double Max)> Pairs(IReadOnlyList<StatLevel> levels)
    {
        var pairs = new List<(double Now, double Max)>(levels.Count);

        foreach (var level in levels) pairs.Add((level.Value, level.Max));

        return pairs;
    }

    private static Dictionary<int, int> FoodTypes(BaseView view)
    {
        var types = new Dictionary<int, int>(view.Items.Count);

        foreach (var item in view.Items)
        {
            if (GameCatalog.Item(item.Id) is { } kind) types[item.Id] = kind.SubCategory;
        }

        return types;
    }

    /// <summary>The running debuffs with a cure in the bag or, with UseHome, the home storage.</summary>
    private List<int>? Curable(Vitals? vitals, ActOptions acts, bool isChinese)
    {
        var config = GameSingleton.Existing<ConfigManager>();

        if (vitals == null || config == null) return null;

        var curable = new List<int>();

        foreach (var buff in vitals.Buffs)
        {
            if (curable.Contains(buff.ConfigId)) continue;

            var rule = BuffBook.Rule(config, buff.ConfigId, isChinese);

            if (rule == null || rule.IsGood || rule.Cures.Count == 0) continue;

            if (ActPlanner.Cure(rule.Cures, View, acts.UseHome) != null) curable.Add(buff.ConfigId);
        }

        return curable;
    }

    private enum Kind
    {
        Info,
        Success,
        Warning,
        Error,
    }

    private void Toast(ActOptions acts, string title, string body, Kind kind, string key)
    {
        if (!acts.Toasts) return;

        try
        {
            var request = new ExiledToolkit.ToastRequest(ActTexts.Source, title)
            {
                Body = body,
                Kind = kind switch
                {
                    Kind.Success => ExiledToolkit.ToastKind.Success,
                    Kind.Warning => ExiledToolkit.ToastKind.Warning,
                    Kind.Error => ExiledToolkit.ToastKind.Error,
                    _ => ExiledToolkit.ToastKind.Info,
                },
                Key = "vitalsigns." + key,
            };

            if (ExiledToolkit.Toasts.Show(request).IsNone) _log("a click toast was not shown: " + title);
        }
        catch (Exception ex)
        {
            _log("a click toast failed: " + ex.Message);
        }
    }
}
