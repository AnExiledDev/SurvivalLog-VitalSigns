using System;
using System.Collections.Generic;
using System.Linq;
using GameCore.HotUpdate;
using VitalSigns.Adapters;
using VitalSigns.Timers.Core;

namespace VitalSigns.Timers.Adapters;

/// <summary>
/// The buffs the tab lists, read from the game's own buff table and language files, so a game update needs no mod
/// update. Kept once the game has its configs and both language tables: neither changes while the game runs.
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

        var isChinese = GameLanguage.IsChinese();
        var entries = new List<BuffEntry>();

        foreach (var buff in table.Values)
        {
            if (buff == null || !Scope.IsTimedVisible(buff.IsHide, buff.BuffDuring)) continue;

            var name = BuffText.Pick(isChinese, Current(config, buff.Name), Text(config, buff.Name, LanguageType.English), Text(config, buff.Name, LanguageType.Chinese), NonEmpty(buff.Name_Local));
            var des = BuffText.Pick(isChinese, Current(config, buff.Des), Text(config, buff.Des, LanguageType.English), Text(config, buff.Des, LanguageType.Chinese), NonEmpty(buff.Des_Local));

            entries.Add(new BuffEntry(buff.ID, buff.IsGood, buff.BuffDisplayType, buff.BuffDuring, name.En, name.Zh, des.En, des.Zh));
        }

        var list = entries.OrderBy(entry => entry.Id).ToList();

        // Until the game has filled both language tables, the other language's text is missing: read again next time.
        if (!BothLanguagesLoaded(config)) return list;

        _entries = list;
        log($"{_entries.Count} visible timed buffs listed");

        return _entries;
    }

    /// <summary>The text of the language the game is showing, which it has from the start.</summary>
    private static string? Current(ConfigManager config, string? key)
    {
        if (string.IsNullOrEmpty(key)) return null;

        try
        {
            var text = config.GetLocalTxt(key);

            return string.IsNullOrWhiteSpace(text) || text == key ? null : text;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static bool BothLanguagesLoaded(ConfigManager config)
    {
        try
        {
            var cache = config.customCache;

            return cache?.EnglishLanguageMap != null && cache.ChineseLanguageMap != null;
        }
        catch (Exception)
        {
            return false;
        }
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
