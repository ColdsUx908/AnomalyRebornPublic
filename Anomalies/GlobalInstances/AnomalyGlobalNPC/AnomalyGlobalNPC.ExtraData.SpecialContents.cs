#if SPECIAL_CONTENTS
using Anomalies.SpecialContents.Developer.EventideReunion;

namespace Anomalies.Common;

public sealed partial class AnomalyGlobalNPC
{
    #region 落日遗逢
    // 仅在回斩/波的伤害结算期间有效，供致死回调使用，不参与网络快照。
    public int EventideReunion_DeathHitOwner = -1;

    // 仅客户端的标记入场/退场进度，不参与网络快照。
    public float EventideReunion_DeathMarkProgress;
    // 服务器权威计时；客户端接收快照后逐帧递减，不使用 Buff。
    public int EventideReunion_DeathMarkTime;
    public int EventideReunion_DeathOwner = -1;
    public int EventideReunion_LifeMarkTime;
    internal int EventideReunion_SamsaraOwner = -1;
    internal EventideReunionSamsaraRecord EventideReunion_SamsaraRecord = new();
    internal ulong EventideReunion_SamsaraStartTick;
    internal int EventideReunion_SamsaraVisualTime;
    #endregion
}
#endif
