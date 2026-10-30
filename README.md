# Vital Signs

Shows more about your stats on the HUD: how each stat's gains and losses are scaled right now, how much running debuffs will still take, where Well Fed starts and ends on the Satiety bar, what each buff does and how much time it has left, what cures a debuff, and how much you carry. It also lets you make buffs and debuffs last longer or shorter.

The game's stat bars show a value and nothing else. Food, buffs and debuffs quietly change how fast each stat rises and falls, and the only way to see how long a buff lasts is to hover it. Vital Signs puts that on the bars themselves. And since every buff has a fixed time, it lets you change those times too: make good buffs last twice as long and bad ones half as long, or slow down only the ones you care about.

## Features

- **Gain multiplier on each bar**: when something scales how much a stat gains, the bar shows it after the stat's name, for example `x1.25` in green or `x0.5` in red.
- **Coming losses shaded on each bar**: the part of a bar that running debuffs will still take before they end is shaded, so you can see where the stat will land. It counts each debuff's layers, cold resistance and how losses are scaled.
- **More in the bar's hover box**: the exact value, every scaled change as a percentage (such as `Losses -25%` or `Natural recovery +50%`), your food modifiers, and each loss still coming with how long it takes. The game's own text can be hidden.
- **Buff hover boxes**: a compact box with the name and time left on one line, what a buff or debuff does to each stat for the layers you have with the values lined up, a bar for the time left, and for debuffs the items that cure or suppress them with how many you have. Items you have come first. A suppressed debuff says what is holding it back.
- **Well Fed lines on the Satiety bar**: one line where Well Fed starts and a fainter one where it ends, read from the game's own rule. The Satiety hover box says the two values.
- **Timer stripe on buffs**: every buff or debuff that runs out gets a thin stripe along its bottom edge that shrinks as its time runs down and turns amber near the end. Buffs that last until something removes them get none.
- **Weight on the Inventory button**: the kilograms you carry and a bar with a mark where Overweight starts, amber when you are close to Overweight and red past it. Hover the button to see your carried weight, the most you can carry, where Overweight starts and how much room you have left.
- **Bar size**: the bars are drawn at 80% of the game's size by default, and the buffs next to them shrink with them. Set any size from 50% to 150%.
- **Any language**: the lines are in English, or in Simplified Chinese when the game is set to Chinese. Buff names come from the game.
- **Survives reloads**: the HUD can reload after a scene change; the mod notices within a couple of seconds and draws everything again.
- **Light on the game**: the mod reads your stats every couple of seconds and only sends them to the HUD when something changed.
- **Log file**: the mod writes what it did at startup and what the HUD answered to `BepInEx\vital-signs.log`.
- **Buff times**: make buffs and debuffs last longer or shorter, from 0.1x to 100x. Set one multiplier for good buffs and one for bad, one per category, or one for any single buff.
- **Timers tab**: the Mod Manager's page for Vital Signs has a Timers tab that lists every buff with a timer, how long it lasts in the game, and how long it lasts with your multipliers, in game time and in real minutes. Changes there take effect at once.
- **Running buffs too**: a buff that is already running gets its remaining time changed when you change its multiplier.
- **Talents still count**: talents that make buffs last longer are multiplied along with the buff.

Only buffs you can see on your character and that have a timer get a new time. Hidden buffs and buffs that last until something removes them keep the game's own behavior. No buff can be given more than 500 game days. The real minutes in the Timers tab follow your day length, so they stay right with Day Length or any other mod that changes how long a day takes.

The HUD part only reads your stats and draws them. With every multiplier at 1, the default, no buff time changes either. Vital Signs adds nothing to your save and does not edit game files. A save keeps the end time of each running buff, so after you remove the mod, buffs already running finish at the time they had, and new buffs use the game's own times.

## Known issues and limits

- Values are read every couple of seconds, so a change can take that long to show.
- The loss shading is an estimate from the debuffs running now. Natural drain, new debuffs and anything you eat or use are not in it.
- Stats UI draws much of the same; use one or the other.
- A buff made to last a very long time shows a large hour count in its hover box, such as `12000:00` for 500 days. The game shows buff times in hours and minutes only.
- Multipliers changed while the game is closed apply to new buffs only. Buffs already running in a save keep the time they had when the game was saved.

## Requirements

