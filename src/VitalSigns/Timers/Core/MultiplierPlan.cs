using System;
using System.Collections.Generic;
using System.Linq;

namespace VitalSigns.Timers.Core;

/// <summary>What the game says about a buff that decides its multiplier.</summary>
public readonly record struct BuffKind(int Id, bool IsGood, int Category);

public enum Source
{
    Global,
    Category,
    Buff,
}

public readonly record struct Resolved(double Value, Source Source);

/// <summary>
/// Every multiplier the player set. A buff takes its own if it has one, else its category's, else the global one for
/// good or bad buffs. Values are already held to <see cref="Limits"/> when the plan is made.
/// </summary>
public sealed class MultiplierPlan
{
    /// <summary>The categories the game sorts buffs into (its BuffDisplayType, 0 to 6).</summary>
    public const int CategoryCount = 7;

    public static readonly MultiplierPlan Unchanged = new(Limits.Default, Limits.Default, new double?[CategoryCount], new Dictionary<int, double>());

    private readonly double?[] _categories;
    private readonly Dictionary<int, double> _buffs;

    public MultiplierPlan(double good, double bad, IReadOnlyList<double?> categories, IReadOnlyDictionary<int, double> buffs)
    {
        Good = Limits.Global(good, out _);
        Bad = Limits.Global(bad, out _);
        _categories = new double?[CategoryCount];

        for (var i = 0; i < CategoryCount && i < categories.Count; i++) _categories[i] = categories[i] is { } value ? Limits.Override(value, out _) : null;

        _buffs = new Dictionary<int, double>();

        foreach (var (id, value) in buffs)
        {
            if (Limits.Override(value, out _) is { } limited) _buffs[id] = limited;
        }
    }

    public double Good { get; }

    public double Bad { get; }

    public IReadOnlyList<double?> Categories => _categories;

    public IReadOnlyDictionary<int, double> Buffs => _buffs;

    public Resolved For(BuffKind buff)
    {
        if (_buffs.TryGetValue(buff.Id, out var own)) return new Resolved(own, Source.Buff);

        if (buff.Category >= 0 && buff.Category < CategoryCount && _categories[buff.Category] is { } category) return new Resolved(category, Source.Category);

        return new Resolved(buff.IsGood ? Good : Bad, Source.Global);
    }

    /// <summary>Whether any buff could come out other than at 1x, so the hooks can stand aside when nothing is set.</summary>
    public bool ChangesAnything =>
        !Limits.IsOne(Good) || !Limits.IsOne(Bad) || _categories.Any(c => c is { } v && !Limits.IsOne(v)) || _buffs.Values.Any(v => !Limits.IsOne(v));

    /// <summary>The same plan with the per-buff multipliers replaced.</summary>
    public MultiplierPlan WithBuffs(IReadOnlyDictionary<int, double> buffs) => new(Good, Bad, _categories, buffs ?? throw new ArgumentNullException(nameof(buffs)));
}
