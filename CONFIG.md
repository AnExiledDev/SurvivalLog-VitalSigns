# Vital Signs settings

Settings live in `BepInEx\config\com.dev.survivallog.vitalsigns.cfg`, created the first time the game starts with the mod. Change them in game on the Mod Manager's Mods screen, or edit the file while the game is closed. Changes made on the Mods screen take effect at once. Delete the file to get the defaults back.

## VitalSigns

| Setting | Default | What it does |
| --- | --- | --- |
| `Enabled` | `true` | Shows the multipliers, loss shading, Well Fed lines, buff timers, hover box details, Fitness bar and carried weight on the HUD. `false` turns the mod off: everything it draws goes away, no warnings show and every buff goes back to the game's own time. |

## Display

| Setting | Default | What it does |
| --- | --- | --- |
| `FitnessBar` | `true` | Shows a Fitness bar under the Life bar, with a hover box that says what Fitness does and how close you are to Weakened. |
| `WeightReadout` | `true` | Shows the weight you carry on the Inventory button, with a bar that marks where Overweight starts. |
| `TimeStripes` | `true` | Shows a stripe under each buff that ends, shrinking as its time runs out. It turns amber in the last 15%. |
| `ExtendedTooltips` | `true` | Adds details to the stat and buff hover boxes: effect values, time left, layers, scaled gains and losses, food modifiers and the losses still to come. The multipliers and loss shading on the bars stay either way. |
| `StatTooltipGameText` | `true` | Keeps the game's own description at the bottom of the stat hover boxes. Only applies when `ExtendedTooltips` is on. |
| `BarSize` | `80` | How big the Satiety, Morale, Stamina, Life and Fitness bars are drawn, in percent of the game's size, from 50 to 150. `100` is the game's size. Their hover boxes have their own size, `BarTooltipSize`. |
| `BarTooltipSize` | `80` | How big the hover boxes of the Satiety, Morale, Stamina, Life and Fitness bars are drawn, in percent of the game's size, from 50 to 150, whatever `BarSize` is. `100` is the game's size. |
| `BuffSize` | `80` | How big the buffs next to the bars are drawn, in percent of the game's size, from 50 to 150. Their hover boxes grow and shrink with them. At the same number as `BarSize` they sit in proportion to the bars. `100` is the game's size. |
| `CuresInTooltips` | `true` | Lists the items that cure or suppress a debuff in its hover box, with how many of each you have. Only applies when `ExtendedTooltips` is on. |

## Alerts

A warning pops up once when a stat drops past its line, and again only after the stat climbs back over it: 5 percent of its maximum for Satiety, Morale, Stamina and Life, 2 points for Fitness. Loading a save with a stat already low does not warn.

| Setting | Default | What it does |
| --- | --- | --- |
| `Satiety` | `true` | Shows a warning when Satiety drops to `LowAt` percent of its maximum. |
| `Morale` | `true` | Shows a warning when Morale drops to `LowAt` percent of its maximum. |
| `Stamina` | `true` | Shows a warning when Stamina drops to `LowAt` percent of its maximum. |
| `Life` | `true` | Shows a warning when Life drops to `LowAt` percent of its maximum. |
| `Fitness` | `true` | Shows a warning when Fitness drops below 40, close to Weakened, and again below 30, where you become Weakened. |
| `LowAt` | `20` | How low Satiety, Morale, Stamina or Life gets before it warns, in percent of the stat's maximum, from 5 to 90. `20` is where the game turns the bar red. |

## Timers

A multiplier changes how long a buff lasts: `1` is the game's own time, `2` makes it last twice as long, `0.5` half as long. Every multiplier goes from `0.1` to `100`. Change them in the Timers tab on the Mod Manager's page for Vital Signs, on the Mods screen, or in the file while the game is closed. Changes made in the Timers tab or on the Mods screen take effect at once.

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
