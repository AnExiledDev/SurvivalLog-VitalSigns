using VitalSigns.Core;
using Xunit;

namespace VitalSigns.Tests;

// The stat change hook runs on every stat change of every character, so a player who never sets a multiplier must
// never pay for it, and a player who does gets it exactly once however often the settings change.
public class DifficultyHookGateTests
{
    private static readonly Difficulty Harder = new(new[] { 1.5, 1, 1, 1 }, new[] { 1.0, 1, 1, 1 });

    [Fact]
    public void The_hook_stays_out_while_every_multiplier_is_1()
    {
        var installs = 0;
        var gate = new DifficultyHookGate(() => installs++);

        gate.Want(Difficulty.Off);
        gate.Want(new Difficulty(new[] { 1.0, 1, 1, 1 }, new[] { 1.0, 1, 1, 1 }));

        Assert.Equal(0, installs);
        Assert.False(gate.IsIn);
    }

    [Fact]
    public void The_hook_goes_in_the_first_time_a_multiplier_is_set()
    {
        var installs = 0;
        var gate = new DifficultyHookGate(() => installs++);

        gate.Want(Difficulty.Off);
        gate.Want(Harder);

        Assert.Equal(1, installs);
        Assert.True(gate.IsIn);
    }

    [Fact]
    public void The_hook_goes_in_only_once_however_the_settings_change_after()
    {
        var installs = 0;
        var gate = new DifficultyHookGate(() => installs++);

        gate.Want(Harder);
        gate.Want(Difficulty.Off);
        gate.Want(new Difficulty(new[] { 1.0, 1, 1, 1 }, new[] { 1.0, 0, 1, 1 }));
        gate.Want(Harder);

        Assert.Equal(1, installs);
    }
}
