using System;
using System.Collections.Generic;
using GameCore.HotUpdate;
using VitalSigns.Core;

namespace VitalSigns.Adapters;

/// <summary>
/// What the tooltips quote from the game's tables: each buff's effect on the stats, the items that cure or hold back a
/// debuff, item and food type names. Rules are read the first time a buff shows and kept per language; the cure map is
/// one pass over the item table, kept until the game swaps its tables.
/// </summary>
internal static class BuffBook
{
    private const string ColdTag = "ColdDebuff";
    private const string SuppressibleTag = "SuppressibleDebuff";
    private const string SuppressorTag = "DebuffSuppressor";
    private const string ColdReduceEffect = "Buff/AE_ColdDebuffReduce";
    private const string CondOnOtherBuffEffect = "Buff/Buff_CondOnOtherBuff";

    private static readonly Dictionary<int, BuffRule> Rules = new();
    private static readonly Dictionary<int, (string Name, string Icon)> Items = new();
    private static readonly Dictionary<int, List<int>> CuresOf = new();
    private static readonly Dictionary<int, List<int>> SuppressorsOf = new();
    private static readonly List<int> SuppressAll = new();
    private static readonly List<string> FoodNames = new();
    private static readonly int[] NoItems = Array.Empty<int>();

    private static object? _buffTable;
    private static bool _isChinese;
    private static bool _isCureMapRead;

    /// <summary>The rule for a buff id, or null when the tables are not loaded or have no such buff.</summary>
    public static BuffRule? Rule(ConfigManager config, int buffId, bool isChinese)
    {
        if (!IsCurrent(config, isChinese)) return null;

        if (Rules.TryGetValue(buffId, out var known)) return known;

        var rule = ReadRule(config, buffId, isChinese);

        if (rule != null) Rules[buffId] = rule;

        return rule;
    }

    public static (string Name, string Icon) Item(ConfigManager config, int itemId, bool isChinese)
    {
        if (Items.TryGetValue(itemId, out var known)) return known;

        var item = config._Config_Item_Dict.TryGetValue(itemId, out var row) && row != null ? (Text(config, row.ItemName, isChinese) ?? "#" + itemId, row.WebIcon ?? "") : ("#" + itemId, "");

        Items[itemId] = item;

        return item;
    }

    /// <summary>Food type names in order, 1 to 11.</summary>
    public static IReadOnlyList<string> Foods(ConfigManager config, bool isChinese)
    {
        if (!IsCurrent(config, isChinese)) return FoodNames;

        if (FoodNames.Count > 0) return FoodNames;

        var types = config._Config_FoodType_Dict;

        for (var food = 1; food <= BuffRule.FoodTypes; food++)
        {
            FoodNames.Add(types != null && types.TryGetValue(food, out var row) && row != null ? Text(config, row.Name, isChinese) ?? "#" + food : "#" + food);
        }

        return FoodNames;
    }

    /// <summary>Cold resistance: 1 less the sum of every running cold reduction, as the game cuts a cold debuff's interval losses.</summary>
    public static double ColdFactor(ConfigManager config, IEnumerable<int> runningBuffIds)
    {
        var reduce = 0.0;

        foreach (var id in runningBuffIds)
        {
            if (Effect(config, id) is { } effect && effect.ConfigName == ColdReduceEffect) reduce += effect.FloatValue0;
        }

        return Math.Clamp(1 - reduce, 0, 1);
    }

    /// <summary>A config reload swaps the tables, and names change with the language: either drops what was read.</summary>
    private static bool IsCurrent(ConfigManager config, bool isChinese)
    {
        var table = config._Config_Buff_Dict;

        if (table == null || table.Count == 0) return false;

        if (ReferenceEquals(table, _buffTable) && isChinese == _isChinese) return true;

        if (!ReferenceEquals(table, _buffTable)) _isCureMapRead = false;

        _buffTable = table;
        _isChinese = isChinese;
        Rules.Clear();
        Items.Clear();
        FoodNames.Clear();

        return true;
    }

    private static BuffRule? ReadRule(ConfigManager config, int buffId, bool isChinese)
    {
        if (!config._Config_Buff_Dict.TryGetValue(buffId, out var buff) || buff == null) return null;

        if (!_isCureMapRead) ReadCureMap(config);

        var effect = config._Config_Effect_Dict.TryGetValue(buff.EffectConfigId, out var row) ? row : null;
        var isSuppressible = HasTag(buff, SuppressibleTag);
        var suppressors = new List<int>();

        if (isSuppressible) suppressors.AddRange(SuppressAll);

        if (SuppressorsOf.TryGetValue(buffId, out var own))
        {
            foreach (var item in own)
            {
                if (!suppressors.Contains(item)) suppressors.Add(item);
            }
        }

        var empty = new double[BuffRule.Stats];
        var noFood = new double[BuffRule.FoodTypes];

        return new BuffRule(buffId, Text(config, buff.Name, isChinese) ?? "#" + buffId, buff.IsGood, HasTag(buff, ColdTag), isSuppressible,
            effect?.BuffInterval ?? 0,
            effect == null ? empty : new double[] { effect.BuffSatietyInterval, effect.BuffMoraleInterval, effect.BuffStaminaInterval, effect.BuffVitalityInterval },
            effect == null ? empty : new double[] { effect.BuffSatietyStart, effect.BuffMoraleStart, effect.BuffStaminaStart, effect.BuffVitalityStart },
            effect == null ? empty : new double[] { effect.BuffSatietyEnd, effect.BuffMoraleEnd, effect.BuffStaminaEnd, effect.BuffVitalityEnd },
            effect == null ? empty : new double[] { effect.BuffSatietyMax, effect.BuffMoraleMax, effect.BuffStaminaMax, effect.BuffVitalityMax },
            effect == null ? empty : new double[] { effect.BuffSatietyAdd, effect.BuffMoraleAdd, effect.BuffStaminaAdd, effect.BuffVitalityAdd },
            effect == null ? empty : new double[] { effect.BuffSatietyDel, effect.BuffMoraleDel, effect.BuffStaminaDel, effect.BuffVitalityDel },
            effect?.BuffSpeed ?? 0,
            effect == null ? noFood : SatietyFood(effect),
            effect == null ? noFood : MoraleFood(effect),
            CuresOf.TryGetValue(buffId, out var cures) ? cures : NoItems,
            suppressors);
    }

