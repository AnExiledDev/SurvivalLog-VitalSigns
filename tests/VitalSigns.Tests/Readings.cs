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
        bool isChinese = false, GameNames? names = null, Details? details = null, Options? options = null) =>
        PageData.Of(new Vitals(stats ?? Stats(), buffs ?? Array.Empty<BuffTime>(), 0, weight, wellFed, details), names ?? English, isChinese, options);

    public static readonly double[] None = { 0, 0, 0, 0 };

    public static readonly double[] NoFood = new double[BuffRule.FoodTypes];

    /// <summary>A buff rule with only what a test sets; everything else does nothing.</summary>
    public static BuffRule Rule(int id, string name = "Wound", bool isGood = false, double interval = 0, double[]? every = null, double[]? atStart = null,
        double[]? atEnd = null, double[]? max = null, double[]? gain = null, double[]? loss = null, double speed = 0, double[]? satietyFood = null,
        double[]? moraleFood = null, int[]? cures = null, int[]? suppressors = null, bool isCold = false, bool isSuppressible = false) =>
        new(id, name, isGood, isCold, isSuppressible, interval, every ?? None, atStart ?? None, atEnd ?? None, max ?? None, gain ?? None, loss ?? None, speed,
            satietyFood ?? NoFood, moraleFood ?? NoFood, cures ?? Array.Empty<int>(), suppressors ?? Array.Empty<int>());

    public static Details DetailsOf(IReadOnlyList<BuffRule>? rules = null, IReadOnlyList<IReadOnlyList<LossLine>>? coming = null, IReadOnlyList<ItemCount>? items = null,
        string suppressedBy = "", IReadOnlyList<string>? foods = null, IReadOnlyList<FoodMod>? foodMods = null, double[]? max = null) =>
        new(max ?? new double[] { 100, 100, 100, 100 }, coming ?? new IReadOnlyList<LossLine>[] { Array.Empty<LossLine>(), Array.Empty<LossLine>(), Array.Empty<LossLine>(), Array.Empty<LossLine>() },
            rules ?? Array.Empty<BuffRule>(), 1, items ?? Array.Empty<ItemCount>(), suppressedBy, foods ?? Array.Empty<string>(), foodMods ?? Array.Empty<FoodMod>());
}
