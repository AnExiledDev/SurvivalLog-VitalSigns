using System;
using System.Globalization;

namespace VitalSigns.Core.Acts;

/// <summary>
/// What one action or item does to the player's stats, in the HUD's row order (Satiety, Morale, Stamina, Life), plus the
/// rise of the Morale and Stamina maximums some furniture gives. Life is the game's Vitality.
/// </summary>
public readonly record struct StatDelta(double Satiety, double Morale, double Stamina, double Life, double MaxMorale = 0, double MaxStamina = 0)
{
    public const int Stats = 4;

    public static readonly StatDelta None = new(0, 0, 0, 0);

    public double this[int stat] => stat switch
    {
        0 => Satiety,
        1 => Morale,
        2 => Stamina,
        3 => Life,
        _ => 0,
    };

    public bool IsNone => this == None;

    /// <summary>Whether it raises anything besides <paramref name="stat"/>: another stat or a maximum.</summary>
    public bool RaisesOther(int stat)
    {
        for (var i = 0; i < Stats; i++)
        {
            if (i != stat && this[i] > 0) return true;
        }

        return MaxMorale > 0 || MaxStamina > 0;
    }

    public StatDelta Plus(StatDelta other) => new(Satiety + other.Satiety, Morale + other.Morale, Stamina + other.Stamina, Life + other.Life,
        MaxMorale + other.MaxMorale, MaxStamina + other.MaxStamina);

    public StatDelta Times(double factor) => new(Satiety * factor, Morale * factor, Stamina * factor, Life * factor, MaxMorale * factor, MaxStamina * factor);

    /// <summary>
    /// The game's preview of a furniture action, as its config writes it: "Satiety:-10;Morale:+5;MaxMorale:+2". A part
    /// it does not know or cannot read is skipped.
    /// </summary>
    public static StatDelta Parse(string? text)
    {
        double satiety = 0, morale = 0, stamina = 0, life = 0, maxMorale = 0, maxStamina = 0;

        foreach (var part in (text ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var colon = part.IndexOf(':');

            if (colon <= 0) continue;

            if (!double.TryParse(part.Substring(colon + 1).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || !double.IsFinite(value)) continue;

            switch (part.Substring(0, colon).Trim())
            {
                case "Satiety":
                    satiety += value;
                    break;
                case "Morale":
                    morale += value;
                    break;
                case "Stamina":
                    stamina += value;
                    break;
                case "Vitality":
                    life += value;
                    break;
                case "MaxMorale":
                    maxMorale += value;
                    break;
                case "MaxStamina":
                    maxStamina += value;
                    break;
            }
        }

        return new StatDelta(satiety, morale, stamina, life, maxMorale, maxStamina);
    }
}
