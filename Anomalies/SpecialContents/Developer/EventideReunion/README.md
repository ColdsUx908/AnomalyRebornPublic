# 落日遗逢（EventideReunion）

落日遗逢是具有普通攻击、相位选择和轮回共鸣的开发者近战武器。当前可通过右键轮盘释放太阳、轮回、生命、死亡、神圣与恒星六相。

## 启用条件与依赖

归属 `SPECIAL_CONTENTS`，单项开关为 `AnomalySpecialContentToggler.EventideReunion`。默认随 `EnableUnfinishedContents`：Debug 开启、Release 关闭。相关物品、弹幕、Buff、Behavior、覆盖绘制与效果提供者遵循该条件。

## 功能概览

普通攻击由 Holdout 管理，右键轮盘松开后由持有者释放选中相位；使用限制与冷却阻止重叠动作。太阳相包含挥斩和突进，轮回相包含旋身、横抽、后撤刺击和延迟伤害标记，死亡相包含突进、回斩、镰刀波、斩杀与治疗灵魂。生命相包含生命树、花瓣洪流、临时生命上限、团队治疗、生命果和光合易伤。神圣相在竖矛后朝光标无敌突刺，从终点投矛引下圣裁光柱；白光熄柱时净化附近玩家，长矛随后化光归体。恒星相持矛朝光标蓄力，蓄满后从矛尖投出光球，命中敌人、新按左键或飞行满半秒时停下，随后星核坍缩并爆发，扩张的波前对扫到的每个目标造成一次伤害，三束星光归体后恢复攻击。

共鸣记录成功释放的相位，提供近战伤害与冷却修正；新相位刷新持续时间，重复相位消耗印记。取消 Buff、死亡或到期会清空共鸣，但不会重置已开始的技能冷却。详细规则见 [MECHANICS.md](MECHANICS.md)。

## 代码导航

| 入口 | 职责 |
| --- | --- |
| [EventideReunion.cs](EventideReunion.cs) | 物品、使用限制与技能分派 |
| [EventideReunionSelection.cs](EventideReunionSelection.cs)、[EventideReunionAspect.cs](EventideReunionAspect.cs) | 轮盘、输入与六相枚举 |
| [EventideReunionDamageConstants.cs](EventideReunionDamageConstants.cs) | 伤害倍率 |
| [NormalAttack](NormalAttack/) | 普通攻击、下劈、粒子和音效 |
| [Sun](Sun/)、[Samsara](Samsara/)、[Life](Life/README.md)、[Death](Death/)、[Sacredness](Sacredness/)、[Star](Star/) | 各相技能、几何、伤害代理、表现与玩家行为 |
| [Resonance](Resonance/) | 共鸣规则、Buff、同步和符文表现 |
| [Rune](Rune/) | 六相选择符文及资产映射 |
| [EventideReunionMarkDrawing.cs](EventideReunionMarkDrawing.cs) | 轮回/死亡标记组合绘制 |
| [EventideReunionAfterimageSystem.cs](EventideReunionAfterimageSystem.cs) | 独立世界阶段的人物残影和施法齿轮 |
| [EventideReunionEffects.cs](EventideReunionEffects.cs) | 专属着色器提供者 |

## 状态、资源与修改约束

玩家和 NPC 数据归统一容器，技能实体保存各自状态。持有者决定输入，服务器负责伤害代理、标记和结算；跨端弹幕引用使用 owner + identity。详细边界见[设计规范](DESIGN_GUIDELINES.md)，修改前必须阅读。

所有子目录使用统一命名空间，受路径差异影响的实体显式指定 Texture。着色器位于 `Assets/Effects/EventideReunion/`，共享武器/刀光、符文和各技能资源按用途分别加载。

## 修改与验证

执行 [Anomalies 构建检查](../../../README.md)。独立资料包的 `Validation/Tests/Anomalies/EventideReunion/` 包含 Sun、Death、Samsara、Life、Resonance、Rendering、SamsaraVisuals 和 ShaderRendering 检查；后者入口为 `ShaderRendering/Verify.ps1`。模块接入变更还需模块组合验证。

游戏内复核六相完整动作、不同重力、玩家死亡、目标失效、地形阻挡、多人归属与中途加入、标记共存、明暗背景、分辨率及关闭着色器。文档中的机制说明不构成游戏内验收结论。
