using Jint;

namespace VitalSigns.Tests;

/// <summary>
/// vitalsigns.js exactly as the plugin embeds it, run in Jint on top of FakePage.js, a page with only the parts of
/// CoreUI1 the script touches. Tests talk to it in JavaScript expressions and read back plain strings, numbers and
/// booleans.
/// </summary>
internal sealed class PageScript
{
    private readonly Engine _engine = new();

    private PageScript(string pageSpec)
    {
        _engine.Execute(Resource("VitalSigns.Tests.FakePage.js"));
        _engine.Execute("var page = buildPage(" + pageSpec + ");");
        _engine.Execute(Resource("VitalSigns.vitalsigns.js"));
    }

    /// <summary>The script in a page drawn from a buildPage spec; the page is the global <c>page</c>.</summary>
    public static PageScript On(string pageSpec = "{}") => new(pageSpec);

    /// <summary>What the plugin's check call does once it has found the frame: the clock, and a reading when it changed.</summary>
    public string Apply(int now, string? data = null) => Text(data == null ? $"window.__slvitals.apply({now})" : $"window.__slvitals.apply({now},{data})");

    /// <summary>The script sent again, as the plugin does when the page says it is missing.</summary>
    public void Reinstall() => _engine.Execute(Resource("VitalSigns.vitalsigns.js"));

    public void Run(string statements) => _engine.Execute(statements);

    public string Text(string expression) => _engine.Evaluate($"String({expression})").AsString();

    public bool Is(string expression) => _engine.Evaluate($"!!({expression})").AsBoolean();

    public double Number(string expression) => _engine.Evaluate($"Number({expression})").AsNumber();

    private static string Resource(string name)
    {
        using var stream = typeof(PageScript).Assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException(name + " is not embedded in the test assembly");
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }
}
