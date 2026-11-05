using System;
using System.Collections.Concurrent;
using System.Threading;
using HarmonyLib;
using VitalSigns.Core;
using VitalSigns.Timers.Core;
using Vuplex.WebView.Internal;

namespace VitalSigns.Timers.Adapters;

/// <summary>
/// Catches the Timers tab's, the Actions tab's and the HUD's messages before the game's web layer sees them (it would log them as unknown) and queues them
/// for the next frame, so nothing runs on the web view's thread. Static because a Harmony prefix has nowhere else to
/// look.
/// </summary>
internal static class PageMessages
{
    private static readonly ConcurrentQueue<PageCommand> Queue = new();
    private static readonly ConcurrentQueue<HudCommand> HudQueue = new();
    private static int _isApplied;
    private static Action<string> _log = _ => { };
    private static int _hasReportedFailure;

    /// <summary>Goes in once, at the mod's start: the HUD's clicks need it even when the buff hooks could not go in.</summary>
    public static void Apply(Harmony harmony, Action<string> log)
    {
        _log = log;

        if (Interlocked.Exchange(ref _isApplied, 1) == 1) return;

        try
        {
            harmony.Patch(AccessTools.Method(typeof(BaseWebView), "HandleMessageEmitted"), prefix: new HarmonyMethod(typeof(PageMessages), nameof(TakeOurs)));
            log("listening for tab messages");
        }
        catch (Exception ex)
        {
            log("could not hook web UI messages, the Timers and Actions tabs will not load and clicks on the HUD do nothing: " + ex.Message);
        }
    }

    public static bool TryTake(out PageCommand command)
    {
        if (Queue.TryDequeue(out var queued))
        {
            command = queued;

            return true;
        }

        command = new PageCommand.Unknown("");

        return false;
    }

    public static bool TryTakeHud(out HudCommand command)
    {
        if (HudQueue.TryDequeue(out var queued))
        {
            command = queued;

            return true;
        }

        command = new HudCommand.Unknown("");

        return false;
    }

    /// <summary>Never throws into the web view: on any failure the message goes on to the game as if the mod were not here.</summary>
    private static bool TakeOurs(string serializedMessage)
    {
        try
        {
            if (HudCommand.IsOurs(serializedMessage))
            {
                HudQueue.Enqueue(HudCommand.Parse(serializedMessage));

                return false;
            }

            if (!PageCommand.IsOurs(serializedMessage)) return true;

            Queue.Enqueue(PageCommand.Parse(serializedMessage));

            return false;
        }
        catch (Exception ex)
        {
            ReportOnce(ex);

            return true;
        }
    }

    /// <summary>Runs on the web view's thread, so the once-only flag is set atomically.</summary>
    private static void ReportOnce(Exception ex)
    {
        if (Interlocked.Exchange(ref _hasReportedFailure, 1) == 0) _log("reading a web UI message failed, it was passed on to the game (reported once): " + ex);
    }
}
