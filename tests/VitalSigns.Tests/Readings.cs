using VitalSigns.Core;

namespace VitalSigns.Tests;

/// <summary>Readings as the plugin builds them, for the page tests.</summary>
internal static class Readings
{
    public static readonly GameNames English = new("Well Fed", "Overweight");

    public static readonly StatRatios Unscaled = new(0, 0, 0, 0);

    public static IReadOnlyList<StatRatios> Stats(StatRatios? satiety = null, StatRatios? morale = null, StatRatios? stamina = null, StatRatios? life = null) =>
        new[] { satiety ?? Unscaled, morale ?? Unscaled, stamina ?? Unscaled, life ?? Unscaled };

    public static string Data(IReadOnlyList<StatRatios>? stats = null, IReadOnlyList<BuffTime>? buffs = null, CarryWeight? weight = null, WellFedLine? wellFed = null,
        bool isChinese = false, GameNames? names = null) =>
        PageData.Of(new Vitals(stats ?? Stats(), buffs ?? Array.Empty<BuffTime>(), 0, weight, wellFed), names ?? English, isChinese);
}
