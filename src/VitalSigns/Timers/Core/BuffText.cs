namespace VitalSigns.Timers.Core;

/// <summary>
/// A buff's name or description in both languages. The game keeps the text of the language it is showing from the start,
/// but fills its English and Chinese tables a while later, so until then only the current language has text. That text
/// stands in for its own language; an empty slot is filled from the table's own field (Chinese) or left empty.
/// </summary>
public static class BuffText
{
    public static (string En, string Zh) Pick(bool isChinese, string? current, string? english, string? chinese, string? local)
    {
        var en = english ?? (isChinese ? null : current) ?? "";
        var zh = chinese ?? (isChinese ? current : null) ?? local ?? "";

        return (en, zh);
    }
}
