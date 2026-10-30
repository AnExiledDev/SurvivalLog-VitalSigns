using System;
using System.IO;
using System.Runtime.CompilerServices;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;

namespace VitalSigns.Timers.Adapters;

/// <summary>
/// Applies settings through Exiled's Toolkit, so a change on the Mod Manager's Mods screen takes effect at once. The
/// mod requires the Toolkit (its warnings are Toolkit toasts); if its live settings still cannot start, each setting
/// is applied once at start and the reason is logged.
/// </summary>
internal static class OptionalLive
{
    public const string ToolkitGuid = "com.dev.survivallog.exiledtoolkit";

    /// <summary>The oldest Toolkit the mod loads with: its toasts and live settings come from it.</summary>
    public const string ToolkitVersion = ">=0.11.0";


    private static readonly Lazy<ManualLogSource> Log = new(() => Logger.CreateLogSource(typeof(OptionalLive).Assembly.GetName().Name ?? "Settings"));

    /// <summary>Whether a Toolkit is installed. Valid from the mod's Load on, since the Toolkit loads first.</summary>
    public static bool IsToolkitInstalled => IL2CPPChainloader.Instance.Plugins.ContainsKey(ToolkitGuid);

    /// <summary>Calls <paramref name="apply"/> with the entry's value now, and again after each change while the Toolkit is installed.</summary>
    public static void Watch<T>(ConfigEntry<T> entry, Action<T> apply) => Watch(() => apply(entry.Value), entry);

    /// <summary>Calls <paramref name="apply"/> now, and again after any of the entries changes while the Toolkit is installed.</summary>
    public static void Watch(Action apply, params ConfigEntryBase[] entries)
    {
        if (IsToolkitInstalled && TryWatchWithToolkit(apply, entries)) return;

        ApplyOnce(apply);
    }

    private static bool TryWatchWithToolkit(Action apply, ConfigEntryBase[] entries)
    {
        try
        {
            WatchWithToolkit(apply, entries);

            return true;
        }
        catch (Exception ex) when (ex is TypeLoadException or MissingMethodException or FileNotFoundException or FileLoadException)
        {
            Log.Value.LogWarning("Exiled's Toolkit is older than 0.4.0, so settings are read once at start: " + ex.Message);

            return false;
        }
    }

    // Kept in its own method so the Toolkit's types are only looked up when it is installed.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WatchWithToolkit(Action apply, ConfigEntryBase[] entries) => ExiledToolkit.LiveSettings.Watch(apply, entries);

    // Matches the Toolkit: a setting that fails to apply is logged and does not stop the mod's start.
    private static void ApplyOnce(Action apply)
    {
        try
        {
            apply();
        }
        catch (Exception ex)
        {
            Log.Value.LogError("could not apply a setting: " + ex);
        }
    }
}
