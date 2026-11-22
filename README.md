# AnomalyReborn

[![Ask DeepWiki](https://deepwiki.com/badge.svg)](https://deepwiki.com/ColdsUx908/AnomalyReborn)

AnomalyReborn 是面向 Terraria / tModLoader 的模组开发仓库，包含以战斗体验为核心的 **异象（Anomalies）**、配套音乐模组 **异象音乐（Anomalies Music）**，以及通用开发框架 **大洋彼岸（Transoceanic）**。

## 项目组成

| 项目 | 说明 |
| --- | --- |
| [Anomalies](Anomalies/README.md) | 引入异象模式，重制部分原版 Boss 的攻击与阶段转换，并提供新物品、部分武器调整、可配置的玩家基础属性提升及自定义视觉效果。 |
| [AnomaliesMusic](AnomaliesMusic/) | 提供主题曲与环境音乐，支持海洋、腐化之地和猩红之地的可配置背景音乐替换；仅在客户端运行，环境音乐可独立使用。 |
| [Transoceanic](Transoceanic/README.md) | 不依赖特定内容模组的通用框架，提供实体行为调度、可选数据同步、几何与数学工具、粒子、绘制和资源加载等基础设施。 |
| [TransoceanicCodeAssist](TransoceanicCodeAssist/) | 为框架及消费项目提供 Roslyn 分析器与源生成器。 |

## 游玩说明

- **Anomalies 必须搭配 Transoceanic 使用**，推荐同时安装 AnomaliesMusic；灾厄模组（Calamity Mod）为可选联动。
- 在大师难度下，使用 **裂天金冕** 开启或关闭异象模式。
- 在传奇难度下，可进一步开启 **异象超凡模式**，挑战更高难度的战斗。
- 音乐、玩家基础属性与视觉效果等选项，可在对应模组配置中调整。

仓库源码与工坊发布版本可能存在差异，请以实际安装版本的内容为准。

## 开发与构建

### 环境要求

- 配置好 tModLoader 模组开发环境。模组项目通过 `../../tModLoader.targets` 导入游戏依赖，克隆仓库后需确保相对目录布局正确。
- 主模组与框架面向 **.NET 8**，使用 C# `preview`。当前源码包含扩展成员与 `field` 访问器，需要支持这些语法的 SDK；已有验证环境使用 **.NET SDK 10**。
- `TransoceanicCodeAssist` 面向 `netstandard2.0`。
- Anomalies 编译时引用相邻目录的 `CalamityMod/CalamityMod.dll`。灾厄在游戏中是可选联动，但当前源码构建仍需该程序集。
- 当前 Anomalies 项目会在预构建阶段调用 `Anomalies/PreBuild/EffectCompiler/EffectCompiler.exe`。构建前需具备该工具及其运行依赖；使用本地工具源码时参阅 `Tools/EffectCompiler/README.md`。

公共解决方案为 [AnomalyRebornPublic.slnx](AnomalyRebornPublic.slnx)，包含本仓库的四个项目；完整解决方案还涉及相邻仓库的源码项目。

从仓库根目录执行 C# 构建检查：

```powershell
dotnet build Anomalies/Anomalies.csproj -p:BuildMod=false -m:1
```

`BuildMod=false` 跳过模组打包，不代表游戏会加载本次构建。进入游戏调试或验收前，需完成正常构建与打包：

```powershell
dotnet build Anomalies/Anomalies.csproj -m:1
```

当前打包日志中的 `WARN: Image loading failed: unknown image type` 已通过对照验证，原因是 tModLoader 无法正确处理模组小图标 `icon_small.png`：读取图标进行比较后，没有在图片转换前复位读取流。保留小图标时出现警告，移除或仅重命名相同图片时不出现，三种情况均打包成功。这条已确认的警告不代表物品或图格材质损坏；其他图片错误仍需单独排查，更新 tML 后也应重新核实。

### 功能模块

Anomalies 提供两个默认开启的可选构建模块。异象难度、Boss 和普通游戏内容属于始终编译和加载的模组主内容：

| 模块 | MSBuild 属性 |
| --- | --- |
| `TWEAK` | `AnomaliesEnableTweak` |
| `SPECIAL_CONTENTS` | `AnomaliesEnableSpecialContents` |

例如，关闭调整模块并重新打包：

```powershell
dotnet build Anomalies/Anomalies.csproj -p:AnomaliesEnableTweak=false -m:1
```

模块开关不是游戏内配置，修改后必须重新构建与打包。菜单、星空和血条默认保留。完整边界与使用方式见 [Anomalies 构建模块](Anomalies/MODULES.md)。

## 文档与参与开发

- [设计规范](DESIGN_GUIDELINES.md)：命名、实体架构、Boss 状态机、模块边界、同步、着色器与验证约定。
- [代理工作指南](AGENTS.md)：仓库定位、开发约束及验证入口。
- [Transoceanic 开发文档](Transoceanic/README.md)：框架接入、API、同步、绘制和版本迁移导航。
- [着色器资源说明](Anomalies/Assets/Effects/README.md)。
- [DeepWiki](https://deepwiki.com/ColdsUx908/AnomalyReborn)：通过仓库知识库浏览或提问。

开始开发前，请阅读设计规范，并按要求在本地根目录创建被 Git 忽略的 `.local-developer`，如实填写开发者名。修改具体模块前，继续阅读对应目录的 README 与设计文档。

验证资料（`Validation/Tests/` 与 `Validation/docs/`）通过与源码构建对应的独立验证资料包提供，不随 Git 仓库分发；`Tools/` 下的离线工具也需确认本地是否具备。取得资料包后，按改动范围运行相应验证。
