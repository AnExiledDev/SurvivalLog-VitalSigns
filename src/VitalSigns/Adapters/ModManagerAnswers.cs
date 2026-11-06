using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ExiledToolkit.Game;
using ExiledToolkit.Web;
using GameCore.HotUpdate;
using VitalSigns.Core;
using VitalSigns.Core.Acts;
using VitalSigns.Timers.Adapters;

namespace VitalSigns.Adapters;

/// <summary>
/// What the Mod Manager's pickers offer for this mod's lists, found by name so the mod needs no reference to it:
/// the food, Life items, Morale actions, beds and buffs the game's own tables hold, each with both names, its picture
/// and what it does, filed under the game's own categories. Asked on the main thread when the Mods screen reads the
/// settings; the item lists are kept until the game's tables change.
/// </summary>
public static class ModManagerAnswers
{
    private static readonly Dictionary<string, (int Version, string Text)> Kept = new(StringComparer.Ordinal);
    private static object? _config;

    internal static Action<string> Log { get; set; } = _ => { };

    /// <summary>The BedModes value as saved, so a bed given a mode stays listed even away from the base.</summary>
    internal static Func<string> SavedBedModes { get; set; } = () => "";

    public static string Entries(string section, string key)
    {
        var config = GameSingleton.Existing<ConfigManager>();

        if (config == null) return "";

        if (!ReferenceEquals(config, _config))
        {
            _config = config;
            Kept.Clear();
        }

        // Read the whole catalog now if the clicks have not needed it yet; the Mods screen asks only when it opens.
        if (!GameCatalog.IsReady && !GameCatalog.Advance(config, () => false)) return "";

        var id = section + "." + key;

        // Beds follow what the base holds, and the buff lists are kept by GameBuffs, so only the item lists are kept here.
        switch (id)
        {
            case "Actions.BedModes":
                return PickerLines.Of(Beds(config));
            case "Hud.FavouriteBuffs":
                return Buffs(config, timedOnly: false);
            case "Timers.Buffs":
                return Buffs(config, timedOnly: true);
        }

        if (Kept.TryGetValue(id, out var kept) && kept.Version == GameCatalog.Version) return kept.Text;

        var text = id switch
        {
            "Actions.FoodSkip" => PickerLines.Of(Items(config, delta => delta.Satiety > 0)),
            "Actions.LifeItems" => PickerLines.Of(Items(config, delta => delta.Life > 0)),
            "Actions.MoraleRoutine" => PickerLines.Of(MoraleActions(config).Concat(Items(config, delta => delta.Morale > 0))),
            _ => "",
        };

        if (text.Length > 0) Kept[id] = (GameCatalog.Version, text);

        return text;
    }

    /// <summary>What a blank list does, shown in place of an empty list.</summary>
    public static string BlankNote(string section, string key) => (section + "." + key) switch
    {
        "Actions.LifeItems" => "Empty: a click uses any item that restores Life, smallest first, from what you have.\n留空：点击时从你身边的物品里使用任何恢复生命的物品，先用小的。",
        "Actions.FoodSkip" => "Empty: a click may eat any food.\n留空：点击时什么食物都可以吃。",
        "Actions.BedModes" => "Empty: every bed is on Auto.\n留空：所有的床都是 Auto。",
        "Actions.MoraleRoutine" => "Empty: no routine yet. With MoraleMode on Routine, a click does these in order.\n留空：还没有日常安排。MoraleMode 为 Routine 时，点击会按顺序做这些。",
        "Hud.FavouriteBuffs" => "Empty: no pinned buffs. Shift-click a buff on the HUD to pin it.\n留空：没有固定的状态。在界面上按住 Shift 点击状态即可固定。",
        "Timers.Buffs" => "Empty: every buff follows its category and global multipliers.\n留空：所有状态都按类别倍数和全局倍数。",
        _ => "",
    };

    private static class Group
    {
        public static readonly (string En, string Zh) Furniture = ("Furniture actions", "家具行动");
        public static readonly (string En, string Zh) InBase = ("In your base", "基地里的");
        public static readonly (string En, string Zh) Built = ("Can be built", "可以建造");
        public static readonly (string En, string Zh) Other = ("Elsewhere", "其他地方");
    }

