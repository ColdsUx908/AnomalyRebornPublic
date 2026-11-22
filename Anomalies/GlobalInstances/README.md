# 全局实例

本目录集中维护 Anomalies 的全局实体和玩家数据。相关行为、同步和配置入口见下表。

## 代码导航

| 入口 | 职责 |
| --- | --- |
| [GlobalInstances/AnomalyGlobalInstances.cs](AnomalyGlobalInstances.cs) | 统一 GlobalType/ModPlayer 容器、AI 数据及框架入口 |
| [GlobalInstances/AnomalyGlobalInstances.CanHit.cs](AnomalyGlobalInstances.CanHit.cs) | 高频全局命中和弹幕碰撞判定 |
| [AnomalyPlayer](AnomalyPlayer/)、[AnomalyGlobalNPC](AnomalyGlobalNPC/)、[AnomalyGlobalProjectile](AnomalyGlobalProjectile/)、[AnomalyGlobalItem](AnomalyGlobalItem/) | 各实体的额外数据、克隆、重置与同步 |
| [AnomalyModTypes.cs](../Game/Entities/AnomalyModTypes.cs) | Anomalies 实体和 Behavior 基类 |
| [AnomalySynchronization.cs](../Game/Networking/AnomalySynchronization.cs) | 消息分派和可选内容接入 |
| [AnomalyConfigs.cs](../Core/Configuration/AnomalyConfigs.cs) | 配置定义 |
| [AnomalySharedData.cs](../Game/World/AnomalySharedData.cs) | 世界共享状态 |
| [BossDeathEffectRegistry.cs](../Game/Combat/BossDeathEffectRegistry.cs) | 否决死亡时的致死特效拦截注册 |
| [Items](../Game/Items/) | 内容标识契约与稀有度 |
| [Behaviors](../Behaviors/) | 出生物品、基础属性和公共实体行为接入 |
| [DangerousTreeAndCactus.cs](../Game/World/DangerousTreeAndCactus.cs) | 异象世界中的树与仙人掌行为 |

## 状态与依赖边界

玩家、NPC、弹幕和物品的实例状态分别归对应容器。每个容器目录的 `*.ExtraData.cs` 保存主内容数据。`*.ExtraData.Tweak.cs` 和 `*.ExtraData.SpecialContents.cs` 分别受 `TWEAK` 和 `SPECIAL_CONTENTS` 控制；克隆、重置和同步入口中的相关引用使用相同开关。Boss 状态优先使用公共类型并归入统一容器，由对应 Boss 解析行为或执行绘制。仍需直接引用 Boss 专属类型的数据及其生命周期处理留在对应 Boss 目录，通过可选部分类方法接入。可变容器需独立克隆或重置，绘制缓存不进入网络快照。

可复用抽象内容基类见 [BaseContents](../BaseContents/)。框架 API 见 [Transoceanic](../../Transoceanic/README.md)。

[ModIntegration](../Core/ModIntegration/) 中的 `AnomalyModReferences` 管理可选模组引用，`BridgeUtils` 和 `BridgeExtensions` 提供兼容调用，`AnomalyModCall` 提供对外接口，`AnomaliesMusicHandler` 接入音乐。新增可选依赖调用须确认加载条件，不能让缺失模组导致默认内容加载失败。

## 修改与验证

遵循[根设计规范](../../DESIGN_GUIDELINES.md)。修改生成调度前读取独立资料包的 `Validation/docs/behavior-generation.md`；修改同步前读取同步审计并以当前代码核实。

执行 [Anomalies 构建检查](../README.md)。同步相关检查位于独立资料包的 `Validation/Tests/Anomalies/Integration/NetworkSync/` 和 `EntitySyncRoundTrip/`，注册检查位于 `Validation/Tests/Anomalies/Loading/`。改变模块接入需检查全部 4 种组合；框架变更还须验证消费方。联机应覆盖专用服务器、中途加入、克隆、死亡与世界重载。
