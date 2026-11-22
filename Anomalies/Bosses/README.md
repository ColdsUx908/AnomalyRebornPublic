# Boss 行为

本目录保存异象 Boss 行为、专属实体、资源和集成代码，统一属于始终编译的模组主内容。

## 启用条件与当前状态

行为还要求世界开启异象模式，并满足各类型的 `ShouldProcess`。逐项属性集中于 [AnomalyBossChangeToggler.cs](AnomalyBossChangeToggler.cs)。`EnableUnfinishedBossChange` 在 Debug 为 true、Release 为 false；逐项开关控制行为启用，不等同于删除该目录或禁止所有专属实体注册。

| 内容 | 默认开关 | 实现概览 |
| --- | --- | --- |
| [史莱姆王](KingSlime/README.md) | true | 跳跃、传送、宝石和 Ultra 延伸阶段 |
| [克苏鲁之眼](EyeofCthulhu/README.md) | true | 冲锋、仆从、旋转和场地阶段 |
| [世界吞噬怪](EaterofWorlds/README.md) | Debug 开启 | 共享生命、追踪移动与传送分合 |
| [克苏鲁之脑](BrainofCthulhu/README.md) | Debug 开启 | 飞眼怪、触须、眩晕、幻象与死亡演出 |
| [蜂王](QueenBee/README.md) | true | 冲锋、蜂刺、蜂群和 Ultra 延伸阶段 |
| [骷髅王](Skeletron/README.md) | true | 头手配合、阶段攻击与最终追击 |
| [传说之夜](Skeletron/LegendaryNight/README.md) | true | 独立召唤、符卡与音乐驱动的二阶段 |
| [史莱姆皇后](QueenSlime/README.md) | Debug 开启 | 地面跳跃、空中凝胶攻击与宝石 |
| [猪龙鱼公爵](DukeFishron/README.md) | Debug 开启 | 冲锋、泡泡、鲨卷风与传送 |
| [光之女皇](EmpressofLight/README.md) | Debug 开启 | 夜间与 Aroma 分支 |

## 代码与依赖边界

每个直属 Boss 文件夹是可独立移除的内容单元。专属掉落、血条、网络及统一容器的专属部分类留在所属目录，通过注册或可选部分类方法连接公共入口。Boss 之间不直接引用类型或专属资源；骷髅王和其子目录传说之夜属于同一直属内容单元。

阶段、行为、局部步骤和时钟通过语义属性访问；布局以各 Boss 主文件或 State 文件为准。服务器负责随机决策、生成及权威切换，客户端按同步状态表现。当前模式允许多人支持，但不构成各场景联机验收的证明。

## 修改与验证

先执行 [Anomalies 构建检查](../README.md)，按内容页运行专项验证。独立资料包中的公共检查从仓库根目录运行：

```powershell
python Validation/Tests/Anomalies/Bosses/BossIsolation/verify_dependencies.py
pwsh -File Validation/Tests/Anomalies/Bosses/BossIsolation/Verify.ps1
```

调整目录或资源归属时必须完整构建并执行隔离验证。游戏内复核普通/Ultra、适用的特殊种子、初始化、转阶段、死亡与退场、多人中途加入及有无可选模组的资源加载。
