using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;

namespace VitalSigns.Timers.Core;

/// <summary>A buff the tab lists: a visible one with a timer, with its text in both languages.</summary>
public sealed record BuffEntry(int Id, bool IsGood, int Category, double Seconds, string NameEn, string NameZh, string DescEn, string DescZh)
{
    public BuffKind Kind => new(Id, IsGood, Category);
}

/// <summary>What a save from the tab asks for, already held to <see cref="Limits"/>.</summary>
public abstract record SaveRequest
{
    private SaveRequest()
    {
    }

    public sealed record Accepted(double Good, double Bad, IReadOnlyList<double?> Categories, IReadOnlyDictionary<int, double> Buffs, IReadOnlyList<string> Notes) : SaveRequest;

    public sealed record Refused(string Problem) : SaveRequest;
}

/// <summary>
/// The C# half of the tab's messages: the data call that fills the tab, the reply to a save, and the reading of what a
/// save sends. The tab gets numbers and the game's own text; it does all its formatting itself.
/// </summary>
public static class TabPage
{
    private const string Receiver = "window.__slvtimers";

    public static string DataCall(MultiplierPlan plan, IReadOnlyList<BuffEntry> buffs, int timeScale, IReadOnlyList<string> problems) =>
        Receiver + "&&" + Receiver + ".data(" + DataJson(plan, buffs, timeScale, problems) + ")";

    public static string SavedCall(bool isSaved, string message) =>
        Receiver + "&&" + Receiver + ".saved(" + (isSaved ? "true" : "false") + "," + JsText.Literal(message) + ")";

    public static string DataJson(MultiplierPlan plan, IReadOnlyList<BuffEntry> buffs, int timeScale, IReadOnlyList<string> problems)
    {
        using var stream = new MemoryStream();

        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("good", plan.Good);
            writer.WriteNumber("bad", plan.Bad);
            writer.WriteStartArray("categories");

            foreach (var category in plan.Categories) writer.WriteNumberValue(category ?? Limits.UseAbove);

            writer.WriteEndArray();
            writer.WriteNumber("min", Limits.Min);
            writer.WriteNumber("max", Limits.Max);
            writer.WriteNumber("timeScale", timeScale);
            WriteBuffs(writer, plan, buffs);
            writer.WriteStartArray("problems");

            foreach (var problem in problems) writer.WriteStringValue(problem);

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        // The JSON goes inside a script, where "</script>" or a line separator in the game's text must stay inert.
        return Encoding.UTF8.GetString(stream.ToArray()).Replace("\u2028", "\\u2028").Replace("\u2029", "\\u2029").Replace("<", "\\u003c");
    }

    public static SaveRequest ReadSave(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            var notes = new List<string>();

            if (root.ValueKind != JsonValueKind.Object) return new SaveRequest.Refused("the tab sent something that is not a save");

            if (!TryNumber(root, "good", out var good) || !TryNumber(root, "bad", out var bad)) return new SaveRequest.Refused("the global multipliers are missing");

            var categories = Categories(root, notes);

            if (categories is null) return new SaveRequest.Refused($"the tab must send {MultiplierPlan.CategoryCount} category multipliers");

            var buffs = root.TryGetProperty("buffs", out var buffsElement) ? OverrideFile.Entries(buffsElement, notes) : new Dictionary<int, double>();

            return new SaveRequest.Accepted(Global(good, "Global Good", notes), Global(bad, "Global Bad", notes), categories, buffs, notes);
        }
        catch (JsonException ex)
        {
            return new SaveRequest.Refused("the tab sent broken JSON: " + ex.Message);
        }
    }

    private static void WriteBuffs(Utf8JsonWriter writer, MultiplierPlan plan, IReadOnlyList<BuffEntry> buffs)
    {
        writer.WriteStartArray("buffs");

        foreach (var buff in buffs)
        {
            var resolved = plan.For(buff.Kind);

            writer.WriteStartObject();
            writer.WriteNumber("id", buff.Id);
            writer.WriteBoolean("good", buff.IsGood);
            writer.WriteNumber("cat", buff.Category);
            writer.WriteNumber("seconds", buff.Seconds);
            writer.WriteNumber("own", plan.Buffs.TryGetValue(buff.Id, out var own) ? own : Limits.UseAbove);
            writer.WriteNumber("mult", resolved.Value);
            writer.WriteString("from", resolved.Source.ToString().ToLowerInvariant());
            writer.WriteString("nameEn", buff.NameEn);
            writer.WriteString("nameZh", buff.NameZh);
            writer.WriteString("descEn", buff.DescEn);
            writer.WriteString("descZh", buff.DescZh);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static List<double?>? Categories(JsonElement root, List<string> notes)
    {
        if (!root.TryGetProperty("categories", out var element) || element.ValueKind != JsonValueKind.Array || element.GetArrayLength() != MultiplierPlan.CategoryCount) return null;

        var categories = new List<double?>();
        var index = 0;

        foreach (var value in element.EnumerateArray())
        {
            if (value.ValueKind != JsonValueKind.Number || !value.TryGetDouble(out var number)) return null;

            var limited = Limits.Override(number, out var wasChanged);

            if (wasChanged) notes.Add($"category {index}: {Show(number)} was changed to {(limited is { } v ? Show(v) : "use global")}");

            categories.Add(limited);
            index++;
        }

        return categories;
    }

    private static double Global(double value, string name, List<string> notes)
    {
        var limited = Limits.Global(value, out var wasChanged);

        if (wasChanged) notes.Add($"{name}: {Show(value)} was changed to {Show(limited)}");

        return limited;
    }

    private static bool TryNumber(JsonElement root, string name, out double value)
    {
        value = 0;

        return root.TryGetProperty(name, out var element) && element.ValueKind == JsonValueKind.Number && element.TryGetDouble(out value);
    }

    private static string Show(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
}
