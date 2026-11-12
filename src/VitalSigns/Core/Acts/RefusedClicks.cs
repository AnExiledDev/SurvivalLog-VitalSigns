using System.Collections.Generic;

namespace VitalSigns.Core.Acts;

/// <summary>
/// The log line for a bar click that found nothing to queue. Each stat, reason and base it planned from is logged
/// once, so clicking the same bar again doesn't fill the log, but a click after the base or the reason changed does.
/// </summary>
public sealed class RefusedClicks
{
    private readonly HashSet<(int, ActWhy, int, int, int)> _logged = new();

    /// <summary>The line to log for this refused click, or null when the same one was logged already.</summary>
    public string? Line(int stat, ActWhy why, int items, int funcs, int beds)
    {
        if (!_logged.Add((stat, why, items, funcs, beds))) return null;

        return $"click on {ActTexts.StatName(stat, false)}: nothing to queue ({why}), planned from {items} items, {funcs} furniture actions, {beds} beds";
    }

    /// <summary>Forgets what was logged, so the next refused click logs again.</summary>
    public void Clear() => _logged.Clear();
}
