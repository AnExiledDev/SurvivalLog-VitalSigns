using System;
using GameCore.HotUpdate;
using VitalSigns.Core;

namespace VitalSigns.Adapters;

/// <summary>
/// What the mod takes from the game's tables: the Well Fed rule and the names of the buffs it quotes. The tables do
/// not change while the game runs, so each is read once (the names once per language) after the game has loaded them.
/// </summary>
internal static class GameTexts
{
    /// <summary>The hidden passive that gives and takes Well Fed (Config_Buff 10009: Satiety at 80 or more adds Well Fed, below 60 removes it).</summary>
    private const int WellFedPassive = 10009;

    private static (int Buff, int From, int Until)? _wellFed;
    private static bool _isWellFedRead;
    private static GameNames? _names;
    private static bool _namesAreChinese;

    public static (int From, int Until)? WellFedRule() => WellFed() is { } rule ? (rule.From, rule.Until) : null;

    public static GameNames Names(bool isChinese)
    {
        if (_names != null && _namesAreChinese == isChinese) return _names;

        var config = GameSingleton.Existing<ConfigManager>();

        if (config?._Config_Buff_Dict == null || config._Config_Buff_Dict.Count == 0) return Fallback(isChinese);

        var language = isChinese ? LanguageType.Chinese : LanguageType.English;
        var wellFedBuff = WellFed()?.Buff ?? 0;

        _names = new GameNames(
            BuffName(config, wellFedBuff, language) ?? Fallback(isChinese).WellFed,
            BuffName(config, GameKey.Buff_BagOverweight, language) ?? Fallback(isChinese).Overweight);
        _namesAreChinese = isChinese;

        return _names;
    }

    private static (int Buff, int From, int Until)? WellFed()
    {
        if (!_isWellFedRead) ReadWellFed();

        return _wellFed;
    }

    /// <summary>The passive's effect holds the buff it gives (IntValue0), the Satiety that gives it (1) and the Satiety below which it goes (3).</summary>
    private static void ReadWellFed()
    {
        var config = GameSingleton.Existing<ConfigManager>();
        var buffs = config?._Config_Buff_Dict;
        var effects = config?._Config_Effect_Dict;

        if (buffs == null || effects == null || buffs.Count == 0) return;

        _isWellFedRead = true;

        if (!buffs.ContainsKey(WellFedPassive)) return;

        var passive = buffs[WellFedPassive];

        if (passive == null || !effects.ContainsKey(passive.EffectConfigId)) return;

        var effect = effects[passive.EffectConfigId];

        if (effect != null && effect.IntValue1 > 0 && effect.IntValue3 > 0) _wellFed = (effect.IntValue0, effect.IntValue1, effect.IntValue3);
    }

    /// <summary>The game answers a key it has no text for with the key itself.</summary>
    private static string? BuffName(ConfigManager config, int buffId, LanguageType language)
    {
        try
        {
            if (!config._Config_Buff_Dict.ContainsKey(buffId)) return null;

            var key = config._Config_Buff_Dict[buffId]?.Name;
            var text = string.IsNullOrEmpty(key) ? null : config.GetLocalTxtByLanguage(key, language);

            return string.IsNullOrWhiteSpace(text) || text == key ? null : text.Trim();
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static GameNames Fallback(bool isChinese) => isChinese ? new GameNames("饱腹富余", "超重") : new GameNames("Well Fed", "Overweight");
}
