# 传说之夜战斗与表现

当前战斗边界与效果说明；导航见 [README.md](README.md)，状态和同步约束见 [ARCHITECTURE.md](ARCHITECTURE.md)。

## 攻击和场地边界

机制六包含五次步进星爆，前四次从开始蓄力起连续执行，每次由中心圆和外侧三环依次起爆，前四次共十六层连续间隔 95 帧。第二至第四次的中心圆分别与第 1、3、5 波激光的伤害起始帧重合，第五次的中心圆与最后一波 12 发激光同时开始造成伤害。只有第一次的中心圆有淡红色预警，从第十二次普通星爆开始淡入。各层在起爆前 6 帧播放普通星爆音效。以竞技场半径为 R，分界半径依次为 R/2、R/√2、√3R/2、R，每层各占场地四分之一。伤害按玩家中心判定，公共边界归内侧。每层造成伤害 24 帧；步进星爆伤害参数独立为 240，普通星爆为 396，继续沿用 ProjectileDamageContainer 的难度换算。

步进星爆第一层复用普通圆形星爆，后三层复用普通圆环星爆，内外半径直接对应伤害边界。各层从起爆帧播放完整 84 帧视觉和共享径向模糊；主体和边缘光使用共用入场包络，在 24 帧伤害窗口内保持峰值，之后以基础包络速度的 75% 退场，震屏幅度为普通星爆的 30%；仅首轮第一层使用淡红色预警。钱币审判使用独立的圆印、环印和十字光流，按单帧结算伤害，视觉时序见末节。

普通圆形/圆环星爆使用独立视觉，视觉留存到第 84 帧，伤害窗口为 24 帧；步进星爆复用相同视觉。CircleVisuals 与 RingVisuals 保留各自粒子配方，共用公共层 StarburstVisuals 的包络与相机脉冲，圆形 Bloom 共用 StarburstCircleDrawing，圆环光晕校准保留在 LegendStarburstBloom；圆形绘制和八张共用材质位于 Visuals/Starburst 与 Assets/Textures/Starburst；确定性随机、二维视图与基础精灵提交来自 Transoceanic。确定性随机的样本消耗顺序属于视觉约定，未使用的保留样本也不能随意跳过。

12 次普通星爆均在起爆前 30 帧开始播放相同的闪烁预告，预告中心和尺度跟随当前慢法阵（GetMagicCircle(1)），起爆后的两帧淡出也保持跟随。预告独立提交，不在圆形爆炸本体中重复绘制；爆炸和伤害使用预测爆点，不因预告跟随而改变。

Overlay 在一次弹幕遍历中收集同一主人的星爆帧，通过临时数组租约分别绘制预告、圆形和圆环图层，保留法阵绘制次序。视图只在本次绘制内共享；后处理 Apply 仍在相机更新后转换爆点。径向模糊由 AnomalyCommonEffects.StarburstRadialBlur 声明并使用统一加载、注册和清理；源码与产物分别位于 Assets/Effects/Common 和 Assets/Effects_Compiled/Common。公共 StarburstPostProcessing 绑定统一 AnomalyScreenShaderData 的 BeforeApply 参数回调，并通过 IStarburstImpactSource 接收 Boss 和武器的反馈帧；不提供额外加载工厂或替换 Filter。关闭着色器时圆形保留纹理本体，圆环使用无 shader 回退；不生成额外伤害实体。

Head 保持阶段与音乐时钟的所有权。`Mechanic3/4/5Timeline` 持有各星爆音乐节点，`LegendaryNightStarburstTimeline` 提供共享节拍查询，`LegendaryNightStarburstParameters` 提供相对窗口、音效和颜色，`Arena.Orbits` 提供连续公转及爆点预测，服务器通过 `LegendStarburst.EnsureAttack` 确保当前窗口存在一枚星爆；星爆自己的 AI 负责音效、伤害与淡出。法阵蓄光通过时序查询计算，不依赖星爆实体是否已经出现。

二阶段法阵在拉回目标前判定本机目标是否越界。场外时间每帧加一，回到场内每帧减一。越界时立即结算并以 30 帧为间隔重复，伤害随越界时间从 30 线性增加，在 120 帧达到上限 396。越界结算沿用错色光球的直接伤害入口并绕过受击无敌帧。

