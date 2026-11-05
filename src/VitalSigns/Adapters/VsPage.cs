using System;
using System.IO;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using GameCore.HotUpdate.ReduxUI;
using VitalSigns.Core;
using VitalSigns.Core.Acts;

namespace VitalSigns.Adapters;

/// <summary>
/// The plugin's contact with the game's web view: it runs JavaScript there and reads the answer. A check carries the
/// game clock every time and the player's reading only when it changed; nothing in the game's state is written. All
/// members run on Unity's main thread; the web view's callback only stores the answer, and the next frame hands it on.
/// The settings can change at any time: the next reading carries them, and the page redraws to match. Turned off, the
/// page is sent an empty reading, which takes away everything drawn, and then nothing more until it is turned on.
/// </summary>
internal sealed class VsPage
{
    private const string ScriptResource = "VitalSigns.vitalsigns.js";
    private const double HeroEverySeconds = 1;

    private readonly PageWatch _watch = new();
    private readonly ReplySlot _replies = new();
    private readonly HeldData _held = new();
    private readonly AlertWatch _alerts = new();
    private readonly Action<string> _log;
    private readonly Func<bool> _isOtherFitnessBarIn;
    private readonly HudActs _acts;
    private Vitals? _vitals;
    private double _nextHeroAt;
    private bool _isEditing;
    private string? _script;
    private bool _isWeightLogged;
    private bool _hasSent;
    private bool _isOn = true;
    private bool? _hasOtherFitnessBar;

    public VsPage(Action<string> log, Func<bool> isOtherFitnessBarIn)
    {
        _log = log;
        _isOtherFitnessBarIn = isOtherFitnessBarIn;
        _acts = new HudActs(log);
    }

    /// <summary>The HUD section: the bars' look, the buff tray, and what clicks on them do.</summary>
    public HudOptions Hud { get; set; } = new();

    /// <summary>The Actions section: what a click on each bar queues.</summary>
    public ActOptions Acts { get; set; } = new();

    /// <summary>The bar clicks, for the test buttons.</summary>
    public HudActs Clicks => _acts;

    /// <summary>The Actions tab in the Mod Manager.</summary>
    public ActionsTab? Tab { get; set; }

    /// <summary>Saves a shift-click on a buff: pins it or unpins it.</summary>
    public Action<int> Favourite { get; set; } = _ => { };

    /// <summary>Saves what the layout editor sends when it closes with Save.</summary>
    public Action<HudCommand.Layout> SaveLayout { get; set; } = _ => { };

    public Options Options { get; set; } = new();

    public AlertOptions Alerts { get; set; } = new();

    /// <summary>VitalSigns/Enabled. Turned on again, the warnings start fresh: a stat already low then does not warn.</summary>
    public bool IsOn
    {
        get => _isOn;
        set
        {
            if (value && !_isOn) _alerts.Reset();

            _isOn = value;

            if (Tab != null) Tab.IsOn = value;

            if (!value) DifficultyHook.HeroId = 0;
        }
    }

    public void Tick(double now)
    {
        var reply = _replies.Take();

        if (reply != null) _held.Answered(reply);

        StepActs(now);

        // Off, with nothing on the page: a page never sent anything has nothing to take away. A reply still goes to
        // the watch first, so it is not left waiting on it.
        if (reply == null && !_isOn && (!_hasSent || _held.Holds(PageData.NoPlayer))) return;

        var turn = _watch.Step(now, reply);

        if (turn.LogLine != null) _log(turn.LogLine);

        if (turn.Send == PageSend.Nothing) return;

        var isChinese = GameLanguage.IsChinese();
        var options = Options;
        var vitals = _isOn ? PlayerVitals.Read(options, isChinese) : null;
        var data = PageData.Of(vitals, GameTexts.Names(isChinese), isChinese, options, HasOtherFitnessBar(), _isOn ? Hud : null, _isOn ? _acts.Reading : null);
        var clock = vitals?.Now ?? PlayerVitals.Now();

        if (_isOn) Warn(vitals, isChinese);

        _vitals = vitals;

        _hasSent = true;

        // A fresh install is a fresh page, with no editor open, whatever the last one had.
        if (turn.Send == PageSend.Install) _isEditing = false;

        var javaScript = turn.Send == PageSend.Install ? PageCalls.Install(Script(), clock, _held.ToInstall(data)) : PageCalls.Check(clock, _held.ToSend(data));

        LogWeightOnce(vitals);

        if (Run(javaScript, now) is { } line) _log(line);
    }