- Survival Log 1.1.20151. Built and tested on this version; a game update can break it until the mod is updated.
- The [BepInEx Pack for Survival Log](https://www.nexusmods.com/survivallog/mods/12), the BepInEx 6 (IL2CPP) build for the game.
- Optional: [Mod Manager](https://www.nexusmods.com/survivallog/mods/73). The Timers tab, where you see every buff and set single buffs, is on this mod's page in the Mod Manager. Without it, set the buff multipliers in the config files.
- Optional: [Exiled's Toolkit](https://github.com/AnExiledDev/SurvivalLog-ExiledToolkit) 0.4.0 or newer. With it, buff multipliers changed on the Mod Manager's Mods screen take effect without restarting the game. The Timers tab does not need it.

## Install

1. Install the [BepInEx Pack for Survival Log](https://www.nexusmods.com/survivallog/mods/12) (if no other mods were installed before, start the game once so BepInEx finishes setup, then quit).
2. Optional, for the Timers tab: install [Mod Manager](https://www.nexusmods.com/survivallog/mods/73) the same way.
3. Download `VitalSigns-x.y.z.zip` from the [latest release](https://github.com/AnExiledDev/SurvivalLog-VitalSigns/releases/latest).
4. Extract the zip into the game folder (the folder with `SurvivalLog.exe`). It holds a `BepInEx` folder, so the DLL lands in the right place. When asked, merge the folders. The result should be:
   `C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\VitalSigns.dll`

## Uninstall

Delete `VitalSigns.dll` from the `BepInEx\plugins` folder. For a clean slate, also delete `BepInEx\config\com.dev.survivallog.vitalsigns.cfg`, `BepInEx\config\VitalSigns.Timers.json` and `BepInEx\vital-signs.log`.

## Configuration

Settings live in `BepInEx\config\com.dev.survivallog.vitalsigns.cfg`, created the first time the game starts with the mod. Change them in game on the Mod Manager's Mods screen, or edit the file while the game is closed. Changes take effect at the next game start. Each setting in the cfg file is explained in English and Simplified Chinese. Each part (the weight, the timer stripes, the hover box details, the game's own stat text and the cure lists) can be turned off, and `BarSize` and `BuffSize` set how big the bars and the buffs are drawn. See [CONFIG.md](CONFIG.md) for every setting.

For buff times, open the Mod Manager, go to Vital Signs and pick the Timers tab. Set the good and bad multipliers and the category ones at the top, and a single buff's multiplier in its row, then press Save. The changes take effect at once, on buffs already running too. The good, bad and category multipliers are also in the cfg file, and single buffs are in `BepInEx\config\VitalSigns.Timers.json`.

## Troubleshooting

Tested on Survival Log 1.1.20151 with BepInEx `6.0.0-be.788`.

Check `BepInEx\LogOutput.log` for `===== Vital Signs 0.5.0 started`. With `Enabled` set to `false` the line ends `off, VitalSigns/Enabled is false`.

Once a game has loaded, look for `page script sent to CoreUI1 (install 1): the stat bars show the readings`. A line ending `no stat bars on the HUD right now` means the mod could not find the stat bars.

For buff times, look for `buff time hooks in`. A line starting with `could not hook buff times` means buff times cannot be changed on this game version; the HUD part still works.

The same lines are in `BepInEx\vital-signs.log`, with a timestamp each, and that file is kept across restarts.

## Feedback and bug reports

Found a bug, have an idea, or something feels off? Come say so on the Core Directive Discord: https://discord.gg/trTsyxUFqg . When reporting a bug, attach `BepInEx\LogOutput.log` and `BepInEx\vital-signs.log`.

To reach me directly, message `exiled.dev` on Discord; you can find me in the [official Survival Log Discord](https://discord.gg/MZGg4gQAUe). I'm also on X as [@AnExiledDev](https://x.com/AnExiledDev).

## Changelog

- **0.5.0**: Set how long buffs and debuffs last, with a Timers tab in the Mod Manager that lists every buff.
- **0.4.0**: Bigger buffs with their own BuffSize setting, and the old buff hover box no longer flashes up first.
- **0.3.1**: The gain multiplier sits after the stat's name instead of on top of it.
- **0.3.0**: Smaller bars by default with a BarSize setting, smaller buff badges and cleaner buff hover boxes.
- **0.2.0**: Loss shading on the bars, buff effect and cure hover boxes, fuller stat hover boxes, a weight bar and settings for each part.
- **0.1.0**: First version.

## Build

This is a BepInEx 6 IL2CPP plugin. It compiles against BepInEx and the game's IL2CPP interop assemblies, so a game install with BepInEx set up and started once is required. Those assemblies come from the game and are not part of this repo. The .NET 8 SDK is required.

```
dotnet build src/VitalSigns -c Release -p:GameDir="C:\path\to\Survival Log"
```

`GameDir` is the game folder. The default in the project file is a local path you will almost certainly need to override. It also needs `ToolkitDll`, the path to a built `ExiledToolkit.dll`. The output DLL is at `src\VitalSigns\bin\Release\VitalSigns.dll`.

The unit tests cover the pure logic, including the buff multiplier rules, the duration math, the per-buff file and the page script, which they run against a small stand-in for the HUD page. They need nothing from the game:

```
dotnet test tests/VitalSigns.Tests
```

## License

MIT. Copyright (c) 2026 AnExiledDev. See [LICENSE](LICENSE).
