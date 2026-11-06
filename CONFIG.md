# Vital Signs settings

Settings live in `BepInEx\config\com.dev.survivallog.vitalsigns.cfg`, created the first time the game starts with the mod. Change them in game on the Mod Manager's Mods screen, or edit the file while the game is closed. Changes made on the Mods screen take effect at once. Delete the file to get the defaults back.

With Mod Manager 0.15.0 or newer, the list settings (`FavouriteBuffs`, `FoodSkip`, `BedModes`, `MoraleRoutine`, `LifeItems` and `Buffs` under Timers) are picked on the Mods screen: each shows its entries as rows with their picture, name and what they do, and ADD opens a list of what the game has, filed under the game's categories, with an Add button on each row. With Mod Manager 0.17.0 or newer, what you have is marked: items you carry or keep anywhere at home show how many, and Morale actions and beds your house has say so, with a "You have" group of them at the top. The food, Morale and Life lists also offer "Any ..." entries for each kind of item (Any Meat, Any Meds, Any Cooked Dish), which stand for every item of that kind you have. In the file they are IDs split by commas; a kind is `g` and a number: `g900` is every cooked dish, `g1001` to `g1011` the food kinds (`g1002` Meat), and the other numbers the game's item categories (`g2` Meds, `g3` Books).

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
| `HeaviestItems` | `5` | How many of the heaviest items in your bag the Inventory button's hover box lists, from 0 to 10, with their weight. Stacks of the same item count as one. `0` lists none. Only applies when `WeightReadout` is on. |
| `TrendArrows` | `true` | Shows an arrow by each stat's value when the stat rose or fell over the last game hour. An arrow shows once ten game minutes have been seen. |
| `MatchHudStyle` | `true` | Draws the hover boxes, the multiplier chips and the bar frames in the colours of the HUD style chosen in Exiled's Toolkit (`HudStyle`). With the classic style they keep this mod's own colours. |
| `BarLook` | `Game` | How the stat bars are drawn. `Game`: the game's own look. `Flat`: no frame or shadow. `Segmented`: split into tenths. |
| `ColourBlind` | `false` | Uses colours that stay apart for red-green colour blindness: blue in place of green and orange in place of red, on the bars, the buffs, the multiplier chips and the hover boxes. |
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
| `BuffEndingPulse` | `true` | Makes a good buff pulse when it is about to end, so you can renew it in time. How soon is `BuffEndingMinutes`. |
| `BuffEndingMinutes` | `60` | How many game minutes before a buff ends it starts to pulse, from 5 to 720. `60` is one game hour. |
| `BuffEndingDebuffs` | `false` | Makes debuffs pulse too when they are about to end. Only applies when `BuffEndingPulse` is on. |
| `LowAt` | `20` | How low Satiety, Morale, Stamina or Life gets before it warns, in percent of the stat's maximum, from 5 to 90. `20` is where the game turns the bar red. |

## Hud

