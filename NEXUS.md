Name: Vital Signs
Summary: Shows how your stats are scaled right now: gain multipliers on each HUD bar, Well Fed lines on Satiety, a timer stripe on every buff that runs out, and your carried weight on the Inventory button. Hover a bar for every scaled gain and loss, or the bag for the room left before Overweight. Reads every couple of seconds and redraws only on change.

Description (BBCode):
[center][img]https://sldb.core-directive.com/media/save/1-save-safe.png[/img][/center]

The game's stat bars show a value and nothing else. Food, buffs and debuffs quietly change how fast each stat rises and falls, and the only way to see how long a buff lasts is to hover it. Vital Signs puts that on the bars themselves.

[size=5]Main features[/size]
[list]
[*][b]Gain multiplier on each bar[/b]: when something scales how much a stat gains, the bar shows it, for example x1.25 in green or x0.5 in red.[/*]
[*][b]More in the bar's hover box[/b]: every scaled change for that stat, such as "Losses x0.75" or "Natural recovery x1.5".[/*]
[*][b]Well Fed lines on the Satiety bar[/b]: one line where Well Fed starts and a fainter one where it ends, read from the game's own rule.[/*]
[*][b]Timer stripe on buffs[/b]: every buff or debuff that runs out gets a thin stripe that shrinks as its time runs down.[/*]
[*][b]Weight on the Inventory button[/b]: the kilograms you carry, amber when you are close to Overweight and red past it. Hover the button for how much room you have left.[/*]
[*][b]English or Chinese[/b]: follows the game's language.[/*]
[*][b]Light on the game[/b]: reads your stats every couple of seconds and only redraws when something changed.[/*]
[/list]
Vital Signs only reads your stats and draws them. It never changes the game's data, adds nothing to your save and does not edit game files. You can remove it at any time.

[size=5]Known issues[/size]
Values are read every couple of seconds, so a change can take that long to show. Stats UI draws much of the same; use one or the other.

[center][img]https://sldb.core-directive.com/img/mods-banner.jpg[/img]
[size=4][url=https://www.nexusmods.com/survivallog/mods/73]Get Mod Manager[/url] to set up all your mods in game  |  [url=https://sldb.core-directive.com/en/]Open the Survival Log wiki[/url] for items, recipes and maps[/size][/center]

[size=5]Requirements[/size]
[list]
[*]Survival Log 1.1.19611. A game update can break it until the mod is updated.[/*]
[*][url=https://www.nexusmods.com/survivallog/mods/12]BepInEx for Survival Log[/url]. Install it first.[/*]
[/list]

[size=5]Installation[/size]
[list=1]
[*]Install [url=https://www.nexusmods.com/survivallog/mods/12]BepInEx[/url] [i](if no other mods were installed before, start the game once so BepInEx finishes setup, then quit)[/i].[/*]
[*]Download the zip from the Files tab.[/*]
[*]Extract it into the game folder (the folder with [font=Courier New]SurvivalLog.exe[/font]). The zip holds a BepInEx folder, so the DLL lands in the right place. When asked, merge the folders. The result should be:
[code]C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\VitalSigns.dll[/code][/*]
[/list]

[size=5]Uninstall[/size]
Delete [font=Courier New]VitalSigns.dll[/font] from the [font=Courier New]BepInEx\plugins[/font] folder.

[size=5]Configuration[/size]
One setting, [font=Courier New]Enabled[/font], in [font=Courier New]BepInEx\config\com.dev.survivallog.vitalsigns.cfg[/font] or on the Mod Manager's Mods screen. It takes effect at the next game start.

[size=5]Feedback[/size]
Found a bug or have an idea? Come say hi on the [url=https://discord.gg/trTsyxUFqg]Core Directive Discord[/url], or post in this page's Posts tab. For bugs, please include [font=Courier New]BepInEx\LogOutput.log[/font] and [font=Courier New]BepInEx\vital-signs.log[/font].
To reach me directly, message [b]exiled.dev[/b] on Discord; you can find me in the [url=https://discord.gg/MZGg4gQAUe]official Survival Log Discord[/url]. I'm also on X as [url=https://x.com/AnExiledDev]@AnExiledDev[/url].

[size=5]Source and license[/size]
Open source under MIT: [url=https://github.com/AnExiledDev/SurvivalLog-VitalSigns]github.com/AnExiledDev/SurvivalLog-VitalSigns[/url]

[line]
[size=5]简体中文[/size]

游戏的属性条只显示一个数值。食物、增益和减益会悄悄改变每项属性上升和下降的速度，而想知道一个状态还能持续多久，只能把鼠标移上去看。Vital Signs 把这些直接显示在属性条上。

[size=5]主要功能[/size]
[list]
[*][b]属性条上的增益倍率[/b]：当某项属性的增加量被放大或缩小时，属性条上会显示倍率，例如绿色的 x1.25 或红色的 x0.5。[/*]
[*][b]悬停提示里的更多信息[/b]：列出这项属性所有被改变的倍率，例如「减少 x0.75」或「自然恢复 x1.5」。[/*]
[*][b]「饱腹」条上的「饱腹富余」界线[/b]：一条线标出获得「饱腹富余」的位置，另一条较淡的线标出失去它的位置，数值来自游戏自身的规则。[/*]
[*][b]状态计时条[/b]：每个会结束的增益或减益底部都有一条细条，随剩余时间缩短。[/*]
[*][b]「背包」按钮上的负重[/b]：显示当前负重（千克），接近「超重」时变成琥珀色，超过后变成红色。鼠标悬停可查看距离「超重」还有多少余量。[/*]
[*][b]中英文[/b]：跟随游戏语言。[/*]
[*][b]不拖慢游戏[/b]：每隔几秒读取一次属性，只有变化时才重新绘制。[/*]
[/list]
Vital Signs 只读取并显示你的属性，从不改动游戏数据，不会往存档里写入任何内容，也不会修改游戏文件。你可以随时移除它。

[size=5]已知问题[/size]
数值每隔几秒读取一次，所以变化可能要等这么久才会显示。Stats UI 显示的内容大多相同，二者选其一即可。

[url=https://www.nexusmods.com/survivallog/mods/73]获取 Mod Manager[/url]，在游戏里设置所有模组。[url=https://sldb.core-directive.com/zh/]打开 Survival Log 维基[/url]，物品、配方、地图一查就有。

[size=5]前置要求[/size]
[list]
[*]Survival Log 1.1.19611。游戏更新后模组可能暂时失效，直到模组更新为止。[/*]
[*][url=https://www.nexusmods.com/survivallog/mods/12]BepInEx for Survival Log[/url]，请先安装。[/*]
[/list]

[size=5]安装[/size]
[list=1]
[*]安装 [url=https://www.nexusmods.com/survivallog/mods/12]BepInEx[/url] [i]（如果之前没有装过其他模组，先启动一次游戏，让 BepInEx 完成初始化，然后退出）[/i]。[/*]
[*]在「文件」页下载压缩包。[/*]
[*]把压缩包解压到游戏目录（[font=Courier New]SurvivalLog.exe[/font] 所在的文件夹）。压缩包里带有 BepInEx 文件夹，DLL 会自动放到正确位置。如果提示合并文件夹，选择合并。解压后应为：
[code]C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\VitalSigns.dll[/code][/*]
[/list]

[size=5]卸载[/size]
从 [font=Courier New]BepInEx\plugins[/font] 文件夹中删除 [font=Courier New]VitalSigns.dll[/font]。

[size=5]配置[/size]
只有一项设置 [font=Courier New]Enabled[/font]，位于 [font=Courier New]BepInEx\config\com.dev.survivallog.vitalsigns.cfg[/font]，也可以在 Mod Manager 的「模组」界面修改，在下次启动游戏时生效。

[size=5]反馈[/size]
发现 bug 或有好点子？欢迎来 [url=https://discord.gg/trTsyxUFqg]Core Directive Discord[/url] 打个招呼，或在本页的「Posts」标签里发帖。报告 bug 时，请附上 [font=Courier New]BepInEx\LogOutput.log[/font] 和 [font=Courier New]BepInEx\vital-signs.log[/font]。
想直接联系我，可以在 Discord 上给 [b]exiled.dev[/b] 发消息，我在 [url=https://discord.gg/MZGg4gQAUe]Survival Log 官方 Discord[/url] 里。也可以在 X 上找我：[url=https://x.com/AnExiledDev]@AnExiledDev[/url]。

[size=5]源代码与许可[/size]
基于 MIT 许可开源：[url=https://github.com/AnExiledDev/SurvivalLog-VitalSigns]github.com/AnExiledDev/SurvivalLog-VitalSigns[/url]
