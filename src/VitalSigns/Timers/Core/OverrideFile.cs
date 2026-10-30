using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace VitalSigns.Timers.Core;

/// <summary>What reading the per-buff file gave: the multipliers it holds and anything in it that was skipped or changed.</summary>
public sealed record OverrideRead(IReadOnlyDictionary<int, double> Buffs, IReadOnlyList<string> Problems);

/// <summary>
/// The per-buff multipliers file: <c>{"buffs": {"200810": 2, "1043": 0.5}}</c>, buff ID to multiplier. A missing or
/// broken file reads as no multipliers, and each bad entry is skipped on its own, so one typo never drops the rest.
/// </summary>
public static class OverrideFile
{
    private const string BuffsKey = "buffs";

    private static readonly JsonDocumentOptions Lenient = new() { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };

    public static OverrideRead Read(string? json)
    {
        var problems = new List<string>();

        if (string.IsNullOrWhiteSpace(json)) return new OverrideRead(new Dictionary<int, double>(), problems);

        try
        {
            using var document = JsonDocument.Parse(json, Lenient);

            if (document.RootElement.ValueKind != JsonValueKind.Object || !document.RootElement.TryGetProperty(BuffsKey, out var buffs))
            {
                problems.Add("the file has no \"buffs\" object, so no buff has its own multiplier");

                return new OverrideRead(new Dictionary<int, double>(), problems);
            }

            return new OverrideRead(Entries(buffs, problems), problems);
        }
        catch (JsonException ex)
        {
            problems.Add("the file is not valid JSON, so no buff has its own multiplier: " + ex.Message);

            return new OverrideRead(new Dictionary<int, double>(), problems);
        }
    }

    /// <summary>Reads a <c>{"id": multiplier}</c> object; also used for the buffs a save from the tab sends.</summary>
    public static Dictionary<int, double> Entries(JsonElement buffs, List<string> problems)
    {
        var entries = new Dictionary<int, double>();

        if (buffs.ValueKind != JsonValueKind.Object)
        {
            problems.Add("\"buffs\" is not an object of buff ID to multiplier");

            return entries;
        }

        foreach (var entry in buffs.EnumerateObject())
        {
            if (!int.TryParse(entry.Name, NumberStyles.None, CultureInfo.InvariantCulture, out var id) || id <= 0)
            {
                problems.Add($"\"{entry.Name}\" is not a buff ID, skipped");

                continue;
            }

            if (entry.Value.ValueKind != JsonValueKind.Number || !entry.Value.TryGetDouble(out var value))
            {
                problems.Add($"buff {id}: the multiplier is not a number, skipped");

                continue;
            }

            var limited = Limits.Override(value, out var wasChanged);

            if (wasChanged) problems.Add(limited is { } v ? $"buff {id}: {Show(value)} is outside {Show(Limits.Min)} to {Show(Limits.Max)}, used {Show(v)}" : $"buff {id}: {Show(value)} is not a multiplier, skipped");

            if (limited is { } kept) entries[id] = kept;
        }

        return entries;
    }

    /// <summary>The file's text, buffs in ID order so a hand edit and a diff stay easy to read.</summary>
    public static string Write(IReadOnlyDictionary<int, double> buffs)
    {
        using var stream = new MemoryStream();

        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();
            writer.WriteStartObject(BuffsKey);

            foreach (var (id, value) in buffs.OrderBy(pair => pair.Key)) writer.WriteNumber(id.ToString(CultureInfo.InvariantCulture), Math.Round(value, 4));

            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray()) + "\n";
    }

    private static string Show(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
}
