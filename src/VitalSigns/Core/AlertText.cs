namespace VitalSigns.Core;

/// <summary>A warning as the toast shows it.</summary>
public readonly record struct AlertNotice(string Title, string Body, bool IsSevere, string Key);

/// <summary>The words of each warning, in English or Simplified Chinese, with the game's own names for the stats.</summary>
public static class AlertText
{
    public const string Source = "Vital Signs";

    private static readonly string[] English = { "Satiety", "Morale", "Stamina", "Life" };
    private static readonly string[] Chinese = { "饱腹", "心情", "体力", "生命" };

    public static AlertNotice Of(Alert alert, bool isChinese)
    {
        // One key per stat: a new warning for the same stat takes the place of the one still on screen.
        var key = "vitalsigns.alert." + alert.Stat;
        var numbers = alert.Value + "/" + alert.Max;

        return alert.Level switch
        {
            AlertLevel.Weakened => isChinese
                ? new AlertNotice("健康低于30", $"健康 {numbers}，你已陷入「亚健康」。", true, key)
                : new AlertNotice("Fitness below 30", $"Fitness {numbers}: you are Weakened.", true, key),
            AlertLevel.NearWeakened => isChinese
                ? new AlertNotice("健康接近「亚健康」", $"健康 {numbers}，低于30会陷入「亚健康」。", false, key)
                : new AlertNotice("Fitness close to Weakened", $"Fitness {numbers}. Below 30 you become Weakened.", false, key),
            _ => isChinese
                ? new AlertNotice($"「{Chinese[(int)alert.Stat]}」偏低", $"「{Chinese[(int)alert.Stat]}」只剩 {numbers}。", false, key)
                : new AlertNotice($"{English[(int)alert.Stat]} is low", $"{English[(int)alert.Stat]} is down to {numbers}.", false, key)
        };
    }
}
