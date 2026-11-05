using System;

namespace VitalSigns.Core;

/// <summary>How the stat bars are drawn: the game's own look, flat (no frame or shadow), or split into tenths.</summary>
public static class BarLook
{
    public const string Game = "Game";
    public const string Flat = "Flat";
    public const string Segmented = "Segmented";

    public static readonly string[] Choices = { Game, Flat, Segmented };

    /// <summary>The page's number for a look: 0 Game, 1 Flat, 2 Segmented. Anything else is the game's look.</summary>
    public static int Index(string? look)
    {
        var at = Array.FindIndex(Choices, choice => string.Equals(choice, look?.Trim(), StringComparison.OrdinalIgnoreCase));

        return at < 0 ? 0 : at;
    }
}
