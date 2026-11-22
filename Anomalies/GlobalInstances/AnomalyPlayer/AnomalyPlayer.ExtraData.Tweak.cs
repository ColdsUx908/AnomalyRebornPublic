#if TWEAK
namespace Anomalies.Common;

public sealed partial class AnomalyPlayer
{
    #region 冲刺输入
    // 仅本地输入，不参与网络同步。
    internal int DashInput_LastDirection;
    internal bool DashInput_LeftWasDown;
    internal bool DashInput_RightWasDown;
    #endregion
}
#endif
