# 调整内容

`TWEAK` 控制原版武器修改、修改标识和玩家基础属性配置与冲刺操作。它独立于异象世界模式及 SPECIAL_CONTENTS。

## 功能与代码导航

| 入口 | 内容 |
| --- | --- |
| [PreHardmode/Magic](PreHardmode/Magic/) | 太空枪、蜜蜂枪 |
| [PreHardmode/Ranged](PreHardmode/Ranged/) | 蜂膝弓、蜜蜂手雷 |
| [PreHardmode/Melee](PreHardmode/Melee/) | 养蜂人 |
| [PreHardmode/Projectiles/Bee.cs](PreHardmode/Projectiles/Bee.cs) | 蜜蜂弹幕行为 |
| [PlayerBaseStatBoosts.cs](../Behaviors/PlayerBaseStatBoosts.cs) | 移速、跳跃、下落、攀绳、放置速度等配置应用 |
| [AnomalyItemTweakTooltip.cs](../UI/Tooltips/AnomalyItemTweakTooltip.cs) | 物品修改提示 |
| [AnomalyConfigs.cs](../Core/Configuration/AnomalyConfigs.cs) | 配置定义及模块边界 |

## 冲刺操作

在按键设置里绑定「单键冲刺」，默认是 `C`。触发时须按住至少一个左右方向键。两键重叠时，以最近新按下的方向为准，同一帧同时按下时选右。冲刺仍由装备提供，沿用其冷却及效果。

客户端配置中的「禁用双击冲刺」默认关闭，可独立切换原版冲刺入口的双击触发。该设置和热键共同作用于原版通用冲刺入口。其他模组自有的冲刺入口由其自身处理。

## 修改约束

公共目录中的配置与行为同样受 `TWEAK` 条件编译控制。武器默认属性、使用行为和弹幕行为需成对检查；生成权威端沿用具体实体的调用路径，避免每端重复发射。数值和适用范围以对应 Behavior 为准。

## 修改与验证

执行 [Anomalies 构建检查](../README.md)。独立资料包 `Validation/Tests/Anomalies/Tweaks/BeeWeapons/` 提供蜜蜂武器专项检查，模块边界由 `Validation/Tests/Anomalies/Integration/Modules/Verify.ps1` 检查。游戏内复核武器使用、修饰语、弹幕伤害和配置开关；关闭 TWEAK 后不应保留对应玩家加成。
