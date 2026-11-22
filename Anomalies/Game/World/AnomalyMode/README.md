# 异象模式

本目录负责异象难度的开启、运行条件和难度联动，属于始终编译的模组主内容。Boss 攻击实现在 [Bosses](../../../Bosses/README.md)。

## 启用条件与功能

裂天金冕提供模式切换入口；世界运行检查要求大师难度，Ultra 条件由统一难度状态处理。当前 `AnomalyHandler.EnableMultiplayerSupport` 为 true。模式查询、存档、网络和 Mod.Call 随主内容提供。

## 代码导航

| 入口 | 职责 |
| --- | --- |
| [HeavenrendCrown.cs](../../../GameContents/AnomalyItems/HeavenrendCrown/HeavenrendCrown.cs) | 使用条件、持有动画与服务端模式切换 |
| [AnomalyHandler.cs](AnomalyHandler.cs) | 世界运行检查、模式操作与界面接入 |
| [AnomalyHandler.Calamity.cs](AnomalyHandler.Calamity.cs) | 灾厄难度联动 |
| [AnomalyPlayerSync.cs](AnomalyPlayerSync.cs) | 玩家侧模式同步接入 |
| [AnomalySharedData.cs](../AnomalySharedData.cs) | 公共世界数据与状态 |
| [AnomalySharedData.QuickAccess.cs](../AnomalySharedData.QuickAccess.cs) | 难度查询接口 |

## 状态与同步

服务器决定权威模式状态。裂天金冕持有弹幕通过 `ai[0]` 同步开启/关闭操作；客户端动画不独立决定世界难度。模式消息由公共收包入口读取。

## 修改与验证

执行 [Anomalies 构建检查](../../../README.md) 和独立资料包的模块组合、网络同步检查。游戏内覆盖金冕使用、Boss 在场、旅行模式、存档重载、加入已有世界及可选灾厄难度联动；多人支持开关不代替实际联机验证。
