# 视觉系统

本目录包含星空、共享星爆、粒子、着色器绑定和渲染调度。菜单、血条与物品提示归 UI；各项效果遵循所属功能的加载条件。

## 代码导航

| 入口 | 职责 |
| --- | --- |
| [MainMenu](../UI/MainMenu/) | 模组菜单与背景 |
| [AnomalyStarVisual.cs](Rendering/AnomalyStarVisual.cs) | 星空视觉 |
| [BossBar](../UI/BossBar/) | 血条样式、阈值指示和绘制入口 |
| [Overlays](Rendering/Overlays/README.md) | 注册式覆盖绘制与物块后绘制 |
| [AnomalyDepthDrawSystem.cs](Rendering/Depth/AnomalyDepthDrawSystem.cs) | 深度绘制调度 |
| [AnomalyDepthDrawSystem.CombatText.cs](Rendering/Depth/AnomalyDepthDrawSystem.CombatText.cs) | 深度战斗文字 |
| [AnomalyNPCParticleSet.cs](Particles/AnomalyNPCParticleSet.cs) | NPC 粒子集合支持 |
| [AnomalyDrawUtils.cs](Drawing/AnomalyDrawUtils.cs) | 项目绘制工具 |

## 资源与生命周期

着色器按[资源说明](../Assets/Effects/README.md)使用统一提供者。Boss 专属血条接入留在 Boss 目录；公共血条不直接依赖 Boss 类型。客户端缓存归实体数据容器或具体实体，不另建仅保存数据的 GlobalType。

绘制需兼容专用服务器、资源未加载、关闭着色器与不同分辨率；共享 Effect 每次设置参数，临时改变的 SpriteBatch 和 GraphicsDevice 状态必须恢复。自有纹理需释放，tModLoader 钩子由框架管理卸载。

## 修改与验证

执行 [Anomalies 构建检查](../README.md)，修改 shader 还需检查 EffectCompiler 的结果。独立资料包 `Validation/Tests/Anomalies/Rendering/` 包含绘制专项检查，`Effects/` 包含效果检查。游戏内覆盖 16:9、4:3、16:10、超宽屏、竖屏、缩放、明暗背景及着色器回退；离线渲染不能代替动态验收。