    /// <summary>A message from the HUD or the Actions tab.</summary>
    public void Handle(HudCommand command, double now)
    {
        switch (command)
        {
            case HudCommand.Act act:
                if (_isOn) _acts.Act(act.Stat, now, Fresh(), Hud, Acts);

                return;
            case HudCommand.Cure cure:
                if (_isOn) _acts.Cure(cure.BuffId, now, Fresh(), Hud, Acts);

                return;
            case HudCommand.Favourite favourite:
                if (_isOn) Favourite(favourite.BuffId);

                return;
            case HudCommand.Layout layout:
                _isEditing = false;
                SaveLayout(layout);

                return;
            case HudCommand.Edit { On: false }:
                _isEditing = false;

                return;
            case HudCommand.Edit:
                Edit(true);

                return;
            case HudCommand.Unknown unknown:
                _log("a message the mod does not know: " + (unknown.Message.Length > 80 ? unknown.Message.Substring(0, 80) + "..." : unknown.Message));

                return;
            default:
                if (Tab != null && !Tab.Handle(command, _acts, Acts)) _log("a message the mod does not know: " + command);

                return;
        }
    }

    /// <summary>The layout editor key: opens the editor, or closes it without saving.</summary>
    public void ToggleEdit() => Edit(!_isEditing);

    /// <summary>Opens or closes the HUD layout editor. The page answers when it has no HUD to edit; the next check puts the script back.</summary>
    public void Edit(bool on)
    {
        if (!_isOn) return;

        var webView = GameSingleton.Existing<ReduxUISystem>()?.GetWebUILayer()?.canvasWebViewPrefab?.WebView;

        if (webView == null) return;

        _isEditing = on;

        try
        {
            webView.ExecuteJavaScript(PageCalls.Edit(on), (Action<string>)(result =>
            {
                if (result == "ok") return;

                _isEditing = false;
                _log("the layout editor did not open: " + result);
            }));
        }
        catch (Exception ex)
        {
            _isEditing = false;
            _log("could not open the layout editor: " + ex.Message);
        }
    }

    /// <summary>
    /// The clicks' plans and the Actions tab every frame, and which character the stat difficulty scales, once a
    /// second while a multiplier is set. These run even when the page has nothing new to send.
    /// </summary>
    private void StepActs(double now)
    {
        if (_isOn)
        {
            _acts.Tick(now, _vitals, Hud, Acts, Tab?.IsShown == true);

            if (now >= _nextHeroAt && !DifficultyHook.Settings.IsOff)
            {
                _nextHeroAt = now + HeroEverySeconds;
                DifficultyHook.HeroId = GameSingleton.Existing<BattleLogicWorld>()?._AgentManager?.GetLeadingRole()?.InstanceId ?? 0;
            }
        }

        Tab?.Tick(now, _acts, Acts);
    }

    /// <summary>The stats as they are this moment, for a click: the last reading can be a second old.</summary>
    private Vitals? Fresh() => PlayerVitals.Read(Options, GameLanguage.IsChinese()) ?? _vitals;

    private void Warn(Vitals? vitals, bool isChinese)
    {
        foreach (var alert in _alerts.Step(vitals?.Levels, vitals?.Fitness, Alerts)) AlertToast.Show(alert, isChinese, _log);
    }

    /// <summary>Looked up at the first reading, once every mod has loaded; mods load in no fixed order.</summary>
    private bool HasOtherFitnessBar()
    {
        if (_hasOtherFitnessBar is { } known) return known;

        _hasOtherFitnessBar = _isOtherFitnessBarIn();

        if (_hasOtherFitnessBar.Value) _log("an old Fitness Bar (before 0.1.2) is installed too, so there are two Fitness bars; the Fitness hover box says to remove FitnessBar.dll or update it");

        return _hasOtherFitnessBar.Value;
    }

    /// <summary>Sends the call; returns a line for the log when it could not leave.</summary>
    private string? Run(string javaScript, double now)
    {
        var webView = GameSingleton.Existing<ReduxUISystem>()?.GetWebUILayer()?.canvasWebViewPrefab?.WebView;

        if (webView == null)
        {
            _held.Answered(PageAnswer.NoWebView);

            return _watch.NotSent(now);
        }

        var call = _replies.Open();

        try
        {
            webView.ExecuteJavaScript(javaScript, (Action<string>)(result => _replies.Offer(call, result ?? "")));

            return null;
        }
        catch (Exception ex)
        {
            _held.Answered(PageAnswer.RunFailedPrefix + ex.Message);

            return _watch.RunFailed(now, ex.Message);
        }
    }

    /// <summary>The bag's numbers once per session, raw, so the page's kilogram reading can be checked against them.</summary>
    private void LogWeightOnce(Vitals? vitals)
    {
        if (_isWeightLogged || vitals?.Weight is not { } weight) return;

        _isWeightLogged = true;
        _log($"Carry weight as the game keeps it: carried {weight.Carried}, max {weight.Max}, overweight above {weight.Overweight}");
    }

    /// <summary>Read on first need: most launches send it once, and some never reach a page that wants it.</summary>
    private string Script()
    {
        if (_script != null) return _script;

        using var stream = typeof(VsPage).Assembly.GetManifestResourceStream(ScriptResource) ?? throw new InvalidOperationException(ScriptResource + " is not embedded");
        using var reader = new StreamReader(stream);

        return _script = reader.ReadToEnd();
    }
}
