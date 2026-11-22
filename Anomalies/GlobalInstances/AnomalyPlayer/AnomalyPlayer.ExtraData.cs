namespace Anomalies.Common;

public sealed partial class AnomalyPlayer
{
    #region 寒心
    public int Coldheart_Phase;
    public int Coldheart_SubPhase;
    #endregion

    #region 减益
    public bool Debuff_DimensionalRend;
    #endregion

    #region 传说之夜
    // 由现存光球的同步计数推导，每帧重置；不单独进入玩家网络快照。
    public int LegendaryNight_DamageBonusStacks;
    public int LegendaryNight_RegenerationBonusStacks;
    public bool LegendaryNight_StarGodBlessingActive;
    public float LegendaryNight_StarGodBlessingOpacity;
    public Item LegendaryNight_VirtualBoneWings;
    #endregion

    #region 玩家进度
    public PlayerDownedBoss Progression_DownedBoss = new();
    #endregion

    #region 通用视觉效果
    public float Visual_DarknessIntensity
    {
        get;
        set => field = MathHelper.Clamp(value, 0f, 1f);
    }
    #endregion
}
