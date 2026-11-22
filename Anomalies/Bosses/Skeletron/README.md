# 普通骷髅王

本目录直属源码实现普通异象与 Ultra 骷髅王。[LegendaryNight](LegendaryNight/README.md) 是传说之夜，本索引只说明其与普通实现共用的接口。

## 启用条件与依赖

属于始终编译的模组主内容，要求异象世界、`AnomalyBossChangeToggler.Skeletron` 及非传说之夜的自定义 AI 标志。开关默认 true；传说之夜有自己的开关及身份判定。

## 代码导航

1. [SkeletronHead.cs](SkeletronHead.cs)：行为启用条件、属性、命中、锁血与死亡回调。
2. [SkeletronHead.State.cs](SkeletronHead.State.cs)：阶段/行为枚举、同步槽位、阶段条件与转场重置。
3. [SkeletronHead.Parameters.cs](SkeletronHead.Parameters.cs)：头部攻击参数与兼容参数入口。
4. [SkeletronHead.AI.cs](SkeletronHead.AI.cs)：同步、目标、初始化、保护、阶段分派、狂暴与退场。
5. [SkeletronHead.Phase1AI.cs](SkeletronHead.Phase1AI.cs)、`.Phase2AI.cs`、`.PhaseChangeAI.cs`、`.Phase3AI.cs`、`.FinalAI.cs`：各阶段实现；具体攻击优先作为阶段方法的本地函数。
6. [SkeletronHead.Combat.cs](SkeletronHead.Combat.cs)：跨阶段攻击切换、悬停运动、特殊世界生成及竞技场查询。
7. [SkeletronHead.Drawing.cs](SkeletronHead.Drawing.cs)、`.FinalDrawing.cs`：头部预警、粒子、血条与最终蓄力绘制。

## 功能概览

阶段流程：初始化 → 有手阶段 → 无手阶段及强化阶段。Ultra 在无手且达到血量门槛后进入三阶段过渡，再循环雨幕、巡卫与骨潮。普通异象在前三种战斗阶段、Ultra 在三阶段达到最终血量门槛后进入静默/蓄力，随后进入最终追击。

头部在仍有存活手部时可被命中但不可被追踪，防御固定为 9999（包括旋转期间），血条通过独立免疫标记保持锁定；全部手部清除后解除该标记、恢复可被追踪并恢复对应行为的防御。转阶段本身的无敌仍按阶段逻辑处理。

一阶段悬停期间（普通射击或静默期）清空全部手部后，停止射击并重新静默 90 帧，随后直接进入二阶段常态攻击；旋转期间清空手部仍沿用无手旋转收尾。一阶段常态总时长保持 480 帧，最后 60 帧停止发射弹幕，蓄力计时继续推进。

Aroma 每轮旋转开始时保留暗黑法师召唤（场上少于 6 只时补 1 只），同帧追加地牢敌怪：等概率选择三类装甲骷髅各 1 只（各自随机变种），或随机一类法师、随机一类枪械骷髅及骷髅李小龙各 1 只；巨型诅咒骷髅头和地牢幽魂分别独立以 1/2 概率在头部中心出现；圣骑士以 1/20 概率出现。地面敌怪寻找有实体空间的落地点，未找到则跳过，旋转结束不召唤。上述世纪之花后地牢敌怪（包括自然生成及全部变种）在世界首次击败骷髅王前不掉落物品，暗黑法师不受此限制。

## 共用支持与资源

| 文件 | 职责 |
| --- | --- |
| [SkeletronKinematics.cs](SkeletronKinematics.cs) | 头部悬停/旋转、手部待机/蓄势运动；逐轴制动及限速 |
| [SkeletronCombat.cs](SkeletronCombat.cs) | 头骨齐射与加速扇形发射 |
| [SkeletronCombatVisuals.cs](SkeletronCombatVisuals.cs) | 骨色与最终冲击波爆发粒子 |
| [SkeletronHandler.cs](SkeletronHandler.cs) | 手部死亡 IL 接入 |
| [SkeletronPhase3Patterns.cs](SkeletronPhase3Patterns.cs) | 头部嵌套 `Phase3Patterns` 的参数、时间表与缺口几何，供攻击、弹幕和预警共同读取 |

普通骷髅王和传说之夜直接调用 `SkeletronKinematics` 与 `SkeletronCombat`，不经过头部、手部或 Handler 转发。修改共用方法时必须同时检查传说之夜调用，但不要把其独立攻击逻辑并入普通状态机。

## 实体与绘制

- [SkeletronHand.cs](SkeletronHand.cs) 保持单文件，包含手部状态机、命名回调与预警。
- [Skull.cs](Skull.cs) 提供飞行协议与初始化；[Skull.AI.cs](Skull.AI.cs) 负责运动；[Skull.Drawing.cs](Skull.Drawing.cs) 负责光束、回退绘制与粒子。采用七个 AI32 槽的容量，第六号槽未使用。
- [Crossbone.cs](Crossbone.cs)、[BoneShockwave.cs](BoneShockwave.cs)、[SkeletronSkullWarning.cs](SkeletronSkullWarning.cs) 和 [SkeletronPhase3Guardian.cs](SkeletronPhase3Guardian.cs) 各自包含完整的小型实体实现。
- [SkeletronBoneArena.cs](SkeletronBoneArena.cs) 提供普通与传说之夜共用的十六边形场地、边界碰撞、同步和视觉边界快照。
- [SkeletronPhase3Arena.cs](SkeletronPhase3Arena.cs) 保存普通三阶段场地与客户端预警快照。场地边界/雾幕由 [SkeletronPhase3ArenaOverlay.cs](SkeletronPhase3ArenaOverlay.cs) 绘制，场内预警由 [SkeletronPhase3WarningOverlay.cs](SkeletronPhase3WarningOverlay.cs) 在物块之后调用 [SkeletronPhase3Warnings.cs](SkeletronPhase3Warnings.cs) 绘制。
- [SkeletronArenaDrawing.cs](SkeletronArenaDrawing.cs) 保留圆形和多边形雾幕入口，资源路径保持不变。
- [SkeletronLoot.cs](SkeletronLoot.cs)、[BossHealthBar.Skeletron.cs](BossHealthBar.Skeletron.cs) 是模块内集成入口；传说之夜客户端实例数据由 [AnomalyGlobalNPC](../../GlobalInstances/AnomalyGlobalNPC/AnomalyGlobalNPC.ExtraData.cs) 持有。

场地边界伤害、移动限制和传说之夜的死亡特效抑制标记分别管理。

## 修改约束与验证

同步槽位、枚举值与 ExtraAI 读写顺序不能因文件拆分而改变。头部 `Byte2` 的零值同时表示首轮悬停尚未结束；后续值表示下一次强化旋转，不得拆成未同步的字段。传送保留槽仍按协议清零，普通 AI 不执行传送。

保留同帧 `goto case`、三连冲锋结束时立即开始下一次后撤、先/后递增时钟及扇形末端采样边界。战斗平衡修改应与结构整理分别评估。

验证资料由独立 `Validation/` 资料包提供：普通骷髅王运行时回归入口为 `Validation/Tests/Anomalies/Bosses/Skeletron.Phase3/`；模块依赖与排除构建入口为 `Validation/Tests/Anomalies/Bosses/BossIsolation/`。编译及模拟宿主不能替代实际游戏的战斗、动态资源、视觉和联机验收。
