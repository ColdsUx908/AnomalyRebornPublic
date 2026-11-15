# 世界吞噬怪

## 战斗与数值

当前为重新设计前的基础版本：移除游猎火球、犁地冲锋、三首轮猎与交错封路，仅保留追踪移动和定期传送分合。旧模块完整快照保存在本地 Git 忽略的 `Backup/EaterofWorlds/2026-11-15T05-37-48-845Z/`，不参与编译与打包。

所有头部使用从 CalamityMod `NPCs/VanillaNPCAIOverrides/Bosses/EaterOfWorldsAI.cs` 移植的逐轴加速与转向逻辑，固定采用可穿行分支，不检测物块，也没有空中下坠分支。普通异象使用 Revengeance 移动参数，Ultra 使用 Death 移动参数；原实现的剩余体节比例改为本项目共享生命比例。保留低血量加速、Ultra 极低血量预判和特殊种子速度修正，不调用灾厄 AI，也不要求安装灾厄。该移植部分版权归 Azafure, LLC，许可证见 [Portal/Calamity-LICENSE.md](Portal/Calamity-LICENSE.md)。

追踪 600 帧（10 秒）后请求一次传送，在完整单体与三条蠕虫之间交替。传送仍先撤离至玩家远处，满足最少 45 帧且全部头部离玩家至少 1100 像素后生成入口和出口；450 帧未能完成撤离则返回追踪并重新计时。出口固定在玩家移动方向前方 600 像素，裂隙蓄力 180 帧后复用原体节执行分合，以 34 速度出场。全部头部离出口至少 1200 像素且尾部展开后重新计时。保留现有裂隙视觉、同步与出场方向预警；传送视效来源见 [Portal/README.md](Portal/README.md)。

保留共享生命 9000（灾厄启用时 12000）、普通 60 / Ultra 90 体节、头/体/尾接触伤害 108/66/54 和防御 8，以及现有多人缩放。删除原 50% 亚阶段及其血条标记。隐藏或未展开的体节不造成接触伤害，传送保护沿用原逻辑。

## 状态与分合

- `ai[0]` 打包主头、局部头、局部前驱、局部后继索引；0 为空，其他为 NPC 索引 + 1。
- `ai[1]` 是不可变的全局体节序号。主头身份不依赖分裂时会改变的局部前驱。
- `ai[2]` 打包 Phase 和 Behavior，`ai[3]` 是行为内部步骤。具体步骤见 Phase1AI 内注释。
- 额外 32 位槽：0 的 bit0 表示分裂、bit1 表示体节轨迹已初始化；1 为盘绕朝向；2/3 保留；4 为已完成的传送次数。
- 额外 64 位槽：0 为出生盘绕中心/传送出口，1 为撤离集结点，2 保留，3 为各头传送锁定方向。Timer1 为当前步骤时钟。
- 主头统一调度头部运动和攻击，服务器负责步骤推进、重组与弹幕生成。客户端从同步的主头关系恢复头部列表，不依赖服务器本地 AllSegments。
- `EaterofWorldsTopology` 生成局部链，转换前验证体节数量、所属关系和顺序。转换复用 NPC，保留共享生命，同时更新 type 和 netID、清理轨迹缓存。传送步骤为 0 撤离、1 入口隐去与出口蓄力、3 出场；Timer1 由服务器在边界归零。死亡先清理子体节，让原版掉落和击杀记录只结算一次；退场直接清理，不触发奖励。

## 验证

自动验证命令：

- `dotnet build Anomalies/Anomalies.csproj --no-restore -m:1 -p:BuildMod=false`
- `dotnet run --project Validation/Tests/Anomalies/Bosses/EaterofWorlds.Topology/EaterofWorlds.Topology.csproj`
- `dotnet run --project Validation/Tests/Anomalies/Bosses/EaterofWorlds.Portals/EaterofWorlds.Portals.csproj`
- `pwsh -NoProfile -File Validation/Tests/Anomalies/Bosses/BossIsolation/Verify.ps1`
- `python Validation/Tests/Anomalies/Bosses/BossIsolation/verify_dependencies.py`

拓扑测试覆盖 60/90 节反复分合、局部链双向一致性、不重复/不遗漏体节、非法分裂请求。传送测试调用实际编译出的裂隙类型，覆盖蓄力快照、中途加入、爆裂淡出、重复快照不累加裂纹缩放，以及绘制缓存不进入额外同步载荷。

仍需游戏内验证：普通/Ultra 至少两次分合循环、低机动装备躲避空间、各血量区间移动、地表/地下/平台/狭窄场地、两次同时召唤、死亡/退场奖励、客户端中途加入、分合时延迟，以及有/无灾厄时的实际伤害和资源加载。静态编译与拓扑测试不能代替这些检查。
