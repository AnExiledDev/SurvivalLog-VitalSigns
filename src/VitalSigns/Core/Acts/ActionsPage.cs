using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace VitalSigns.Core.Acts;

/// <summary>
/// Everything the Actions tab draws, gathered by the adapter: the settings as the config file writes them, whether the
/// base is still being read and whether the player is at home, each bar's plan now, and the base as a click sees it.
/// Items holds what is at hand and, after it, the kinds the settings name or the tab offers that are not (Missing,
/// count 0). Beds holds every bed at home and, for a bed type the settings name that is not there, one with id 0.
/// </summary>
public sealed record ActionsView(IReadOnlyDictionary<string, string> Settings, bool IsScanning, bool AtHome, IReadOnlyList<BarPlan?> Plans,
    IReadOnlyList<ActOption> Items, IReadOnlyList<ActOption> Funcs, IReadOnlyList<Bed> Beds, IReadOnlyList<string> Best);

/// <summary>
/// The C# half of the Actions tab: the call that hands it its data. The tab gets numbers, keys and the game's own names;
/// it does all its wording and drawing itself, in the player's language.
/// </summary>
public static class ActionsPage
{
    public const string Receiver = "window.__slvactions";

    public static string DataCall(ActionsView view) => Receiver + "&&" + Receiver + ".data(" + DataJson(view) + ")";

    public static string DataJson(ActionsView view)
    {
        using var stream = new MemoryStream();

        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            WriteSettings(writer, view.Settings);
            WriteLimits(writer);
            writer.WriteBoolean("scanning", view.IsScanning);
            writer.WriteBoolean("atHome", view.AtHome);
            WriteStats(writer, view.Plans);
            writer.WriteStartArray("items");

            foreach (var item in view.Items) WriteOption(writer, item);

            writer.WriteEndArray();
            writer.WriteStartArray("funcs");

            foreach (var func in view.Funcs) WriteOption(writer, func);

            writer.WriteEndArray();
            WriteBeds(writer, view.Beds);
            writer.WriteStartArray("best");

            foreach (var key in view.Best) writer.WriteStringValue(key);

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        // The JSON goes inside a script, where "</script>" or a line separator in the game's text must stay inert.
        return Encoding.UTF8.GetString(stream.ToArray()).Replace("\u2028", "\\u2028").Replace("\u2029", "\\u2029").Replace("<", "\\u003c");
    }

    private static void WriteSettings(Utf8JsonWriter writer, IReadOnlyDictionary<string, string> settings)
    {
        writer.WriteStartObject("s");

        foreach (var pair in settings) writer.WriteString(pair.Key, pair.Value);

        writer.WriteEndObject();
    }

    /// <summary>The ranges and choices the tab's controls offer, the same ones the config holds them to.</summary>
    private static void WriteLimits(Utf8JsonWriter writer)
    {
        writer.WriteStartObject("lim");
        Range(writer, "foodAbove", FoodPlan.MinAbove, FoodPlan.MaxAbove);
        Range(writer, "foodMost", FoodPlan.MinMostItems, FoodPlan.MaxMostItems);
        Range(writer, "napBelow", SleepPlan.MinNapBelow, SleepPlan.MaxNapBelow);
        Range(writer, "lifeMost", LifePlan.MinMostItems, LifePlan.MaxMostItems);
        Choices(writer, "goals", FoodPlan.Goals);
        Choices(writer, "moraleModes", MoralePlan.Modes);
        Choices(writer, "bedModes", SleepPlan.Modes);
        writer.WriteEndObject();
    }

    private static void Range(Utf8JsonWriter writer, string name, int min, int max)
    {
        writer.WriteStartArray(name);
        writer.WriteNumberValue(min);
        writer.WriteNumberValue(max);
        writer.WriteEndArray();
    }

    private static void Choices(Utf8JsonWriter writer, string name, IEnumerable<string> choices)
    {
        writer.WriteStartArray(name);

        foreach (var choice in choices) writer.WriteStringValue(choice);

        writer.WriteEndArray();
    }

