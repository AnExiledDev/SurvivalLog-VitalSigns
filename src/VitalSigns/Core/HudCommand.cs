using System;
using System.Globalization;

namespace VitalSigns.Core;

/// <summary>
/// A message from the HUD (<c>slmod|vitalsigns|hud|...</c>): a click on a bar (act|part), on a debuff's cure
/// (cure|buff), a shift-click pinning a buff (fav|buff), the layout editor wanted (edit|1), closing (edit|0) or
/// saving (layout|parts|tray|collapse).
/// </summary>
public abstract record HudCommand
{
    public const string HudPrefix = "slmod|vitalsigns|hud|";

    private HudCommand()
    {
    }

    public sealed record Act(int Stat) : HudCommand;

    public sealed record Cure(int BuffId) : HudCommand;

    public sealed record Favourite(int BuffId) : HudCommand;

    public sealed record Edit(bool On) : HudCommand;

    public sealed record Layout(string Parts, int Tray, bool Collapse) : HudCommand;

    public sealed record Unknown(string Message) : HudCommand;

    public static HudCommand Parse(string message)
    {
        if (!message.StartsWith(HudPrefix, StringComparison.Ordinal)) return new Unknown(message);

        var parts = message.Substring(HudPrefix.Length).Split('|');

        switch (parts[0])
        {
            case "act" when parts.Length == 2 && Number(parts[1]) is { } stat && stat < 4:
                return new Act(stat);
            case "cure" when parts.Length == 2 && Number(parts[1]) is { } cure && cure > 0:
                return new Cure(cure);
            case "fav" when parts.Length == 2 && Number(parts[1]) is { } fav && fav > 0:
                return new Favourite(fav);
            case "edit" when parts.Length == 2 && parts[1] is "0" or "1":
                return new Edit(parts[1] == "1");
            case "layout" when parts.Length == 4 && Number(parts[2]) is { } tray && tray < HudChoices.Trays.Length && parts[3] is "0" or "1":
                return new Layout(parts[1], tray, parts[3] == "1");
            default:
                return new Unknown(message);
        }
    }

    private static int? Number(string text) => int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var value) ? value : null;
}
