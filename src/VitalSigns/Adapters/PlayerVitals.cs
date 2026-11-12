using System;
using System.Collections.Generic;
using ExiledToolkit.Game;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using Il2CppInterop.Runtime;
using VitalSigns.Core;
using VitalSigns.Core.Acts;

namespace VitalSigns.Adapters;

/// <summary>
/// Reads the player as the HUD needs it: how each stat's changes are scaled, the running buffs that end, the game
/// clock, carried weight, and the Well Fed line. Read only; nothing the game keeps is changed. Null before a player
/// exists.
/// </summary>
internal static class PlayerVitals
{
    /// <summary>The HUD's rows, top to bottom. The game calls Life "Vitality".</summary>
    private static readonly (AttrName Gain, AttrName Loss, AttrName NaturalGain, AttrName NaturalLoss)[] Rows =
    {
        (AttrName.SatietyAdd_Ratio, AttrName.SatietyDel_Ratio, AttrName.SatietyAutoAdd_Ratio, AttrName.SatietyAutoDel_Ratio),
        (AttrName.MoraleAdd_Ratio, AttrName.MoraleDel_Ratio, AttrName.MoraleAutoAdd_Ratio, AttrName.MoraleAutoDel_Ratio),
        (AttrName.StaminaAdd_Ratio, AttrName.StaminaDel_Ratio, AttrName.StaminaAutoAdd_Ratio, AttrName.StaminaAutoDel_Ratio),
        (AttrName.VitalityAdd_Ratio, AttrName.VitalityDel_Ratio, AttrName.VitalityAutoAdd_Ratio, AttrName.VitalityAutoDel_Ratio)
    };

    private static readonly Dictionary<int, int> Counts = new();
    private static readonly System.Diagnostics.Stopwatch SinceCount = new();

    /// <summary>Home counts walk every storage, and only change when the player moves items, so they are read every few seconds at most.</summary>
    private const double CountSeconds = 10;

    public static Vitals? Read(Options options, bool isChinese)
    {
        var world = GameSingleton.Existing<BattleLogicWorld>();
        var role = world?._AgentManager?.GetLeadingRole();
        var clock = world?._GameTimeManager;
        var attributes = role == null ? null : AgentTools.GetAgentComponent<AttributeComponent>(role);

        if (role == null || clock == null || attributes?.AttrDict == null) return null;

        var stats = Stats(attributes);
        var component = AgentTools.GetAgentComponent<BuffComponent>(role);
        var buffs = Buffs(component);
        var now = clock.GetTotalSeconds();

        var weight = Weight(AgentTools.GetAgentComponent<BagComponent>(role));

        return new Vitals(stats, buffs, now, weight, WellFed(attributes), Details(world!, attributes, component, stats, buffs, now, options, isChinese),
            Fitness(attributes), Levels(attributes), Heavy(world!, role.InstanceId, weight, options, isChinese));
    }

    private static readonly List<BagStack> Stacks = new();
    private static IReadOnlyList<HeavyItem>? _heavy;
    private static (int Carried, long Owner, int Most, bool IsChinese)? _heavyKey;

