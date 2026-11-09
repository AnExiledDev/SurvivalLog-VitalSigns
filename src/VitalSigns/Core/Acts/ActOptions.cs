using System.Collections.Generic;

namespace VitalSigns.Core.Acts;

/// <summary>
/// What a click on each bar does, from the Actions settings. Satiety, Morale, Stamina and Life turn each bar's click on
/// or off. UseHome lets a click take items from the home storage as well as the bag. AddToQueue puts what a click picks
/// after the actions already queued, planned from where the bars will be once those are done; off, a click replaces
/// the queue the way the game's clicks do.
/// </summary>
public sealed record ActOptions(
    bool Satiety = true,
    bool Morale = true,
    bool Stamina = true,
    bool Life = true,
    bool UseHome = true,
    bool AddToQueue = true,
    bool Toasts = true,
    string FoodGoal = FoodPlan.GoalMax,
    int FoodAbove = FoodPlan.DefaultAbove,
    int FoodMostItems = FoodPlan.DefaultMostItems,
    IReadOnlyList<string>? FoodSkip = null,
    bool FoodAvoidMoraleLoss = false,
    string BedModes = "",
    int NapBelow = SleepPlan.DefaultNapBelow,
    string MoraleMode = MoralePlan.ModeBest,
    IReadOnlyList<string>? Routine = null,
    bool MoraleItems = false,
    bool AvoidLifeLoss = true,
    IReadOnlyList<string>? LifeItems = null,
    int LifeMostItems = LifePlan.DefaultMostItems)
{
    public bool IsOn(int stat) => stat switch
    {
        0 => Satiety,
        1 => Morale,
        2 => Stamina,
        3 => Life,
        _ => false,
    };

    public bool AnyOn => Satiety || Morale || Stamina || Life;
}
