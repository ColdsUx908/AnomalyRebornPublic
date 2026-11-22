# 迁移到 Transoceanic 1.4

本文面向从 1.3 升级到 1.4 的消费方。请按项目使用的接口核对下表，并使用 1.4 重新编译。

## 版本与依赖

版本号写作 `1.M` 或 `1.M.P`，第三位为零时省略。首位固定为 1。大型更新递增 M 并从 P = 0 开始；兼容的小型更新只递增 P。例如，1.4.1 可以修复实现或新增兼容接口；移除公开成员、破坏已有调用或改变无法与旧端互通的网络布局，应进入下一个大型版本。

同时更新开发引用、对应的 TransoceanicCodeAssist 分析器（如有使用）以及测试环境的框架模组。重新编译并打包消费方，尤其注意新增可选参数也可能改变 DLL 的方法签名。使用 Ocean ExtraAI 的客户端和服务器部署同一构建。本次版本标记本身没有改变网络载荷。

## 旧调用迁移清单

| 旧用法 | 1.4 中的处理 |
| --- | --- |
| 直接访问全局 Handler 的行为集合 | 通过 BehaviorQueries.EnumerateNPC、EnumerateProjectile、EnumerateItem 或 EnumeratePlayer 查询。需要绑定时传入当前实体，手写辅助调用显式传入回调名。 |
| Behavior 自行提供 Mod 或覆写 Load(Mod mod) | 使用继承的 Mod，在无参 Load() 中初始化；依赖完整内容注册的操作放到 PostSetupContent。手动内容由所属模组 AddContent。 |
| 修改 ParticleSet.Particles，或用 OrderFunction 排序 | 用 TrySpawnParticle 逐项提交并处理失败，按期望的绘制顺序提交。粒子类型级注册和运行实例分别使用 ParticleDefinition 与 Particle。 |
| 依赖框架自动提供辉光着色器 | 在调用 LightweightBloom 时提供所属模组管理的 Effect。 |
| 通过物品 Behavior 转发 NetSend / NetReceive | 将对应的读写成对迁至所属 ModItem 或 GlobalItem，并核对写入与读取顺序。 |
| 旧同步代码仍访问 OceanAI64 或依赖旧计时器索引 | 按现行 NPC 12 个及弹幕 7 个 AI32 数据槽重新核对槽位，双方同步升级。 |

历史改动的完整签名示例、命令与 Detour 生命周期、寻敌及粒子注册细节见[既有迁移记录](Migration-1.3.md)。同步入口和线格式见[网络说明](../Game/Networking/README.md)。

## 高频命中与碰撞扩展

单实体行为集合可用 GetMethodGroup(nameof(...)) 取得可复用的查询句柄。HasCandidates(type) 只判断是否登记过候选；实际调度通过 TryGetBehavior 重新绑定实体并判断 ShouldProcess。调用返回的行为时沿用 LockEntity 作用域。

普通全局弹幕行为可以继续覆写 Colliding。按类型转发碰撞的适配器可覆写 HasCollisionHandler；只有类型注册在配方加载完成到卸载期间都固定时才覆写 HasFixedCollisionTypes 为 true。在 CanHitNPC 中提前排除远处目标的消费方，应先用 BehaviorQueries.MayOverrideProjectileCollision(type) 确认是否需要将判断交给全局覆盖，同时考虑投影和扩大的目标碰撞箱。接入细节见[行为查询指南](GettingStarted.md)。

升级后检查实际打包的 DLL、内容加载与卸载。涉及自定义碰撞的消费方应复测擦边、深度投影和第三方覆盖；启用同步的消费方还应验证联机与中途加入。

## 聊天命令

原有 `TOCommand` 改为继承 `ModCommand`，实现 `Action(CommandCaller caller, string input, string[] args)`。需要保留参数大小写时重写 `IsCaseSensitive`。移除了 `//chat`、redo 历史和 `TOPlayer.CommandCallInfo`。例如直接输入 `/debug on`，也可使用强制命令前缀 `//debug on`。