    /// <summary>
    /// The heaviest kinds of item in the bag, read again only when the carried weight, the player, the list's length or
    /// the language changes: the game builds a new list of the bag for every ask.
    /// </summary>
    private static IReadOnlyList<HeavyItem>? Heavy(BattleLogicWorld world, long owner, CarryWeight? weight, Options options, bool isChinese)
    {
        if (weight is not { } carried || !options.Weight || options.HeavyItems <= 0) return null;

        var key = (carried.Carried, owner, options.HeavyItems, isChinese);

        if (_heavyKey == key) return _heavy;

        _heavyKey = key;
        _heavy = null;

        var items = world._ItemManager;
        var config = GameSingleton.Existing<ConfigManager>();

        if (items == null || config?._Config_Item_Dict == null) return null;

        try
        {
            var list = items.GetItemDataList(owner);

            Stacks.Clear();

            for (var i = 0; list != null && i < list.Count; i++)
            {
                var item = list[i];

                if (item == null || item._ItemCount_k__BackingField <= 0 || !config._Config_Item_Dict.ContainsKey(item._ItemConfigId_k__BackingField)) continue;

                Stacks.Add(new BagStack(item._ItemConfigId_k__BackingField, items.GetItemActualWeight(item), item._ItemCount_k__BackingField));
            }

            var heaviest = Core.Heaviest.Of(Stacks, Math.Min(options.HeavyItems, Core.Heaviest.Max));
            var named = new List<HeavyItem>(heaviest.Count);

            foreach (var kind in heaviest) named.Add(new HeavyItem(BuffBook.Item(config, kind.Id, isChinese).Name, kind.Grams, kind.Count));

            _heavy = named;
        }
        catch (Exception)
        {
            // A game change to the bag's types leaves the hover box with the weight alone.
        }

        return _heavy;
    }

    /// <summary>Each stat's value and maximum, for the warnings and the bar clicks. A stat the player lacks reads 0 of 0, which never warns.</summary>
    private static List<StatLevel> Levels(AttributeComponent attributes) =>
        StatReads.Levels(id => attributes.AttrDict.ContainsKey((AttrName)id), id => attributes.GetTotalValue_Float((AttrName)id), id => attributes.GetTotalValue_Int((AttrName)id));

    /// <summary>Fitness the way the game reads its own bars (LeadingRole.SyncAttr2UI): the value rounded from its float total, the maximum a whole number.</summary>
    private static FitnessReading? Fitness(AttributeComponent attributes)
    {
        if (!attributes.AttrDict.ContainsKey(AttrName.Health)) return null;

        return new FitnessReading((int)Math.Round(attributes.GetTotalValue_Float(AttrName.Health)), attributes.GetTotalValue_Int(AttrName.MaxHealth));
    }

    private static Details? Details(BattleLogicWorld world, AttributeComponent attributes, BuffComponent? component, List<StatRatios> stats, List<BuffTime> buffs,
        int now, Options options, bool isChinese)
    {
        var config = GameSingleton.Existing<ConfigManager>();

        if (config?._Config_Buff_Dict == null || config._Config_Effect_Dict == null) return null;

        var rules = new Dictionary<int, BuffRule>();
        var ruleList = new List<BuffRule>();
        var suppressedBy = new List<string>();

        var good = new List<int>();

        foreach (var buff in buffs)
        {
            if (rules.ContainsKey(buff.ConfigId) || BuffBook.Rule(config, buff.ConfigId, isChinese) is not { } rule) continue;

            rules[buff.ConfigId] = rule;
            ruleList.Add(rule);

            if (rule.IsGood) good.Add(rule.Id);
        }

        var running = new List<int>();

        if (component?.buffArgsMap != null)
        {
            foreach (var args in component.buffArgsMap.Values)
            {
                var buffConfig = args?.config;

                if (buffConfig == null) continue;

                running.Add(buffConfig.ID);

                if (BuffBook.HasTag(buffConfig, "DebuffSuppressor") && BuffBook.Rule(config, buffConfig.ID, isChinese) is { } suppressor && !suppressedBy.Contains(suppressor.Name))
                {
                    suppressedBy.Add(suppressor.Name);
                }
            }
        }

        var cold = BuffBook.ColdFactor(config, running);
        var losses = new double[BuffRule.Stats];

        for (var i = 0; i < losses.Length; i++) losses[i] = stats[i].Loss;

        var max = StatReads.Maxes(id => attributes.AttrDict.ContainsKey((AttrName)id), id => attributes.GetTotalValue_Int((AttrName)id), 100);

        return new Details(max, ComingLoss.Of(now, buffs, rules, cold, losses), options.Tooltips ? ruleList : Array.Empty<BuffRule>(), cold,
            options.Tooltips && options.Cures ? Items(world, config, ruleList, isChinese) : Array.Empty<ItemCount>(), string.Join(" / ", suppressedBy),
            options.Tooltips ? BuffBook.Foods(config, isChinese) : Array.Empty<string>(), FoodMods(attributes),
            options.Tooltips ? GainSources.Of(GainParts(world, component, config, isChinese), BuffRule.Stats) : null, good);
    }

