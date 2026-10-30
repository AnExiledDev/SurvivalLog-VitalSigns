using System;

namespace VitalSigns.Timers.Core;

/// <summary>A message from the tab: <c>slmod|vitalsigns|timers|load</c>, <c>slmod|vitalsigns|timers|save|{json}</c>, or the page saying the script is gone.</summary>
public abstract record PageCommand
{
    public const string Prefix = "slmod|vitalsigns|timers|";

    private const string LoadVerb = "load";
    private const string NoScriptVerb = "noscript";
    private const string SaveVerb = "save|";

    private PageCommand()
    {
    }

    public sealed record NoScript : PageCommand;

    public sealed record Load : PageCommand;

    public sealed record Save(string Json) : PageCommand;

    public sealed record Unknown(string Message) : PageCommand;

    public static bool IsOurs(string? message) => message is not null && message.StartsWith(Prefix, StringComparison.Ordinal);

    public static PageCommand Parse(string message)
    {
        if (!IsOurs(message)) return new Unknown(message);

        var verb = message.Substring(Prefix.Length);

        if (verb == LoadVerb) return new Load();

        if (verb == NoScriptVerb) return new NoScript();

        if (verb.StartsWith(SaveVerb, StringComparison.Ordinal)) return new Save(verb.Substring(SaveVerb.Length));

        return new Unknown(message);
    }
}
