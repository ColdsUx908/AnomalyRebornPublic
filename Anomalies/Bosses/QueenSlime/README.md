# 史莱姆皇后

地面阶段使用跳跃、下砸与传送，空中阶段使用凝胶攻击和下砸，并有宝石、小史莱姆及 Ultra 延伸阶段。

## 启用条件与依赖

属于始终编译的模组主内容，运行时要求异象模式及 `AnomalyBossChangeToggler.QueenSlime`。逐项开关随 EnableUnfinishedBossChange：Debug 开启、Release 关闭。开关定义和公共边界见 [Boss 索引](../README.md)。本项目实现战斗行为，可选灾厄的接入以对应兼容回调为准。

## 代码导航

- [QueenSlime.cs](QueenSlime.cs)：阶段、参数、同步属性和绘制。
- [QueenSlime.AI.cs](QueenSlime.AI.cs)：地面、空中及转阶段逻辑。
- [JewelHandler.cs](JewelHandler.cs)、[JewelNPC.cs](JewelNPC.cs)、[AmethystJewel.cs](AmethystJewel.cs)：专属宝石。
- [QueenSlimeMinion.cs](QueenSlimeMinion.cs)、[QueenSlimeGel.cs](QueenSlimeGel.cs)、[QueenSlimeSmash.cs](QueenSlimeSmash.cs)、[QueenSlimeSpike.cs](QueenSlimeSpike.cs)：小怪和攻击实体。

## 状态、资源与修改约束

宝石实现只归本 Boss，不引用史莱姆王模块。复核地空切换、下砸接触伤害、宝石/小怪清理、Ultra 转阶段及特殊种子参数。

状态布局和变化标记以主文件语义属性为准。服务器负责权威切换和生成，客户端按同步状态表现；不要将仅绘制的缓存加入网络快照。专属资源及实体留在本目录，公共材质与效果通过统一资源提供者访问。

## 修改与验证

执行 [Anomalies 构建检查](../../README.md)和 [Boss 隔离检查](../README.md)。游戏内覆盖普通异象、Ultra、适用的特殊种子、完整阶段切换、死亡/退场及多人中途加入；检查无灾厄时的行为和资源加载。构建及静态检查不代表完成游戏内验收。
