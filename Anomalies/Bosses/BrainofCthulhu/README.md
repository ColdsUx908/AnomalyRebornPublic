# 克苏鲁之脑

本目录实现飞眼怪、触须、眩晕、转阶段、幻象和死亡演出。战斗实现采用 CalamityMod 克脑行为与相关效果，名称和幻象死亡提示使用对应英中文本；运行时不要求安装灾厄。

## 启用条件与功能

属于始终编译的模组主内容，要求异象世界及 `AnomalyBossChangeToggler.BrainofCthulhu`；默认 Debug 开启、Release 关闭。普通异象使用 Death 参数，Ultra 不额外改变该 AI。飞眼怪数量统一为 `CreeperCount = 30`。

战斗和血腥脊椎召唤不要求猩红环境；入场、飞眼怪攻击方向与 CrimsonEyes 追击使用地表行为。70% 转阶段和 25% 强制幻象等条件由状态属性统一解释。可选灾厄调用隔离于兼容类，包含肾上腺素重置和防御损伤交互。

## 代码导航

| 文件 | 职责 |
| --- | --- |
| [BrainofCthulhu.cs](BrainofCthulhu.cs) | 属性、参数、命中与死亡 |
| [BrainofCthulhu.State.cs](BrainofCthulhu.State.cs) | 阶段、行为、局部步骤及同步槽位 |
| [BrainofCthulhu.AI.cs](BrainofCthulhu.AI.cs) | 初始化、分派、目标与离场 |
| [BrainofCthulhu.Phase1AI.cs](BrainofCthulhu.Phase1AI.cs)、[PhaseChangeAI](BrainofCthulhu.PhaseChangeAI.cs)、[Phase2AI](BrainofCthulhu.Phase2AI.cs) | 一阶段、转阶段及二阶段攻击 |
| [BrainofCthulhu.Drawing.cs](BrainofCthulhu.Drawing.cs) | 主脑、幻象及触须绘制 |
| [BrainByteList.cs](BrainByteList.cs)、同文件中的 `BrainTargetSet` | 攻击队列与玩家目标集合 |
| [Visuals](Visuals/)、[Sounds](Sounds/)、[Assets](Assets/) | 专属演出、音效和纹理 |

## 状态、资源与修改约束

Behavior 不保存实体实例字段。主脑状态使用 NPC.ai 和 AnomalyAI32/64，攻击队列及目标集合打包进快照；触须、动画帧与残影为客户端数据。飞眼怪阶段由主脑 Phase 驱动，局部计时归自身 AI。Boss 专属容器、血条与网络接入留在本目录。

粒子、Bézier、Verlet 与寻敌等通用能力使用 Transoceanic；依赖变种系统的震屏与兼容调用使用 Anomalies 公共支持。护盾由 [BrainofCthulhuEffects.cs](BrainofCthulhuEffects.cs) 提供，通用预警和模糊由 AnomalyCommonEffects 提供，统一交给 AnomalyEffects 加载。护盾使用 Vein2、Voronoi 程序噪声，采样亮度补偿保留在 shader 内。

BrainBloodShot 和 CreeperBloodShot 自身实现血弹 AI 与绘制。真假脑材质选择同时考虑资源包和灾厄换皮配置；冲刺幻象按主脑实际材质取帧。修改时核对真假脑命中、眩晕与阶段重叠、致死演出和退场清理。

## 修改与验证

执行 [Anomalies 构建检查](../../README.md)和 [Boss 隔离检查](../README.md)。独立资料包提供：

```powershell
dotnet run --project Validation/Tests/Anomalies/Bosses/BrainofCthulhu.State/BrainofCthulhu.State.csproj
python Validation/Tests/Anomalies/Bosses/BrainofCthulhu.State/verify_assets.py
```

检查覆盖队列/目标集合、跨数组边界、克隆、快照及资源引用。游戏内需复核飞眼怪死亡和再生、触须断裂、眩晕中转阶段、真假脑同时命中、超时惩罚、死亡、远离/全员死亡退场、猩红外召唤、中途加入与无灾厄环境。
