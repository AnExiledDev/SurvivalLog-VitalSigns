using System.Collections.Generic;

namespace VitalSigns.Core;

/// <summary>
/// How one stat's changes are scaled right now, as the game keeps it: 0 means unchanged, 0.5 means half again, -0.25
/// a quarter less. The game multiplies a change by (1 + ratio): Gain for anything that raises the stat, Loss for anything
/// that lowers it, NaturalGain and NaturalLoss for its own slow rise and fall over time.
/// </summary>
public readonly record struct StatRatios(double Gain, double Loss, double NaturalGain, double NaturalLoss);

/// <summary>A running buff with an end: its config id, the game second it ends, and its full length in game seconds.</summary>
public readonly record struct BuffTime(int ConfigId, int EndsAt, int Duration);

/// <summary>What the player carries, the most they can carry, and the weight above which they are Overweight, in grams.</summary>
public readonly record struct CarryWeight(int Carried, int Max, int Overweight);

/// <summary>Satiety reaching From gives Well Fed; dropping below Until takes it away. Max is the Satiety bar's maximum.</summary>
public readonly record struct WellFedLine(int From, int Until, int Max);

/// <summary>
/// Everything the HUD needs from the player in one reading. Stats are in the HUD's row order: Satiety, Morale,
/// Stamina, Life. Now is the game clock in game seconds, which the buff ends are measured on.
/// </summary>
public sealed record Vitals(IReadOnlyList<StatRatios> Stats, IReadOnlyList<BuffTime> Buffs, int Now, CarryWeight? Weight, WellFedLine? WellFed);

/// <summary>Game names the page quotes, in the game's current language.</summary>
public sealed record GameNames(string WellFed, string Overweight);