    /// <summary>
    /// What each running buff adds to a stat's gain ratio, as the game itself recorded it when the buff applied
    /// (AE_BuffAdd5Attr.AddHistory, kept in the buff effect's AttrHistory). A suppressed buff's record is not applied and
    /// is left out. Only read: the history is looked up, never made.
    /// </summary>
    private static List<(int Stat, string Name, double Ratio)> GainParts(BattleLogicWorld world, BuffComponent? component, ConfigManager config, bool isChinese)
    {
        var parts = new List<(int Stat, string Name, double Ratio)>();
        var effects = world._BTEffectManager;
        var map = component?.buffArgsMap;

        if (effects == null || map == null) return parts;

        try
        {
            var key = Il2CppType.Of<AttrHistory>();
            var scaling = (double)GameKey.Attr_ScalingRatio;

            if (!(scaling > 0)) return parts;

            foreach (var buff in map.Values)
            {
                var buffConfig = buff?.config;
                var children = buffConfig == null ? null : effects.GetBtArgsById(buff!.EffectBtArgsID)?.ChildArgsMap;

                if (children == null || !children.ContainsKey(key)) continue;

                var records = children[key]?.TryCast<AttrHistory>()?.AttrDataList;

                if (records == null) continue;

                var name = BuffBook.Rule(config, buffConfig!.ID, isChinese)?.Name ?? ("#" + buffConfig.ID);

                foreach (var record in records.Values)
                {
                    if (record == null || !record.IsApplied) continue;

                    var stat = Array.FindIndex(Rows, row => row.Gain == record.Name);

                    if (stat >= 0) parts.Add((stat, name, record.Value / scaling));
                }
            }
        }
        catch (Exception)
        {
            // A game change to these types leaves the hover box with just the total.
        }

        return parts;
    }

    /// <summary>Every cure and suppressor the shown buffs name, with how many the player has in the bag, at home and in the drawer (the game's crafting count).</summary>
    private static List<ItemCount> Items(BattleLogicWorld world, ConfigManager config, List<BuffRule> rules, bool isChinese)
    {
        var items = new List<ItemCount>();
        var manager = world._ItemManager;
        var isCountDue = !SinceCount.IsRunning || SinceCount.Elapsed.TotalSeconds >= CountSeconds;

        if (isCountDue)
        {
            Counts.Clear();
            SinceCount.Restart();
        }

        foreach (var rule in rules)
        {
            Add(rule.Cures);
            Add(rule.Suppressors);
        }

        return items;

        void Add(IReadOnlyList<int> ids)
        {
            foreach (var id in ids)
            {
                if (items.Exists(item => item.Id == id)) continue;

                if (!Counts.TryGetValue(id, out var count))
                {
                    count = Count(manager, id);
                    Counts[id] = count;
                }

                var (name, icon) = BuffBook.Item(config, id, isChinese);

                items.Add(new ItemCount(id, name, icon, count));
            }
        }
    }

    private static int Count(ItemManager? manager, int itemId)
    {
        try
        {
            return manager?.GetAvailableItemCount(itemId) ?? 0;
        }
        catch (Exception)
        {
            return 0;
        }
    }

    /// <summary>The player's own food modifiers that are not zero: how much more or less each food type feeds Satiety and Morale.</summary>
    private static List<FoodMod> FoodMods(AttributeComponent attributes)
    {
        var mods = new List<FoodMod>();

        for (var food = 1; food <= BuffRule.FoodTypes; food++)
        {
            AddMod(0, (AttrName)(60000 + food), food);
            AddMod(1, (AttrName)(70000 + food), food);
        }

        return mods;

        void AddMod(int stat, AttrName name, int food)
        {
            var ratio = Ratio(attributes, name);

            if (Math.Abs(ratio) > 0.00005) mods.Add(new FoodMod(stat, food, ratio));
        }
    }

