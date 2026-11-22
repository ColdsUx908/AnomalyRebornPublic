# 克苏鲁之眼

包含悬停、普通/快速/横向冲锋、旋转与仆从攻击；阶段切换由血量和难度参数决定，后段包含竞技场与场地之眼。

## 启用条件与依赖

属于始终编译的模组主内容，运行时要求异象模式及 `AnomalyBossChangeToggler.EyeofCthulhu`。逐项开关默认 true。开关定义和公共边界见 [Boss 索引](../README.md)。本项目实现战斗行为，可选灾厄的接入以对应兼容回调为准。

## 代码导航

- [EyeofCthulhu.cs](EyeofCthulhu.cs)：阶段与行为、参数、状态和绘制回调。
- [EyeofCthulhu.AI.cs](EyeofCthulhu.AI.cs)：阶段分派及具体攻击。
- [EyeofCthulhuArena.cs](EyeofCthulhuArena.cs) 及其部分类：场地、场地之眼和属性修改。
- [EyeofCthulhuHandler.cs](EyeofCthulhuHandler.cs)：模块接入；同目录血弹、冲击波和仆从类型负责各自行为。

## 状态、资源与修改约束

主体和场地应一起检查生命周期、目标失效、转阶段锁血及最终收场。保留客户端表现状态与权威状态的边界，避免场地主人失效后继续造成伤害。

状态布局和变化标记以主文件语义属性为准。服务器负责权威切换和生成，客户端按同步状态表现；不要将仅绘制的缓存加入网络快照。专属资源及实体留在本目录，公共材质与效果通过统一资源提供者访问。

## 修改与验证

竞技场视野暗幕由公共 EnhancedDarknessSystem 合成，在 All 遮罩队列中使用 ArenaMask 优先级，晚于武器星爆等 Attack 特效绘制。竞技场继续提交原有光源及强度，不另建专属暗幕或改变战斗状态。

执行 [Anomalies 构建检查](../../README.md)和 [Boss 隔离检查](../README.md)。游戏内覆盖普通异象、Ultra、适用的特殊种子、完整阶段切换、死亡/退场及多人中途加入；检查无灾厄时的行为和资源加载。构建及静态检查不代表完成游戏内验收。
