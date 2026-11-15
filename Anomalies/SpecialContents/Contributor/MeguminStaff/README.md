# 惠惠的法杖

贡献者传奇魔法武器，归属 `SPECIAL_CONTENTS`；单项开关为 `AnomalySpecialContentToggler.MeguminStaff`。关闭总模块或单项开关时，物品、蓄力与光束弹幕、耗竭 Buff、稀有度和氛围系统均不加载。

按住使用键蓄力 180 帧，随后持续发射随鼠标移动的竖直光束。初次施法消耗 160 魔力；光束预热 18 帧后每 10 帧消耗 12 魔力。光束自生成起最多持续 600 帧（10 秒，包含预热），达到上限、停止使用或魔力不足时立即停止伤害并进入 18 帧收束淡出阶段，结束后施加 480 帧耗竭。基础伤害为 4500，独立 NPC 无敌帧为 3 帧。焦点限制在玩家周围 1200 世界像素内；伤害柱沿世界高度贯穿，不受地形阻挡。

配方：狱火叉 ×1、红玉 ×999、夜明锭 ×40、耀斑碎片 ×400，在远古操纵机制作。

`MeguminStaff.cs` 定义法杖物品；`MeguminExhaustion.cs` 定义耗竭 Buff；`MeguminChant` 处理蓄力；`MeguminBeam` 处理持续伤害与停止；`MeguminAtmosphere` 叠加局部暗幕和闪光；`MeguminDrawing` 提交法阵、光束与回退绘制。所属客户端读取鼠标、生成光束并支付持续魔力，其他端使用同步状态；纯视觉数据不进入网络载荷。

法阵与光束着色器位于 `Assets/Effects/MeguminStaff/MeguminCircle.fx`、`MeguminLaser.fx`，提示文字位于 `Assets/Effects/MeguminStaff/MeguminStaff.fx`，由 `AnomalyEffects` 统一管理。光束绘制端点通过逆相机矩阵置于视口外，使用世界纵坐标采样噪声，不在鼠标焦点截断。法阵使用局部 UV，文字使用 UI 像素坐标，不额外叠加屏幕长宽比修正。关闭着色器时使用贴图与线段回退；光束回退使用框架 `TOTextures.Extra.BasicCircle`，由框架管理加载生命周期。旧图为 128×128，中央可见圆直径 64；框架图为 64×64，因此目标绘制尺寸乘以 0.5，保持原可见圆大小和中心。两图边缘像素略有差异。

自动化验证与改动记录按仓库原有约定放在本地独立资料包 `Validation/Tests/`、`Validation/docs/`，不纳入 Git。模块入口为 `Validation/Tests/Anomalies/Integration/Modules/Verify.ps1`。构建检查不代表已完成游戏内战斗、多人或各种分辨率下的视觉验收。