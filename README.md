# Vital Signs

Shows more about your stats on the HUD: how each stat's gains and losses are scaled right now, how much running debuffs will still take, where Well Fed starts and ends on the Satiety bar, what each buff does and how much time it has left, what cures a debuff, how much you carry, and your Fitness, which the game never shows. It also lets you make buffs and debuffs last longer or shorter.

The game's stat bars show a value and nothing else. Food, buffs and debuffs quietly change how fast each stat rises and falls, and the only way to see how long a buff lasts is to hover it. Vital Signs puts that on the bars themselves. And since every buff has a fixed time, it lets you change those times too: make good buffs last twice as long and bad ones half as long, or slow down only the ones you care about.

## Features

- **Gain multiplier on each bar**: when something scales how much a stat gains, a chip just left of the bar shows it, for example `x1.25` in green or `x0.5` in red. Hover over it to see each buff that raises or lowers it, with its amount.
- **Coming losses shaded on each bar**: the part of a bar that running debuffs will still take before they end is shaded, so you can see where the stat will land. It counts each debuff's layers, cold resistance and how losses are scaled.
- **More in the bar's hover box**: the exact value, every scaled change as a percentage (such as `Losses -25%` or `Natural recovery +50%`), your food modifiers, and each loss still coming with how long it takes. The game's own text can be hidden.
- **Buff hover boxes**: a compact box with the name and time left on one line, what a buff or debuff does to each stat for the layers you have with the values lined up, a bar for the time left, and for debuffs the items that cure or suppress them with how many you have. Items you have come first. A suppressed debuff says what is holding it back.
- **Well Fed lines on the Satiety bar**: one line where Well Fed starts and a fainter one where it ends, read from the game's own rule. The Satiety hover box says the two values.
- **Timer stripe on buffs**: every buff or debuff that runs out gets a thin stripe along its bottom edge that shrinks as its time runs down and turns amber near the end. Buffs that last until something removes them get none.
- **Fitness bar**: a bar under Life shows your Fitness, a stat the game keeps but never shows. It is green when fine, amber within 10 of Weakened and red below 30, where Weakened starts. Its hover box says what Fitness does and how close you are to Weakened. This replaces the Fitness Bar mod: from Fitness Bar 0.1.2 on, it turns itself off when Vital Signs is installed, so only one Fitness bar shows. An older Fitness Bar still draws its own, and the Fitness hover box then says to remove or update it.
- **Low stat warnings**: a pop-up when Satiety, Morale, Stamina or Life drops to 20% of its maximum, where the game turns the bar red, and when Fitness drops below 40, close to Weakened, and below 30, where you become Weakened. Each warns once per drop, and again only after the stat climbs a little back. Set the line and turn each one off in the Alerts settings.
- **Weight on the Inventory button**: the kilograms you carry and a bar with a mark where Overweight starts, amber when you are close to Overweight and red past it. Hover the button to see your carried weight, the most you can carry, where Overweight starts, how much room you have left and the 5 heaviest items in your bag (set how many, or none).
- **Trend arrows**: a small green arrow up or red arrow down by a stat's value when it rose or fell over the last game hour.
- **Buffs about to end pulse**: a good buff pulses when it has an hour of game time or less left, so you can renew it before it runs out. Set how soon, or make debuffs pulse too.
- **Styles**: the hover boxes, the multiplier chips and the bar frames take the colours of the HUD style chosen in Exiled's Toolkit (0.18.0 or newer). The stat bars can be drawn flat or split into tenths, and a colour-blind setting uses blue and orange in place of green and red.
- **Sizes**: the bars and the buffs next to them are drawn at 80% of the game's size by default, and the bars' hover boxes at the game's own size so their text stays easy to read. Set each from 50% to 150%.
- **Darker hover boxes**: the stat hover boxes are as dark as the buff ones, so the HUD behind them no longer shows through.
- **Any language**: the lines are in English, or in Simplified Chinese when the game is set to Chinese. Buff names come from the game.
- **Survives reloads**: the HUD can reload after a scene change; the mod notices within a couple of seconds and draws everything again.
- **Light on the game**: the mod reads your stats every couple of seconds and only sends them to the HUD when something changed.
- **Settings apply at once**: every setting changed on the Mod Manager's Mods screen takes effect right away, with no restart. Turning the mod off takes away everything it draws and puts every buff back on the game's own time.
- **Log file**: the mod writes what it did at startup and what the HUD answered to `BepInEx\vital-signs.log`.
- **Click a bar to act**: click Satiety to eat, Stamina to sleep in the best bed at home, Morale to do something that raises it, or Life to use the items you chose. The actions go into the game's own queue. Satiety eats to the maximum, to Well Fed or a set percent past it, and never past the maximum. Morale does the action that also raises another stat first, then the biggest gain; a second click while it is queued takes the next one. Or set a routine, and a click queues all of it in order. A toast says what was queued, or why nothing was.
- **Actions tab**: Vital Signs' page in the Mod Manager has an Actions tab that sets up the clicks. It lists every furniture action and item each click can use, with what it gives. The ones your base or bag lacks are greyed out. It shows what each click would queue right now, lets you choose food to skip, build the Morale routine, pick the Life items and set how each bed type is used (Auto, Sleep, Nap or Skip).
- **Click a debuff to cure it**: a debuff with a cure in your bag or home storage is marked. Click it to queue the cure, from the bag first.
- **Compact HUD**: a bar can hide while it is full, or above a level you set, and come back as soon as the stat drops. Bars with buffs next to them can stay.
- **Warning pulse**: a bar pulses once it drops to the warning line, before the game turns it red.
- **Queue projection**: each bar marks where the stat will be once the actions in the queue are done.
- **Bars while exploring**: small Satiety, Morale, Stamina and Life bars in a corner of the screen while you explore, where the game hides its own. Pick the corner and the size.
- **Buff tray**: sort the buffs by time left, keep good buffs and debuffs apart, move them to another side of the bars, or fold each row into one badge. Shift-click a buff to pin it to the front and draw it larger.
- **Layout editor**: drag each bar and the buffs where you want them and set their size. Open it with the 「Edit HUD layout」 button in the Actions tab, or a key you set.
- **Stat difficulty**: make each stat fall faster or slower, and rise more or less, from 0x to 5x. It applies to your character only.
- **Buff times**: make buffs and debuffs last longer or shorter, from 0.1x to 100x. Set one multiplier for good buffs and one for bad, one per category, or one for any single buff.
- **Timers tab**: the Mod Manager's page for Vital Signs has a Timers tab that lists every buff with a timer, how long it lasts in the game, and how long it lasts with your multipliers, in game time and in real minutes. Changes there take effect at once.
- **Running buffs too**: a buff that is already running gets its remaining time changed when you change its multiplier.
- **Talents still count**: talents that make buffs last longer are multiplied along with the buff.

