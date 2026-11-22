# 落日遗逢设计规范

本文件约定当前实现必须保持的组织、同步、碰撞和绘制边界。功能导航见 [README.md](README.md)，机制说明见 [MECHANICS.md](MECHANICS.md)；通用规则遵循[仓库设计规范](../../../../DESIGN_GUIDELINES.md)。

## 文件职责与命名

- 根目录保存物品入口、选择轮盘、技能枚举、伤害常量和跨技能绘制支持。
- 普攻及其音效放入 `NormalAttack/`；技能专属 AI、几何、伤害代理、玩家行为、图层和资源分别放入 `Sun/`、`Samsara/`、`Life/`、`Death/`、`Sacredness/`、`Star/`。
- `Resonance/` 管理共鸣；`Rune/` 统一管理六相选择符文。`Resonance/SamsaraResonance.png` 只用于共鸣 Buff，不作为轮回 NPC 标记或结算特效。
- 所有源码使用 `Anomalies.SpecialContents.Developer.EventideReunion` 命名空间，子目录不添加命名空间层级。类型使用 `EventideReunion<技能名>` 前缀。
- 跨技能支持放入公共文件，技能不得依赖其他技能的内部实现。玩家状态归 AnomalyPlayer，NPC 状态归 AnomalyGlobalNPC，Behavior 只实现行为。

## 资源与本地化

受目录和命名空间差异影响的内容实体必须显式重写 `Texture`，自行绘制也不能省略。纹理与音效通过对应 Assets 类访问：共享资源为 `EventideReunionAssets`，符文为 `Rune/EventideReunionRuneAssets.cs`，普通攻击及技能各有自己的提供者。

固定纹理通过 `LoadTexture` 字段加载，绘制时不重复请求。资源标识使用 `/` 且省略扩展名；移动时检查主纹理、发光、显式请求、音效和本地化。不得无条件引用可选模组的资产。

本地化统一归 `Mods.Anomalies.SpecialContents.Developer.EventideReunion`。物品 Category 为 `SpecialContents.Developer`，由 tModLoader 追加物品名；专属弹幕和 Buff 使用物品提供的完整分类常量。有独立效果身份的日冕耀斑、寂灭镰波与收割之魂使用各自 DisplayName；动作载体可共用武器名称。死亡印记是字段机制，不注册 Buff、图标或对应文本。

## 权威状态与生命周期

- 持有者读取轮盘、光标及方向；生成技能的 Action 回调登记共鸣并初始化状态，服务器生成伤害代理和执行标记结算。
- 跨端实体引用使用 owner + identity，不能使用本地弹幕槽位。数据写入/读取顺序、变化标记、克隆和重置必须一致。
- 轮回横抽结束后的刺击方向由持有者再次读取并同步；缺少方向的远端等待，不自行使用本机光标推进。
- 轮回伤害账本只在服务器保存，网络仅传表现所需状态；NPC 计时由系统驱动，不依赖 NPC AI 是否执行或施法者是否在线。
- 死亡临时命中归属必须在命中前设置、命中后清除，因为直接击杀的 OnKill 可能早于命中后回调。共享生命部件映射到根实体，每次击杀只生成一次治疗灵魂。
- 玩家死亡、Buff 取消、目标消失和世界卸载分别处理机制状态和客户端缓存。纯视觉缓存不进入快照。

## 碰撞与轨迹

所有带宽度的扫掠统一使用 `RotatedRectangle.FromLineSegment`，保持完整宽度、缩放与移动采样。死亡突进另求首次接触距离以选择最近目标；地形截断立即结束突进，有安全位置时先退回，再回斩。

轮回武器、刀光、因果线与伤害代理共用 SpinVector、TurnVector 和刺击几何。横抽固定投影平面，不把投影后的角度/长度重新作二维插值。转矛和横抽的服务器伤害代理每阶段对同一 NPC 最多命中一次，只有刺击命中集合参与标记选取。

因果线在 AI 中按解析曲线采样，旋身、横抽、后撤与刺击只追加轨迹；收招冻结后按弧长从尾端连续裁剪。未回收到的点不滑动、不整体缩向矛尖。微短末段可合并至精确活动端点，冻结后不得继续合并。轨迹是客户端表现，不额外推进伤害时钟。

## 绘制与资源生命周期

人物残影和施法齿轮由 `EventideReunionAfterimageSystem.PostDrawTiles` 在独立批次绘制。禁止从持有弹幕 PreDraw 或人物图层直接/间接调用玩家渲染器：其全局缓存不支持重入，即使换渲染器实例也不能隔离。临时人物姿态与 SpriteBatch/GraphicsDevice 状态在 finally 恢复。

`EventideReunionMarkDrawing` 先绘制轮回齿轮、再绘制死亡四角星。两标记使用世界像素空间，共存尺寸和光晕参数每次重设；死亡星体保持水平/竖直对称。持续标记、轮回结算和施法齿轮共用 DrawSamsara，避免重复实现形状。

公共刀光使用 `EventideReunionBladeTrail.DrawArc`；每次设置 DeathPalette、SamsaraPalette，确保共享 Effect 不串色。因果线通过 ThreadDrawing 和专属 shader 绘制连续顶点带，显式设置可见弧长边界，使用抗锯齿和圆形端帽。

刺击矛光在实体矛之后使用同一 SpriteBatch 绘制，连续柔边矛形纹理在运行时生成并缓存，卸载时在主线程释放。专属 shader 由统一提供者加载；各调用处理资源为空、专用服务器和关闭 shader 的情况。

以世界像素表达尺寸；使用 MagicPixel 绘制像素线段时明确采样单位区域，避免纹理原尺寸再次放大几何。屏幕遮罩按高度统一尺度，局部 UV 不重复施加屏幕比例修正。

## 修改与验证

修改某技能时同步核对选择入口、共鸣登记、伤害常量、统一数据容器、同步、资源和本地化。新增技能不为未实现职责创建空文件；未实现的释放分支不能授予共鸣印记。

验证入口见 [README.md](README.md)。涉及轨迹和绘制时检查正反重力、不同人物尺寸、快速转身、精准矛尖终点、标记共存、玩家渲染调用图和共享 Effect 配色恢复；涉及伤害时检查共享生命、重复观察、目标消失、溢出及联机归属。验证结果与机制说明分开，不在本文记录开发轮次。
