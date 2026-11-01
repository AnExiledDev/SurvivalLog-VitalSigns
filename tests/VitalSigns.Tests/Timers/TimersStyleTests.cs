using System.Text.RegularExpressions;
using Xunit;

namespace VitalSigns.Tests.Timers;

/// <summary>
/// The Timers tab is drawn inside the Mod Manager, whose own .chips and .list rules add padding, wrapping and a bottom
/// border. On the tab that padded the filter buttons into tall boxes and opened a gap above the sticky table header
/// where scrolled rows showed through, and its category names were cut off with "...".
/// </summary>
public class TimersStyleTests
{
    private static readonly string Script = Resource("VitalSigns.timers.js");

    [Theory]
    [InlineData(".chips")]
    [InlineData(".list")]
    public void The_tab_undoes_the_mod_managers_padding(string selector)
    {
        Assert.Contains("padding:0", Rule(selector));
    }

    [Fact]
    public void The_filter_buttons_stay_on_one_line_without_the_mod_managers_gap()
    {
        var chips = Rule(".chips");

        Assert.Contains("flex-wrap:nowrap", chips);
        Assert.Contains("gap:0", chips);
    }

    [Fact]
    public void Category_names_wrap_instead_of_being_cut_off()
    {
        var label = Rule(".mult .k");

        Assert.DoesNotContain("ellipsis", label);
        Assert.DoesNotContain("nowrap", label);
    }

    private static string Rule(string selector)
    {
        var match = Regex.Match(Script, "'" + Regex.Escape(selector) + "\\{([^}]*)\\}'");

        Assert.True(match.Success, selector + " has no rule in timers.js");

        return match.Groups[1].Value;
    }

    private static string Resource(string name)
    {
        using var stream = typeof(TimersStyleTests).Assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException(name + " is not embedded in the test assembly");
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }
}
