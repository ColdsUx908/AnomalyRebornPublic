namespace Anomalies.Bosses.EaterofWorlds;

public sealed partial class EaterofWorldsHead
{
    private void SpawnPortal(Vector2 center, bool entrance, int duration = PortalChargeDuration)
    {
        if (!TONetworkEnvironment.NotClient)
            return;
        Projectile.NewProjectileAction(SourceAI, center, Vector2.Zero,
            entrance ? ModContent.ProjectileType<EaterEntryPortal>() : ModContent.ProjectileType<EaterTeleportRift>(), 0, 0f,
            action: portal =>
            {
                portal.ai[0] = NPC.whoAmI + 1;
                if (portal.ModProjectile is EaterTeleportRift rift)
                    rift.RiftLifetime = duration;
                portal.timeLeft = duration + 45;
            });
    }

    private void SplitThroughPortal(List<NPC> heads)
    {
        switch (CurrentLocalPhase)
        {
            case 0: // Withdraw the old chains before fixing a single exit and all outgoing bearings.
                if (Timer1 == 1 && TONetworkEnvironment.NotClient)
                {
                    Vector2 away = (NPC.Center - Target.Center).SafeNormalize(new Vector2(AttackSide, 0f));
                    AttackCenter = Target.Center + away * PortalRetreatDistance;
                }
                foreach (NPC head in heads)
                    MoveTowards(head, AttackCenter, 38f, 6f);
                if (TONetworkEnvironment.NotClient && Timer1 >= 45
                    && (heads.All(head => head.Distance(Target.Center) >= PortalSafeDistance) || Timer1 >= 120))
                {
                    CoilingCenter = Target.Center + Target.velocity.SafeNormalize(new Vector2(AttackSide, 0f)) * PortalExitDistance;
                    CoilingStartAngle = (Target.Center - CoilingCenter).ToRotation();
                    foreach (NPC head in heads)
                    {
                        Vector2 direction = (head.Center - Target.Center).SafeNormalize(Vector2.UnitY);
                        GetInstance(head).SetRoute(head.Center, direction);
                        SpawnPortal(head.Center + direction * 120f, true);
                    }
                    SpawnPortal(CoilingCenter, false);
                    ChangeStep(1);
                }
                break;
            case 1: // Hidden segments are relinked on the frame the rift opens.
                foreach (NPC head in heads)
                    head.velocity = GetInstance(head).LaunchDirection * 28f;
                if (Timer1 < PortalChargeDuration || !TONetworkEnvironment.NotClient)
                    break;
                List<NPC> transformed = TransformToWorms(RegularHeadCount);
                if (transformed.Count != RegularHeadCount)
                {
                    RemoveEncounter();
                    return;
                }
                foreach (NPC segment in AllSegments)
                {
                    segment.Center = CoilingCenter;
                    segment.velocity = Vector2.Zero;
                    segment.Opacity = 0f;
                    Array.Fill(segment.oldPos, segment.position);
                    Array.Fill(segment.oldRot, segment.rotation);
                    segment.netUpdate = true;
                }
                for (int i = 0; i < transformed.Count; i++)
                {
                    Vector2 direction = PortalExitDirection(i);
                    GetInstance(transformed[i]).SetRoute(CoilingCenter, direction);
                    transformed[i].velocity = direction * OutwardChargeSpeed;
                }
                ChangeStep(3);
                break;
            case 3: // Real emergence still determines when this topology is fully unfolded.
                foreach (NPC head in heads)
                    head.velocity = GetInstance(head).LaunchDirection * OutwardChargeSpeed;
                if (TONetworkEnvironment.NotClient && Timer1 >= 12
                    && heads.All(head => head.Distance(CoilingCenter) >= OutwardChargeDistance)
                    && AllSegments.All(segment => segment.Distance(CoilingCenter) >= 60f))
                    ChangeAttack(Behavior.Phase1_Hunt);
                break;
        }
    }
}