机制二的半场拍击伤害在 Head 中结算。`LegendaryNightElementBarrage` 由机制七调度，负责八元素载体补建、发射、声音与出现粒子；Arena 只提供圆的位置、半径与旋转。

机制五在 1:28:15 开始稀疏落星和雨伞史莱姆。三波坐骑按一阶段常态符卡的对应间距从场地上方直接下落，同时启动玩家头顶的轮盘。轮盘在每轮第 90 帧锁币，锁定币种继续显示在玩家头顶直至结算，复制币和蓝圈出现在法阵中心，第 190、380、570 帧各结算一次审判。连续星爆期间，左右手各自处理蓄势、挥击和回收，最后一拍回收接入举球动作。

中心黄色光球及膨胀后的恒星共用 `SphereRadius` 圆形接触判定，黄色阶段伤害为300，开始变红后为450，命中施加15秒群星威压；完全消散后停止碰撞。错色拦截的180伤害与蓝星撞击按吸收数量计算的伤害均为直接结算值，使用 `PlayerHurtDamageIgnoresDifficultyScaling` 避免 `Player.Hurt` 按弹幕来源再次乘难度倍率；防御、减伤和免疫规则由受击流程处理。其他使用 `ProjectileDamageContainer` 的攻击使用难度换算。

恒星系统的三个主要弹幕职责：

- `LegendEnergySphere`：中心星、光球生成/拦截/吸收、玩家奖励和循环音效。
- `LegendBlueStar`：蓝星公转、透视投影、物质转移和吸积流缓存；通过 owner + identity 引用中心星，服务器生成撞击冲击波。
- `LegendCompanionShockwave`：独立持有爆发半径、伤害、单次命中标记和爆裂粒子。

`Attacks/Stellar/LegendStellarShockwave.cs` 定义扩张与绘制基类；蓝星结算实现位于同目录 `LegendCompanionShockwave.cs`，终幕实现位于 `Attacks/Finale/LegendFinalShockwave.cs`。两者保留各自伤害规则。普通符卡的 `LegendShockwave` 通过 `BaseShockwaveProjectile` 使用通用的 `Shockwave` 着色器绘制波前、余波与火花。关闭着色器时回退到纹理光环。

Overlay 按既定绘制层次调用中心星的完整绘制入口，由中心星组合蓝星后层、星体、光球、核心、蓝星前层。终幕冲击波使用自身的普通弹幕绘制入口，由终幕陨星在落点生成并独立计时。主体被成功击杀后，陨星同步记录成功标记，仍在终幕生成无伤害的淡金色光环：外圈与失败版本使用相同的最大半径与扩张曲线，并减弱爆炸音量和震屏；失败版本使用秒杀判定。成功版本使用共享着色器的 CurtainCall Pass 绘制，包含浓金色光环和十六枚符文星芒，中心圆角五角星展开后接续两轮烟花；关闭着色器时使用光环回退，并继续绘制烟花。

冲击波接入现有径向模糊与位移震屏入口，不增设全屏 Filter。以普通星爆为 1 倍，伴星结算按实际结算伤害的平方根映射为 0.8～1.8 倍（限制极大吸收量的相机冲击），转阶段落星 1.4 倍，终幕失败 2 倍、成功谢幕 0.35 倍，普通符卡随基础缩放取 0.2～0.7 倍。反馈窗口分别为伴星 38 帧、转阶段 32 帧、失败 44 帧、成功及普通符卡 24 帧；它们只控制起爆冲击，不延续到星云余辉。强度按实际半径和玩家距离衰减，重叠时分别选取当前最强的震屏和模糊，不叠加。伴星、转阶段和终幕只使用对应的统一相机反馈，避免重复；关闭着色器时仍保留位移震屏。所有反馈由实体年龄恢复，不补播过期冲击，不修改伤害、同步协议或几何扩张。

## 着色器命名与职责

着色器按实际视觉职责命名，恒星系统统一使用 LegendStellar 前缀。文件名、所属 Effects 提供者字段和编译产物保持一致；实体类型名不随着色器重命名。资源继续由统一容器加载和清理。

