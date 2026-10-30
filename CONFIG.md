# Vital Signs settings

Settings live in `BepInEx\config\com.dev.survivallog.vitalsigns.cfg`, created the first time the game starts with the mod. Change them in game on the Mod Manager's Mods screen, or edit the file while the game is closed. Changes take effect at the next game start. Delete the file to get the defaults back.

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
| `BarSize` | `80` | How big the Satiety, Morale, Stamina and Life bars are drawn, in percent of the game's size, from 50 to 150. The buffs next to each bar and their hover boxes grow and shrink with it. `100` is the game's size. |
| `CuresInTooltips` | `true` | Lists the items that cure or suppress a debuff in its hover box, with how many of each you have. Only applies when `ExtendedTooltips` is on. |