    /// <summary>Every item kind one use of which changes the stat <paramref name="wanted"/> picks, as "i" and its id, on its category's shelf.</summary>
    private static IEnumerable<PickerEntry> Items(ConfigManager config, Func<StatDelta, bool> wanted)
    {
        var entries = new List<(int Rank, PickerEntry Entry)>();
        var table = config._Config_Item_Dict;
        var groups = new Dictionary<int, (string En, string Zh)>();

        foreach (var (id, item) in GameCatalog.AllItems)
        {
            if (!wanted(item.Delta) || !table.TryGetValue(id, out var row) || row == null) continue;

            var rank = PickerGroups.Rank(row.Category, row.SubCategory);

            if (!groups.TryGetValue(rank, out var group))
            {
                var keyName = PickerGroups.NameKey(row.Category, row.SubCategory);

                group = Names(config, keyName, null, "#" + row.Category.ToString(CultureInfo.InvariantCulture));
                groups[rank] = group;
            }

            var (en, zh) = Names(config, row.ItemName, row.ItemName_Local, "#" + id);
            var note = PickerLines.Delta(item.Delta);

            entries.Add((rank, new PickerEntry(ActKeys.Item(id), en, zh, Icon(row.WebIcon, row.Icon), group.En, group.Zh, note.En, note.Zh)));
        }

        return entries.OrderBy(pair => pair.Rank).ThenBy(pair => pair.Entry.NameEn, StringComparer.OrdinalIgnoreCase).ThenBy(pair => pair.Entry.Id, StringComparer.Ordinal)
            .Select(pair => pair.Entry);
    }

    /// <summary>Every furniture action that raises Morale, as "f" and its id, noting the first furniture that offers it.</summary>
    private static List<PickerEntry> MoraleActions(ConfigManager config)
    {
        var offeredBy = new Dictionary<int, Config_Furniture>();

        foreach (var row in config._Config_Furniture_Dict.Values)
        {
            if (row == null) continue;

            foreach (var func in GameCatalog.FuncsOf(row.ID)) offeredBy.TryAdd(func, row);
        }

        var entries = new List<PickerEntry>();

        foreach (var (id, func) in GameCatalog.AllFuncs)
        {
            if (func.IsRest || func.Delta.Morale <= 0) continue;

            var (en, zh) = Names(config, func.NameKey, null, "#" + id);
            var note = PickerLines.Delta(func.Delta);
            string? icon = Icon(func.Icon, null);

            if (offeredBy.TryGetValue(id, out var furniture))
            {
                var (atEn, atZh) = Names(config, furniture.Name, furniture.Name_Local, "#" + furniture.ID);

                note = (note.En + ", at the " + atEn, note.Zh + "，在「" + atZh + "」");
                icon = Icon(furniture.ICON, null) ?? icon;
            }

            entries.Add(new PickerEntry(ActKeys.Furniture(id), en, zh, icon, Group.Furniture.En, Group.Furniture.Zh, note.En, note.Zh));
        }

        return entries.OrderBy(entry => entry.NameEn, StringComparer.OrdinalIgnoreCase).ThenBy(entry => entry.Id, StringComparer.Ordinal).ToList();
    }

    /// <summary>The beds a mode matters for, by furniture id: what packages build, what the base holds and what already has a mode.</summary>
    private static List<PickerEntry> Beds(ConfigManager config)
    {
        var built = new HashSet<int>();

        foreach (var item in config._Config_Item_Dict.Values)
        {
            if (item != null && item.TargetFurnitureID > 0) built.Add(item.TargetFurnitureID);
        }

        var kinds = new List<BedKind>();
        var rows = new Dictionary<int, Config_Furniture>();

        foreach (var row in config._Config_Furniture_Dict.Values)
        {
            if (row == null) continue;

            double sleep = 0, nap = 0;

            foreach (var funcId in GameCatalog.FuncsOf(row.ID))
            {
                if (GameCatalog.Func(funcId) is not { } func) continue;

                if (func.Rest is RestKind.Sleep or RestKind.Beauty) sleep = Math.Max(sleep, func.Delta.Stamina);
                else if (func.Rest == RestKind.Nap) nap = Math.Max(nap, func.Delta.Stamina);
            }

            if (sleep <= 0 && nap <= 0) continue;

            kinds.Add(new BedKind(row.ID, sleep, nap, built.Contains(row.ID)));
            rows[row.ID] = row;
        }

        var inBase = new HashSet<int>(BaseScan.View.Beds.Select(bed => bed.ConfigId));
        var saved = new HashSet<int>(SleepPlan.ParseModes(SavedBedModes()).Keys);
        var picked = BedChoices.Pick(kinds, inBase, saved);
        var names = picked.Select(bed => Names(config, rows[bed.ConfigId].Name, rows[bed.ConfigId].Name_Local, "#" + bed.ConfigId)).ToList();
        var entries = new List<PickerEntry>();

        for (var i = 0; i < picked.Count; i++)
        {
            var bed = picked[i];
            var row = rows[bed.ConfigId];
            var (en, zh) = names[i];
            var group = inBase.Contains(bed.ConfigId) ? Group.InBase : bed.IsBuilt ? Group.Built : Group.Other;
            var note = BedChoices.Note(bed);

            // Two kinds of bed with one name tell themselves apart by their id.
            if (names.Count(other => other.En == en) > 1)
            {
                en += " (#" + bed.ConfigId.ToString(CultureInfo.InvariantCulture) + ")";
                zh += "（#" + bed.ConfigId.ToString(CultureInfo.InvariantCulture) + "）";
            }

            entries.Add(new PickerEntry(bed.ConfigId.ToString(CultureInfo.InvariantCulture), en, zh, Icon(row.ICON, null), group.En, group.Zh, note.En, note.Zh));
        }

        return entries;
    }