    /// <summary>
    /// The player's modifiers on a stat change, for what a click plans: the stats' Gain and Loss ratios, every food
    /// type's ratio and the difficulty's bonus on Life from items. Null before a player exists.
    /// </summary>
    public static StatScale? ReadScale()
    {
        var role = GameSingleton.Existing<BattleLogicWorld>()?._AgentManager?.GetLeadingRole();
        var attributes = role == null ? null : AgentTools.GetAgentComponent<AttributeComponent>(role);

        if (attributes?.AttrDict == null) return null;

        double lifeBonus;

        try
        {
            lifeBonus = DifficultyTools.GetVitalityItemAddDelta();
        }
        catch (Exception)
        {
            lifeBonus = 0;
        }

        return new StatScale(Stats(attributes), FoodMods(attributes), lifeBonus);
    }

    /// <summary>Each bar's value and maximum now, for what a click plans: the HUD's reading may be two seconds old. Null before a player exists.</summary>
    public static IReadOnlyList<StatLevel>? ReadLevels()
    {
        var role = GameSingleton.Existing<BattleLogicWorld>()?._AgentManager?.GetLeadingRole();
        var attributes = role == null ? null : AgentTools.GetAgentComponent<AttributeComponent>(role);

        return attributes?.AttrDict == null ? null : Levels(attributes);
    }

    /// <summary>The game clock alone, for the checks that send nothing else.</summary>
    public static int Now() => GameSingleton.Existing<BattleLogicWorld>()?._GameTimeManager?.GetTotalSeconds() ?? 0;

    private static List<StatRatios> Stats(AttributeComponent attributes)
    {
        var stats = new List<StatRatios>(Rows.Length);

        foreach (var row in Rows)
        {
            stats.Add(new StatRatios(Ratio(attributes, row.Gain), Ratio(attributes, row.Loss), Ratio(attributes, row.NaturalGain), Ratio(attributes, row.NaturalLoss)));
        }

        return stats;
    }

    /// <summary>The game's float total is already divided down to a plain ratio and held to the stat's limits.</summary>
    private static double Ratio(AttributeComponent attributes, AttrName name) =>
        attributes.AttrDict.ContainsKey(name) ? attributes.GetTotalValue_Float(name) : 0;

    /// <summary>Buffs the HUD shows, with or without an end; hidden buffs have no badge. Sorted the way the badges are matched.</summary>
    private static List<BuffTime> Buffs(BuffComponent? component)
    {
        var buffs = new List<BuffTime>();
        var running = component?.buffArgsMap;

        if (running == null) return buffs;

        foreach (var buff in running.Values)
        {
            var config = buff?.config;

            if (buff == null || config == null || config.IsHide) continue;

            buffs.Add(new BuffTime(config.ID, buff.TimeEndTime, (int)Math.Max(0, Math.Round(config.BuffDuring)), Math.Max(1, buff.BuffCount), buff.IsSuppressed));
        }

        buffs.Sort((a, b) => a.ConfigId != b.ConfigId ? a.ConfigId.CompareTo(b.ConfigId) : a.EndsAt.CompareTo(b.EndsAt));

        return buffs;
    }

    private static CarryWeight? Weight(BagComponent? bag)
    {
        if (bag == null) return null;

        var max = bag.GetMaxBurden();

        return max > 0 ? new CarryWeight(bag.GetBagWeight(), max, bag.GetOverweightThreshold()) : null;
    }

    private static WellFedLine? WellFed(AttributeComponent attributes)
    {
        if (GameTexts.WellFedRule() is not { } rule || !attributes.AttrDict.ContainsKey(AttrName.MaxSatiety)) return null;

        return new WellFedLine(rule.From, rule.Until, attributes.GetTotalValue_Int(AttrName.MaxSatiety));
    }
}
