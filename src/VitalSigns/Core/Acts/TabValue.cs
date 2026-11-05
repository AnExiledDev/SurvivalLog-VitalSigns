using System;
using System.Collections.Generic;
using System.Globalization;

namespace VitalSigns.Core.Acts;

/// <summary>What kind of value a setting the Actions tab may change holds, so what the tab sends can be checked first.</summary>
public enum TabValueKind
{
    Flag,
    Whole,
    Number,
    Choice,
    Keys,
    BedModes,
    Ids,
}

/// <summary>
/// One setting the Actions tab may change: its config name ("Actions/FoodGoal"), its kind, and the range or the choices
/// it takes. Anything the tab sends goes through <see cref="Normal"/>, which returns the value as the config file writes
/// it, or null when it is not one this setting takes.
/// </summary>
public sealed record TabSetting(string Name, TabValueKind Kind, double Min = 0, double Max = 0, IReadOnlyList<string>? Choices = null)
{
    public const int MostTextLength = 2000;

    public string? Normal(string? value)
    {
        var text = (value ?? "").Trim();

        if (text.Length > MostTextLength) return null;

        switch (Kind)
        {
            case TabValueKind.Flag:
                return text is "1" or "true" ? "true" : text is "0" or "false" ? "false" : null;
            case TabValueKind.Whole:
                return Read(text) is { } whole ? Math.Round(Math.Clamp(whole, Min, Max)).ToString(CultureInfo.InvariantCulture) : null;
            case TabValueKind.Number:
                return Read(text) is { } number ? (Math.Round(Math.Clamp(number, Min, Max) * 100) / 100).ToString(CultureInfo.InvariantCulture) : null;
            case TabValueKind.Choice:
                foreach (var choice in Choices ?? Array.Empty<string>())
                {
                    if (string.Equals(choice, text, StringComparison.OrdinalIgnoreCase)) return choice;
                }

                return null;
            case TabValueKind.Keys:
                return ActKeys.Join(ActKeys.Parse(text));
            case TabValueKind.BedModes:
                return BedModesText(text);
            case TabValueKind.Ids:
                return string.Join(",", HudChoices.Ids(text));
            default:
                return null;
        }
    }

    private static double? Read(string text) =>
        double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && double.IsFinite(value) ? value : null;

    private static string BedModesText(string text)
    {
        var modes = new SortedDictionary<int, string>();

        foreach (var pair in SleepPlan.ParseModes(text))
        {
            if (pair.Value != SleepPlan.ModeAuto) modes[pair.Key] = pair.Value;
        }

        var parts = new List<string>();

        foreach (var pair in modes) parts.Add(pair.Key.ToString(CultureInfo.InvariantCulture) + ":" + pair.Value);

        return string.Join(",", parts);
    }
}