| Setting | Default | What it does |
| --- | --- | --- |
| `HideFullBars` | `false` | Hides a stat bar while it is full, or above `HideAbove`, so the HUD only shows what needs you. It comes back as soon as the stat drops. |
| `HideAbove` | `100` | Above what level, in percent of the maximum, a bar hides, from 50 to 100. `100` hides only a full bar. Only applies when `HideFullBars` is on. |
| `KeepBarsWithBuffs` | `true` | Keeps a full bar showing while it has buffs next to it, so they stay where you look for them. Only applies when `HideFullBars` is on. |
| `WarnPulse` | `true` | Makes a stat bar pulse once it drops to the warning line (`LowAt` under Alerts), before the game turns it red. |
| `BuffSort` | `Game` | The order of the buffs next to the bars. `Game`: the game's order. `EndingFirst`: the one that ends soonest first. `LongestFirst`: the one with the most time left first. Buffs without a timer go last. |
| `BuffGroup` | `Mixed` | Whether good buffs and debuffs are kept apart. `Mixed`: together. `BuffsFirst`: good buffs first. `DebuffsFirst`: debuffs first. |
| `FavouriteBuffs` | empty | Buffs pinned to the front of their row and drawn larger, as buff IDs split by commas. Shift-click a buff on the HUD to pin it or unpin it. |
| `FavouriteSize` | `130` | How big a pinned buff is drawn, in percent of the other buffs, from 100 to 200. |
| `CollapseBuffs` | `false` | Folds each row's buffs into one badge with their count. Hover over it to see them all. Pinned buffs stay out. |
| `Layout` | empty | Where each bar sits and how big, as the layout editor saves it. Empty: the game's own places. |
| `BuffTray` | `Left` | Which side of the bars the buffs sit on. `Left` is the game's place. `Right`, `Above` and `Below` move them. |
| `ExploreBars` | `TopRight` | Shows small Satiety, Morale, Stamina and Life bars in a corner of the screen while you explore, where the game hides its own: `TopLeft`, `TopRight`, `BottomLeft` or `BottomRight`. `Off` hides them. |
| `ExploreBarsSize` | `100` | How big the exploring bars are drawn, in percent, from 50 to 150. |
| `QueueProjection` | `true` | Marks on each bar where the stat will be once the actions in the queue are done. |
| `ClickBars` | `true` | A click on a stat bar queues what raises it. What each click does is set under Actions. |
| `ClickCures` | `true` | A click on a debuff queues its cure: one in the bag first, then one in the home storage. A debuff with a cure at hand is marked. |
| `EditLayoutKey` | `None` | The key that opens and closes the layout editor. `None`: open it with the 「Edit HUD layout」 button in the pause menu. Ignored while you type in a text box. |

### The layout editor

Open it with the 「Edit HUD layout」 button in the pause menu, or with `EditLayoutKey`. The button closes the menu and opens the editor. Every bar and the buffs show, hidden ones too. Drag a part to move it and use the mouse wheel over it to resize it. Pick which side the buffs sit on and whether they fold into one badge, then press Save, or Cancel to leave everything as it was. While the editor is open, a click on a bar does nothing.

## Actions

What a click on each stat bar queues.

A click adds the same actions you could queue by hand. Furniture and beds come from your home, and items in the home storage are used only while you are at home.

| Setting | Default | What it does |
| --- | --- | --- |
| `ClickSatiety` | `true` | A click on the Satiety bar eats food that raises it, as `FoodGoal` says. |
| `ClickMorale` | `true` | A click on the Morale bar does something that raises it, as `MoraleMode` says. |
| `ClickStamina` | `true` | A click on the Stamina bar rests in the best bed at home, as `BedModes` says. |
| `ClickLife` | `true` | A click on the Life bar uses the items in `LifeItems`. |
| `UseHomeStorage` | `true` | Lets clicks use items from the home storage too while you are at home, not only the bag. |
| `AddToQueue` | `true` | Adds a click's actions after the ones already queued. `false`: they replace the queue. |
| `Toasts` | `true` | Says in a toast what a click queued, or why it queued nothing. |
| `FoodGoal` | `Max` | How far a click on Satiety eats. `Max`: to the maximum. `WellFed`: to Well Fed. `AboveWellFed`: `FoodAbovePercent` past Well Fed. Food that would go past the maximum is never eaten. |
| `FoodAbovePercent` | `10` | How far past Well Fed `AboveWellFed` eats, in percent of the maximum, from 0 to 100. |
| `FoodMostItems` | `4` | The most food one click on Satiety eats, from 1 to 10. |
| `FoodSkip` | empty | Food a click never eats, as `i` and an item ID, or `g` and a kind of food for all of that kind, split by commas, such as `i1203,g1002`. |
| `FoodAvoidMoraleLoss` | `false` | Leaves out food that lowers Morale. |
| `BedModes` | empty | How a click on Stamina uses each bed type, as type:mode split by commas, such as `20001:Sleep` (the Wooden Bed). `Auto` naps when only `NapBelow` percent or less is missing and sleeps otherwise. `Sleep` always sleeps and `Nap` always naps. `Skip` never uses that bed. A bed type not listed is `Auto`. |
| `NapBelow` | `25` | With a bed on `Auto`, naps when this much Stamina or less is missing, in percent of the maximum, from 0 to 100, and sleeps when more is. |
| `MoraleMode` | `Best` | What a click on Morale queues. `Best`: one action, those that raise another stat too first, then the biggest gain. Clicking again while it is queued takes the next one. `Routine`: every action of `MoraleRoutine` that can be done now, in that order. |
| `MoraleRoutine` | empty | Your Morale routine, in order: `f` and a furniture action ID, `i` and an item ID, or `g` and a kind of item for the one of that kind you have that gives the most Morale, split by commas, such as `f12,i3001,g3`. |
| `MoraleItems` | `false` | Lets `Best` use items that raise Morale too, not only furniture. |
| `AvoidLifeLoss` | `true` | Leaves out Morale actions that cost Life. |
| `LifeItems` | empty | The items a click on Life uses, in order, as `i` and an item ID, or `g` and a kind of item for every one of that kind you have, the most Life first, split by commas, such as `i1203,g900`. Empty: any item that restores Life, smallest first. |
| `LifeMostItems` | `1` | The most items one click on Life uses, from 1 to 10. It stops once Life is full. |

