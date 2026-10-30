using System.Collections.Generic;
using VitalSigns.Timers.Core;
using Xunit;

namespace VitalSigns.Tests.Timers;

public class OverrideFileTests
{
    [Fact]
    public void A_missing_file_is_no_overrides_and_no_problem()
    {
        var read = OverrideFile.Read(null);

        Assert.Empty(read.Buffs);
        Assert.Empty(read.Problems);
    }

    [Fact]
    public void Good_entries_are_read_with_comments_and_trailing_commas_allowed()
    {
        var read = OverrideFile.Read("{\n // a note\n \"buffs\": { \"200810\": 2, \"1043\": 0.5, }\n}");

        Assert.Equal(2, read.Buffs[200810]);
        Assert.Equal(0.5, read.Buffs[1043]);
        Assert.Empty(read.Problems);
    }

    [Fact]
    public void A_broken_file_reads_as_no_overrides_with_a_problem()
    {
        var read = OverrideFile.Read("{ \"buffs\": { \"1\": 2 ");

        Assert.Empty(read.Buffs);
        Assert.Single(read.Problems);
    }

    [Fact]
    public void A_file_without_buffs_reads_as_no_overrides_with_a_problem()
    {
        var read = OverrideFile.Read("[1, 2]");

        Assert.Empty(read.Buffs);
        Assert.Single(read.Problems);
    }

    [Fact]
    public void One_bad_entry_is_skipped_without_dropping_the_rest()
    {
        var read = OverrideFile.Read("{\"buffs\": {\"abc\": 2, \"-4\": 2, \"0\": 2, \"5\": \"two\", \"6\": -1, \"7\": 3, \"8\": 0}}");

        Assert.Equal(new Dictionary<int, double> { [7] = 3 }, read.Buffs);
        Assert.Equal(5, read.Problems.Count);
    }

    [Fact]
    public void An_entry_outside_the_range_is_held_to_it_and_reported()
    {
        var read = OverrideFile.Read("{\"buffs\": {\"9\": 5000, \"10\": 0.001}}");

        Assert.Equal(Limits.Max, read.Buffs[9]);
        Assert.Equal(Limits.Min, read.Buffs[10]);
        Assert.Equal(2, read.Problems.Count);
    }

    [Fact]
    public void Written_files_are_sorted_rounded_and_read_back_the_same()
    {
        var text = OverrideFile.Write(new Dictionary<int, double> { [300] = 1.234567, [20] = 2 });

        Assert.True(text.IndexOf("\"20\"", System.StringComparison.Ordinal) < text.IndexOf("\"300\"", System.StringComparison.Ordinal));
        Assert.EndsWith("\n", text);

        var read = OverrideFile.Read(text);

        Assert.Equal(new Dictionary<int, double> { [20] = 2, [300] = 1.2346 }, read.Buffs);
        Assert.Empty(read.Problems);
    }
}
