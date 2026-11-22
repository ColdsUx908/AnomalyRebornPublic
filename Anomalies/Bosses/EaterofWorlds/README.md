# 世界吞噬怪

## 启用与战斗流程

世吞属于主内容，需要异象世界与 `AnomalyBossChangeToggler.EaterofWorlds`。该独立开关默认 Debug 开启、Release 关闭。各链共享主头生命，基础为 9000（启用灾厄时为 12000），沿用现有多人缩放。普通异象有 60 节，Ultra 有 90 节，节数含头尾。

| 共享生命 | 形态 | 行为 |
| --- | --- | --- |
| 100%～70% | 一条完整长虫 | 追猎上挑、锁线贯穿、追猎上挑、大弧折返循环 |
| 70%～35% | 两条等长虫 | 轮换领攻，两个头瞄准同一点，按固定节拍先后贯穿并分担折返 |
| 35% 以下 | 三条等长虫 | 两头轮流贯穿，第三头从侧翼接下一轮；普通异象在本阶段结束 |
| Ultra 10% | 一次性转场 | 旧虫链进入裂隙，紫色腐化冲击波出现，重组为 60 节长虫和十条 3 节短虫。共享生命在 120 帧内对数缓出恢复到最大生命的 50% |
| Ultra 50%～25% | 长虫开口围猎＋十条短虫横列 | 长虫沿固定圆心盘绕。短虫在缺口两肩排直、预警并贯穿，随后退回可攻击的位置 |
| Ultra 25% 以下 | 原形态，下一列改为接力 | 每组五条，第二拨比第一拨晚 46 帧，缺口错开 52 像素，冲刺方向相差 0.24 弧度 |

第一次越过普通分裂阈值时会撤离、打开入口并为出口蓄力 180 帧。方向在蓄力开始时确定。重组复用原体节；出口附近的透明体节没有接触伤害。出场后等身体展开，再开始追猎。一次伤害跨过两条普通阈值时可以直接进入三虫。Ultra 10% 优先于普通分裂，致死伤害及从体尾传入的伤害受同一主头生命下限约束。转场中各节免伤、没有接触伤害。真正击杀时由主头统一清理与结算。

追猎沿用从 CalamityMod `NPCs/VanillaNPCAIOverrides/Bosses/EaterOfWorldsAI.cs` 移植的穿地加速与转向，使用本项目共享生命比例、难度和特殊种子修正。移植代码版权归 Azafure, LLC。[灾厄许可](../../Licenses/Calamity-LICENSE.txt) · [裂隙资源来源](Portal/README.md)

## 预警、圆弧与网络

主头在服务器选定目标、推进阶段、生成弹幕并保存路线。预警系统在独立的世界绘制入口画出屏外来袭的紫色细线，线束交于瞄准点，即将出手的一束更亮。常规贯穿锁线 46 帧，通常以 26 像素/帧冲刺 52 帧。极远的锁点按同一时长延长冲程，确保穿过锁点至少 320 像素。下一头按固定时钟发射。预警和发射共用起点、方向与冲程。预警使用普通 LDR SpriteBatch，在关闭着色器时同样可见。

Ultra 长链沿实际体节弧长展开。半径取 700 像素与「弧长 / 200° 对应的弧度」中的较大值，切向速度为 12 像素/帧。以普通或特殊种子体节间距计算均保留开口。短虫各自按锁定方向排直头、体、尾，准备和收招时可以输出；仅在本组冲出的时段具有接触伤害。目标由开口跑出时完成当前波，圈内仍有活玩家则优先换目标，全员远离时在波间预告并移动圆心。

- 原版 `ai[0]` 打包主头、局部头、前驱和后继，`ai[1]` 是不可变全局序号。`ai[2]` 的 Byte0/Byte1 保存阶段与行为，`ai[3]` 为行为局部步骤。
- 额外 32 位槽 0 保存分裂与轨迹初始化标志，1 为出口角度，2、3、5 分别为局部圆弧半径、角速度和头角，4 为攻击轮次。圆弧头角每 60 帧纠正一次。
- 额外 64 位槽 0 为出生烟团或裂隙中心，1 为固定攻击中心，2 为局部圆心，3、4 分别为每个局部头锁定的方向、发射点。`Timer1` 是步骤时钟，转场的 `Timer2` 保存回血起值。客户端按同一份稀疏快照预测，不需要前几轮的路线历史。
- [主入口](EaterofWorldsHead.AI.cs)、[状态与追踪运动](EaterofWorldsHead.State.cs)、[一阶段](EaterofWorldsHead.Phase1AI.cs)、[裂隙分裂](EaterofWorldsHead.Portals.cs)、[Ultra 转场](EaterofWorldsHead.PhaseChangeAI.cs)、[二阶段](EaterofWorldsHead.Phase2AI.cs)、[预警绘制](EaterofWorldsHead.Drawing.cs)。
- [拓扑](EaterofWorldsTopology.cs)和[圆弧运动](EaterofWorldsHead.CircularMovement.cs)可单独使用。`TransformToWorms(amount)` 等分或合并，`TransformToUltraWorms()` 返回长头在前的 11 个头。`StartCircularMovement(center, radius, headAngle, angularSpeed)` 立即重排局部链，需在接触保护内启动；`StopCircularMovement()` 恢复基础跟随。

## 验证入口

独立验证资料包中的 `EaterofWorlds.Topology`、`EaterofWorlds.CircularMovement`、`EaterofWorlds.Portals` 分别检查拓扑、圆弧和裂隙快照。`Validation/Tests/Anomalies/Bosses/EaterofWorlds.Combat/Verify.ps1` 打包并在独立 tModLoader 世界驱动实际 NPC 更新，检查普通与 Ultra 的攻击轮换、回血、分组、重选圆心与击杀。加 `-Render` 可将专服记录的锁线帧通过实际绘制入口回放为 PNG。联机网络回归入口为 `Validation/Tests/Anomalies/Integration/NetworkSync`，Boss 模块排除编译入口为 `Validation/Tests/Anomalies/Bosses/BossIsolation/Verify.ps1`。
