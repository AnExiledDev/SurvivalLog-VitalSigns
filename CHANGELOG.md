# Changelog

## 0.9.2

- More tests for when stat difficulty starts working. Nothing changes in play: the multipliers still do nothing until one is set off 1.

## 0.9.1

- The food note in the Actions tab now says what a Satiety click eats: just enough to reach the goal, never past full.

## 0.9.0

- Click a stat bar to queue what raises it. Satiety eats to the maximum, to Well Fed, or a set percent past it. Stamina sleeps or naps in the best bed at home. Morale does the action that also raises another stat first, then the biggest gain, and a second click takes the next one; or set a routine and a click runs all of it. Life uses the items you choose. Turn each bar off with ClickSatiety, ClickMorale, ClickStamina and ClickLife.
- A new Actions tab on Vital Signs' page in the Mod Manager sets up the clicks. It lists every furniture action and item each click can use, greys out the ones your base or bag lacks, shows what each click would queue now, and has every setting behind it.
- Set how a click uses each bed type: Auto, Sleep, Nap or Skip. Auto naps when only NapBelow percent or less of Stamina is missing.
- Click a debuff to queue its cure, from the bag first, then from the home storage. A debuff with a cure at hand is marked. Turn it off with ClickCures.
- A bar can hide while it is full, or above a level you set, and come back as soon as the stat drops. Turn it on with HideFullBars.
- A bar pulses once it drops to the warning line, before the game turns it red. Turn it off with WarnPulse.
- Each bar marks where the stat will be once the actions in the queue are done. Turn it off with QueueProjection.
- Small Satiety, Morale, Stamina and Life bars show in a corner while you explore, where the game hides its own. Set the corner and size with ExploreBars and ExploreBarsSize.
- Sort the buffs by time left, keep good buffs and debuffs apart, move them to another side of the bars, or fold each row into one badge. Shift-click a buff to pin it to the front and draw it larger.
- A layout editor moves and resizes each bar and the buffs. Open it with the 「Edit HUD layout」 button in the Actions tab, or set EditLayoutKey.
- Difficulty multiplies how fast each stat falls and how much it rises, from 0 to 5, for your character only.
- Made for Survival Log 1.1.20151.

## 0.8.0

- Trend arrows: a small arrow by a stat's value shows when it rose or fell over the last game hour.
- A good buff now pulses when it has an hour or less left, so you can renew it in time. Set how soon with BuffEndingMinutes, or make debuffs pulse too with BuffEndingDebuffs.
- The Inventory button's hover box now lists the 5 heaviest items in your bag, with their weight. Set how many with HeaviestItems.
- The hover boxes, the multiplier chips and the bar frames now take the colours of the HUD style chosen in Exiled's Toolkit 0.18.0 or newer. Turn it off with MatchHudStyle.
- BarLook draws the stat bars flat, with no frame or shadow, or split into tenths.
- ColourBlind uses blue in place of green and orange in place of red.
- Made for Survival Log 1.1.20151.

## 0.7.5

- The Mod Manager no longer says Vital Signs settings need a game restart. They all apply right away.
- Made for Survival Log 1.1.20151.

## 0.7.4

- The Timers tab now shows buff names and descriptions in English when the game is in English. Before, they could show in Chinese.
- The Timers tab's category names are no longer cut off, the filter buttons are the right size, and rows no longer show through above the table header.
- BarTooltipSize now starts at 80%, the same as BarSize and BuffSize. A size you already set is kept.
- Made for Survival Log 1.1.20151.

## 0.7.3

- Hover over the multiplier chip on a bar to see what makes it up: each buff that raises or lowers the stat's gain, with its amount, and anything else as Other.
- Made for Survival Log 1.1.20151.

## 0.7.2

- The multiplier on a bar is now a solid green or red chip just left of the bar, so the bar's name and value no longer run past its edge.
- Hover boxes that would run off the top or bottom of the screen now move to stay on it, and their arrow still points at the bar or buff.
- A buff's own description is now shown one sentence to a line, in slightly larger and brighter text.
- Made for Survival Log 1.1.20151.

## 0.7.1

