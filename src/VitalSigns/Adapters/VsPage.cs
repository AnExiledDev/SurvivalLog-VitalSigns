using System;
using System.IO;
using GameCore.HotUpdate.ReduxUI;
using VitalSigns.Core;

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

    private readonly PageWatch _watch = new();
    private readonly ReplySlot _replies = new();
    private readonly HeldData _held = new();
    private readonly AlertWatch _alerts = new();
    private readonly Action<string> _log;
    private readonly Func<bool> _isOtherFitnessBarIn;
    private string? _script;
    private bool _isWeightLogged;
    private bool _hasSent;
    private bool _isOn = true;
    private bool? _hasOtherFitnessBar;

    public VsPage(Action<string> log, Func<bool> isOtherFitnessBarIn)
    {
        _log = log;
        _isOtherFitnessBarIn = isOtherFitnessBarIn;
    }

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
        }
    }

    public void Tick(double now)
    {
        var reply = _replies.Take();

        if (reply != null) _held.Answered(reply);

        // Off, with nothing on the page: a page never sent anything has nothing to take away. A reply still goes to
        // the watch first, so it is not left waiting on it.
        if (reply == null && !_isOn && (!_hasSent || _held.Holds(PageData.NoPlayer))) return;

        var turn = _watch.Step(now, reply);

        if (turn.LogLine != null) _log(turn.LogLine);

        if (turn.Send == PageSend.Nothing) return;

        var isChinese = GameLanguage.IsChinese();
        var options = Options;
        var vitals = _isOn ? PlayerVitals.Read(options, isChinese) : null;
        var data = PageData.Of(vitals, GameTexts.Names(isChinese), isChinese, options, HasOtherFitnessBar());
        var clock = vitals?.Now ?? PlayerVitals.Now();

        if (_isOn) Warn(vitals, isChinese);

        _hasSent = true;
        var javaScript = turn.Send == PageSend.Install ? PageCalls.Install(Script(), clock, _held.ToInstall(data)) : PageCalls.Check(clock, _held.ToSend(data));

        LogWeightOnce(vitals);

        if (Run(javaScript, now) is { } line) _log(line);
    }

    private void Warn(Vitals? vitals, bool isChinese)
    {
        foreach (var alert in _alerts.Step(vitals?.Levels, vitals?.Fitness, Alerts)) AlertToast.Show(alert, isChinese, _log);
    }

    /// <summary>Looked up at the first reading, once every mod has loaded; mods load in no fixed order.</summary>
    private bool HasOtherFitnessBar()
    {
        if (_hasOtherFitnessBar is { } known) return known;

        _hasOtherFitnessBar = _isOtherFitnessBarIn();

        if (_hasOtherFitnessBar.Value) _log("the Fitness Bar mod is installed too, so there are two Fitness bars; the Fitness hover box says to remove FitnessBar.dll");

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
