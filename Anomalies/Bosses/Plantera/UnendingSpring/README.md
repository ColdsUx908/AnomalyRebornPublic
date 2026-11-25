# 永劫之春

Unending Spring 是世纪之花的独立超级挑战。Debug 构建下，在异象世界左键使用「测试物品」召唤。场上已有世纪之花时不能重复召唤。普通世纪之花保留自己的行为入口；永劫之春由服务器在生成时设置 CustomAIType = 1。

## 战斗

Phase1 先展示 180 帧玫瑰曲线，再进入 510 帧对峙。交叉种子、追踪种子环、双蝶花阵按组随机不放回，跨组首尾不重复。战斗生命降至半血附近时中断攻击并锁血。转场第 235 帧展开第二条激光并变身，第 442 帧恢复行动。

Phase2 每组抽取钩爪闪电与刺球雨、五连冲刺、花瓣轮阵、双股种子与扇区弹阵各一次。常规对峙持续 510 帧，冲刺结束清场后的对峙持续 630 帧。

## 实现

- [主体 AI](Body/PlanteraUnending.AI.cs) 负责目标检查、阶段分派、追击和每帧收尾。[状态](Body/PlanteraUnending.State.cs)、[一阶段](Body/PlanteraUnending.Phase1AI.cs)、[半血转场](Body/PlanteraUnending.PhaseChangeAI.cs) 和[二阶段招式](Body/Phase2/)分别维护。悖论的攻击调度、运动和藤蔓绘制按职责分文件。
- [钩爪](Hooks/UnendingSpringHook.cs) 保存主人和序号并执行部件行为。主体发出的钩爪攻击命令位于 [Body/Phase2](Body/Phase2/)。[场地](Arena/)包含控制器和客户端绘制系统。
- [攻击实体](Attacks/)按招式归档。玫瑰曲线的光束绘制与其弹幕同目录；花瓣轮阵的阵形计算与绘制分别位于 Geometry 和 Drawing 部分类。[Rendering](Rendering/)存放多种攻击共用的绘制与清场粒子。
- [贴图](Assets/)由各攻击按原资源路径引用。场地着色器位于 Anomalies/Assets/Effects/UnendingSpring。

贴图的离线生成器和配方位于 Tools/TextureGenerator/UnendingSpring。运行 Generate.py 可独立重建 14 张贴图，并逐字节复制原版 Flower.png 和配套高光 ChargeFlowerOutline.png。传入 --verify 和 CenturyBloom.zip 路径可复核裁剪区域与预乘 RGBA 的像素误差。场地流纹复用 Wave2 的生成配方，闪电按四个动画帧的共同中心线裁剪。
