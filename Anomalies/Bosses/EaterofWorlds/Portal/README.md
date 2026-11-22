# 世界吞噬怪传送视效

本目录保存裂隙纹理和来源说明，行为与加载入口位于[世界吞噬怪目录](../README.md)，属于始终编译的模组主内容。

## 来源与许可

改编代码和部分资源来自 CalamityMod 的 DoGTeleportRift、DoGP1EndPortal、DoGRiftCrack、DoGDistortionMetaball、DoGVisualsManager 及相关粒子，版权归 Azafure, LLC。来源仓库为 [CalamityModPublic](https://github.com/CalamityTeam/CalamityModPublic)，许可原文见 [灾厄许可原文](../../../Licenses/Calamity-LICENSE.txt)。下列独立程序生成纹理与导入资源应分别识别，不能将程序生成图描述为灾厄原图副本。

## 代码与资源导航

| 入口 | 职责 |
| --- | --- |
| [EaterPortalAssets.cs](../EaterPortalAssets.cs) | 裂纹、爆裂和星芒资产 |
| [EaterTeleportRift.cs](../EaterTeleportRift.cs)、[EaterEntryPortal.cs](../EaterEntryPortal.cs) | 出口和入口生命周期、绘制 |
| [EaterDistortionSystem.cs](../EaterDistortionSystem.cs) | 扭曲绘制 |
| [EaterofWorldsEffects.cs](../EaterofWorldsEffects.cs) | 专属着色器提供者 |
| [EaterRiftBloom.cs](../EaterRiftBloom.cs)、[EaterRiftLight.cs](../EaterRiftLight.cs) | 裂隙专属粒子 |

火花使用框架 PointingParticle 与原版 ThePerfectGlow，扩散脉冲使用 TexturePulseParticle。公共 BloomRing 位于 Anomalies 的公共 Extra 纹理目录；BloomCircle、Light、BasicCircle 及纯色纹理由框架提供。各 Boss 通过公共资源复用，不互相引用专属资源。

## 程序资源与绘制

| 资源 | 当前格式与用途 | 本地生成入口 |
| --- | --- | --- |
| CrackedGlass_Glowing.png | 960×640 灰度裂纹，三层旋转和 shader 显露 | `Tools/TextureGenerator/CrackedGlass/` |
| PlasmaExplosion.png、ShineExplosion1.png、ShineExplosion2.png | 2048×2048，不透明灰度 RGB；爆发主体与尖峰 | `Tools/TextureGenerator/PortalExplosions/` |
| RiftStar.png | 256×2048，白色 RGB 与透明度亮度；两束垂直叠加的蓄力星芒 | `Tools/TextureGenerator/RiftStar/Generate.ps1` |

公共噪声由 `AnomalyTextures.Noise` 加载，生成配方在本地 `Tools/NoiseGenerator/`。工具随独立资料提供，使用前确认存在；复现以当前源码、配方和固定种子为准。

RiftStar 按 64×64 逻辑尺寸归一化，独立使用 LinearClamp。裂隙 shader 由统一加载器管理，采用 Main / ps_3_0 / Technique1 / Pass0；屏幕 UV 效果按 16:9 基准修正，精灵局部坐标不重复修正。

## 修改与验证

修改纹理后核对独立重生成、部署哈希、尺寸、通道、透明度及运行时引用；修改噪声需显式审核独立资料包的基线并运行全量噪声验证。修改 shader 需检查 EffectCompiler 退出码和产物。

战斗同步与生命周期验证见[父级说明](../README.md)。离线生成、预览和资源检查不代表游戏内视觉验收，仍需检查蓄力、爆发、淡出、分辨率、着色器回退与专用服务器。
