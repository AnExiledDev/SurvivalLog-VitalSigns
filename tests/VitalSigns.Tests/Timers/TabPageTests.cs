using System.Collections.Generic;
using System.Text.Json;
using VitalSigns.Timers.Core;
using Xunit;

namespace VitalSigns.Tests.Timers;

public class TabPageTests
{
    private const string Categories = "[0,0,0,0,0,0,0]";

    [Fact]
    public void A_good_save_is_accepted_as_sent()
    {
        var save = Assert.IsType<SaveRequest.Accepted>(TabPage.ReadSave("{\"good\":2,\"bad\":0.5,\"categories\":[0,3,0,0,0,0,0],\"buffs\":{\"11\":4}}"));

        Assert.Equal(2, save.Good);
        Assert.Equal(0.5, save.Bad);
        Assert.Null(save.Categories[0]);
        Assert.Equal(3, save.Categories[1]);
        Assert.Equal(new Dictionary<int, double> { [11] = 4 }, save.Buffs);
        Assert.Empty(save.Notes);
    }

    [Fact]
    public void A_save_with_values_out_of_range_is_held_to_it_and_says_so()
    {
        var save = Assert.IsType<SaveRequest.Accepted>(TabPage.ReadSave("{\"good\":900,\"bad\":0,\"categories\":[0,-1,500,0,0,0,0],\"buffs\":{\"11\":0.0001}}"));

        Assert.Equal(Limits.Max, save.Good);
        Assert.Equal(Limits.Min, save.Bad);
        Assert.Null(save.Categories[1]);
        Assert.Equal(Limits.Max, save.Categories[2]);
        Assert.Equal(Limits.Min, save.Buffs[11]);
        Assert.Equal(5, save.Notes.Count);
    }

    [Theory]
    [InlineData("{\"bad\":1,\"categories\":" + Categories + "}")]
    [InlineData("{\"good\":\"2\",\"bad\":1,\"categories\":" + Categories + "}")]
    [InlineData("{\"good\":1,\"bad\":1}")]
    [InlineData("{\"good\":1,\"bad\":1,\"categories\":[0,0,0]}")]
    [InlineData("{\"good\":1,\"bad\":1,\"categories\":[0,0,0,0,0,0,\"x\"]}")]
    [InlineData("[1]")]
    [InlineData("{\"good\":1,")]
    public void A_save_missing_a_part_is_refused_and_changes_nothing(string json)
    {
        Assert.IsType<SaveRequest.Refused>(TabPage.ReadSave(json));
    }

    [Fact]
    public void A_save_without_buffs_clears_them()
    {
        var save = Assert.IsType<SaveRequest.Accepted>(TabPage.ReadSave("{\"good\":1,\"bad\":1,\"categories\":" + Categories + "}"));

        Assert.Empty(save.Buffs);
    }

    [Fact]
    public void The_data_says_which_multiplier_each_buff_uses()
    {
        var categories = new double?[MultiplierPlan.CategoryCount];

        categories[2] = 3;

        var plan = new MultiplierPlan(2, 0.5, categories, new Dictionary<int, double> { [30] = 7 });
        var buffs = new[]
        {
            new BuffEntry(10, true, 1, 3600, "Satiety", "饱腹", "Full", "饱"),
            new BuffEntry(20, false, 2, 600, "Lonely", "孤独", "", ""),
            new BuffEntry(30, false, 5, 60, "Bleeding", "受伤流血", "", ""),
        };

        using var doc = JsonDocument.Parse(TabPage.DataJson(plan, buffs, 48, new[] { "a problem" }));
        var root = doc.RootElement;
        var list = root.GetProperty("buffs");

        Assert.Equal(2, root.GetProperty("good").GetDouble());
        Assert.Equal(0, root.GetProperty("categories")[0].GetDouble());
        Assert.Equal(3, root.GetProperty("categories")[2].GetDouble());
        Assert.Equal(48, root.GetProperty("timeScale").GetInt32());
        Assert.Equal("a problem", root.GetProperty("problems")[0].GetString());

        Assert.Equal("global", list[0].GetProperty("from").GetString());
        Assert.Equal(2, list[0].GetProperty("mult").GetDouble());
        Assert.Equal(0, list[0].GetProperty("own").GetDouble());
        Assert.Equal("category", list[1].GetProperty("from").GetString());
        Assert.Equal(3, list[1].GetProperty("mult").GetDouble());
        Assert.Equal("buff", list[2].GetProperty("from").GetString());
        Assert.Equal(7, list[2].GetProperty("own").GetDouble());
        Assert.Equal("饱腹", list[0].GetProperty("nameZh").GetString());
    }

    [Fact]
    public void Game_text_cannot_end_the_script_it_is_sent_in()
    {
        var buffs = new[] { new BuffEntry(1, true, 0, 60, "</script><b>x", "a\u2028b", "", "") };
        var call = TabPage.DataCall(MultiplierPlan.Unchanged, buffs, 1, new string[0]);

        Assert.DoesNotContain("<", call);
        Assert.DoesNotContain("\u2028", call);
        Assert.StartsWith("window.__slvtimers&&window.__slvtimers.data(", call);
    }

    [Fact]
    public void The_save_reply_carries_its_message_as_a_safe_string()
    {
        Assert.Equal("window.__slvtimers&&window.__slvtimers.saved(true,\"\")", TabPage.SavedCall(true, ""));
        Assert.Contains("saved(false,", TabPage.SavedCall(false, "it's \"bad\""));
    }
}
