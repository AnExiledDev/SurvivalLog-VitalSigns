using System;
using System.Collections.Generic;
using System.Linq;
using ExiledToolkit.Game;
using GameCore.HotUpdate;
using VitalSigns.Timers.Core;

namespace VitalSigns.Timers.Adapters;

/// <summary>
/// Every buff the game shows (not its hidden ones), for the buff pickers on the Mods screen, read from the game's
/// buff table and language files, so a game update needs no mod update. Kept once the game has its configs and both
/// language tables: neither changes while the game runs.
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
            if (buff == null || buff.IsHide) continue;

            var name = BuffText.Pick(isChinese, GameNames.Translate(config, buff.Name), GameNames.Translate(config, buff.Name, LanguageType.English),
                GameNames.Translate(config, buff.Name, LanguageType.Chinese), GameNames.NonEmpty(buff.Name_Local));
            var des = BuffText.Pick(isChinese, GameNames.Translate(config, buff.Des), GameNames.Translate(config, buff.Des, LanguageType.English),
                GameNames.Translate(config, buff.Des, LanguageType.Chinese), GameNames.NonEmpty(buff.Des_Local));

            entries.Add(new BuffEntry(buff.ID, buff.IsGood, buff.BuffDisplayType, buff.BuffDuring, name.En, name.Zh, des.En, des.Zh));
        }

        var list = entries.OrderBy(entry => entry.Id).ToList();

        // Until the game has filled both language tables, the other language's text is missing: read again next time.
        if (!BothLanguagesLoaded(config)) return list;

        _entries = list;
        log($"{_entries.Count} shown buffs listed, {_entries.Count(entry => entry.IsTimed)} of them timed");

        return _entries;
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
}
