using System.Globalization;
using ExiledToolkit.Web;

namespace VitalSigns.Core;

/// <summary>
/// The JavaScript the plugin runs in the game's web view. The web view runs it in Root.html; the HUD's stat bars live in
/// the CoreUI1 page, an iframe inside Root, so every call first finds that frame and then talks to vitalsigns.js inside
/// it. Each call answers with one of the <see cref="PageAnswer"/> strings.
/// </summary>
public static class PageCalls
{
    private const string Api = "w.__slvitals";

    /// <summary>The frame search the other CoreUI1 mods use: the iframe whose page is CoreUI1.html.</summary>
    private const string FindFrame =
        "var fs=document.querySelectorAll('iframe'),w=null;" +
        "for(var i=0;i<fs.length;i++){var c=fs[i].contentWindow;if(!c)continue;" +
        "try{if(!/CoreUI1\\.html/i.test(String(c.location)))continue;}catch(e){continue;}w=c;break;}" +
        "if(!w)return '" + PageAnswer.NoPage + "';";

    /// <summary>
    /// Cheap enough to send every couple of seconds: says whether the script is there and hands it the game clock.
    /// <paramref name="data"/> is a <see cref="PageData"/> reading, or null when the page already has this one.
    /// </summary>
    public static string Check(int now, string? data) =>
        Wrap($"if(!{Api})return '{PageAnswer.Missing}';{Apply(now, data)}");

    /// <summary>Evaluates the script inside the CoreUI1 frame, so it sees that page's Vue app, styles and click-through.</summary>
    public static string Install(string script, int now, string data) =>
        Wrap($"w.eval({JsText.Literal(script)});if(!{Api})return '{PageAnswer.InstallLeftNothing}';{Apply(now, data)}");

    /// <summary>Opens or closes the HUD layout editor; answers "ok", or Missing when the page lost the script.</summary>
    public static string Edit(bool on) => Wrap($"if(!{Api})return '{PageAnswer.Missing}';return String({Api}.edit({(on ? "true" : "false")}));");

    private static string Apply(int now, string? data) => $"return String({Api}.apply({Number(now)}{(data == null ? "" : "," + data)}));";

    private static string Wrap(string body) => "(function(){try{" + FindFrame + body + "}catch(e){return 'error: '+e;}})()";

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
}
