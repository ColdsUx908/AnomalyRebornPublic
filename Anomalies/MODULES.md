# Anomalies 构建模块

主内容始终编译和加载：异象难度、开启物品、Boss 行为及专属实体、GameContents 具体内容及商店/掉落接入。原 ANOMALY_MODE、GAME_CONTENTS 标签及构建属性已移除。世界难度、Boss 独立开关及音乐依赖等逐项条件仍适用。

| 标签 | MSBuild 属性 | 内容 |
| --- | --- | --- |
| `TWEAK` | `AnomaliesEnableTweak` | Tweaks 武器修改、修改标识、玩家基础属性配置与行为 |
| `SPECIAL_CONTENTS` | `AnomaliesEnableSpecialContents` | SpecialContents 内容、出生赠送及同步接入 |

两个属性默认 true，仅接受 true / false。关闭两个可选模块：

```powershell
dotnet build Anomalies/Anomalies.csproj -p:AnomaliesEnableTweak=false -p:AnomaliesEnableSpecialContents=false
```

C# 检查可追加 `-p:BuildMod=false`；实际进入游戏前须重新打包。全关仍保留全部主内容、菜单、星空、血条与公共支持。

## 内容与接入边界

props 只按整个目录排除 Tweaks、SpecialContents，不处理单文件或恢复例外。目录外接入使用对应条件编译标签；共享基类位于 BaseContents，Testing 物品仅要求 DEBUG。新增模块只能由项目所有者 ColdsUx 最终决定并提交。

- 古战场祭坛、坠星精华、星星之神的记忆、钱币袋及叩星均为主内容。仪式、激活音效和专属掉落接入留在骷髅王目录；保留可选部分类方法以支持单独移除该 Boss。关闭 `AnomalyBossChangeToggler.LegendaryNight` 时右键祭坛不会激活仪式。已持有的钱币袋不依赖 Boss 开关。
- 普通超凡骷髅王掉落坠星精华，传说之夜掉落钱币袋，保留原运行时掉落条件。
- HoneyedWildbloom、宠物与 Buff 始终存在，蜂后掉落保留异象世界条件。
- 世界模式存档、网络与 Mod.Call 始终提供主内容逻辑；不保留旧模块关闭时的 false 回退。网络协议不变。
- 测试物品的 Boss 专属操作继续通过可选接入支持 Boss 目录移除。

## 逐项内容开关

特殊内容另有逐项加载开关，集中于 `Anomalies/SpecialContents/AnomalySpecialContentToggler.cs`（`EventideReunion`、`ImmaculateWhite`、`MeguminStaff`、`VacuousBlack`）。它们使用带私有 setter 的静态布尔属性；落日遗逢默认值取 `EnableUnfinishedContents`（Debug 开启、Release 关闭），其余默认 `true`。将对应初始值改为 `false` 后重新构建并打包，对应物品、专属弹幕和 Buff 不再加载，出生赠送也跳过关闭的物品。落日遗逢的专属行为与绘制接入同时停止加载。这些开关仍受 `SPECIAL_CONTENTS` 总模块约束；测试物品仍仅由 `DEBUG` 控制。


## 验证

运行独立资料包的 `Validation/Tests/Anomalies/Integration/Modules/Verify.ps1`：构建全部 4 种组合并检查实际程序集中的主内容、可选内容及配置/行为边界，最后恢复默认构建。游戏加载、战斗和联机仍须实际验收。