Only buffs you can see on your character and that have a timer get a new time. Hidden buffs and buffs that last until something removes them keep the game's own behavior. No buff can be given more than 500 game days. The real minutes in the Timers tab follow your day length, so they stay right with Day Length or any other mod that changes how long a day takes.

The HUD part only reads your stats and draws them. With every multiplier at 1, the default, no buff time changes either. Vital Signs adds nothing to your save and does not edit game files. A save keeps the end time of each running buff, so after you remove the mod, buffs already running finish at the time they had, and new buffs use the game's own times.

Vital Signs was originally inspired by [Stats UI](https://www.nexusmods.com/survivallog/mods/27) by ivmakk0, and has since grown well past it, with UI size settings, a Fitness bar, low stat warnings, settings that apply live, and buff duration multipliers.

## Known issues and limits

- Values are read every couple of seconds, so a change can take that long to show.
- The loss shading is an estimate from the debuffs running now. Natural drain, new debuffs and anything you eat or use are not in it.
- Stats UI and Vital Signs only conflict in what they draw on screen. If you don't need the extra features, Stats UI works just as well.
- Fitness Bar draws the same Fitness bar. With both installed you get two, and the Fitness hover box says to remove `FitnessBar.dll`.
- A warning comes when a stat drops past its line while you play. Loading a save with a stat already low does not warn.
- With the mod turned off, an open Timers tab stays until the Mod Manager page reloads.
- A buff made to last a very long time shows a large hour count in its hover box, such as `12000:00` for 500 days. The game shows buff times in hours and minutes only.
- Multipliers changed while the game is closed apply to new buffs only. Buffs already running in a save keep the time they had when the game was saved.
- A click on a bar uses only what the game would let you queue by hand. Furniture must be at home, and items in the home storage are used only while you are at home.
- The queue projection counts what each queued action gives as the game describes it. Losses over time while the queue runs are not in it.
- The bars while exploring are read every couple of seconds, like the HUD ones.
- With a Difficulty multiplier at 0, game effects that react to that stat rising or falling no longer react, since the change they see is 0.

## Requirements

- Survival Log 1.1.20151. Built and tested on this version; a game update can break it until the mod is updated.
- The [BepInEx Pack for Survival Log](https://www.nexusmods.com/survivallog/mods/12), the BepInEx 6 (IL2CPP) build for the game.
- Optional: [Mod Manager](https://www.nexusmods.com/survivallog/mods/73). The Timers tab, where you see every buff and set single buffs, is on this mod's page in the Mod Manager. Without it, set the buff multipliers in the config files.
- [Exiled's Toolkit](https://www.nexusmods.com/survivallog/mods/61) 0.11.0 or newer. The low stat warnings are its pop-ups, and settings changed on the Mod Manager's Mods screen take effect at once through it.

## Install

1. Install the [BepInEx Pack for Survival Log](https://www.nexusmods.com/survivallog/mods/12) (if no other mods were installed before, start the game once so BepInEx finishes setup, then quit).
2. Install [Exiled's Toolkit](https://www.nexusmods.com/survivallog/mods/61) 0.11.0 or newer.
3. Optional, for the Timers tab: install [Mod Manager](https://www.nexusmods.com/survivallog/mods/73) the same way.
4. Download `VitalSigns-x.y.z.zip` from the [latest release](https://github.com/AnExiledDev/SurvivalLog-VitalSigns/releases/latest) or from [Nexus Mods](https://www.nexusmods.com/survivallog/mods/133).
5. Extract the zip into the game folder (the folder with `SurvivalLog.exe`). It holds a `BepInEx` folder, so the DLL lands in the right place. When asked, merge the folders. The result should be:
   `C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\VitalSigns.dll`

## Uninstall

Delete `VitalSigns.dll` from the `BepInEx\plugins` folder. For a clean slate, also delete `BepInEx\config\com.dev.survivallog.vitalsigns.cfg`, `BepInEx\config\VitalSigns.Timers.json` and `BepInEx\vital-signs.log`.

## Configuration

Settings live in `BepInEx\config\com.dev.survivallog.vitalsigns.cfg`, created the first time the game starts with the mod. Change them in game on the Mod Manager's Mods screen, or edit the file while the game is closed. Changes made on the Mods screen take effect at once. Each setting in the cfg file is explained in English and Simplified Chinese. Each part (the Fitness bar, the weight, the timer stripes, the hover box details, the game's own stat text and the cure lists) can be turned off, and `BarSize`, `BuffSize` and `BarTooltipSize` set how big the bars, the buffs and the bars' hover boxes are drawn. `HeaviestItems`, `TrendArrows`, `MatchHudStyle`, `BarLook` and `ColourBlind` set the heaviest items list, the arrows and the look. Under Alerts, each low stat warning can be turned off, `LowAt` sets how low a stat gets before it warns, and `BuffEndingPulse`, `BuffEndingMinutes` and `BuffEndingDebuffs` set which buffs pulse before they end, and how soon. Under Hud are the compact HUD, the warning pulse, the buff order and pins, the layout, the exploring bars, the queue projection and the clicks on bars and debuffs. Under Actions is what each click does, and under Difficulty how fast each stat falls and rises. See [CONFIG.md](CONFIG.md) for every setting.

To set up the clicks, open the Mod Manager, go to Vital Signs and pick the Actions tab. Every change there is saved at once.

For buff times, open the Mod Manager, go to Vital Signs and pick the Timers tab. Set the good and bad multipliers and the category ones at the top, and a single buff's multiplier in its row, then press Save. The changes take effect at once, on buffs already running too. The good, bad and category multipliers are also in the cfg file, and single buffs are in `BepInEx\config\VitalSigns.Timers.json`.

## Troubleshooting

Tested on Survival Log 1.1.20151 with BepInEx `6.0.0-be.788`.

Check `BepInEx\LogOutput.log` for `===== Vital Signs 0.9.2 started`. With `Enabled` set to `false` the line ends `off, VitalSigns/Enabled is false`.

Once a game has loaded, look for `page script sent to CoreUI1 (install 1): the stat bars show the readings`. A line ending `no stat bars on the HUD right now` means the mod could not find the stat bars.

Each warning writes a line such as `warning: Satiety Low at 18/100`. A line ending `the toast was not shown` means Exiled's Toolkit could not show it.

For buff times, look for `buff time hooks in`. A line starting with `could not hook buff times` means buff times cannot be changed on this game version; the HUD part still works.

A click on a bar writes a line such as `click on Satiety: queued ...`. A line starting with `could not hook stat changes` means the Difficulty settings do nothing on this game version; the rest still works.

The same lines are in `BepInEx\vital-signs.log`, with a timestamp each, and that file is kept across restarts.

## Feedback and bug reports

Found a bug, have an idea, or something feels off? Come say so on the Core Directive Discord: https://discord.gg/trTsyxUFqg . When reporting a bug, attach `BepInEx\LogOutput.log` and `BepInEx\vital-signs.log`.

To reach me directly, message `exiled.dev` on Discord; you can find me in the [official Survival Log Discord](https://discord.gg/MZGg4gQAUe). I'm also on X as [@AnExiledDev](https://x.com/AnExiledDev).

## Changelog

- **0.9.2**: More tests for stat difficulty; nothing changes in play.
- **0.9.1**: The food note in the Actions tab now says what a Satiety click eats.
- **0.9.0**: Click a bar to eat, sleep, raise Morale or use Life items, an Actions tab to set it up, click a debuff to cure it, a compact HUD, a warning pulse, queue projection, bars while exploring, buff sorting and pinning, a layout editor and stat difficulty.
- **0.8.0**: Trend arrows on the bars, a pulse on buffs about to end, the heaviest items in the Inventory hover box, colours that match the Toolkit's HUD style, flat or segmented bars, and colour-blind colours.
- **0.7.5**: The Mod Manager no longer says settings need a restart; they all apply right away.
- **0.7.4**: The Timers tab shows buffs in English in an English game and lays out cleanly, and BarTooltipSize starts at 80%.
- **0.7.3**: Hovering the multiplier chip lists each buff that changes it, with its amount.
- **0.7.2**: The gain multiplier is a solid chip left of the bar, hover boxes stay on screen, and buff descriptions show one sentence to a line.
- **0.7.1**: The note about the Fitness Bar mod shows only for a Fitness Bar too old to turn itself off.
- **0.7.0**: Low stat warnings, every setting applies at once, and a note in the Fitness hover box when the Fitness Bar mod is also installed. Exiled's Toolkit 0.11.0 is now required.
- **0.6.0**: A Fitness bar under Life, which replaces the Fitness Bar mod, a separate size for the bars' hover boxes, and darker stat hover boxes.
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
