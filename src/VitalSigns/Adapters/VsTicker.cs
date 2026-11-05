using System;
using System.Collections.Generic;
using ExiledToolkit;
using ExiledToolkit.Kit;
using ExiledToolkit.Web;
using UnityEngine;
using VitalSigns.Core;

namespace VitalSigns.Adapters;

/// <summary>Gives the page watch a step every frame. A failure is logged the first time it is seen and the next frame carries on.</summary>
public sealed class VsTicker : MonoBehaviour
{
    private const int MostFailuresLogged = 20;

    internal static VsPage? Page;
    internal static Action<string> Log = _ => { };
    internal static Plugin.HudSettings? Hud;

    /// <summary>The HUD's and the Actions tab's messages.</summary>
    internal static PageMailbox? Messages;

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

            while (Messages != null && Messages.TryTake(out var message)) Page.Handle(HudCommand.Parse(message), now);

            if (Hud is { } hud && KeyGate.Fires(hud.EditKey.Value, KeyCode.None, Input.GetKeyDown, () => Typing.IsActive)) Page.ToggleEdit();

            Page.Tick(now);
        }
        catch (Exception ex)
        {
            if (ReportedFailures.Count < MostFailuresLogged && ReportedFailures.Add(ex.GetType().Name + ": " + ex.Message)) Log("Vital Signs step failed (each failure logged once): " + ex);
        }
    }
}
