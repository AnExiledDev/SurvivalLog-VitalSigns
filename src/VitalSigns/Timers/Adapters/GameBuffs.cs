using System;
using System.Collections.Generic;
using System.Linq;
using GameCore.HotUpdate;
using VitalSigns.Adapters;
using VitalSigns.Timers.Core;

namespace VitalSigns.Timers.Adapters;

/// <summary>
/// The buffs the tab lists, read from the game's own buff table and language files, so a game update needs no mod
/// update. Read once the game has its configs, then kept: the table does not change while the game runs.
/// </summary>
internal static class GameBuffs
{
    private static IReadOnlyList<BuffEntry>? _entries;

    public static IReadOnlyList<BuffEntry>? Entries(Action<string> log)
    {
        if (_entries != null) return _entries;

        var config = GameSingleton.Existing<ConfigManager>();
        var table = config?._Config_Buff_Dict;

        if (config == null || table == null || table.Count == 0) return null;

        var entries = new List<BuffEntry>();

        foreach (var buff in table.Values)
        {
            if (buff == null || !Scope.IsTimedVisible(buff.IsHide, buff.BuffDuring)) continue;

            entries.Add(new BuffEntry(buff.ID, buff.IsGood, buff.BuffDisplayType, buff.BuffDuring,
                Text(config, buff.Name, LanguageType.English) ?? "", Text(config, buff.Name, LanguageType.Chinese) ?? NonEmpty(buff.Name_Local) ?? "",
                Text(config, buff.Des, LanguageType.English) ?? "", Text(config, buff.Des, LanguageType.Chinese) ?? NonEmpty(buff.Des_Local) ?? ""));
        }

        _entries = entries.OrderBy(entry => entry.Id).ToList();
        log($"{_entries.Count} visible timed buffs listed");

        return _entries;
    }

    /// <summary>The game answers a key it has no text for with the key itself.</summary>
    private static string? Text(ConfigManager config, string? key, LanguageType language)
    {
        if (string.IsNullOrEmpty(key)) return null;

        try
        {
            var text = config.GetLocalTxtByLanguage(key, language);

            return string.IsNullOrWhiteSpace(text) || text == key ? null : text;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string? NonEmpty(string? text) => string.IsNullOrWhiteSpace(text) ? null : text;
}
