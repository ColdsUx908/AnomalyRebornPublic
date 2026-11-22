# 传说之夜

传说之夜是骷髅王目录内的独立战斗实现，包含召唤仪式、一阶段常态与符卡、转场以及音乐驱动的二阶段。与[普通骷髅王](../README.md)共享部分运动、发射和场地支持。

## 启用条件与依赖

属于始终编译的模组主内容，要求基础异象条件及 `AnomalyBossChangeToggler.LegendaryNight`（默认 true）。主体以 `CustomAIType == 1` 识别；召唤初始化期间另有仪式身份兜底。普通头部与传说之夜分开判定，不共用一套攻击状态机。

全部子目录使用 `Anomalies.Bosses.Skeletron.LegendaryNight` 命名空间。专属资源跟随功能目录，实体显式指定实际 Texture；着色器由 LegendaryNightEffects 声明并统一加载。

## 功能概览

一阶段由主体选择常态、自转和符卡，头手配合完成攻击。转场处理清场、回血、召手与祝福；二阶段按绝对音乐时钟推进七套机制，包含场地、拍击、落星、钱币审判、星爆、恒星系统与终幕。

[MECHANICS.md](MECHANICS.md) 说明当前攻击、伤害边界和视觉；[ARCHITECTURE.md](ARCHITECTURE.md) 说明代码入口、时钟、槽位与必须保持的同步顺序。

## 代码导航

| 目录 | 职责 |
| --- | --- |
| [Head](Head/)、[Hand](Hand/) | 主体/手部状态机、动作与绘制 |
| [Head/Phase2](Head/Phase2/) | 七套机制及音乐时间表 |
| [Arena](Arena/) | 场地生命周期、碰撞、法阵、公转与元素圆 |
| [Attacks](Attacks/) | Bone、Slime、Stars、Coins、Starburst、Stellar、Finale 专属攻击 |
| [Scene](Scene/) | 天空、星点、云层、照明、世界时间与音乐 |
| [Rendering](Rendering/) | 跨实体绘制顺序、遮罩、预警与共享绘制 |
| [Players](Players/)、[Buffs](Buffs/) | 祝福行为、玩家光晕、减益和提示 |
| [Summoning](Summoning/) | 召唤仪式、激活音效及祭坛可选接入；祭坛、精华、召唤物与钱币袋位于 [GameContents/LegendaryNight](../../../GameContents/LegendaryNight/README.md) |
| [Particles](Particles/)、[Combat](Combat/) | 共享粒子与本地玩家伤害结算 |

## 状态与修改约束

主体拥有阶段和音乐时钟，场地提供几何，攻击实体管理各自生命周期；不能让绘制层推进权威战斗状态。实体引用使用 owner + identity，纯视觉缓存和防重复本地命中位图不加入快照。

共享方法变更需同时检查普通骷髅王。参数、状态枚举值、伤害换算和时钟推进顺序均需独立核对，不能仅靠文件结构推断。

## 客户端战斗音乐

一阶段可选用 Calamity Mod Extra Music（内部名 `ClamExtraMusic`）提供的《Curse of the Dungeon》。客户端须启用该模组，并打开其骷髅王音乐开关；否则使用原版 Boss 1。二阶段从本地可选模组 `LegendaryNightExtraMusic` 读取专用曲目，缺少该模组或曲目时使用光之女皇音乐。两项选择相互独立，服务器按原有战斗时钟推进机制。

## 修改与验证

执行 [Anomalies 构建检查](../../../README.md)。独立资料包入口为 `Validation/Tests/Anomalies/LegendaryNight/Verify.ps1`，覆盖源码/资源组织、机制计算、实际 AI 的同步恢复、快照往返、Boss 隔离、模块组合及 Release 构建；运行要求以脚本和资料包为准。

当前异象模式允许多人支持。离线 AI 和序列化检查不代表游戏内联机通过；仍需实际专用服务器加载、中途加入、延迟、死亡/退场、完整战斗、音乐节拍、分辨率及关闭着色器的视觉验收。

圆形星爆的绘制、Bloom、包络、屏幕反馈及共用纹理由 Anomalies 的 Visuals/Starburst 与 Assets/Textures/Starburst 提供，叩星和传说之夜使用相同入口。公共反馈通过 IStarburstImpactSource 查询实体；径向模糊由 AnomalyCommonEffects.StarburstRadialBlur 加载，音效位于 Assets/Sounds/Starburst.ogg。Boss 时钟、伤害、圆环及其光晕校准仍留在本目录。