| 分类 | 名称 | 职责 |
| --- | --- | --- |
| LegendaryNight | LegendSlapFlame | 机制二 3D 拍击的半场金色流焰。橙色边缘沿拍击危险区并集绘制，亮度取逐像素最大值；星爆和钱币使用各自纹理回退 |
| Common | Shockwave | 通用扩张冲击波 |
| LegendaryNight | LegendStellarExplosion | Pass0 为基础爆炸，CurtainCall 为成功谢幕，SupernovaRemnant 为伴星撞击与终幕失败的星云，GoldenImpact 为转阶段落星 |
| LegendaryNight | LegendStellarSphere | 星体基础球面、星野和 Core 星核层 |
| LegendaryNight | LegendStellarPhotosphere | 恒星光球层、日冕与消散 |
| LegendaryNight | LegendStellarAccretion | 星体间的物质吸积流 |
| LegendaryNight | LegendStellarLaser | 恒星激光主体 |
| LegendaryNight | LegendStellarLaserWarning | 恒星激光预警 |
| LegendaryNight | LegendStellarLightOrb | 恒星系统的小型光球 |
| Skeletron | SkeletronArenaMist | 场地外侧雾幕 |
| LegendaryNight | LegendaryNightArenaEdge | 场地金色边缘 |
| Common | GoldText | 名称与提示的金色文字 |
| BossBars | Skeletron | 骷髅王血条，包含传说之夜表现 |

一阶段转二阶段的落星由 `LegendTransitionShockwave` 独立绘制。它展开双层金色外圈，向外抛出星屑，并留下带金色星芒的短时星云。

终幕两种冲击波的内外圈独立消散。成功时外圈符文星芒逐渐消散，内圈五角星炸成两轮带尾迹的金色火花，受重力下坠并逐颗熄灭。失败时外壳缓慢变稀，内侧抛射物则扩散成更持久的冷色残云。

两种终幕冲击波的外圈先沿扩张曲线推进，第 36 帧起连续减速并继续越过初始半径，直至消散。内圈和失败判定沿用各自的时间与半径。

落星飞行星屑使用本模块的 `StarTrailParticle` 及蓝、黄、橙、红四种颜色粒子，贴图位于 `Attacks/Stars/`。蓝、黄贴图分别保留原版 Gore 17、16，橙、红版本保留星形轮廓与高光调色。暖色拖尾使用黄橙星芒；蓝色拖尾保留原版黄蓝比例及十周年世界变体。生成频率、初始漂移、缩放衰减和发光沿用原版，生命周期与清理由 ParticleHandler 管理，不生成原版 Gore 实体。

## 钱币审判特效与 LDR 渲染

四种钱币共用 `CoinJudgmentPalette` 的鲜亮金属色：铜橙、珠白银色、亮金和蓝白铂色。轮盘、倒计时、预警边界、束身与星屑均使用相同配色。

铜币与金币共用 `CoinRadialVisuals`：实心圆印和空心环印采用相同的同心刻线、交错金属光流、径向冲击波与星屑。它们借用星爆的环形几何表达，但不用普通星爆的粒子配方；金币内孔保持为安全区，半径直接取实际伤害边界。

银币与铂金币使用 `CoinCrossVisuals` 的正十字和斜十字。视觉宽度倍率为 2.65，外鞘、双侧细轨与多速光流共用径向钱币的材质和金属色。束身第 5 帧（实际审判前 3 帧）接管预警区域后，预警遮罩和边界同时停止提交；之后收束时不重新显示。铜金在审判前 2 帧接管边界。关闭着色器时按相同顺序绘制纹理回退，边界按同一时序消退。

四种审判都从实际结算帧起共用普通星爆的 20 帧径向画面模糊，通过同一个 Filter 选择最新起爆项，不按币种增加全屏 Filter。银铂使用十字震屏，不重复叠加共享位移震屏；铜金使用共享脉冲。

全部使用 `SurfaceFormat.Color` 和双尺度 LDR 柔光，不使用 HDR 管线。粒子在加色捕获时按贴图批量提交，两束十字共用一次状态计算。星爆粒子方向固定、圆形火花按轨迹缩放，全覆盖滤波跳过重复清屏。专用服务器不访问视觉资源。
