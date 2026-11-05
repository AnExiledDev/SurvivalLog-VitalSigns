using System.Collections.Generic;

namespace VitalSigns.Core.Acts;

/// <summary>
/// The player's modifiers as the game applies them to a stat change (BaseAgent.CostAttrValue): a food's Satiety and
/// Morale gain first by the food type's own ratio, then any change by the stat's Gain ratio when it raises the stat or
/// its Loss ratio when it lowers it. Life from an item also gets the difficulty's item bonus. Ratios is in the HUD's row
/// order; FoodMods holds only the food ratios that are not zero. Difficulty is this mod's own multiplier on every change,
/// so a plan made with it matches what the change will really be.
/// </summary>
public sealed record StatScale(IReadOnlyList<StatRatios> Ratios, IReadOnlyList<FoodMod> FoodMods, double LifeItemBonus)
{
    public Difficulty Difficulty { get; init; } = Difficulty.Off;

    public static readonly StatScale None = new(new StatRatios[0], new FoodMod[0], 0);

    /// <summary>What a use of an item of food type <paramref name="food"/> does once the player's modifiers are applied.</summary>
    public StatDelta Item(StatDelta raw, int food)
    {
        var satiety = raw.Satiety > 0 ? raw.Satiety * (1 + Food(0, food)) : raw.Satiety;
        var morale = raw.Morale > 0 ? raw.Morale * (1 + Food(1, food)) : raw.Morale;
        var life = raw.Life > 0 ? raw.Life * (1 + LifeItemBonus) : raw.Life;

        return Action(raw with { Satiety = satiety, Morale = morale, Life = life });
    }

    /// <summary>What a furniture action or any other change does once the stats' Gain and Loss ratios are applied.</summary>
    public StatDelta Action(StatDelta raw) => new(Scale(0, raw.Satiety), Scale(1, raw.Morale), Scale(2, raw.Stamina), Scale(3, raw.Life), raw.MaxMorale, raw.MaxStamina);

    private double Scale(int stat, double value)
    {
        if (value == 0) return value;

        var scaled = stat < Ratios.Count ? value * (1 + (value > 0 ? Ratios[stat].Gain : Ratios[stat].Loss)) : value;

        return scaled * (value > 0 ? Difficulty.Gain(stat) : Difficulty.Drain(stat));
    }

    private double Food(int stat, int food)
    {
        foreach (var mod in FoodMods)
        {
            if (mod.Stat == stat && mod.Food == food) return mod.Ratio;
        }

        return 0;
    }
}
