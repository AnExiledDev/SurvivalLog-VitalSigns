namespace VitalSigns.Core;

/// <summary>
/// How big the stat bars (BarSize) and their buff badges (BuffSize) are drawn, in percent of the game's own size. Both
/// settings share this default and range. The page zooms each bar by one and each row's badges, with their stripes and
/// hover boxes, by the other.
/// </summary>
public static class BarSize
{
    public const int Default = 80;
    public const int Min = 50;
    public const int Max = 150;

    /// <summary>The page's zoom for a percent from the config; a value outside the range is held to its nearest end.</summary>
    public static double Zoom(int percent) => System.Math.Clamp(percent, Min, Max) / 100.0;
}
