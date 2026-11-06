using System;
using System.Collections.Generic;
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
/// and what it does. Asked on the main thread when the Mods screen reads the settings; each list is kept until the
/// game's tables change.
/// </summary>
public static class ModManagerAnswers
{
    private static readonly Dictionary<string, (int Version, string Text)> Kept = new(StringComparer.Ordinal);
    private static object? _config;

    internal static Action<string> Log { get; set; } = _ => { };

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

        if (Kept.TryGetValue(id, out var kept) && kept.Version == GameCatalog.Version) return kept.Text;

        var text = id switch
        {
            "Actions.FoodSkip" => PickerLines.Of(Items(config, delta => delta.Satiety > 0).OrderBy(item => item.NameEn, StringComparer.OrdinalIgnoreCase)),
            "Actions.LifeItems" => PickerLines.Of(LifeItems(config)),
            "Actions.MoraleRoutine" => PickerLines.Of(MoraleActions(config).Concat(Items(config, delta => delta.Morale > 0, Group.Items))),
            "Actions.BedModes" => PickerLines.Of(Beds(config)),
            "Hud.FavouriteBuffs" => Buffs(timedOnly: false),
            "Timers.Buffs" => Buffs(timedOnly: true),
            _ => "",
        };

        // The buff lists are kept by GameBuffs, which reads again until the game has both languages.
        if (text.Length > 0 && section != "Hud" && section != "Timers") Kept[id] = (GameCatalog.Version, text);

        return text;
    }

    /// <summary>What a blank LifeItems stands for: every item that restores Life, smallest first.</summary>
    public static string Blank(string section, string key)
    {
        if (section != "Actions" || key != "LifeItems") return "";

        var config = GameSingleton.Existing<ConfigManager>();

        if (config == null || (!GameCatalog.IsReady && !GameCatalog.Advance(config, () => false))) return "";

        return ActKeys.Join(LifeItems(config).Select(item => item.Id));
    }

    public static string BlankNote(string section, string key) => section == "Actions" && key == "LifeItems"
        ? "Left blank: every item that restores Life, smallest first. A click uses the ones you have.\n留空：使用所有恢复生命的物品，先用小的。点击时只用你身边有的。"
        : "";

    private static class Group
    {
        public static readonly (string En, string Zh) Items = ("Items", "物品");
        public static readonly (string En, string Zh) Furniture = ("Furniture", "家具");
        public static readonly (string En, string Zh) Good = ("Buffs", "增益");
        public static readonly (string En, string Zh) Bad = ("Debuffs", "负面");
    }

    private static IEnumerable<PickerEntry> LifeItems(ConfigManager config) =>
        Items(config, delta => delta.Life > 0).Select(entry => (entry, GameCatalog.Item(ActKeys.Items(entry.Id)[0])!.Delta.Life))
            .OrderBy(pair => pair.Life).ThenBy(pair => pair.entry.Id, StringComparer.Ordinal).Select(pair => pair.entry);

    /// <summary>Every item kind one use of which changes the stat <paramref name="wanted"/> picks, as "i" and its id.</summary>
    private static List<PickerEntry> Items(ConfigManager config, Func<StatDelta, bool> wanted, (string En, string Zh)? group = null)
    {
        var entries = new List<PickerEntry>();
        var table = config._Config_Item_Dict;

        foreach (var (id, item) in GameCatalog.AllItems.OrderBy(pair => pair.Key))
        {
            if (!wanted(item.Delta) || !table.TryGetValue(id, out var row) || row == null) continue;

            var (en, zh) = Names(config, row.ItemName, row.ItemName_Local, "#" + id);
            var note = PickerLines.Delta(item.Delta);

            entries.Add(new PickerEntry(ActKeys.Item(id), en, zh, Icon(row.WebIcon, row.Icon), group?.En ?? "", group?.Zh ?? "", note.En, note.Zh));
        }

        return entries;
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

        foreach (var (id, func) in GameCatalog.AllFuncs.OrderBy(pair => pair.Key))
        {
            if (func.IsRest || func.Delta.Morale <= 0) continue;

            var (en, zh) = Names(config, func.NameKey, null, "#" + id);
            var note = PickerLines.Delta(func.Delta);

            if (offeredBy.TryGetValue(id, out var furniture))
            {
                var (atEn, atZh) = Names(config, furniture.Name, furniture.Name_Local, "#" + furniture.ID);

                note = (note.En + ", at the " + atEn, note.Zh + "，在「" + atZh + "」");
            }

            entries.Add(new PickerEntry(ActKeys.Furniture(id), en, zh, Icon(func.Icon, null), Group.Furniture.En, Group.Furniture.Zh, note.En, note.Zh));
        }

        return entries;
    }

    /// <summary>Every kind of furniture that offers sleep or a nap, by its furniture id.</summary>
    private static List<PickerEntry> Beds(ConfigManager config)
    {
        var entries = new List<PickerEntry>();

        foreach (var row in config._Config_Furniture_Dict.Values)
        {
            if (row == null) continue;

            var funcs = GameCatalog.FuncsOf(row.ID);

            if (!funcs.Contains(GameCatalog.SleepFunc) && !funcs.Contains(GameCatalog.NapFunc)) continue;

            var (en, zh) = Names(config, row.Name, row.Name_Local, "#" + row.ID);
            var stamina = funcs.Select(GameCatalog.Func).Where(func => func != null).Max(func => func!.Delta.Stamina);
            var note = PickerLines.Delta(new StatDelta(0, 0, stamina, 0));

            entries.Add(new PickerEntry(row.ID.ToString(System.Globalization.CultureInfo.InvariantCulture), en, zh, Icon(row.ICON, null), NoteEn: "Up to " + note.En, NoteZh: "最多" + note.Zh));
        }

        return entries.OrderBy(entry => entry.NameEn, StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>The buffs the game shows, good ones first. The game has no picture for a buff, so none is given.</summary>
    private static string Buffs(bool timedOnly)
    {
        var buffs = GameBuffs.Entries(Log);

        if (buffs == null) return "";

        return PickerLines.Of(buffs.Where(buff => !timedOnly || buff.IsTimed).OrderBy(buff => buff.IsGood ? 0 : 1).ThenBy(buff => buff.Id).Select(buff =>
        {
            var group = buff.IsGood ? Group.Good : Group.Bad;
            var time = buff.IsTimed ? PickerLines.Duration(buff.Seconds) : ("", "");
            var noteEn = buff.IsTimed ? "Lasts " + time.Item1 + ". " + buff.DescEn : buff.DescEn;
            var noteZh = buff.IsTimed ? "持续 " + time.Item2 + "。" + buff.DescZh : buff.DescZh;

            return new PickerEntry(buff.Id.ToString(System.Globalization.CultureInfo.InvariantCulture), buff.NameEn, buff.NameZh, null, group.En, group.Zh, noteEn, noteZh);
        }));
    }

    private static (string En, string Zh) Names(ConfigManager config, string? key, string? local, string fallback)
    {
        var en = ExiledToolkit.Game.GameNames.Translate(config, key, LanguageType.English);
        var zh = ExiledToolkit.Game.GameNames.Translate(config, key, LanguageType.Chinese) ?? ExiledToolkit.Game.GameNames.NonEmpty(local);

        return (en ?? zh ?? fallback, zh ?? en ?? fallback);
    }

    private static string? Icon(string? webIcon, string? icon) => WebIcons.Icons.Resolve(ExiledToolkit.Game.GameNames.NonEmpty(webIcon) ?? ExiledToolkit.Game.GameNames.NonEmpty(icon));
}
