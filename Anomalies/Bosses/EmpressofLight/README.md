# 光之女皇

本目录包含夜间和 Aroma 两种光之女皇行为分支，当前属于未完成 Boss 内容。

## 启用条件与依赖

属于始终编译的模组主内容，两分支都要求基础异象条件及 `AnomalyBossChangeToggler.EmpressofLight`。该属性随 `EnableUnfinishedBossChange`，默认 Debug 开启、Release 关闭。

夜间分支额外要求 `!Aroma && !Main.dayTime`；Aroma 分支要求 `Aroma`，优先级为 10。具体条件以各自 `ShouldProcess` 为准，不应把本目录描述为全部时段共用的一套状态机。

## 功能与代码导航

| 文件 | 职责 |
| --- | --- |
| [Night/EmpressofLight.Night.cs](Night/EmpressofLight.Night.cs) | 夜间 Behavior 身份、条件及灾厄 AI 覆盖策略 |
| [Night/EmpressofLight.NightAI.cs](Night/EmpressofLight.NightAI.cs) | 夜间攻击与阶段逻辑 |
| [EmpressOfLight.Aroma.cs](EmpressOfLight.Aroma.cs) | Aroma 攻击、伤害参数、阶段与退场 |

攻击使用光之女皇的彩虹、太阳舞和长矛等实体。Aroma 分支在自身 AI 中设置大师/死亡参数，不使用灾厄 Boss Rush 状态选择攻击参数。

## 状态与修改约束

当前 AI 使用原版 `npc.ai` 分支与计时，修改前逐项核对其含义，不能按其他 Boss 的 Phase/Behavior 字节布局解释。保持服务端生成条件、攻击间隔、白天狂暴及目标检查。两分支都明确禁止灾厄的原版 AI 覆盖入口。

## 修改与验证

执行 [Anomalies 构建检查](../../README.md)和 [Boss 隔离检查](../README.md)。游戏内分别检查夜间普通异象、Ultra、Aroma、昼夜切换、目标死亡、多人中途加入以及有无灾厄的行为。默认 Release 关闭与源码中已有攻击实现是不同状态，不代表该战斗已经完成验收。
