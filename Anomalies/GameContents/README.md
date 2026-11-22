# 普通游戏内容

本目录的具体内容属于始终编译的模组主内容。内容存在条件与掉落、商店等获取入口分别控制，详见 [MODULES.md](../MODULES.md)。

## 功能与代码导航

| 入口 | 当前内容与接入 |
| --- | --- |
| [LegendaryNight](LegendaryNight/) | 古战场祭坛、坠星精华、星星之神的记忆和钱币袋；仪式及 Boss 掉落入口由骷髅王目录接入 |
| [AnomalyItems/HoneyedWildbloom](AnomalyItems/HoneyedWildbloom/) | 物品、宠物 Buff 与 FriendlyBeePet；蜂后掉落要求异象世界 |
| [AnomalyItems/AnomalyItemLoot.cs](AnomalyItems/AnomalyItemLoot.cs) | 内容掉落支持 |
| [LegendaryNight/StarfallKnocker.cs](LegendaryNight/StarfallKnocker.cs) | 叩星（Starfall Knocker）；5 秒蓄力、2D 挥击与圆形星爆，详见 [说明](LegendaryNight/README.md) |
| [Drinks](Drinks/) | 珍珠奶茶、梅子绿茶及覆盆子红茶 |
| [Buffs/DimensionalRend.cs](Buffs/DimensionalRend.cs) | 维度撕裂：按速度计算持续伤害与受伤增幅 |
| [MusicBoxes](MusicBoxes/) | Gods Weep — Divine Lament、Deep Blue — Echoes of the Azure、Godforsaken — Forsaken Bloodland、Rainpath — Pathway to Reverie 四种音乐盒及图格 |
| [TravelShopHandler.cs](Drinks/TravelShopHandler.cs) | 旅行商店接入 |

## 依赖与资源

音乐盒及图格还要求加载 AnomaliesMusic；音乐绑定、微光转换与图格交互由 [BaseMusicBox.cs](../BaseContents/BaseMusicBox.cs) 提供。其他共享抽象基类位于 `BaseContents/`，不属于本目录的具体内容注册。

中英文本地化位于 `Localization/` 的 GameContents 分组。变更物品、宠物或音乐盒时一起核对主材质、图格材质、Buff、音轨名称与文本键。

## 修改与验证

执行 [Anomalies 构建检查](../README.md)；改变模块边界或公共接入时运行独立资料包的模块组合验证。游戏内覆盖掉落前置条件、已持有物品的使用、宠物持续/死亡清理、旅行商店，以及音乐模组存在/缺失时的音乐盒加载、录制、放置、交互和微光转换。