- Fitness Bar 0.1.2 and newer turn themselves off when Vital Signs is installed, so the Fitness hover box now asks to remove Fitness Bar only when an older version still draws a second bar.
- Made for Survival Log 1.1.20151.

## 0.7.0

- New: warnings when a stat runs low. Satiety, Morale, Stamina and Life warn at 20% of their maximum, where the game turns the bar red, and Fitness warns below 40 and again below 30, where you become Weakened.
- Each warning comes once per drop, and again only after the stat climbs a little back. The new Alerts settings turn each one off and set the line.
- Every setting now takes effect at once, with no restart. Turning the mod off takes away everything it draws and puts every buff back on the game's own time.
- With the Fitness Bar mod also installed, the Fitness hover box says to remove it.
- Exiled's Toolkit 0.11.0 or newer is now required.

## 0.6.0

- New: a Fitness bar under the Life bar. Fitness is a stat the game keeps but never shows; it lowers your chance of getting sick, and below 30 you become Weakened.
- The Fitness bar is green when fine, amber within 10 of Weakened and red below 30. Its hover box says what Fitness does and how close you are to Weakened.
- This replaces the Fitness Bar mod. Remove `FitnessBar.dll` if you have it, or you get two bars. The new FitnessBar setting turns it off.
- New: the BarTooltipSize setting sets how big the bars' hover boxes are drawn, from 50% to 150%, apart from BarSize. It starts at 100%, the game's size, so the hover box text is bigger than before.
- The stat hover boxes are darker, the same as the buff ones, so the HUD behind them no longer shows through.

## 0.5.0

- New: make buffs and debuffs last longer or shorter, from 0.1x to 100x. Good buffs and bad buffs each have their own multiplier.
- New: each buff category can have its own multiplier, which replaces the good and bad ones for that category.
- New: any single buff can have its own multiplier, set in the Timers tab on the Mod Manager's page for Vital Signs.
- The Timers tab lists every buff with a timer, how long it lasts in the game, and how long it lasts with your multipliers, in game time and in real minutes.
- Changing a multiplier also changes buffs that are already running, and talents that make buffs last longer are multiplied along with the buff.
- With every multiplier at 1, the default, every buff keeps the game's own time.

## 0.4.0

- Buffs next to the bars are bigger, so they look in proportion to the bars.
- The new BuffSize setting sets how big the buffs are drawn, from 50% to 150%, apart from the bars. It starts at 80%, the same as BarSize.
- Fixed: hovering a buff showed the game's old hover box for a moment before the new one.

## 0.3.1

- A stat's gain multiplier now sits right after the stat's name instead of on top of it.

## 0.3.0

- The bars are drawn at 80% of the game's size by default, and the buffs next to them shrink with them. The new BarSize setting sets the size, from 50% to 150%.
- Buff badges and their text are smaller, so a long list of buffs takes less room.
- Buff hover boxes are smaller and easier to read: the name and time left share the top line, each effect has its value lined up on the right, and the cure and suppress lists have their own headings.
- The game's description sits at the bottom of the buff hover box in smaller, softer text, and the box is darker so the HUD behind it no longer shows through.
- Updated for Survival Log 1.1.20151.

## 0.2.0

- Each stat bar shades the part that running debuffs will still take before they end.
- The stat hover box now shows the exact value, every scaled gain and loss as a percentage, your food modifiers and each loss still coming with how long it takes.
- Hovering a buff or debuff shows what it does to each stat, how many layers you have, how much time is left and a bar for it.
- Debuff hover boxes list the items that cure or suppress them, with how many you have. Items you have come first.
- A suppressed debuff says what is holding it back.
- A buff's timer stripe turns amber when little time is left.
- The Inventory button has a weight bar with a mark where Overweight starts.
- The Life bar shows a heart, and the bar text has an outline so it stays readable.
- New settings let you turn off the weight, the timer stripes, the extra hover box details, the game's own stat text and the cure lists.

## 0.1.0

- First release.
- Each stat bar shows its gain multiplier when something scales it, and its hover box lists every scaled change.
- The Satiety bar shows where Well Fed starts and ends.
- Buffs and debuffs that run out get a stripe that shrinks as their time runs down.
- The Inventory button shows the weight you carry, and its hover box shows how much room you have before Overweight.
