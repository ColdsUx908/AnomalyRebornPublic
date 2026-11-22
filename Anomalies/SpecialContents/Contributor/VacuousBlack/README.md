# 纯黑（VacuousBlack）

纯黑是随世界进度成长的传奇召唤武器，召唤跟随玩家的黑色圆形领域。

## 启用条件与依赖

归属 `SPECIAL_CONTENTS`，单项开关为 `AnomalySpecialContentToggler.VacuousBlack`，默认 true。物品、Buff 和召唤物采用同一加载条件。

成长阶段与伤害倍率直接调用[纯白](../ImmaculateWhite/ImmaculateWhite.cs)的静态方法。两个内容的加载开关独立，但源码并非可分别删除的独立单元。

## 功能概览

使用要求至少一个可用召唤栏位。同一玩家已有召唤物时，再次使用增加其 `minionSlots`，而非创建另一枚召唤物。伤害随栏位和世界进度计算，显示大小及击退强度按栏位插值。召唤物停留在玩家中心，以圆形区域造成接触伤害；普通敌人与特殊日期条件下的击退规则见 `OnHitNPC`。

## 代码导航

| 文件 | 职责 |
| --- | --- |
| [VacuousBlack.cs](VacuousBlack.cs) | 物品、栏位叠加、成长调用、配方与提示 |
| [VacuousBlackBuff.cs](VacuousBlackBuff.cs) | Buff 与统一玩家召唤标记接入 |
| [VacuousBlackMinion.cs](VacuousBlackMinion.cs) | 持续时间、伤害、栏位同步、圆形碰撞和黑色光环 |

## 状态、同步与资源

持续存在标记为 `AnomalyPlayer.Minion_VacuousBlack`，玩家死亡时清除。召唤物通过额外 AI 成对读写 `minionSlots`；栏位增加后请求同步。碰撞半径与绘制使用相同纹理半径和缩放。

召唤物默认材质指向框架透明纹理，实际绘制使用框架 BloomCircleLarge。物品和 Buff 文本归 `SpecialContents.Contributor.VacuousBlack`。

## 修改与验证

遵循[特殊内容约定](../../README.md)。检查空闲栏位不足、重复召唤、Buff 取消、死亡、成长节点、圆形边缘碰撞及栏位同步；修改纯白的共享成长方法也需复核本内容。构建和模块检查见 [Anomalies](../../../README.md)，动态伤害与联机需游戏内验收。
