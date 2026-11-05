using System.Collections.Generic;
using Jint;
using VitalSigns.Timers.Core;
using Xunit;

namespace VitalSigns.Tests.Timers;

/// <summary>
/// timers.js exactly as the plugin embeds it, with Exiled's Toolkit's page API loaded first. The tab and the plugin
/// only find each other through the mod's id, the message prefix and the receiver name, so these check the script and
/// the C# side agree on all three.
/// </summary>
public class TimersScriptTests
{
    private const string Bridge = "var window = this; var sent = []; window.vuplex = { postMessage: function (m) { sent.push(m); } };";

    private readonly Engine _engine = new();

    public TimersScriptTests()
    {
        _engine.Execute(Bridge);
        _engine.Execute(Resource("ExiledToolkit.api.js"));
        _engine.Execute(Resource("VitalSigns.timers.js"));
    }

    /// <summary>The game can run the tab's script before the Toolkit's: the tab still asks for its data, once the Toolkit is there.</summary>
    [Fact]
    public void A_tab_loaded_before_the_toolkit_asks_for_its_data_once_the_toolkit_loads()
    {
        var engine = new Engine();

        engine.Execute(Bridge);
        engine.Execute(Resource("VitalSigns.timers.js"));

        Assert.Equal(0, engine.Evaluate("sent.length").AsNumber());

        engine.Execute(Resource("ExiledToolkit.api.js"));

        Assert.Equal(1, engine.Evaluate("sent.length").AsNumber());
        Assert.IsType<PageCommand.Load>(PageCommand.Parse(engine.Evaluate("sent[0]").AsString()));
    }

    [Fact]
    public void The_tab_goes_on_the_vital_signs_page_in_the_mod_manager()
    {
        Assert.Equal("com.dev.survivallog.vitalsigns", Text("window.__modmanagerTabs[0].guid"));
        Assert.Equal("timers", Text("window.__modmanagerTabs[0].id"));
    }

    [Fact]
    public void The_tab_asks_for_its_data_in_a_message_the_plugin_takes()
    {
        var message = Text("sent[0]");

        Assert.True(PageCommand.IsOurs(message));
        Assert.IsType<PageCommand.Load>(PageCommand.Parse(message));
    }

    [Fact]
    public void The_plugin_answers_reach_the_script()
    {
        var plan = new MultiplierPlan(2, 0.5, new double?[7], new Dictionary<int, double>());
        var buffs = new[] { new BuffEntry(11, true, 1, 3600, "Satiety", "饱腹", "", "") };

        Assert.True(Is("window.__slvtimers && typeof window.__slvtimers.data === 'function'"));

        _engine.Execute(TabPage.DataCall(plan, buffs, 60, new List<string>()));
        _engine.Execute(TabPage.SavedCall(true, ""));
    }

    [Fact]
    public void Sending_the_script_again_does_not_add_a_second_tab()
    {
        _engine.Execute(Resource("VitalSigns.timers.js"));

        Assert.Equal(1, _engine.Evaluate("window.__modmanagerTabs.length").AsNumber());
    }

    private string Text(string expression) => _engine.Evaluate($"String({expression})").AsString();

    private bool Is(string expression) => _engine.Evaluate($"!!({expression})").AsBoolean();

    private static string Resource(string name)
    {
        using var stream = typeof(TimersScriptTests).Assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException(name + " is not embedded in the test assembly");
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }
}
