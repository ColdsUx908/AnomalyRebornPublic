using Anomalies.Visuals.BossBar;

namespace Anomalies.Common;

public sealed partial class AnomalyGlobalNPC
{
    #region 变种 AI 状态
    public bool AnomalyAI_IsRunning => AnomalyAI_Timer > 0;
    public int AnomalyAI_Timer;
    public int AnomalyAI_UltraTimer;
    #endregion

    #region 血条显示
    public bool BossBar_Enabled;
    public bool BossBar_Enraged;
    public List<HPThresholdIndicator> BossBar_HPThresholdIndicators = [];
    public bool BossBar_Immune;
    public bool BossBar_IncreasingDefenseOrDR;
    public bool BossBar_IsSmall;
    public int BossBar_UltraTimer;
    #endregion

    #region 克苏鲁之脑
    // 客户端绘制数据，不参与网络快照。
    public List<PathAfterimageParticle> BrainOfCthulhu_AfterImages = [];
    public Rectangle BrainOfCthulhu_Frame = new(0, 0, 198, 180);
    #endregion

    #region 伤害减免
    public IDynamicDRHandler DamageReduction_DynamicHandler;
    #endregion

    #region 深度绘制
    /// <summary>
    /// 本帧的视觉锚点：实体局部位置、未投影的世界目标和混合权重。仅影响绘制。
    /// </summary>
    internal (Vector2 LocalOffset, Vector2 Target, float Weight)? Depth_DrawAnchor;
    #endregion

    #region 世界吞噬者
    // 服务端使用的体节顺序。体节行为在使用时按当前类型解析。
    internal List<NPC> EaterOfWorlds_Segments { get; private set; } = [];
    #endregion

    #region 通用状态
    /// <summary>
    /// 特殊标志位，供外部判定使用。
    /// <br/>每帧都会被重置为默认值，外部判定需要在每帧内使用。
    /// </summary>
    public BitArray64 General_SpecialFlags = default;
    #endregion

    #region 网络引用缓存
    // Per-NPC local lookup hints. The authoritative owner/identity remains in AnomalyAI64.
    private ProjectileNetReference[] _Network_ProjectileReferenceCache = new ProjectileNetReference[AISlot2 - 1];
    #endregion

    #region 传说之夜骷髅王
    // 客户端绘制数据，按手分别分配，不参与网络快照。
    internal LocalDustState Skeletron_HandBloodDust;
    // 各端消费三次拍击的位图；服务器用于伤害事件，客户端用于屏幕震动，不同步。
    internal int Skeletron_LegendaryNightHalfFieldHits;
    #endregion

    #region NPC专属粒子
    /// <summary>
    /// 仅客户端、按需创建的专属粒子集合；使用世界坐标，在本NPC所有绘制之前提交，不参与同步。
    /// </summary>
    public AnomalyNPCParticleSet Visual_Particles { get; private set; }
    #endregion

    #region 独立实例数据
    public bool TimeFreeze_Immune;
    public int TimeFreeze_Remaining { get; internal set; }
    #endregion
}
