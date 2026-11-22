namespace Anomalies.Common;

public sealed partial class AnomalyGlobalItem
{
    partial void CloneExtraData(AnomalyGlobalItem clone)
    {
        clone.Data = (Union32[])Data.Clone();
        clone.Data2 = (Union64[])Data2.Clone();
    }
}
