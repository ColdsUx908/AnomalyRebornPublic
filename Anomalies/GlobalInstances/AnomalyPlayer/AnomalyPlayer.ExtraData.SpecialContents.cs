#if SPECIAL_CONTENTS
namespace Anomalies.Common;

public sealed partial class AnomalyPlayer
{
    #region 落日遗逢
    public int EventideReunion_DeathCooldown;
    public int EventideReunion_DeathProtection;
    // 当帧由生命果实际增加的整数上限，供本地生命条使用。
    public int EventideReunion_LifeBlessingBonus;
    // 本地到期检测，不参与网络快照；Buff 使用原版同步。
    public bool EventideReunion_LifeBlessingWasActive;
    public int EventideReunion_LifeCooldown;
    public byte EventideReunion_ResonanceMask;
    public int EventideReunion_ResonanceTime;

    // 落日遗逢的本地输入状态，不参与网络快照。
    public bool EventideReunion_RightWasDown;
    public int EventideReunion_SacrednessCooldown;
    public int EventideReunion_SamsaraCooldown;
    public int EventideReunion_StarCooldown;
    public int EventideReunion_SunCooldown;
    public int EventideReunion_SunFallProtection;
    #endregion

    #region 召唤物
    public bool Minion_VacuousBlack;
    #endregion
}
#endif
