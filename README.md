# 直接替换 / Quick Replace

[Steam 创意工坊 / Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3831773702) · [下载 / Releases](https://github.com/w7lau/probably-stolen-quick-replace/releases)

把新滤芯拖到水壶上，或把新电池拖到电器上，就能直接换下旧的，不用先取出来。

把电池拖到充电器本体上：有空位先放入。放不下时，没充满的电池优先换出已满电的，让出位置继续充电；有多块满电电池时，取电量最多的一块。没有合适的满电电池，或拖入的电池已满时，才按剩余电量换出比手里电量更多的一块。打开充电器后，仍可拖到指定电池上交换，或拖到空格里放入。

旧件会放回新件原来的物品栏，放不下就不换。支持游戏允许安装的滤芯、电池等物品，暂不支持成叠物品。

枪械配件也能直接替换，包括顾客改装枪上自己装的配件。顾客原装且不能拆卸的配件仍按游戏规则处理。

物品升级、滴管吸液、滴液和容器间倒液体仍按原方式操作。

在游戏 Mod 管理中启用，并重启游戏。

| 换滤芯 / Filters | 换电池 / Batteries |
| --- | --- |
| ![换滤芯](workshop/filter.gif) | ![换电池](workshop/battery.gif) |

Drag a new filter onto a water pitcher, or a new battery onto an appliance, to replace the old one without removing it first.

Drop a battery onto the charger itself to fill empty space first. If there is no room, a battery that needs charging takes the place of a fully charged one, choosing the most energy among fully charged batteries. If none is eligible, or the incoming battery is already full, take a battery only if it has more remaining energy. Inside the open charger, drop onto a specific battery to swap with it, or onto empty space to place it there.

The old item goes back to the inventory the new one came from. If there is no room, nothing is swapped. Works with filters, batteries and other items the device accepts. Stacks are not supported.

Gun attachments can also be replaced directly, including your own attachments on a customer's service gun. Original customer parts that the game does not allow you to remove stay in place.

Item upgrades, drawing, dripping and pouring liquids keep their normal behavior.

Enable the mod in Mod management and restart the game.

## 下载 / Download

从 [Releases](https://github.com/w7lau/probably-stolen-quick-replace/releases) 下载 ZIP，将其中的 `QuickReplace` 文件夹解压到游戏的 `Mods` 目录，在 Mod 管理中启用并重启游戏。

Download the ZIP from Releases, extract `QuickReplace` into the game's `Mods` folder, enable it and restart the game.

## 构建 / Build

需要 .NET SDK 和本地游戏文件。源码使用 MIT 许可，不分发游戏或 Harmony 的程序集。

Requires a .NET SDK and a local game installation. Game and Harmony assemblies are not distributed.

```powershell
./build.ps1 -ManagedDir 'D:\Games\Probably Stolen\Probably Stolen_Data\Managed'
dotnet run --project tests/QuickReplace.Tests.csproj -c Release
```

构建结果在 `dist`。原创代码采用 [MIT License](LICENSE)。
