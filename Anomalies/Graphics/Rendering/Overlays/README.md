# Overlay 加载与绘制

Overlay 提供按绘制阶段注册的覆盖效果，属于[视觉系统](../../README.md)的公共支持。

## 启用条件与代码导航

[CustomOverlay.cs](CustomOverlay.cs) 定义 CustomOverlay、PostTilesOverlay 和管理器。两个基类均继承 `RegisteredContent<T>`，使用 tModLoader 内容注册与生命周期；可重写 `IsLoadingEnabled(Mod)`，返回 false 的类型不进入更新与绘制集合。

管理器始终编译和加载。各具体效果还需遵循自身所属模块和单项开关，例如落日遗逢的 SunFlash、DeathVignette 使用 EventideReunion 开关。[EnhancedDarknessSystem.cs](EnhancedDarknessSystem.cs) 提供公共黑暗效果支持。

## 调度与生命周期

管理器在 PostSetupContent 收集受框架支持模组的实际注册实例，更新和绘制使用同一实例。CustomOverlay 按 RenderLayers 绘制，PostTilesOverlay 在物块后绘制；不反射重建实例，不绕过构造函数，也不重写封闭的 Register/SetupContent。

卸载时管理器清空集合，自有资源按生命周期释放，tModLoader 管理钩子卸载。具体效果负责专用服务器、资源为空、共享 shader 参数及绘制状态恢复。

## 实体提交与优先级

`RenderLayers.All` 位于前景液体之后、场景 Filter 之前。该阶段使用 [OverlayQueue](OverlayQueue.cs) 统一收集与排序：先收集已注册的 CustomOverlay，再按实体槽位顺序收集活跃 NPC、弹幕、世界掉落物和玩家，分别查询 ModNPC、ModProjectile、ModItem 和玩家的 ModPlayer 是否实现 `IOverlaySource`。实体中心在屏幕外或弹幕 hide 不影响收集，不依赖 PreDraw，不读取背包物品或未激活实体。

来源实现 `SubmitOverlays(OverlayQueue)`，根据当前视觉状态提交 `IOverlayDrawable`。实体可同时实现两个接口，直接提交自身；一个来源也可提交多个长期持有的绘制对象，分别指定优先级。不要每帧创建绘制包装对象或捕获委托。玩家继续遵守统一 AnomalyPlayer 数据容器与 Behavior 约束，不为接入遮罩另建存储数据的 ModPlayer；原版实体 Behavior 等未自动枚举的来源，可通过模块内 CustomOverlay 的 SubmitOverlays 收集并转交。

```csharp
public void SubmitOverlays(OverlayQueue overlays)
{
    if (Projectile.active && IsVisible)
        overlays.Submit(this, OverlayPriority.Attack);
}

public void DrawOverlay(SpriteBatch spriteBatch)
{
    // 使用实体当前状态绘制；自行 Begin/End，返回时批次必须结束。
}
```

队列仅允许在收集期间提交，同一绘制对象按引用去重，首次提交的优先级生效。数值越大越晚绘制；同优先级保持提交顺序。队列每次绘制重新收集，在 finally、世界退出及卸载时清空，不跨帧持有实体。菜单与专用服务器不收集或绘制。

| 优先级 | 用途 |
| --- | --- |
| Background (-100) | 底层铺色 |
| Default (0) | 既有复合场景效果，默认值 |
| Attack (100) | 武器星爆等独立攻击特效 |
| ArenaMask (200) | 场地外部遮挡、视野暗幕 |
| Foreground (300) | 明确需要覆盖暗幕的最终前景效果 |

`EnhancedDarknessSystem` 使用 ArenaMask，因此克眼竞技场暗幕晚于武器星爆绘制；这是公共暗幕的统一优先级，不改变克眼的光源、强度和战斗逻辑。优先级仅约束本管理器的 All 阶段，不重排 Terraria、其他模组钩子或后续屏幕 Filter。

CustomOverlay 默认将自身按 Priority 提交，既有 Draw 的 All 分支保持可用；复杂场景仍可在内部维持底色、法阵、攻击和批量 Bloom 的顺序。其他 RenderLayers 继续沿用原入口；PostTilesOverlay 保留物块后阶段，不与 All 混合排序。管理器在 All 阶段暂存并恢复调用方 SpriteBatch 状态；每个绘制对象须自行配对 Begin/End，恢复临时渲染目标，不在提交或绘制阶段推进战斗状态。

## 修改与验证

执行 [Anomalies 构建检查](../../../README.md)，注册行为检查见独立资料包 `Validation/Tests/Anomalies/Loading/`，模块边界见 `Integration/Modules/Verify.ps1`。游戏内检查效果开关、绘制层次、世界切换、专用服务器与着色器回退。
