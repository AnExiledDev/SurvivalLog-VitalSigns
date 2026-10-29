using System;
using System.IO;
using GameCore.HotUpdate.ReduxUI;
using VitalSigns.Core;

namespace VitalSigns.Adapters;

/// <summary>
/// The plugin's contact with the game's web view: it runs JavaScript there and reads the answer. A check carries the
/// game clock every time and the player's reading only when it changed; nothing in the game's state is written. All
/// members run on Unity's main thread; the web view's callback only stores the answer, and the next frame hands it on.
/// </summary>
internal sealed class VsPage
{
    private const string ScriptResource = "VitalSigns.vitalsigns.js";

    private readonly PageWatch _watch = new();
    private readonly ReplySlot _replies = new();
    private readonly HeldData _held = new();
    private readonly Action<string> _log;
    private string? _script;
    private bool _isWeightLogged;

    public VsPage(Action<string> log)
    {
        _log = log;
    }

    public void Tick(double now)
    {
        var reply = _replies.Take();

        if (reply != null) _held.Answered(reply);

        var turn = _watch.Step(now, reply);

        if (turn.LogLine != null) _log(turn.LogLine);

        if (turn.Send == PageSend.Nothing) return;

        var vitals = PlayerVitals.Read();
        var isChinese = GameLanguage.IsChinese();
        var data = PageData.Of(vitals, GameTexts.Names(isChinese), isChinese);
        var clock = vitals?.Now ?? PlayerVitals.Now();
        var javaScript = turn.Send == PageSend.Install ? PageCalls.Install(Script(), clock, _held.ToInstall(data)) : PageCalls.Check(clock, _held.ToSend(data));

        LogWeightOnce(vitals);

        if (Run(javaScript, now) is { } line) _log(line);
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
