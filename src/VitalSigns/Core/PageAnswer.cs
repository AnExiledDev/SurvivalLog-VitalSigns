namespace VitalSigns.Core;

/// <summary>What the page calls answer. Anything else the page says is passed on to the log as it came.</summary>
public static class PageAnswer
{
    /// <summary>The HUD's stat bars show what the plugin handed over.</summary>
    public const string Ok = "ok";

    /// <summary>The script is in the page, but the HUD has no stat bars right now.</summary>
    public const string NoBar = "nobar";

    /// <summary>The script is in the page, but there is no player to read, so everything the mod drew is taken away.</summary>
    public const string NoPlayer = "noplayer";

    /// <summary>Root.html has no CoreUI1 frame: the main menu, or a load in progress.</summary>
    public const string NoPage = "nopage";

    /// <summary>The CoreUI1 frame is there but vitalsigns.js is not: the first visit, or the page reloaded.</summary>
    public const string Missing = "missing";

    /// <summary>The script ran but did not leave its entry point behind.</summary>
    public const string InstallLeftNothing = "install left no script";

    /// <summary>Not from the page: the game's web view is not up yet, so nothing was sent.</summary>
    public const string NoWebView = "no web view";

    /// <summary>Not from the page: nothing came back in time.</summary>
    public const string NoAnswer = "no answer";

    /// <summary>Not from the page: starts every answer where the web view threw instead of taking the call; the rest is its message.</summary>
    public const string RunFailedPrefix = "run failed: ";
}
