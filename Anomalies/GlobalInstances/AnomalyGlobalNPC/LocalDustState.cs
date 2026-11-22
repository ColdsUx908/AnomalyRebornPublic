namespace Anomalies.Common;

/// <summary>
/// 由实体持有的本地尘埃和帧计时。
/// </summary>
internal sealed class LocalDustState
{
    internal List<Dust> Dust { get; } = [];
    internal bool Initialized;
    internal ulong? LastSpawn;
    internal ulong LastUpdate;
}
