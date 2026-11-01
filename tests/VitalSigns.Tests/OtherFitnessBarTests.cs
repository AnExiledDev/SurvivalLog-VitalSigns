using VitalSigns.Core;
using Xunit;

namespace VitalSigns.Tests;

public class OtherFitnessBarTests
{
    [Fact]
    public void No_Fitness_Bar_installed_means_no_second_bar()
    {
        Assert.False(OtherFitnessBar.Draws(null));
    }

    /// <summary>The versions before 0.1.2 do not know about Vital Signs and still draw their bar, so the warning stays.</summary>
    [Theory]
    [InlineData("0.1.1")]
    [InlineData("0.1.0")]
    public void An_old_Fitness_Bar_draws_a_second_bar(string version)
    {
        Assert.True(OtherFitnessBar.Draws(version));
    }

    /// <summary>From 0.1.2 Fitness Bar stays off beside Vital Signs, so there is one bar and nothing to warn about.</summary>
    [Theory]
    [InlineData("0.1.2")]
    [InlineData("0.2.0")]
    [InlineData("1.0.0")]
    public void A_Fitness_Bar_that_steps_aside_draws_no_second_bar(string version)
    {
        Assert.False(OtherFitnessBar.Draws(version));
    }
}
