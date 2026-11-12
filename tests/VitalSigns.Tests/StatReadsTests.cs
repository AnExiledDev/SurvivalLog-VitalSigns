using System.Collections.Generic;
using VitalSigns.Core;
using Xunit;

namespace VitalSigns.Tests;

/// <summary>
/// The maximums are whole numbers in the game, and its float read of one gives 0. A maximum of 0 made every bar
/// click say there was nothing to do, and silenced the low warnings, so the maximums must come from the int read.
/// </summary>
public class StatReadsTests
{
    /// <summary>A player as the game keeps one: the values are floats, the maximums whole numbers.</summary>
    private sealed class FakeAttributes
    {
        private readonly Dictionary<int, double> _values = new();
        private readonly Dictionary<int, int> _maxes = new();

        public FakeAttributes(double value, int max)
        {
            foreach (var id in StatReads.ValueIds) _values[id] = value;
            foreach (var id in StatReads.MaxIds) _maxes[id] = max;
        }

        public bool Has(int id) => _values.ContainsKey(id) || _maxes.ContainsKey(id);

        // GetTotalValue_Float on a whole-number attribute returns 0.
        public double ReadFloat(int id) => _values.TryGetValue(id, out var value) ? value : 0;

        // GetTotalValue_Int on a float attribute truncates.
        public int ReadInt(int id) => _maxes.TryGetValue(id, out var max) ? max : (int)_values[id];

        public void Remove(int id)
        {
            _values.Remove(id);
            _maxes.Remove(id);
        }
    }

    [Fact]
    public void Every_stat_reads_its_whole_number_maximum()
    {
        var player = new FakeAttributes(42.5, 130);

        var levels = StatReads.Levels(player.Has, player.ReadFloat, player.ReadInt);

        Assert.Equal(new[] { new StatLevel(42.5, 130), new StatLevel(42.5, 130), new StatLevel(42.5, 130), new StatLevel(42.5, 130) }, levels);
    }

    [Fact]
    public void A_low_stat_read_from_the_game_warns()
    {
        var watch = new AlertWatch();
        var player = new FakeAttributes(80, 100);

        watch.Step(StatReads.Levels(player.Has, player.ReadFloat, player.ReadInt), new FitnessReading(80, 100), new AlertOptions());

        var low = new FakeAttributes(10, 100);
        var alerts = watch.Step(StatReads.Levels(low.Has, low.ReadFloat, low.ReadInt), new FitnessReading(80, 100), new AlertOptions());

        Assert.Equal(4, alerts.Count);
    }

    [Fact]
    public void A_stat_the_player_lacks_reads_zero_of_zero()
    {
        var player = new FakeAttributes(50, 100);
        player.Remove(102);

        var levels = StatReads.Levels(player.Has, player.ReadFloat, player.ReadInt);

        Assert.Equal(new StatLevel(0, 0), levels[1]);
        Assert.Equal(new StatLevel(50, 100), levels[0]);
    }

    [Fact]
    public void The_hover_maximums_are_whole_numbers_and_a_missing_one_falls_back()
    {
        var player = new FakeAttributes(50, 120);
        player.Remove(105);

        Assert.Equal(new double[] { 120, 120, 120, 100 }, StatReads.Maxes(player.Has, player.ReadInt, 100));
    }
}
