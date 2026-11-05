using System;
using System.Collections.Generic;
using UnityEngine;

namespace VitalSigns.Adapters;

/// <summary>Gives the page watch a step every frame. A failure is logged the first time it is seen and the next frame carries on.</summary>
public sealed class VsTicker : MonoBehaviour
{
    private const int MostFailuresLogged = 20;

    internal static VsPage? Page;
    internal static Action<string> Log = _ => { };
    internal static Plugin.HudSettings? Hud;

    private static readonly HashSet<string> ReportedFailures = new();

    public VsTicker(IntPtr pointer) : base(pointer)
    {
    }

    private void Update()
    {
        if (Page == null) return;

        try
        {
            var now = Time.unscaledTime;

            while (Timers.Adapters.PageMessages.TryTakeHud(out var command)) Page.Handle(command, now);

            if (Hud is { } hud && hud.EditKey.Value != KeyCode.None && Input.GetKeyDown(hud.EditKey.Value) && !Timers.Adapters.OptionalLive.IsTyping()) Page.ToggleEdit();

            Page.Tick(now);
        }
        catch (Exception ex)
        {
            if (ReportedFailures.Count < MostFailuresLogged && ReportedFailures.Add(ex.GetType().Name + ": " + ex.Message)) Log("Vital Signs step failed (each failure logged once): " + ex);
        }
    }
}
