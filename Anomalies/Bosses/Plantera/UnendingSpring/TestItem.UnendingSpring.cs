#if DEBUG
namespace Anomalies.Testing;

public sealed partial class TestItem
{
    static partial void CanSummonUnendingSpring(Player player, ref bool available) =>
        available |= player.altFunctionUse != 2 && CanStartUnendingSpring(player);

    private static bool CanStartUnendingSpring(Player player) => player.active && !player.dead && !player.ghost
        && AnomalySharedData.Anomaly && Bosses.AnomalyBossChangeToggler.UnendingSpring && !NPC.AnyNPCs(NPCID.Plantera);

    static partial void UseUnendingSpring(Player player, ref bool used)
    {
        if (player.whoAmI != Main.myPlayer || player.altFunctionUse == 2 || !CanStartUnendingSpring(player))
            return;
        used = true;
        if (Main.netMode == NetmodeID.SinglePlayer)
            SummonUnendingSpring(player);
        else
        {
            ModPacket packet = AnomalySynchronization.GetCAPacket();
            packet.Write(AnomalySynchronization.ID.RequestTestUnendingSpring);
            packet.Send();
        }
    }

    internal static void SummonUnendingSpring(Player player)
    {
        if (!TONetworkEnvironment.NotClient || player.HeldItem.type != ModContent.ItemType<TestItem>() || !CanStartUnendingSpring(player))
            return;
        Vector2 position = player.Center - Vector2.UnitY * 480f;
        NPC.NewNPCAction(new EntitySource_BossSpawn(player), position, NPCID.Plantera, action: boss =>
        {
            boss.Center = position;
            boss.target = player.whoAmI;
            boss.Anomaly.CustomAIType = 1;
        });
    }
}
#endif