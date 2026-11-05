using System;
using System.IO;
using GameCore.HotUpdate.ReduxUI;
using VitalSigns.Adapters;
using VitalSigns.Timers.Core;

namespace VitalSigns.Timers.Adapters;

/// <summary>
/// A tab's script in the game's web UI: the Timers tab or the Actions tab. The web view can reload at any time and
/// lose it, so the page is asked every couple of seconds whether it still has it, and is sent it again when it says no.
/// </summary>
internal sealed class WebPage
{
    private const float CheckSeconds = 2f;

    private readonly Action<string> _log;
    private readonly string _resource;
    private readonly string _check;
    private readonly string _name;
    private string? _script;
    private float _nextCheckAt;
    private bool _hasReportedRunFailure;

    /// <summary>The Timers tab.</summary>
    public WebPage(Action<string> log) : this(log, "VitalSigns.timers.js", "window.__slvtimers", PageCommand.Prefix, "Timers")
    {
    }

    /// <param name="resource">The embedded script.</param>
    /// <param name="global">What the script sets on the window, which says it is there.</param>
    /// <param name="prefix">The tab's message prefix; the page posts its "noscript" when the script is gone.</param>
    public WebPage(Action<string> log, string resource, string global, string prefix, string name)
    {
        _log = log;
        _resource = resource;
        _name = name;
        _check = global + "||window.vuplex&&window.vuplex.postMessage('" + prefix + "noscript')";
    }

    public void KeepInstalled(float now)
    {
        if (now < _nextCheckAt) return;

        _nextCheckAt = now + CheckSeconds;
        Run(_check);
    }

    public void Install()
    {
        if (Run(Script())) _log(_name + " tab script sent to the web UI");
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

    private string Script() => _script ??= Resource(_resource);

    private static string Resource(string name)
    {
        using var stream = typeof(WebPage).Assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException(name + " is not embedded");
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }
}
