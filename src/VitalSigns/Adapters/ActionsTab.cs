using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using GameCore.HotUpdate;
using VitalSigns.Core;
using VitalSigns.Core.Acts;
using VitalSigns.Timers.Adapters;

namespace VitalSigns.Adapters;

/// <summary>
/// The Actions tab in the Mod Manager. Keeps its script in the web UI, sends it what the base has and what each bar's
/// click would do (at most once a second while it is open, and only when that changed), and writes the settings it
/// changes to the config, which applies them at once like a change on the Mods screen. Main thread only.
/// </summary>
internal sealed class ActionsTab
{
    private const double SendEverySeconds = 1;
    private const int MostBest = 12;

    private readonly Action<string> _log;
    private readonly WebPage _page;
    private readonly Dictionary<string, (TabSetting Setting, ConfigEntryBase Entry)> _settings = new(StringComparer.Ordinal);
    private string? _lastJson;
    private double _nextSendAt;
    private bool _isShown;

    public ActionsTab(Action<string> log, IEnumerable<(TabSetting Setting, ConfigEntryBase Entry)> settings)
    {
        _log = log;
        _page = new WebPage(log, "VitalSigns.actions.js", ActionsPage.Receiver, HudCommand.TabPrefix, "Actions");

        foreach (var (setting, entry) in settings) _settings[setting.Name] = (setting, entry);
    }

    /// <summary>VitalSigns/Enabled. Off, the tab is not put back after the page reloads.</summary>
    public bool IsOn { get; set; } = true;

    /// <summary>Whether the tab is open, so the base is read for it even with every click off.</summary>
    public bool IsShown => _isShown && IsOn;

    public void Tick(double now, HudActs acts, ActOptions options)
    {
        if (IsOn) _page.KeepInstalled((float)now);

        if (!IsShown || now < _nextSendAt) return;

        _nextSendAt = now + SendEverySeconds;
        Send(acts, options);
    }

    /// <summary>A message from the tab. Returns true when it was one of the tab's.</summary>
    public bool Handle(HudCommand command, HudActs acts, ActOptions options)
    {
        switch (command)
        {
            case HudCommand.TabNoScript:
                // The page reloaded: whatever was open is gone, and the tab says so again once it is opened.
                _isShown = false;

                if (IsOn) _page.Install();

                return true;
            case HudCommand.TabLoad:
                _isShown = true;
                _lastJson = null;
                Send(acts, options);

                return true;
            case HudCommand.Shown shown:
                _isShown = shown.On;
                _lastJson = null;
                _nextSendAt = 0;

                return true;
            case HudCommand.Set set:
                Set(set.Key, set.Value, acts);

                return true;
            default:
                return false;
        }
    }

    private void Set(string key, string value, HudActs acts)
    {
        if (!_settings.TryGetValue(key, out var found))
        {
            _log("the Actions tab sent a setting it may not change: " + key);

            return;
        }

        var normal = found.Setting.Normal(value);

        if (normal == null)
        {
            _log($"the Actions tab sent a value {key} does not take: {(value.Length > 80 ? value.Substring(0, 80) + "..." : value)}");
            _lastJson = null;
            _nextSendAt = 0;

            return;
        }

        try
        {
            if (found.Entry.GetSerializedValue() != normal) found.Entry.SetSerializedValue(normal);
        }
        catch (Exception ex)
        {
            _log($"could not save {key}: {ex.Message}");
        }

        acts.Replan();
        _lastJson = null;
        _nextSendAt = 0;
    }

    private void Send(HudActs acts, ActOptions options)
    {
        var json = ActionsPage.DataCall(View(acts, options, GameLanguage.IsChinese()));

        if (json == _lastJson) return;

        _lastJson = json;
        _page.Call(json);
    }

    private ActionsView View(HudActs acts, ActOptions options, bool isChinese)
    {
        var settings = new Dictionary<string, string>(_settings.Count, StringComparer.Ordinal);

        foreach (var (name, (_, entry)) in _settings) settings[name] = entry.GetSerializedValue();

        var view = acts.View;
        var config = GameSingleton.Existing<ConfigManager>();
        var items = Items(view, options, config, isChinese);
        var beds = Beds(view, options, config, isChinese);
        var best = new List<string>();

        foreach (var option in MoralePlan.Ranked(ActPlanner.MoraleOptions(view, options), options.AvoidLifeLoss))
        {
            if (best.Count == MostBest) break;

            best.Add(option.Key);
        }

        return new ActionsView(settings, acts.IsScanning, view.AtHome, acts.Plans, items, view.Funcs, beds, best);
    }

    /// <summary>
    /// The items at hand, then every other item kind with a stat gain, so the tab can offer one before the player has
    /// it. Those are greyed out and scaled by the player's modifiers like the ones at hand.
    /// </summary>
    private static List<ActOption> Items(BaseView view, ActOptions options, ConfigManager? config, bool isChinese)
    {
        var items = new List<ActOption>(view.Items);

        if (config == null || !GameCatalog.IsReady) return items;

        var held = new HashSet<int>();

        foreach (var item in view.Items) held.Add(item.Id);

        var named = new HashSet<int>(ActKeys.Items(ActKeys.Join(Keys(options))));
        var scale = PlayerVitals.ReadScale() ?? StatScale.None;
        var why = ActTexts.NotHeld(view.AtHome, isChinese);
        var extra = new List<ActOption>();

        foreach (var (id, kind) in GameCatalog.AllItems)
        {
            if (held.Contains(id)) continue;

            var delta = kind.Delta;

            if (!(delta.Satiety > 0 || delta.Morale > 0 || delta.Stamina > 0 || delta.Life > 0) && !named.Contains(id)) continue;

            var (name, icon) = BuffBook.Item(config, id, isChinese);

            extra.Add(new ActOption(ActKeys.Item(id), ActKind.Item, id, name, icon, scale.Item(delta, kind.SubCategory), ActState.Missing, why));
        }

        extra.Sort((a, b) => a.Id.CompareTo(b.Id));
        items.AddRange(extra);

        return items;
    }

    private static IEnumerable<string> Keys(ActOptions options)
    {
        foreach (var key in options.FoodSkip ?? Array.Empty<string>()) yield return key;

        foreach (var key in options.Routine ?? Array.Empty<string>()) yield return key;

        foreach (var key in options.LifeItems ?? Array.Empty<string>()) yield return key;
    }

    /// <summary>The beds at home, then the bed types the settings give a mode that are not there, so a mode can still be changed back.</summary>
    private static List<Bed> Beds(BaseView view, ActOptions options, ConfigManager? config, bool isChinese)
    {
        var beds = new List<Bed>(view.Beds);
        var types = new HashSet<int>();

        foreach (var bed in view.Beds) types.Add(bed.ConfigId);

        foreach (var type in SleepPlan.ParseModes(options.BedModes).Keys)
        {
            if (types.Contains(type)) continue;

            var name = config != null && config._Config_Furniture_Dict.TryGetValue(type, out var row) && row != null ? BuffBook.Text(config, row.Name, isChinese) : null;

            beds.Add(new Bed(0, type, name ?? "#" + type, null, null, null, 0, false));
        }

        return beds;
    }
}
