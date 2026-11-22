# Anomalies（异象）

Anomalies 提供异象难度、原版 Boss 行为修改、游戏物品、特殊武器和视觉系统。本页是当前源码的开发导航；可用内容还受构建配置与内容开关控制。

## 启用条件与依赖

运行时必须安装 Transoceanic；AnomaliesMusic 和 Calamity Mod 为可选联动。源码构建所需的 SDK、tModLoader targets 和灾厄程序集见[仓库构建说明](../README.md)。

模组主内容、两个可选构建模块及接入边界见 [MODULES.md](MODULES.md)。菜单、星空和血条默认保留，测试物品由 `DEBUG` 控制。Boss 与特殊内容还有逐项开关，不能仅凭目录存在判断 Release 可用性。

## 内容导航

| 板块 | 职责 |
| --- | --- |
| [异象模式](Game/World/AnomalyMode/README.md) | 模式开启、难度状态与兼容入口 |
| [Boss](Bosses/README.md) | 九个 Boss 目录、逐项条件及专属战斗 |
| [调整内容](Tweaks/README.md) | 武器修改和玩家基础属性配置 |
| [普通游戏内容](GameContents/README.md) | 钱币袋、宠物、饮品、音乐盒与 Buff |
| [特殊内容](SpecialContents/README.md) | 纯白、纯黑、惠惠法杖、落日遗逢及资源准备目录 |
| [Core](Core/) | 配置、命令、加载及模组联动 |
| [Game](Game/) | 战斗、实体、掉落、网络和世界状态 |
| [Behaviors](Behaviors/) | 跨玩法的实体与玩家行为接入 |
| [全局实例](GlobalInstances/README.md) | 统一 GlobalType 和 ModPlayer 数据容器 |
| [UI](UI/) | 菜单、血条、按键和物品提示 |
| [Graphics](Graphics/README.md) | 星空、星爆、粒子、着色器绑定及渲染调度 |
| [着色器](Assets/Effects/README.md) | 资源归属、提供者、编译与生命周期 |

## 开发约束

先阅读[设计规范](../DESIGN_GUIDELINES.md)及目标目录文档。README 描述当前功能和实现入口，局部设计规范描述必须保持的约束；文档不保存开发轮次或改动日志。复杂机制的专题说明与所属内容放在一起，跨项目的许可原文、授权记录和使用位置见[许可索引](Licenses/INDEX.tsv)。

`BaseContents/` 提供共享抽象内容类型；项目内工具和数据类型随战斗、网络、绘制等职责归档。已有 C# 命名空间和内容标识沿用迁移前的名称。无 Anomalies 依赖的通用能力归 Transoceanic。`Localization/` 保存中英文本地化，`Testing/` 保存 DEBUG 测试物品。

## 修改与验证

以下命令从仓库根目录执行：

```powershell
dotnet build Anomalies/Anomalies.csproj -p:BuildMod=false -m:1
```

当前 csproj 的 PreBuild 调用部署的 EffectCompiler。应检查编译器退出码与日志；`BuildMod=false` 不进行模组打包，进入游戏前还需正常打包。

测试源码和脚本由独立验证资料包提供，不随 Git 分发。通用入口为 `Validation/Tests/Anomalies/Verify.ps1`；模块组合入口为 `Validation/Tests/Anomalies/Integration/Modules/Verify.ps1`。文档列出命令不表示本版本已经通过验证。

纯文档修改检查内容、相对链接与差异。代码修改按范围验证；构建、静态检查和离线模拟不能代替游戏内加载、完整战斗、联机及视觉验收。
