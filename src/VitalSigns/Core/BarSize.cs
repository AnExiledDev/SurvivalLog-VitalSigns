namespace VitalSigns.Core;

/// <summary>
/// How big the stat bars and their buff badges are drawn, in percent of the game's own size. The page zooms each stat
/// row by it, so the badges, stripes and hover boxes on that row shrink or grow with the bar.
/// </summary>
public static class BarSize
{
    public const int Default = 80;
    public const int Min = 50;
    public const int Max = 150;

    /// <summary>The page's zoom for a percent from the config; a value outside the range is held to its nearest end.</summary>
    public static double Zoom(int percent) => System.Math.Clamp(percent, Min, Max) / 100.0;
}
