namespace Anomalies.Common;

public sealed partial class AnomalyGlobalItem
{
    private const int DataSlot = 64;
    private const int DataSlot2 = 32;

    public Union32[] Data { get; private set; } = new Union32[DataSlot];
    public Union64[] Data2 { get; private set; } = new Union64[DataSlot2];
}
