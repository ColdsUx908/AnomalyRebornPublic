# 纯白（ImmaculateWhite）

纯白是随世界进度成长的传奇远程武器，使用持弓弹幕发射追踪箭。

## 启用条件与依赖

归属 `SPECIAL_CONTENTS`，单项开关为 `AnomalySpecialContentToggler.ImmaculateWhite`，默认 true。物品、持弓和箭矢采用同一加载条件；出生赠送由公共入口检查开关。

[纯黑](../VacuousBlack/README.md) 直接调用本物品类型的 `GetPhase` 和 `GetDamageMultiplier`。关闭纯白加载不移除这些静态方法；删除本目录源码则会影响纯黑引用。

## 功能概览

成长阶段根据世界击败记录计算，覆盖肉前、困难模式及月亮领主后；伤害倍率集中在 `GetDamageMultiplier`。持弓读取玩家光标、选择弹药并按射击间隔发射 ImmaculateBolt。具备分裂能力时，右键发射降低伤害的持续穿透箭；普通分裂箭命中后生成小型箭。箭矢的追踪、目标限制及命中衰减由弹幕自身实现。

## 代码导航

| 文件 | 职责 |
| --- | --- |
| [ImmaculateWhite.cs](ImmaculateWhite.cs) | 物品、成长阶段、伤害倍率、配方与提示 |
| [ImmaculateWhiteBow.cs](ImmaculateWhiteBow.cs) | 持弓、光标方向、弹药、射击间隔和模式标记 |
| [ImmaculateBolt.cs](ImmaculateBolt.cs) | 箭矢运动、追踪、分裂、碰撞及绘制 |

## 状态、同步与资源

所属客户端读取输入并生成箭矢，持弓方向通过弹幕状态同步，`ShootSpeedMultiplier` 通过额外 AI 同步。每帧从基础间隔计算实际射速，不能在已有间隔上累乘。持弓本身不造成接触伤害。

持弓复用物品材质，箭矢显式引用原版 Extra 资源。本地化归 `SpecialContents.Contributor.ImmaculateWhite`，物品名称由贡献者分组自动追加类型名。

## 修改与验证

遵循[特殊内容约定](../../README.md)。修改成长方法同时检查纯黑；修改射击需核对左/右键、弹药不足、修饰语、目标失效、分裂和持续穿透。验证所属客户端生成、远端持弓方向以及开关关闭后的出生物品。构建和模块检查见 [Anomalies](../../../README.md)，游戏内战斗与联机需单独验收。
