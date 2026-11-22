# 传说之夜代码与同步

代码导航见 [README.md](README.md)，战斗表现见 [MECHANICS.md](MECHANICS.md)。

## 文件职责

- `LegendaryNightHandler.cs`：共享路径、颜色、主体身份检查、主体 Behavior 查询和祝福粒子。
- [Head/SkeletronLegendHead.cs](Head/SkeletronLegendHead.cs)：Behavior 身份、默认属性、受击与死亡规则。
- [Head/SkeletronLegendHead.State.cs](Head/SkeletronLegendHead.State.cs)：枚举、同步槽位映射、竞技场引用、阶段和表现查询；所有伤害容器、直接结算伤害、伤害公式和增减伤倍率集中在“伤害数据”region。
- [Head/SkeletronLegendHead.AI.cs](Head/SkeletronLegendHead.AI.cs)：目标选择、退场、阶段分派及跨阶段攻击辅助。
- [Head/SkeletronLegendHead.Phase1AI.cs](Head/SkeletronLegendHead.Phase1AI.cs)：常态/自转（Spin）与符卡组合，符卡池与行为切换。
- [Head/SkeletronLegendHead.PhaseChangeAI.cs](Head/SkeletronLegendHead.PhaseChangeAI.cs)：转场开始、活动攻击清理、回血、召手、祝福和进入二阶段。
- [Head/SkeletronLegendHead.Avatar.cs](Head/SkeletronLegendHead.Avatar.cs)：化身选择、切换、计时、朝向和切换粒子。
- [Head/SkeletronLegendHead.Drawing.cs](Head/SkeletronLegendHead.Drawing.cs)：主体、化身、拖尾、光晕、图标和血条着色。
- [Head/Phase2/SkeletronLegendHead.Phase2AI.cs](Head/Phase2/SkeletronLegendHead.Phase2AI.cs)：二阶段时钟、阶段推进、七套机制分派及竞技场恢复。
- [Head/Phase2/SkeletronLegendHead.Mechanic1.cs](Head/Phase2/SkeletronLegendHead.Mechanic1.cs) 至 `Mechanic7.cs`：每个文件包含一套机制的参数、纯计算及主体 AI；不合并机制。各 `MechanicNTimeline` 和 `MechanicNParameters` 保持为 Head 的嵌套类型，分别集中音乐绝对帧与相对时长、运动/攻击参数和纯计算。
- [Hand/SkeletronLegendHand.cs](Hand/SkeletronLegendHand.cs)：身份、实体规则、动作槽位映射、共享几何与主体发出的手部命令。
- [Hand/SkeletronLegendHand.AI.cs](Hand/SkeletronLegendHand.AI.cs)：主人验证、公共更新、手部运动优先级及各运动的本地函数。
- [Hand/SkeletronLegendHand.Drawing.cs](Hand/SkeletronLegendHand.Drawing.cs)：手掌、预警、脱离骨节绘制、悬停框和血条位置。

## 状态与时间

`Phase` 是战斗阶段，`Behavior` 是一阶段常态/自转（Spin）等行为，`Spell` 是独立符卡选择，`CurrentLocalPhase` 是当前行为内部步骤。阶段和行为分别保留在原版 AI 联合体的 Byte0、Byte1，符卡保留在 Byte2。

| 时间 | 用途 | 重置边界 |
| --- | --- | --- |
| `Timer1` | 当前行为或局部步骤的帧数 | 按各行为切换规则重置 |
| `Timer2` | 转场回血插值的初始生命值 | 转场开始时保存 |
| `Timer5` / `Phase2MusicElapsedTime` | 从转场开始累计的音乐绝对帧 | 开始转场时归零，机制切换不重置 |
| `Phase2ElapsedTime` | 二阶段生存时间，供动态减伤使用 | 进入二阶段时归零，最多累计 10800 帧 |
| `Mechanic2Age` | 相对机制 2 起点的帧数 | 由音乐时钟和同步起点计算 |

不要按枚举声明位置重新编号：机制 5/6/7 的网络值分别是 8/9/10，`Phase2_Finale` 的协议保留值为 7。阶段分类由显式查询统一解释，不依赖枚举大小顺序；未知网络值不激活二阶段音乐或场地。

## 同步数据

| 数据 | 当前同步位置 |
| --- | --- |
| 竞技场引用 | `AnomalyAI64[1]` 的 owner + identity |
| 一阶段审判币种位图、机制五首轮审判币种 | `AnomalyAI32[0]` 的低四位和高位 |
| 化身、化身计时、化身旋转 | `AnomalyAI32[1]`、`[2]`、`[3]` |
| 生存时间、正在召手、拍击左右位图、机制 2 音乐起点 | `AnomalyAI32[4]` 至 `[7]` |
| 停靠起点/终点/初速度 | `AnomalyAI64[2]`、`[3]`、`[4]`，仅停靠期间有效 |
| 机制 2 拍击起点/目标 | `AnomalyAI64[2]`、`[3]` |
| 机制 3 跳跃起点/落点 | `AnomalyAI64[2]`、`[3]` |
| 机制 4 回中起点/冲刺速度 | `AnomalyAI64[2]`、`[3]` |
| 停靠状态/拍击锁定序号/史莱姆雨目标 | `AnomalyAI32[8]`，按机制解释 |

上述复用通过语义属性访问，不能增加 Behavior 实例存储。Head 的业务容量为 9 个 AI32 槽和 5 个 AI64 槽；容器另行附加尾部同步标记槽。二维向量完整占用一个 AI64 槽，对应 `AIChanged64` 标记。

