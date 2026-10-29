using System.Text.Json;
using VitalSigns.Core;
using Xunit;

namespace VitalSigns.Tests;

public class PageCallsTests
{
    /// <summary>A JSON string literal is a JavaScript one, so a text that survives a JSON read survives the page.</summary>
    [Theory]
    [InlineData("var a = 1;")]
    [InlineData("el.setAttribute(\"data-interactive\", '');")]
    [InlineData("var re = /CoreUI1\\.html/i;")]
    [InlineData("two\nlines\r\tand a tab")]
    [InlineData("ends with a backslash\\")]
    [InlineData("line\u2028separator\u2029")]
    public void The_page_script_reaches_the_page_exactly_as_written(string script)
    {
        var literal = PageCalls.Quote(script);

        Assert.Equal(script, JsonSerializer.Deserialize<string>(literal));
        Assert.DoesNotContain('\n', literal);
        Assert.DoesNotContain('\u2028', literal);
        Assert.DoesNotContain('\u2029', literal);
    }

    [Fact]
    public void The_install_call_carries_the_whole_script_and_the_reading()
    {
        const string script = "(function(){window.__slvitals={apply:function(){return \"ok\";}};})();\n// last line";
        var call = PageCalls.Install(script, 3600, "{zh:false}");

        Assert.Contains(PageCalls.Quote(script), call);
        Assert.Contains("w.__slvitals.apply(3600,{zh:false})", call);
    }

    /// <summary>The clock goes every check; the reading only when the page does not already hold it.</summary>
    [Fact]
    public void A_check_carries_the_clock_and_the_reading_only_when_given_one()
    {
        Assert.Contains("w.__slvitals.apply(7200))", PageCalls.Check(7200, null));
        Assert.Contains("w.__slvitals.apply(7200,null))", PageCalls.Check(7200, PageData.NoPlayer));
    }
}
