using System.Collections.Generic;

namespace VitalSigns.Core;

/// <summary>
/// What one buff does to the four HUD stats, read from its effect. Every stat array is in the HUD's row order
/// (Satiety, Morale, Stamina, Life) and holds the value for one layer:
/// Every is the change each Interval seconds, AtStart and AtEnd the change when it begins and ends, Max the change to
/// the stat's maximum, Gain and Loss the change to how gains and losses are scaled (0.5 is half again).
/// Speed scales move speed. SatietyFood and MoraleFood hold the change to how food type n (index n - 1) feeds each stat.
/// IsCold marks a cold debuff, whose interval losses cold resistance cuts down. IsSuppressible marks a debuff a
/// suppressor (Analgesia) holds back. Cures lists the items that remove it, Suppressors the items that hold it back.
/// </summary>
public sealed record BuffRule(int Id, string Name, bool IsGood, bool IsCold, bool IsSuppressible, double Interval,
    IReadOnlyList<double> Every, IReadOnlyList<double> AtStart, IReadOnlyList<double> AtEnd, IReadOnlyList<double> Max,
    IReadOnlyList<double> Gain, IReadOnlyList<double> Loss, double Speed, IReadOnlyList<double> SatietyFood, IReadOnlyList<double> MoraleFood,
    IReadOnlyList<int> Cures, IReadOnlyList<int> Suppressors)
{
    public const int Stats = 4;
    public const int FoodTypes = 11;
}
