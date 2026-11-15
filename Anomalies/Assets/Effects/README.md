# Anomalies 着色器资源

源码统一存放在本目录，编译产物位于 `../Effects_Compiled/`，缓存位于 `../Effects_Cache/`；三者保持相同相对路径。源码和产物按 Common、BossBars、Boss 名或整体项目名归档，默认扁平存放，按需增加子目录。

- `Common/`：通用发光、遮罩、冲击波、径向模糊、文字等；引用位于 `AnomalyCommonEffects.cs`。`GoldText` 由钱币袋和传说之夜共用，不依赖 Boss 模块。
- `BossBars/`：默认开启的血条；提供者位于 `Visuals/BossBar/AnomalyBossBarEffects.cs`。
- `BrainofCthulhu/`、`EaterofWorlds/`、`Skeletron/`：对应 Boss 专属资源，提供者位于各自 Boss 源码目录。`SkeletronArenaMist` 供普通骷髅王与传说之夜共用。
- `LegendaryNight/`：传说之夜专属资源，提供者位于 `Bosses/Skeletron/LegendaryNight/LegendaryNightEffects.cs`。
- `EventideReunion/`、`MeguminStaff/`：特殊内容资源，提供者随所属源码目录编译，且遵循逐项内容开关。

提供者继承 `AnomalyEffectProvider`，声明相对 `EffectDirectory`，公开静态字段使用 `MiscShaderContainer` 或原生 `Filter`。字段名与资源文件名一致，按字母排序。多级公开静态嵌套类对应多级资源目录。提供者不重复声明加载或卸载方法；特殊内容通过 `IsLoadingEnabled` 控制是否参与原生加载流程。

`AnomalyEffects` 根据字段类型统一请求资产、注册、清理引用及失活 Filter；专用服务器和关闭着色器配置时不请求资产。注册键为 `<模组名>:<归属>/<嵌套目录>/<字段名>`，例如 `Anomalies:Common/RadialBlur`。默认 Pass 为 `Pass0`，特殊入口可使用 `[ShaderPass("PassName")]`。绘制代码仍须判空并保留已有回退，每次使用显式设置参数及 Pass。

专属容器不放入公共注册清单，不在公共层添加指向 Boss 类型的引用。移除 Boss 目录或关闭模块后，其提供者不再编译，集中资源树中保留的资源不会因此自动加载。

修改后使用部署的 EffectCompiler 编译资源，再执行 C# 构建；目录迁移需检查旧 XNB 残留、Boss 隔离和模块组合。当前项目 PreBuild 已启用编译器调用，以实际 csproj 为准。构建与离线测试不代表游戏内视觉验收。
