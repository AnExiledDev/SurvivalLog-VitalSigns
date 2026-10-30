using System;
using System.IO;
using GameCore.HotUpdate.ReduxUI;
using VitalSigns.Adapters;
using VitalSigns.Timers.Core;

namespace VitalSigns.Timers.Adapters;

/// <summary>
/// The tab's script in the game's web UI. The web view can reload at any time and lose it, so the page is asked every
/// couple of seconds whether it still has it, and is sent it again when it says no.
/// </summary>
internal sealed class WebPage
{
    private const float CheckSeconds = 2f;
    private const string TabResource = "VitalSigns.timers.js";
    private const string NoScriptReply = "window.vuplex&&window.vuplex.postMessage('" + PageCommand.Prefix + "noscript')";

    private readonly Action<string> _log;
    private string? _script;
    private float _nextCheckAt;
    private bool _hasReportedRunFailure;

    public WebPage(Action<string> log)
    {
        _log = log;
    }

    public void KeepInstalled(float now)
    {
        if (now < _nextCheckAt) return;

        _nextCheckAt = now + CheckSeconds;
        Run("window.__slvtimers||" + NoScriptReply);
    }

    public void Install()
    {
        if (Run(Script())) _log("tab script sent to the web UI");
    }

    public void Call(string javaScript) => Run(javaScript);

    private bool Run(string javaScript)
    {
        var webView = GameSingleton.Existing<ReduxUISystem>()?.GetWebUILayer()?.canvasWebViewPrefab?.WebView;

        if (webView == null) return false;

        try
        {
            webView.ExecuteJavaScript(javaScript, null);

            return true;
        }
        catch (Exception ex)
        {
            if (!_hasReportedRunFailure) _log("could not run script in the web UI (reported once): " + ex.Message);

            _hasReportedRunFailure = true;

            return false;
        }
    }

    private string Script() => _script ??= Resource(TabResource);

    private static string Resource(string name)
    {
        using var stream = typeof(WebPage).Assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException(name + " is not embedded");
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }
}
