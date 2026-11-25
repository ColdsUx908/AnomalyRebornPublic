#if DEBUG
namespace Anomalies.Common;

public sealed partial class AnomalySynchronization
{
    static partial void ApplyTestUnendingSpring(int playerIndex)
    {
        if ((uint)playerIndex < Main.maxPlayers)
            Testing.TestItem.SummonUnendingSpring(Main.player[playerIndex]);
    }
}
#endif