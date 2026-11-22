namespace Anomalies.Common;

/// <summary>
/// 在实体的网络权威端施加以游戏刻计量的冻结。
/// </summary>
public sealed class AnomalyTimeFreezeSystem : ModSystem
{
    public override void Load()
    {
        On_NPC.UpdateNPC += UpdateNPC;
        On_Projectile.Update += UpdateProjectile;
        On_NPC.AI += RunNPCAI;
        On_Projectile.AI += RunProjectileAI;
    }

    public static void Freeze(NPC npc, int ticks)
    {
        if (!npc.active || npc.Anomaly.TimeFreeze_Immune || ticks <= npc.Anomaly.TimeFreeze_Remaining)
            return;

        npc.Anomaly.TimeFreeze_Remaining = ticks;
        if (Main.netMode != NetmodeID.SinglePlayer)
            NetMessage.SendData(MessageID.SyncNPC, number: npc.whoAmI);
    }

    private static void UpdateNPC(On_NPC.orig_UpdateNPC orig, NPC npc, int index)
    {
        if (npc.active && npc.Anomaly.TimeFreeze_Remaining > 0)
        {
            npc.Anomaly.TimeFreeze_Remaining--;
            return;
        }
        orig(npc, index);
    }

    private static void RunNPCAI(On_NPC.orig_AI orig, NPC npc)
    {
        if (!npc.active || npc.Anomaly.TimeFreeze_Remaining == 0)
            orig(npc);
    }

    public static void Freeze(Projectile projectile, int ticks)
    {
        if (!projectile.active || projectile.Anomaly.TimeFreeze_Immune || ticks <= projectile.Anomaly.TimeFreeze_Remaining)
            return;

        projectile.Anomaly.TimeFreeze_Remaining = ticks;
        if (Main.netMode != NetmodeID.SinglePlayer)
            NetMessage.SendData(MessageID.SyncProjectile, number: projectile.whoAmI);
    }

    private static void UpdateProjectile(On_Projectile.orig_Update orig, Projectile projectile, int index)
    {
        if (projectile.active && projectile.Anomaly.TimeFreeze_Remaining > 0)
        {
            projectile.Anomaly.TimeFreeze_Remaining--;
            return;
        }
        orig(projectile, index);
    }

    private static void RunProjectileAI(On_Projectile.orig_AI orig, Projectile projectile)
    {
        if (!projectile.active || projectile.Anomaly.TimeFreeze_Remaining == 0)
            orig(projectile);
    }
}