    private static double[] SatietyFood(Config_Effect e) => new double[]
    {
        e.BuffSatietyFood1Add_Ratio, e.BuffSatietyFood2Add_Ratio, e.BuffSatietyFood3Add_Ratio, e.BuffSatietyFood4Add_Ratio, e.BuffSatietyFood5Add_Ratio,
        e.BuffSatietyFood6Add_Ratio, e.BuffSatietyFood7Add_Ratio, e.BuffSatietyFood8Add_Ratio, e.BuffSatietyFood9Add_Ratio, e.BuffSatietyFood10Add_Ratio,
        e.BuffSatietyFood11Add_Ratio
    };

    private static double[] MoraleFood(Config_Effect e) => new double[]
    {
        e.EF_BuffMoraleFood1Add_Ratio, e.EF_BuffMoraleFood2Add_Ratio, e.EF_BuffMoraleFood3Add_Ratio, e.EF_BuffMoraleFood4Add_Ratio, e.EF_BuffMoraleFood5Add_Ratio,
        e.EF_BuffMoraleFood6Add_Ratio, e.EF_BuffMoraleFood7Add_Ratio, e.EF_BuffMoraleFood8Add_Ratio, e.EF_BuffMoraleFood9Add_Ratio, e.EF_BuffMoraleFood10Add_Ratio,
        e.EF_BuffMoraleFood11Add_Ratio
    };

    /// <summary>
    /// Every usable item's action effect: the buffs it removes are its cures. A buff it adds that is a suppressor holds
    /// back every suppressible debuff; a buff it adds that only works while another buff runs holds back that one.
    /// </summary>
    private static void ReadCureMap(ConfigManager config)
    {
        _isCureMapRead = true;
        CuresOf.Clear();
        SuppressorsOf.Clear();
        SuppressAll.Clear();

        var actions = config._Config_Action_Dict;
        var effects = config._Config_Effect_Dict;
        var buffs = config._Config_Buff_Dict;

        if (config._Config_Item_Dict == null || actions == null || effects == null) return;

        foreach (var item in config._Config_Item_Dict.Values)
        {
            if (item == null || item.CantUse || item.UseAction <= 0) continue;

            if (!actions.TryGetValue(item.UseAction, out var action) || action == null) continue;

            if (!effects.TryGetValue(action.EffectConfigID, out var effect) || effect == null) continue;

            if (effect.EDelBuff != null)
            {
                foreach (var removed in effect.EDelBuff) AddTo(CuresOf, removed, item.ID);
            }

            if (effect.EAddBuff == null) continue;

            foreach (var added in effect.EAddBuff)
            {
                if (!buffs.TryGetValue(added, out var addedBuff) || addedBuff == null) continue;

                if (HasTag(addedBuff, SuppressorTag) && !SuppressAll.Contains(item.ID)) SuppressAll.Add(item.ID);

                if (effects.TryGetValue(addedBuff.EffectConfigId, out var addedEffect) && addedEffect?.ConfigName == CondOnOtherBuffEffect)
                {
                    AddTo(SuppressorsOf, addedEffect.IntValue0, item.ID);
                }
            }
        }
    }

    private static void AddTo(Dictionary<int, List<int>> map, int key, int item)
    {
        if (!map.TryGetValue(key, out var list)) map[key] = list = new List<int>();

        if (!list.Contains(item)) list.Add(item);
    }

    private static Config_Effect? Effect(ConfigManager config, int buffId) =>
        config._Config_Buff_Dict.TryGetValue(buffId, out var buff) && buff != null && config._Config_Effect_Dict.TryGetValue(buff.EffectConfigId, out var effect) ? effect : null;

    public static bool HasTag(Config_Buff buff, string tag)
    {
        var tags = buff.TagList;

        if (tags == null) return false;

        for (var i = 0; i < tags.Count; i++)
        {
            if (tags[i] == tag) return true;
        }

        return false;
    }

    /// <summary>The game answers a key it has no text for with the key itself.</summary>
    private static string? Text(ConfigManager config, string? key, bool isChinese)
    {
        if (string.IsNullOrEmpty(key)) return null;

        try
        {
            var text = config.GetLocalTxtByLanguage(key, isChinese ? LanguageType.Chinese : LanguageType.English);

            return string.IsNullOrWhiteSpace(text) || text == key ? null : text.Trim();
        }
        catch (Exception)
        {
            return null;
        }
    }
}