    /// <summary>
    /// The buffs the game shows, filed by the kind the game gives them, good ones first; those a talent gives on a shelf of
    /// their own. The game draws no picture for a buff, so a talent's buff shows the talent's, and a buff named after an
    /// item (Bandage, Painkillers) the item's. Buffs that share a name show their id.
    /// </summary>
    private static string Buffs(ConfigManager config, bool timedOnly)
    {
        var buffs = GameBuffs.Entries(Log);

        if (buffs == null) return "";

        var talents = TalentIcons(config);
        var items = ItemIcons(config);
        var shown = buffs.Where(buff => !timedOnly || buff.IsTimed).ToList();
        var named = shown.GroupBy(buff => buff.NameEn, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

        return PickerLines.Of(shown
            .Select(buff => (Buff: buff, Icon: talents.TryGetValue(buff.Id, out var icon) ? icon : items.TryGetValue(buff.NameEn, out var item) ? item : null, Group: PickerGroups.Buff(buff.Category, talents.ContainsKey(buff.Id))))
            .OrderBy(e => e.Group.Rank).ThenBy(e => e.Buff.IsGood ? 0 : 1).ThenBy(e => e.Buff.NameEn, StringComparer.OrdinalIgnoreCase).ThenBy(e => e.Buff.Id)
            .Select(e =>
            {
                var buff = e.Buff;
                var id = buff.Id.ToString(CultureInfo.InvariantCulture);
                var twin = named[buff.NameEn] > 1 ? " (#" + id + ")" : "";
                var time = buff.IsTimed ? PickerLines.Duration(buff.Seconds) : ("", "");
                var noteEn = buff.IsTimed ? "Lasts " + time.Item1 + ". " + buff.DescEn : buff.DescEn;
                var noteZh = buff.IsTimed ? "持续 " + time.Item2 + "。" + buff.DescZh : buff.DescZh;

                return new PickerEntry(id, buff.NameEn + twin, buff.NameZh + twin, e.Icon, e.Group.En, e.Group.Zh, noteEn, noteZh);
            }));
    }

    /// <summary>Each item's picture by its English name, the first item of a name winning.</summary>
    private static Dictionary<string, string> ItemIcons(ConfigManager config)
    {
        var icons = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var row in config._Config_Item_Dict.Values)
        {
            if (row == null || ExiledToolkit.Game.GameNames.Translate(config, row.ItemName, LanguageType.English) is not { } name || icons.ContainsKey(name)) continue;

            if (Icon(row.WebIcon, row.Icon) is { } icon) icons[name] = icon;
        }

        return icons;
    }

    private static Dictionary<int, string> TalentIcons(ConfigManager config)
    {
        var icons = new Dictionary<int, string>();
        var table = config._Config_Talent_Dict;

        if (table == null) return icons;

        foreach (var talent in table.Values)
        {
            if (talent == null || talent.BuffID <= 0 || icons.ContainsKey(talent.BuffID)) continue;

            if (Icon(talent.WebIcon, talent.Icon) is { } icon) icons[talent.BuffID] = icon;
        }

        return icons;
    }

    private static (string En, string Zh) Names(ConfigManager config, string? key, string? local, string fallback)
    {
        var en = ExiledToolkit.Game.GameNames.Translate(config, key, LanguageType.English);
        var zh = ExiledToolkit.Game.GameNames.Translate(config, key, LanguageType.Chinese) ?? ExiledToolkit.Game.GameNames.NonEmpty(local);

        return (en ?? zh ?? fallback, zh ?? en ?? fallback);
    }

    private static string? Icon(string? webIcon, string? icon) => WebIcons.Icons.Resolve(ExiledToolkit.Game.GameNames.NonEmpty(webIcon) ?? ExiledToolkit.Game.GameNames.NonEmpty(icon));
}
