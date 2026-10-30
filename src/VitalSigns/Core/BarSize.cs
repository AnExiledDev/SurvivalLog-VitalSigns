namespace VitalSigns.Core;

/// <summary>
/// How big the stat bars (BarSize), their buff badges (BuffSize) and the bars' hover boxes (BarTooltipSize) are drawn, in
/// percent of the game's own size. All three share this range; the bars and buffs share a default. The page zooms each
/// bar by the first, each row's badges, with their stripes and hover boxes, by the second, and the bars' hover boxes by
/// the third, apart from their bar.
/// </summary>
public static class BarSize
{
    public const int Default = 80;

    /// <summary>The bars' hover boxes start at the game's size: at the bars' 80% their text was too small to read easily.</summary>
    public const int TooltipDefault = 100;
    public const int Min = 50;
    public const int Max = 150;

    /// <summary>The page's zoom for a percent from the config; a value outside the range is held to its nearest end.</summary>
    public static double Zoom(int percent) => System.Math.Clamp(percent, Min, Max) / 100.0;
}
