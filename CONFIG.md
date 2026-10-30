# Vital Signs settings

Settings live in `BepInEx\config\com.dev.survivallog.vitalsigns.cfg`, created the first time the game starts with the mod. Change them in game on the Mod Manager's Mods screen, or edit the file while the game is closed. The VitalSigns and Display settings take effect at the next game start; for the buff multipliers, see Timers below. Delete the file to get the defaults back.

## VitalSigns

| Setting | Default | What it does |
| --- | --- | --- |
| `Enabled` | `true` | Shows the multipliers, loss shading, Well Fed lines, buff timers, hover box details and carried weight on the HUD. `false` turns the mod off and the HUD stays as the game draws it. Takes effect at the next game start. |

## Display

Each setting takes effect at the next game start.

| Setting | Default | What it does |
| --- | --- | --- |
| `WeightReadout` | `true` | Shows the weight you carry on the Inventory button, with a bar that marks where Overweight starts. |
| `TimeStripes` | `true` | Shows a stripe under each buff that ends, shrinking as its time runs out. It turns amber in the last 15%. |
| `ExtendedTooltips` | `true` | Adds details to the stat and buff hover boxes: effect values, time left, layers, scaled gains and losses, food modifiers and the losses still to come. The multipliers and loss shading on the bars stay either way. |
| `StatTooltipGameText` | `true` | Keeps the game's own description at the bottom of the stat hover boxes. Only applies when `ExtendedTooltips` is on. |
| `BarSize` | `80` | How big the Satiety, Morale, Stamina and Life bars are drawn, in percent of the game's size, from 50 to 150. Their hover boxes grow and shrink with them. `100` is the game's size. |
| `BuffSize` | `80` | How big the buffs next to the bars are drawn, in percent of the game's size, from 50 to 150. Their hover boxes grow and shrink with them. At the same number as `BarSize` they sit in proportion to the bars. `100` is the game's size. |
| `CuresInTooltips` | `true` | Lists the items that cure or suppress a debuff in its hover box, with how many of each you have. Only applies when `ExtendedTooltips` is on. |

## Timers

A multiplier changes how long a buff lasts: `1` is the game's own time, `2` makes it last twice as long, `0.5` half as long. Every multiplier goes from `0.1` to `100`. Change them in the Timers tab on the Mod Manager's page for Vital Signs, on the Mods screen, or in the file while the game is closed. Changes made in the Timers tab take effect at once. Changes made on the Mods screen take effect at once with Exiled's Toolkit installed, and at the next game start without it.

Each buff uses the most specific multiplier it has: its own multiplier first, then its category's, then the good or bad one. Only buffs you can see on your character and that have a timer are changed. Hidden buffs and buffs that last until something removes them keep the game's own behavior.

| Setting | Default | What it does |
| --- | --- | --- |
| `GlobalGood` | `1` | Multiplies how long good buffs last. |
| `GlobalBad` | `1` | Multiplies how long bad buffs (debuffs) last. |

## TimerCategories

Each setting replaces `GlobalGood` and `GlobalBad` for the buffs in that category, good and bad alike. `0` uses the global multipliers. The game sorts its buffs into these groups; the names are the mod's.

| Setting | Default | Buffs, for example |
| --- | --- | --- |
| `Calm` | `0` | Tranquility, Steady Focus |
| `FoodAndSickness` | `0` | Satiety, sweets, poisoning, diarrhea |
| `Mood` | `0` | Vim and Vigor, Wish Fulfilled, Lonely |
| `EnergyAndSleep` | `0` | Sweet Dreams, Sore Muscles, Energy Drink |
| `ExerciseAndWeather` | `0` | Athlete, Vigorous Exercise, Drowned Rat |
| `HealthAndInjury` | `0` | Bleeding, Severely Infected, Wind Chill |
| `Exploration` | `0` | Knuckle Down, Rest and Recovery, Fine Haul |

## Single buffs

A buff's own multiplier replaces every other one for that buff. Set them in the Timers tab, which saves them to `BepInEx\config\VitalSigns.Timers.json`. You can also edit that file while the game is closed. It maps a buff's ID to its multiplier:

```json
{
  "buffs": {
    "1043": 2,
    "200810": 0.5
  }
}
```

The Timers tab shows each buff's ID beside its name. A value outside `0.1` to `100` is moved to the nearest end, and an entry that is not a buff ID or not a number is skipped; `BepInEx\LogOutput.log` and the top of the Timers tab say which. The previous file is kept as `VitalSigns.Timers.json.bak` each time the tab saves.
