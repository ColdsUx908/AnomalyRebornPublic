# 慧慧的法杖

贡献者传奇魔法武器，归属 `SPECIAL_CONTENTS`；单项开关为 `AnomalySpecialContentToggler.MeguminExplosion`。关闭总模块或单项开关时，物品、蓄力与光束弹幕、耗竭 Buff、稀有度和氛围系统均不加载。

按住使用键蓄力 180 帧，随后持续发射随鼠标移动的竖直光束。初次施法消耗 160 魔力；光束预热 18 帧后每 10 帧消耗 12 魔力。停止使用或魔力不足时停止伤害并淡出，结束后施加 480 帧耗竭。基础伤害为 4500，局部 NPC 命中间隔为 15 帧。焦点限制在玩家周围 1200 世界像素内；伤害柱沿世界高度贯穿，不受地形阻挡。

配方：狱火叉 ×1、红玉 ×999、夜明锭 ×40、耀斑碎片 ×400，在远古操纵机制作。

`MeguminExplosion.cs` 定义物品与耗竭 Buff；`MeguminCasting` 处理蓄力；`MeguminBlast` 处理持续伤害与停止；`MeguminAtmosphere` 叠加局部暗幕和闪光；`ExplosionDrawing` 提交法阵、光束与回退绘制。所属客户端读取鼠标、生成光束并支付持续魔力，其他端使用同步状态；纯视觉数据不进入网络载荷。

法阵与光束着色器位于 `Assets/Effects/Primitives/MeguminCircle.fx`、`MeguminLaser.fx`，提示文字位于 `Assets/Effects/Texts/MeguminStaff.fx`，由 `AnomalyEffects` 统一管理。光束绘制端点通过逆相机矩阵置于视口外，使用世界纵坐标采样噪声，不在鼠标焦点截断。法阵使用局部 UV，文字使用 UI 像素坐标，不额外叠加屏幕长宽比修正。关闭着色器时使用贴图与线段回退；回退贴图在物品 Load 阶段加载，由 tML 管理资产，TOCleanup 清理引用，专用服务器跳过。

自动化验证与改动记录按仓库原有约定放在本地独立资料包 `Validation/Tests/`、`Validation/docs/`，不纳入 Git。模块入口为 `Validation/Tests/Anomalies/Integration/Modules/Verify.ps1`。构建检查不代表已完成游戏内战斗、多人或各种分辨率下的视觉验收。