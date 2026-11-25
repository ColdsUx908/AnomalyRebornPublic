using Anomalies.Bosses.Skeletron.LegendaryNight;

namespace Anomalies.Common;

public sealed partial class AnomalySynchronization
{
    static partial void ApplyLegendaryNightHalfFieldHit(ProjectileNetReference arenaReference)
    {
        Projectile arena = arenaReference.Resolve();
        PlayerDeathReason source = arena.ModProjectile is LegendaryNightPhase2Arena
            ? PlayerDeathReason.ByProjectile(arena.owner, arena.whoAmI)
            : PlayerDeathReason.LegacyDefault();
        LegendaryNightDamage.HurtLocalPlayer(source, SkeletronLegendHead.SlapDamage, suppressDamage: true);
    }
}
