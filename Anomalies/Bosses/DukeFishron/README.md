# 猪龙鱼公爵

前两阶段组合冲锋、泡泡和鲨卷风，第三阶段使用传送与冲锋，并按低血量细分。普通异象与 Ultra 使用不同阶段阈值和攻击间隔。

## 启用条件与依赖

属于始终编译的模组主内容，运行时要求异象模式及 `AnomalyBossChangeToggler.DukeFishron`。逐项开关随 EnableUnfinishedBossChange：Debug 开启、Release 关闭。开关定义和公共边界见 [Boss 索引](../README.md)。本项目实现战斗行为，可选灾厄的接入以对应兼容回调为准。

## 代码导航

- [DukeFishron.cs](DukeFishron.cs)：阶段与行为枚举、参数、状态、动画和绘制。
- [DukeFishron.AI.cs](DukeFishron.AI.cs)：目标处理、阶段分派与攻击。
- [DetonatingBubble.cs](DetonatingBubble.cs)：泡泡行为。

## 状态、资源与修改约束

Phase/Behavior 使用 AI_Union_1 的 Byte0/Byte1；局部步骤和空闲攻击阶段使用 AI_Union_3 的两个 short，不能套用其他 Boss 的槽位。检查海洋边界、第三阶段传送、泡泡生命周期及目标失效。

状态布局和变化标记以主文件语义属性为准。服务器负责权威切换和生成，客户端按同步状态表现；不要将仅绘制的缓存加入网络快照。专属资源及实体留在本目录，公共材质与效果通过统一资源提供者访问。

## 修改与验证

执行 [Anomalies 构建检查](../../README.md)和 [Boss 隔离检查](../README.md)。游戏内覆盖普通异象、Ultra、适用的特殊种子、完整阶段切换、死亡/退场及多人中途加入；检查无灾厄时的行为和资源加载。构建及静态检查不代表完成游戏内验收。
