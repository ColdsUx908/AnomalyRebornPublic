# 蜂王

围绕常规/直接冲锋、蜂刺和多种蜂群组织攻击，Ultra 包含低血量延伸阶段。蜂巢单元、友方蜜蜂及专属蜂刺在本目录实现。

## 启用条件与依赖

属于始终编译的模组主内容，运行时要求异象模式及 `AnomalyBossChangeToggler.QueenBee`。逐项开关默认 true。开关定义和公共边界见 [Boss 索引](../README.md)。本项目实现战斗行为，可选灾厄的接入以对应兼容回调为准。

## 代码导航

- [QueenBee.cs](QueenBee.cs)：阶段、参数、状态、命中和绘制。
- [QueenBee.AI.cs](QueenBee.AI.cs)：冲锋、蜂刺、蜂群及阶段推进。
- [QueenBeeHandler.cs](QueenBeeHandler.cs)：蜂巢碎片生成、蜂蜜碰撞和模块接入。
- [CombCell.cs](CombCell.cs)、[FriendlyBee.cs](FriendlyBee.cs)、[HugeStinger.cs](HugeStinger.cs)：蜂巢、友方蜜蜂和巨型蜂刺。

## 状态、资源与修改约束

核对蜂巢主人、蜂群生成、距离狂暴与阶段清理。HoneyedWildbloom 属于主内容，蜂后掉落保留世界异象模式条件。

状态布局和变化标记以主文件语义属性为准。服务器负责权威切换和生成，客户端按同步状态表现；不要将仅绘制的缓存加入网络快照。专属资源及实体留在本目录，公共材质与效果通过统一资源提供者访问。

## 修改与验证

执行 [Anomalies 构建检查](../../README.md)和 [Boss 隔离检查](../README.md)。游戏内覆盖普通异象、Ultra、适用的特殊种子、完整阶段切换、死亡/退场及多人中途加入；检查无灾厄时的行为和资源加载。构建及静态检查不代表完成游戏内验收。