## Difficulty

Each multiplier goes from `0` to `5`. `1` is the game's own. It counts every change alike: the slow change over time, food, rest, buffs and furniture. Only your character is changed, not other characters. The clicks on the bars and the queue marks count these multipliers. Game effects that react to a stat changing see the changed amount, so with a multiplier at `0` they no longer react to that stat rising or falling.

| Setting | Default | What it does |
| --- | --- | --- |
| `SatietyDrain` | `1` | Multiplies every loss of Satiety. `2` makes it fall twice as fast, `0` stops it falling. |
| `MoraleDrain` | `1` | Multiplies every loss of Morale. |
| `StaminaDrain` | `1` | Multiplies every loss of Stamina. |
| `LifeDrain` | `1` | Multiplies every loss of Life. |
| `SatietyGain` | `1` | Multiplies every gain of Satiety. `2` makes food, rest and the rest raise it twice as much, `0` stops it rising. |
| `MoraleGain` | `1` | Multiplies every gain of Morale. |
| `StaminaGain` | `1` | Multiplies every gain of Stamina. |
| `LifeGain` | `1` | Multiplies every gain of Life. |

## Timers

A multiplier changes how long a buff lasts: `1` is the game's own time, `2` makes it last twice as long, `0.5` half as long. Every multiplier goes from `0.1` to `100`. Change them on the Mods screen, or in the file while the game is closed. Changes made on the Mods screen take effect at once, on buffs already running too.

Each buff uses the most specific multiplier it has: its own multiplier first, then its category's, then the good or bad one. Only buffs you can see on your character and that have a timer are changed. Hidden buffs and buffs that last until something removes them keep the game's own behavior.

| Setting | Default | What it does |
| --- | --- | --- |
| `GlobalGood` | `1` | Multiplies how long good buffs last. |
| `GlobalBad` | `1` | Multiplies how long bad buffs (debuffs) last. |
| `Buffs` | empty | Buffs with a multiplier of their own, as buff ID:multiplier split by commas, such as `200810:2,1043:0.5`. See Single buffs below. |

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

A buff's own multiplier, set in `Buffs` under Timers, replaces every other one for that buff. On the Mods screen, add a buff from the picker, which lists every buff with a timer and how long it lasts, then step its multiplier. A value outside `0.1` to `100` is moved to the nearest end, and an entry that is not a buff ID and a number is skipped.

Earlier versions kept these in `BepInEx\config\VitalSigns.Timers.json`. The first start of 0.10.0 moves them into `Buffs` and renames the file to `VitalSigns.Timers.json.old`.
