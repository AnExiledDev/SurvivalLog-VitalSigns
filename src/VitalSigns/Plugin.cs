using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using VitalSigns.Adapters;

namespace VitalSigns;

[BepInPlugin(Guid, "Vital Signs", Version)]
public sealed class Plugin : BasePlugin
{
    private const string Guid = "com.dev.survivallog.vitalsigns";
    private const string Version = "0.1.0";

    public override void Load()
    {
        var isEnabled = Config.Bind("VitalSigns", "Enabled", true, new ConfigDescription(
            "Show more on the HUD: how each stat's gains and losses are scaled, the Well Fed lines on the Satiety bar, a timer stripe on each buff that ends, and the weight you carry on the Inventory button. Off: the mod does nothing. Takes effect at the next game start.\n" +
            "在屏幕上显示更多信息：各项属性的增减倍率、「饱腹」条上的「饱腹富余」界线、每个有时限的状态上的计时条，以及「背包」按钮上的当前负重。关闭后本模组不做任何改动。下次启动游戏时生效。", null, "ModManager.Restart")).Value;
        var log = new VsLog(Log, Path.Combine(Paths.BepInExRootPath, "vital-signs.log"));

        if (!isEnabled)
        {
            log.Write($"===== Vital Signs {Version} started: off, VitalSigns/Enabled is false");

            return;
        }

        log.Write($"===== Vital Signs {Version} started");

        VsTicker.Log = log.Write;
        VsTicker.Page = new VsPage(log.Write);

        AddComponent<VsTicker>();
    }
}
