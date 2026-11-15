namespace Anomalies.Bosses.EaterofWorlds;

public sealed partial class EaterofWorldsHead
{
    private void Phase1CombatAI()
    {
        List<NPC> heads = GetEncounterHeads();
        if (heads.Count == 0)
            return;
        Timer1++;
        switch (CurrentBehavior)
        {
            case Behavior.Phase1_Hunt:
                foreach (NPC head in heads)
                    PursueTarget(head);
                if (Timer1 >= TeleportInterval)
                    ChangeAttack(CurrentlySplit ? Behavior.Phase1_Combine : Behavior.Phase1_Split);
                break;
            case Behavior.Phase1_Split:
                TransformAttack(false);
                break;
            case Behavior.Phase1_Combine:
                TransformAttack(true);
                break;
            default:
                ChangeAttack(Behavior.Phase1_Hunt);
                break;
        }
        foreach (NPC head in heads)
        {
            if (TONetworkEnvironment.NotClient && Timer1 % 45 == 0)
                head.netUpdate = true;
        }

        void TransformAttack(bool combine)
        {
            // 0: retreat; 1: enter while the exit rift charges; 3: emerge in the new topology.
            switch (CurrentLocalPhase)
            {
                case 0:
                    if (Timer1 == 1 && TONetworkEnvironment.NotClient)
                    {
                        Vector2 away = (NPC.Center - Target.Center).SafeNormalize(new Vector2(AttackSide, 0f));
                        AttackCenter = Target.Center + away * PortalRetreatDistance;
                    }
                    foreach (NPC head in heads)
                        MoveTowards(head, AttackCenter, 38f, 6f);
                    if (Timer1 >= 45 && heads.All(head => head.Distance(Target.Center) >= PortalSafeDistance)
                        && TONetworkEnvironment.NotClient)
                    {
                        // Fix the destination for the entire warning, so a moving player can dodge it.
                        CoilingCenter = Target.Center + Target.velocity.SafeNormalize(new Vector2(AttackSide, 0f)) * PortalExitDistance;
                        foreach (NPC head in heads)
                        {
                            Vector2 direction = (head.Center - Target.Center).SafeNormalize(Vector2.UnitY);
                            GetInstance(head).LaunchDirection = direction;
                            SpawnPortal(head.Center + direction * 120f, true);
                            head.netUpdate = true;
                        }
                        SpawnPortal(CoilingCenter, false);
                        ChangeStep(1);
                    }
                    else if (Timer1 >= FormationTimeout * 3)
                        ChangeAttack(Behavior.Phase1_Hunt);
                    break;
                case 1:
                    foreach (NPC head in heads)
                        head.velocity = GetInstance(head).LaunchDirection * 28f;
                    if (Timer1 >= PortalChargeDuration && TONetworkEnvironment.NotClient)
                    {
                        // Hidden links are changed on the same boundary as the rift explosion and launch.
                        List<NPC> transformed = TransformToWorms(combine ? 1 : 3);
                        if (transformed.Count == 0)
                        {
                            RemoveEncounter();
                            return;
                        }
                        foreach (NPC head in transformed)
                        {
                            int group = (GetInstance(head).SegmentIndex - 1) / (TotalSegmentCount / 3);
                            Vector2 direction = CoilLaunchDirection(group, combine);
                            GetInstance(head).LaunchDirection = direction;
                            head.velocity = direction * OutwardChargeSpeed;
                        }
                        foreach (EaterofWorldsSegmentContainer segment in AllSegments)
                        {
                            NPC entity = segment.NPC;
                            entity.Center = CoilingCenter;
                            entity.Opacity = 0f;
                            if (entity.type != NPCID.EaterofWorldsHead)
                                entity.velocity = Vector2.Zero;
                            Array.Fill(entity.oldPos, entity.position);
                            Array.Fill(entity.oldRot, entity.rotation);
                            entity.netUpdate = true;
                        }
                        ChangeStep(3);
                    }
                    break;
                case 3:
                    foreach (NPC head in heads)
                        head.velocity = GetInstance(head).LaunchDirection * OutwardChargeSpeed;
                    // Wait for the final tail to emerge before scheduling the next attack.
                    if (TONetworkEnvironment.NotClient && Timer1 >= 12
                        && heads.All(head => head.Distance(CoilingCenter) >= OutwardChargeDistance)
                        && AllSegments.All(segment => segment.NPC.Distance(CoilingCenter) >= 60f))
                    {
                        CycleIndex++;
                        ChangeAttack(Behavior.Phase1_Hunt);
                    }
                    break;
            }
        }
    }
}