手的原版 `ai[0]` 和 `ai[1]` 保留左右侧和主人索引。`ai[2]` 的常态动作与脚本动作分别由 `NormalAction`、`ScriptedAction` 解释；机制 5 的 `ai[3]` 是 1-based 已锁定攻击序号。机制 5 的起点/终点分别使用 `AnomalyAI64[0/1]`，机制 6 准备阶段的相对起点复用机制 5 起点槽；机制 2 入场复用这两个槽保存相对起点和初速度，由服务器锁定，并用动作值 6 区分常态动作，保证客户端等待及中途加入可恢复。入场用固定 Hermite 曲线衔接普通待机惯性，非拍击手保持下垂朝向，随预警移至本体侧下方，留出横向骨节空间并从玩家侧旁掠过，继续沿用本体深度和绘制锚点，不另存状态。

## 实体数据槽位查阅

每个实体主声明统一使用 XML `<summary>` 描述用途，`<remarks>` 内的 `<para>` 依次列出原版 `ai`、`localAI`、框架 Timer、额外同步载荷和客户端状态。未使用的槽位明确说明；继承的有效槽位也列在具体实体中。

弹幕 Timer1/Timer2 分别对应 `OceanAI32[2].I`、`[3].I`；NPC Timer1/Timer2/Timer5 分别对应 `OceanAI32[4].I`、`[5].I`、`[8].I`。这些不属于原版 `ai` 数组。主体/手部的槽位复用说明位于各自主文件的类型 XML 注释；对应语义属性仍放在 State 或主文件中。

所有跨端弹幕引用使用 owner + identity。粒子缓存、一次性本地音效/命中标记和绘制顶点不加入网络快照。世界时间系统独立于客户端天空运行，专用服务器仍能控制时间。

## 必须保留的顺序

主入口先用上帧竞技场激活状态选择目标，再清除激活标志，由本帧符卡重新开启。二阶段由 `AdvanceMechanics` 的 switch 表和循环统一推进；保留同帧连续跨越多个机制、随后增加 `Timer1`、最后只执行最终选中机制的顺序。前两次切换保留客户端音乐预测，其余由服务器推进，不能用单次互斥分支替换循环或移动计时位置。

召唤早期尚未赋值主人和自定义 AI 标志，因此保留仪式和 `IsSummoningHands` 的身份兜底。手部锁定数据未到达时，客户端保留等待分支。机制 2 绘制尺寸、悬停框及血条位置必须一起维护。手掌自定义绘制沿用原版 type 36 的底部对齐中心，常规 DrawNPCs/悬停作用域已临时加入 netOffset，不得重复添加。常态待机远距离复用普通手部逐轴追随，近距离使用相对头部的阻尼收敛，避免固定加速度在目标两侧形成持续振荡；普通手部不启用此可选收敛分支。



## 机制二的同步与读取边界

机制二保持在 [Head/Phase2/SkeletronLegendHead.Mechanic2.cs](Head/Phase2/SkeletronLegendHead.Mechanic2.cs) 单文件内，参数和纯查询集中在 `Mechanic2Parameters`，实际 AI 按侧位图、伤害、回程、场地等待、主体轨迹和攻击生成组织。本地函数直接服务于该 AI，不另建机制对象或实体状态容器。

三次拍击的随机左右位图及每次起点/目标由服务器锁定。客户端缺少位图、当前拍击锁定或最后一次回程锁定时停速等待，不自行使用本机目标位置补写同步槽。完整快照到达后按音乐年龄恢复轨迹。主体暂缺有效目标时，客户端等待服务器状态，不自行决定退场；一阶段血量触发转场也由服务器决定。

`GetStrikeIndex` 查询手部动作所属的拍击，`GetBodyStrikeIndex` 查询主体后撤至回收的有效窗口；两者结束边界不同，不可互相替换。拍击前以手部动作为提示，半场金色火焰在命中前淡入五帧，在命中帧达到峰值，随后淡出。半场伤害在服务器的拍击帧判定。各端用独立位图避免重复发送伤害事件或重复播放震动；该位图不进入快照，实体初始化及克隆时重置。

拍击落星先于主体、阶段或场地引用到达客户端时等待，依赖恢复后继续执行；服务器负责确认退场。服务器在拍击帧按场地与玩家碰撞箱确定受击者，只向命中的玩家发送 `LegendaryNightHalfFieldHit`。消息携带场地弹幕的 owner 和 identity，用于伤害来源；场地在客户端缺失时仍结算伤害。客户端独立播放预警和震动，不以本地时钟决定半场伤害。

客户端进入世界时，若服务器初始 NPC 快照中的战斗已进入二阶段音乐，则为该场战斗记录一次音乐定位。当对应曲目首次播放时，将专用 Ogg 音轨定位到主体音乐帧对应的采样点，并立即清除定位请求。原版备用曲和无法定位的旧 MP3 音乐包沿用正常播放。

分发前还检查实际专用服务器加载。公共黑暗遮罩的内容初始化在服务器跳过客户端粒子纹理读取，卸载时清理借用引用；通用时钟在首次宿主更新前返回零，支持内容注册期间的稀有度颜色查询。这些检查独立于 C# 构建和离线快照验证。

## 共用运动与发射

普通骷髅王与传说之夜直接调用父目录的 `SkeletronKinematics`（悬停、旋转、手部待机和蓄势）及 `SkeletronCombat`（头骨与加速扇形发射）。运动实现不由 `SkeletronHand` 或 `SkeletronHandler` 转发。
