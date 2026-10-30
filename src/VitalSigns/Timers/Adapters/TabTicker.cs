using System;
using System.Collections.Generic;
using GameCore.HotUpdate;
using VitalSigns.Timers.Core;
using UnityEngine;

namespace VitalSigns.Timers.Adapters;

/// <summary>
/// Runs once a frame: keeps the tab's script in the web UI and answers the tab's messages on the game's own thread.
/// Static fields because the game makes this component itself.
/// </summary>
public sealed class TabTicker : MonoBehaviour
{
    internal static WebPage? Page;
    internal static Multipliers? Settings;
    internal static Action<string> Log = _ => { };
    private static bool _hasReportedFailure;

    public TabTicker(IntPtr pointer) : base(pointer)
    {
    }

    private void Update()
    {
        if (Page == null || Settings == null) return;

        try
        {
            Page.KeepInstalled(Time.unscaledTime);

            while (PageMessages.TryTake(out var command)) Answer(command, Page, Settings);
        }
        catch (Exception ex)
        {
            if (!_hasReportedFailure) Log("the Timers tab failed (reported once): " + ex);

            _hasReportedFailure = true;
        }
    }

    private static void Answer(PageCommand command, WebPage page, Multipliers settings)
    {
        switch (command)
        {
            case PageCommand.NoScript:
                page.Install();
                break;
            case PageCommand.Load:
                SendData(page, settings, settings.FileProblems);
                break;
            case PageCommand.Save save:
                Save(save.Json, page, settings);
                break;
            case PageCommand.Unknown unknown:
                Log("unknown tab message: " + unknown.Message);
                break;
        }
    }

    private static void Save(string json, WebPage page, Multipliers settings)
    {
        switch (TabPage.ReadSave(json))
        {
            case SaveRequest.Accepted accepted:
                Write(accepted, page, settings);
                break;
            case SaveRequest.Refused refused:
                Log("tab save refused: " + refused.Problem);
                page.Call(TabPage.SavedCall(false, refused.Problem));
                break;
        }
    }

    private static void Write(SaveRequest.Accepted accepted, WebPage page, Multipliers settings)
    {
        try
        {
            settings.Save(accepted);
        }
        catch (Exception ex)
        {
            Log("could not save the multipliers: " + ex.Message);
            page.Call(TabPage.SavedCall(false, ex.Message));

            return;
        }

        foreach (var note in accepted.Notes) Log("tab save: " + note);

        page.Call(TabPage.SavedCall(true, ""));
        SendData(page, settings, accepted.Notes);
    }

    private static void SendData(WebPage page, Multipliers settings, IReadOnlyList<string> problems)
    {
        var buffs = GameBuffs.Entries(Log) ?? Array.Empty<BuffEntry>();

        page.Call(TabPage.DataCall(settings.Current(), buffs, TimeScale(), problems));
    }

    private static int TimeScale()
    {
        try
        {
            return Toolset.TimeScale;
        }
        catch (Exception)
        {
            return 0;
        }
    }
}
