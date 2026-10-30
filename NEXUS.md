Name: Vital Signs
Summary: Shows more about your stats on the HUD and lets you set how long buffs last. Bars get gain multipliers, coming-loss shading and Well Fed lines; buffs get timer stripes and hover boxes with effects and cures. Make buffs and debuffs last 0.1x to 100x, by good or bad, category or single buff, in a Timers tab that lists every buff.

Description (BBCode):
[center][img]https://sldb.core-directive.com/media/save/1-save-safe.png[/img][/center]

The game's stat bars show a value and nothing else. Food, buffs and debuffs quietly change how fast each stat rises and falls, and the only way to see how long a buff lasts is to hover it. Vital Signs puts that on the bars themselves. And since every buff has a fixed time, it lets you change those times too: make good buffs last twice as long and bad ones half as long, or slow down only the ones you care about.

[size=5]Main features[/size]
[list]
[*][b]Gain multiplier on each bar[/b]: when something scales how much a stat gains, the bar shows it, for example x1.25 in green or x0.5 in red.[/*]
[*][b]Coming losses shaded on each bar[/b]: the part of a bar that running debuffs will still take is shaded, so you can see where the stat will land.[/*]
[*][b]More in the bar's hover box[/b]: the exact value, every scaled change as a percentage, your food modifiers and each loss still coming with how long it takes.[/*]
[*][b]Buff hover boxes[/b]: a compact box with the name and time left on one line, then what a buff or debuff does to each stat and how many layers you have. Debuffs list the items that cure or suppress them, with how many you have.[/*]
[*][b]Well Fed lines on the Satiety bar[/b]: one line where Well Fed starts and a fainter one where it ends, read from the game's own rule.[/*]
[*][b]Timer stripe on buffs[/b]: every buff or debuff that runs out gets a thin stripe that shrinks as its time runs down and turns amber near the end.[/*]
[*][b]Weight on the Inventory button[/b]: the kilograms you carry, amber when you are close to Overweight and red past it. Hover the button for how much room you have left.[/*]
[*][b]Bar and buff size[/b]: the bars and the buffs next to them are drawn at 80% of the game's size by default. Set each one from 50% to 150%.[/*]
[*][b]English or Chinese[/b]: follows the game's language.[/*]
[*][b]Light on the game[/b]: reads your stats every couple of seconds and only redraws when something changed.[/*]
[*][b]Buff times[/b]: make buffs and debuffs last longer or shorter, from 0.1x to 100x. Set one multiplier for good buffs and one for bad, one per category, or one for any single buff.[/*]
[*][b]Timers tab[/b]: Vital Signs' page in the Mod Manager has a Timers tab that lists every buff with a timer, how long it lasts in the game, and how long it lasts with your multipliers, in game time and in real minutes.[/*]
[*][b]Running buffs too[/b]: a buff that is already running gets its remaining time changed when you change its multiplier.[/*]
[*][b]Talents still count[/b]: talents that make buffs last longer are multiplied along with the buff.[/*]
[/list]
Only buffs you can see on your character and that have a timer get a new time. Hidden buffs and buffs that last until something removes them keep the game's own behavior. No buff can be given more than 500 game days.

The HUD part only reads your stats and draws them. With every multiplier at 1, the default, no buff time changes either. Vital Signs adds nothing to your save and does not edit game files. A save keeps the end time of each running buff, so after you remove the mod, buffs already running finish at the time they had, and new buffs use the game's own times.

[size=5]Known issues[/size]
Values are read every couple of seconds, so a change can take that long to show. The loss shading counts only the debuffs running now, not natural drain or anything you eat. Stats UI draws much of the same; use one or the other. A buff made to last a very long time shows a large hour count in its hover box, such as [font=Courier New]12000:00[/font] for 500 days. Multipliers changed while the game is closed apply to new buffs only.

[center][img]https://sldb.core-directive.com/img/mods-banner.jpg[/img]
[size=4][url=https://www.nexusmods.com/survivallog/mods/73]Get Mod Manager[/url] to set up all your mods in game  |  [url=https://sldb.core-directive.com/en/]Open the Survival Log wiki[/url] for items, recipes and maps[/size][/center]

[size=5]Requirements[/size]
[list]
[*]Survival Log 1.1.20151. A game update can break it until the mod is updated.[/*]
[*][url=https://www.nexusmods.com/survivallog/mods/12]BepInEx for Survival Log[/url]. Install it first.[/*]
[*]Optional: [url=https://www.nexusmods.com/survivallog/mods/73]Mod Manager[/url]. The Timers tab, where you see every buff and set single buffs, is on this mod's page in the Mod Manager. Without it, set the buff multipliers in the config files.[/*]
[*]Optional: [url=https://www.nexusmods.com/survivallog/mods/61]Exiled's Toolkit[/url] 0.4.0 or newer. With it, buff multipliers changed on the Mod Manager's Mods screen take effect without restarting the game. The Timers tab does not need it.[/*]
[/list]

[size=5]Installation[/size]
[list=1]
[*]Install [url=https://www.nexusmods.com/survivallog/mods/12]BepInEx[/url] [i](if no other mods were installed before, start the game once so BepInEx finishes setup, then quit)[/i].[/*]
[*]Optional, for the Timers tab: install Mod Manager the same way.[/*]
[*]Download the zip from the Files tab.[/*]
[*]Extract it into the game folder (the folder with [font=Courier New]SurvivalLog.exe[/font]). The zip holds a BepInEx folder, so the DLL lands in the right place. When asked, merge the folders. The result should be:
[code]C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\VitalSigns.dll[/code][/*]
[/list]

[size=5]Uninstall[/size]
Delete [font=Courier New]VitalSigns.dll[/font] from the [font=Courier New]BepInEx\plugins[/font] folder.

[size=5]Configuration[/size]
Settings are in [font=Courier New]BepInEx\config\com.dev.survivallog.vitalsigns.cfg[/font] or on the Mod Manager's Mods screen, and take effect at the next game start. [font=Courier New]Enabled[/font] turns the mod on or off. Under Display you can turn off the weight, the timer stripes, the hover box details, the game's own stat text and the cure lists, and BarSize and BuffSize set how big the bars and the buffs are drawn (80% of the game's size by default).

For buff times, open the Mod Manager, go to Vital Signs and pick the Timers tab. Set the good and bad multipliers and the category ones at the top, and a single buff's multiplier in its row, then press Save. The changes take effect at once, on buffs already running too. They are also in the cfg file under Timers and TimerCategories, and single buffs are in [font=Courier New]BepInEx\config\VitalSigns.Timers.json[/font]. See [url=https://github.com/AnExiledDev/SurvivalLog-VitalSigns/blob/main/CONFIG.md]CONFIG.md[/url] for every setting.

[size=5]Feedback[/size]
Found a bug or have an idea? Come say hi on the [url=https://discord.gg/trTsyxUFqg]Core Directive Discord[/url], or post in this page's Posts tab. For bugs, please include [font=Courier New]BepInEx\LogOutput.log[/font] and [font=Courier New]BepInEx\vital-signs.log[/font].
To reach me directly, message [b]exiled.dev[/b] on Discord; you can find me in the [url=https://discord.gg/MZGg4gQAUe]official Survival Log Discord[/url]. I'm also on X as [url=https://x.com/AnExiledDev]@AnExiledDev[/url].

[size=5]Source and license[/size]
Open source under MIT: [url=https://github.com/AnExiledDev/SurvivalLog-VitalSigns]github.com/AnExiledDev/SurvivalLog-VitalSigns[/url]

[line]
[size=5]简体中文[/size]

游戏的属性条只显示一个数值。食物、增益和减益会悄悄改变每项属性上升和下降的速度，而想知道一个状态还能持续多久，只能把鼠标移上去看。Vital Signs 把这些直接显示在属性条上。每个状态的持续时间也是固定的，Vital Signs 还能让你改变这些时间：让增益状态持续两倍久、负面状态只持续一半，或者只放慢你在意的那几个。

[size=5]主要功能[/size]
[list]
[*][b]属性条上的增益倍率[/b]：当某项属性的增加量被放大或缩小时，属性条上会显示倍率，例如绿色的 x1.25 或红色的 x0.5。[/*]
[*][b]属性条上的即将扣除量[/b]：正在生效的减益在结束前还会扣掉的部分会以阴影标出，一眼就能看到属性会降到哪里。[/*]
[*][b]悬停提示里的更多信息[/b]：精确数值、所有被改变的倍率（以百分比显示）、你的食物倍率，以及每一项即将扣除的数值和所需时间。[/*]
[*][b]状态悬停提示[/b]：紧凑的提示框，名称和剩余时间在同一行，下面是增益或减益对每项属性的影响和当前层数。减益还会列出可以治愈或压制它的物品，以及你拥有的数量。[/*]
[*][b]「饱腹」条上的「饱腹富余」界线[/b]：一条线标出获得「饱腹富余」的位置，另一条较淡的线标出失去它的位置，数值来自游戏自身的规则。[/*]
[*][b]状态计时条[/b]：每个会结束的增益或减益底部都有一条细条，随剩余时间缩短，快结束时变成琥珀色。[/*]
[*][b]「背包」按钮上的负重[/b]：显示当前负重（千克），接近「超重」时变成琥珀色，超过后变成红色。鼠标悬停可查看距离「超重」还有多少余量。[/*]
[*][b]属性条与状态大小[/b]：属性条和旁边的状态默认按游戏原尺寸的 80% 显示，两者可分别在 50% 到 150% 之间设置。[/*]
[*][b]中英文[/b]：跟随游戏语言。[/*]
[*][b]不拖慢游戏[/b]：每隔几秒读取一次属性，只有变化时才重新绘制。[/*]
[*][b]状态持续时间[/b]：让增益和负面状态持续更久或更短，范围 0.1 到 100 倍。增益状态一个倍数，负面状态一个倍数，也可以每个类别一个，或给任意单个状态设置。[/*]
[*][b]「计时」标签页[/b]：Mod Manager 里 Vital Signs 的页面上有「计时」标签页，列出所有有计时的状态，显示游戏原本的持续时间和按你的倍数算出的持续时间，分别以游戏时间和现实分钟显示。[/*]
[*][b]正在生效的状态也会变[/b]：改变倍数时，正在生效的状态的剩余时间也会随之改变。[/*]
[*][b]天赋照样有效[/b]：延长状态时间的天赋会和状态一起乘以倍数。[/*]
[/list]
只改变角色身上看得见、并且有计时的状态。隐藏的状态和要等某件事才会消失的状态保持游戏原样。任何状态最多持续 500 个游戏日。

屏幕显示部分只读取并显示你的属性。所有倍数保持默认的 1 时，状态时间也不会改变。Vital Signs 不会往存档里写入任何内容，也不会修改游戏文件。存档会记下每个正在生效的状态的结束时间，所以移除模组后，正在生效的状态按原来的时间结束，新的状态使用游戏原本的时间。

[size=5]已知问题[/size]
数值每隔几秒读取一次，所以变化可能要等这么久才会显示。阴影只计算当前正在生效的减益，不包括自然消耗和你吃下或使用的东西。Stats UI 显示的内容大多相同，二者选其一即可。持续时间很长的状态，提示框里会显示很大的小时数，例如 500 天显示为 [font=Courier New]12000:00[/font]。在游戏关闭时修改的倍数只对新的状态生效。

[url=https://www.nexusmods.com/survivallog/mods/73]获取 Mod Manager[/url]，在游戏里设置所有模组。[url=https://sldb.core-directive.com/zh/]打开 Survival Log 维基[/url]，物品、配方、地图一查就有。

[size=5]前置要求[/size]
[list]
[*]Survival Log 1.1.20151。游戏更新后模组可能暂时失效，直到模组更新为止。[/*]
[*][url=https://www.nexusmods.com/survivallog/mods/12]BepInEx for Survival Log[/url]，请先安装。[/*]
[*]可选：[url=https://www.nexusmods.com/survivallog/mods/73]Mod Manager[/url]。查看所有状态、设置单个状态的「计时」标签页，在 Mod Manager 里本模组的页面上。没装的话，在配置文件中设置倍数。[/*]
[*]可选：[url=https://www.nexusmods.com/survivallog/mods/61]Exiled's Toolkit[/url] 0.4.0 或更新版本。装了它，在 Mod Manager 的「模组」界面改动的状态倍数无需重启游戏即可生效。「计时」标签页不需要它。[/*]
[/list]

[size=5]安装[/size]
[list=1]
[*]安装 [url=https://www.nexusmods.com/survivallog/mods/12]BepInEx[/url] [i]（如果之前没有装过其他模组，先启动一次游戏，让 BepInEx 完成初始化，然后退出）[/i]。[/*]
[*]可选（用于「计时」标签页）：用同样的方式安装 Mod Manager。[/*]
[*]在「文件」页下载压缩包。[/*]
[*]把压缩包解压到游戏目录（[font=Courier New]SurvivalLog.exe[/font] 所在的文件夹）。压缩包里带有 BepInEx 文件夹，DLL 会自动放到正确位置。如果提示合并文件夹，选择合并。解压后应为：
[code]C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\VitalSigns.dll[/code][/*]
[/list]

[size=5]卸载[/size]
从 [font=Courier New]BepInEx\plugins[/font] 文件夹中删除 [font=Courier New]VitalSigns.dll[/font]。

[size=5]配置[/size]
设置位于 [font=Courier New]BepInEx\config\com.dev.survivallog.vitalsigns.cfg[/font]，也可以在 Mod Manager 的「模组」界面修改，在下次启动游戏时生效。[font=Courier New]Enabled[/font] 用于开关模组。在 Display 分组里可以分别关闭负重显示、计时条、悬停提示详情、游戏自带的属性说明和治疗物品列表，BarSize 和 BuffSize 分别设置属性条和状态的显示大小（默认均为游戏原尺寸的 80%）。

状态持续时间：打开 Mod Manager，进入 Vital Signs，选择「计时」标签页。在顶部设置增益、负面和各类别的倍数，在状态所在的行设置单个状态的倍数，然后点「保存」。改动立即生效，正在生效的状态也会改变。这些倍数也在配置文件的 Timers 和 TimerCategories 分组中，单个状态的倍数在 [font=Courier New]BepInEx\config\VitalSigns.Timers.json[/font] 中。完整说明见 [url=https://github.com/AnExiledDev/SurvivalLog-VitalSigns/blob/main/CONFIG.md]CONFIG.md[/url]（英文）。

[size=5]反馈[/size]
发现 bug 或有好点子？欢迎来 [url=https://discord.gg/trTsyxUFqg]Core Directive Discord[/url] 打个招呼，或在本页的「Posts」标签里发帖。报告 bug 时，请附上 [font=Courier New]BepInEx\LogOutput.log[/font] 和 [font=Courier New]BepInEx\vital-signs.log[/font]。
想直接联系我，可以在 Discord 上给 [b]exiled.dev[/b] 发消息，我在 [url=https://discord.gg/MZGg4gQAUe]Survival Log 官方 Discord[/url] 里。也可以在 X 上找我：[url=https://x.com/AnExiledDev]@AnExiledDev[/url]。

[size=5]源代码与许可[/size]
基于 MIT 许可开源：[url=https://github.com/AnExiledDev/SurvivalLog-VitalSigns]github.com/AnExiledDev/SurvivalLog-VitalSigns[/url]
