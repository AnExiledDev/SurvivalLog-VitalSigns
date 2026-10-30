using System;
using VitalSigns.Core;

namespace VitalSigns.Adapters;

/// <summary>Shows a stat warning as an Exiled's Toolkit toast, and writes it to the log.</summary>
internal static class AlertToast
{
    public static void Show(Alert alert, bool isChinese, Action<string> log)
    {
        var notice = AlertText.Of(alert, isChinese);
        var request = new ExiledToolkit.ToastRequest(AlertText.Source, notice.Title)
        {
            Body = notice.Body,
            Kind = notice.IsSevere ? ExiledToolkit.ToastKind.Error : ExiledToolkit.ToastKind.Warning,
            Key = notice.Key,
        };

        log($"warning: {alert.Stat} {alert.Level} at {alert.Value}/{alert.Max}");

        if (ExiledToolkit.Toasts.Show(request).IsNone) log("a warning toast was not shown: " + notice.Title);
    }
}