    private static void WriteStats(Utf8JsonWriter writer, IReadOnlyList<BarPlan?> plans)
    {
        writer.WriteStartArray("stats");

        for (var stat = 0; stat < StatDelta.Stats; stat++)
        {
            var plan = stat < plans.Count ? plans[stat] : null;

            writer.WriteStartObject();
            writer.WriteBoolean("can", plan?.CanAct ?? false);
            writer.WriteString("tip", plan?.Tip ?? "");
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static void WriteOption(Utf8JsonWriter writer, ActOption option)
    {
        writer.WriteStartObject();
        writer.WriteString("k", option.Key);
        writer.WriteString("n", option.Name);
        writer.WriteString("ic", option.Icon);
        WriteDelta(writer, option.Delta);
        writer.WriteNumber("st", (int)option.State);

        if (option.Why.Length > 0) writer.WriteString("why", option.Why);

        if (option.Kind == ActKind.Item)
        {
            writer.WriteNumber("c", option.Count);
            writer.WriteNumber("b", option.InBag);
        }
        else if (option.Place.Length > 0)
        {
            writer.WriteString("at", option.Place);
        }

        writer.WriteEndObject();
    }

    /// <summary>Satiety, Morale, Stamina, Life, then the rise of the Morale and Stamina maximums, each to one decimal.</summary>
    private static void WriteDelta(Utf8JsonWriter writer, StatDelta delta)
    {
        writer.WriteStartArray("d");

        for (var stat = 0; stat < StatDelta.Stats; stat++) writer.WriteNumberValue(Round(delta[stat]));

        writer.WriteNumberValue(Round(delta.MaxMorale));
        writer.WriteNumberValue(Round(delta.MaxStamina));
        writer.WriteEndArray();
    }

    /// <summary>
    /// One entry per bed type, in the order the base has them: how many of it are at home (0 when only the settings
    /// name it), its best restore bonus, and each rest it offers as a state (Ready, Busy, Missing; -1 when the type has
    /// no such rest) and what it gives, the best one of that type's beds.
    /// </summary>
    private static void WriteBeds(Utf8JsonWriter writer, IReadOnlyList<Bed> beds)
    {
        var types = new List<int>();
        var byType = new Dictionary<int, List<Bed>>();

        foreach (var bed in beds)
        {
            if (!byType.TryGetValue(bed.ConfigId, out var list))
            {
                byType[bed.ConfigId] = list = new List<Bed>();
                types.Add(bed.ConfigId);
            }

            list.Add(bed);
        }

        writer.WriteStartArray("beds");

        foreach (var type in types)
        {
            var list = byType[type];
            var count = 0;
            var restore = 0.0;

            foreach (var bed in list)
            {
                if (bed.FurnitureId != 0) count++;

                restore = Math.Max(restore, bed.Restore);
            }

            writer.WriteStartObject();
            writer.WriteNumber("t", type);
            writer.WriteString("n", list[0].Name);
            writer.WriteNumber("c", count);
            writer.WriteNumber("r", Round(restore));
            Rest(writer, "sleep", list, b => b.Sleep);
            Rest(writer, "nap", list, b => b.Nap);
            Rest(writer, "beauty", list, b => b.Beauty);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static void Rest(Utf8JsonWriter writer, string name, List<Bed> beds, Func<Bed, ActOption?> rest)
    {
        ActOption? best = null;

        foreach (var bed in beds)
        {
            if (rest(bed) is { } option && (best == null || option.State < best.State)) best = option;
        }

        writer.WriteStartObject(name);
        writer.WriteNumber("st", best == null ? -1 : (int)best.State);

        if (best != null)
        {
            writer.WriteNumber("g", Round(best.Delta.Stamina));

            if (best.Why.Length > 0) writer.WriteString("why", best.Why);
        }

        writer.WriteEndObject();
    }

    private static double Round(double value) => double.IsFinite(value) ? Math.Round(value * 10) / 10 : 0;
}
