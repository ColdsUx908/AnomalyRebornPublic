# 特殊内容

特殊武器及开发者、贡献者和捐赠者内容归属 `SPECIAL_CONTENTS`，总模块关闭时本目录源码整体排除。

## 启用条件与当前状态

[AnomalySpecialContentToggler.cs](AnomalySpecialContentToggler.cs) 控制已接入的独立内容。属性初始值在构建时确定，不是游戏内设置。

| 内容 | 默认启用 | 状态与入口 |
| --- | --- | --- |
| [纯白](Contributor/ImmaculateWhite/README.md) | true | 成长型远程武器、持弓与追踪箭 |
| [纯黑](Contributor/VacuousBlack/README.md) | true | 成长型召唤武器与圆形接触伤害 |
| [惠惠的法杖](Contributor/MeguminStaff/README.md) | true | 蓄力、持续光束和耗竭 |
| [落日遗逢](Developer/EventideReunion/README.md) | Debug 开启，Release 关闭 | 普攻、太阳/轮回/死亡三相与共鸣；其余三相尚无释放实现 |
| [CosmicDischarge](Developer/CosmicDischarge/) | 未接入内容开关 | 纹理资源及纹理类骨架，尚无武器实现 |
| [HiganbanaPyre](Developer/HiganbanaPyre/) | 未接入内容开关 | 仅武器图片，尚无内容实现 |

在 [Donors.json](Donor/Donors.json) 中维护捐赠者信息。已有 ID 保持不变，同一捐赠者的新记录按日期追加到 Donations 数组。开发者、贡献者和捐赠者是内容归属分类，不增加构建模块。

## 公共接入与依赖

出生物品由 [AnomalyStartingItems.cs](../Behaviors/AnomalyStartingItems.cs) 接入，并检查对应内容开关。玩家数据、克隆、重置及同步归 [AnomalyPlayer](../GlobalInstances/AnomalyPlayer/)；具体机制行为留在所属内容。

纯黑直接使用纯白类型的成长阶段和伤害倍率计算。单项开关控制加载，不能把关闭纯白等同于删除纯白源码。

## 修改与验证

遵循 [MODULES.md](../MODULES.md) 和各内容页。跨模块接入修改须运行独立资料包 `Validation/Tests/Anomalies/Integration/Modules/Verify.ps1`。验证覆盖单项关闭、总模块关闭、出生赠送、玩家死亡/重进、所属客户端生成及专用服务器；视觉还需检查关闭着色器和不同分辨率。
