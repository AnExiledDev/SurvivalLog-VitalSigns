using System.Collections.Generic;

namespace VitalSigns.Core;

/// <summary>
/// How one stat's changes are scaled right now, as the game keeps it: 0 means unchanged, 0.5 means half again, -0.25
/// a quarter less. The game multiplies a change by (1 + ratio): Gain for anything that raises the stat, Loss for anything
/// that lowers it, NaturalGain and NaturalLoss for its own slow rise and fall over time.
/// </summary>
public readonly record struct StatRatios(double Gain, double Loss, double NaturalGain, double NaturalLoss);

/// <summary>
/// A running buff the HUD shows: its config id, the game second it ends (-1 when it lasts until removed), its full length
/// in game seconds, how many layers are stacked, and whether a suppressor (Analgesia) holds its effects back.
/// </summary>
public readonly record struct BuffTime(int ConfigId, int EndsAt, int Duration, int Layers = 1, bool IsSuppressed = false);

/// <summary>What the player carries, the most they can carry, and the weight above which they are Overweight, in grams.</summary>
public readonly record struct CarryWeight(int Carried, int Max, int Overweight);

/// <summary>Satiety reaching From gives Well Fed; dropping below Until takes it away. Max is the Satiety bar's maximum.</summary>
public readonly record struct WellFedLine(int From, int Until, int Max);

/// <summary>
/// Everything the HUD needs from the player in one reading. Stats are in the HUD's row order: Satiety, Morale,
/// Stamina, Life. Now is the game clock in game seconds, which the buff ends are measured on.
/// </summary>
public sealed record Vitals(IReadOnlyList<StatRatios> Stats, IReadOnlyList<BuffTime> Buffs, int Now, CarryWeight? Weight, WellFedLine? WellFed,
    Details? Details = null, FitnessReading? Fitness = null);

/// <summary>
/// The player's Fitness as the game would draw it on a bar: the value rounded to a whole number, and the maximum. In the
/// game's code this stat is Health (AttrName 4, its maximum AttrName 104); players see it as Fitness.
/// </summary>
public readonly record struct FitnessReading(int Value, int Max);

/// <summary>An item a tooltip lists as a cure or suppressor: its name, its web icon path and how many the player has at home.</summary>
public readonly record struct ItemCount(int Id, string Name, string Icon, int Count);

/// <summary>The player's own food modifier: Stat 0 is Satiety, 1 Morale; Food is the food type (1 to 11); Ratio as in StatRatios.</summary>
public readonly record struct FoodMod(int Stat, int Food, double Ratio);

/// <summary>
/// What the tooltips and the loss shading draw from, besides the ratios. Max and Coming are in the HUD's row order.
/// Rules holds one entry per buff id on the HUD. SuppressedBy names the running suppressors, empty when none.
/// </summary>
public sealed record Details(IReadOnlyList<double> Max, IReadOnlyList<IReadOnlyList<LossLine>> Coming, IReadOnlyList<BuffRule> Rules, double ColdFactor,
    IReadOnlyList<ItemCount> Items, string SuppressedBy, IReadOnlyList<string> Foods, IReadOnlyList<FoodMod> FoodMods);

/// <summary>
/// Which parts the player turned on in the config, and how big the stat bars, their buffs and the bars' hover boxes are
/// drawn (percent).
/// </summary>
public sealed record Options(bool Weight = true, bool Stripes = true, bool Tooltips = true, bool GameText = true, bool Cures = true, int BarSize = Core.BarSize.Default,
    int BuffSize = Core.BarSize.Default, bool Fitness = true, int TooltipSize = Core.BarSize.TooltipDefault);

/// <summary>Game names the page quotes, in the game's current language.</summary>
public sealed record GameNames(string WellFed, string Overweight);
