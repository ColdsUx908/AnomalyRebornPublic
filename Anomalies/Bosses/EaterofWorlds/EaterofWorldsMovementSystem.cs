namespace Anomalies.Bosses.EaterofWorlds;

public sealed class EaterofWorldsMovementSystem : ModSystem
{
    public override void PreUpdateNPCs()
    {
        if (!AnomalySharedData.Anomaly || !AnomalyBossChangeToggler.EaterofWorlds)
            return;
        foreach (NPC npc in Main.ActiveNPCs)
        {
            if (npc.type != NPCID.EaterofWorldsHead || !npc.Anomaly.ShouldRunAnomalyAI || npc.Anomaly.TimeFreeze_Remaining > 0)
                continue;
            EaterofWorldsHead owner = EaterofWorldsHead.GetInstance(npc);
            if (owner.IsFirstSegment && (owner.CurrentPhase != EaterofWorldsHead.Phase.Initialize
                || owner.CurrentBehavior == EaterofWorldsHead.Behavior.Despawn))
                owner.AdvanceEncounter();
        }
    }

    public override void PostUpdateNPCs()
    {
        if (!AnomalySharedData.Anomaly || !AnomalyBossChangeToggler.EaterofWorlds)
            return;
        foreach (NPC npc in Main.ActiveNPCs)
        {
            if (npc.type != NPCID.EaterofWorldsHead || !npc.Anomaly.ShouldRunAnomalyAI)
                continue;
            EaterofWorldsHead local = EaterofWorldsHead.GetInstance(npc);
            if (!local.ValidHead || local.LocalHead != npc || npc.Anomaly.TimeFreeze_Remaining > 0 || local.Head.Anomaly.TimeFreeze_Remaining > 0)
                continue;
            bool circular = local.CircularMovementActive;
            if (!circular)
                EaterofWorldsHead.SmoothHeadRotation(npc);
            if (!circular || Main.netMode == NetmodeID.MultiplayerClient)
                FollowHead(local, circular);
        }
    }

    // NPC.UpdateNPC integrates heads in slot order. Articulate the chain only after all heads
    // have moved, so even a neck in an earlier NPC slot attaches to this frame's actual head.
    private static void FollowHead(EaterofWorldsHead local, bool circular)
    {
        NPC head = local.NPC;
        EaterofWorldsHead owner = local.HeadBehavior;
        bool emerging = owner.CurrentBehavior == EaterofWorldsHead.Behavior.Phase1_Split && owner.CurrentLocalPhase == 3;
        NPC front = head;
        NPC rear = local.Next;
        float heading = head.rotation - MathHelper.PiOver2;
        int ordinal = local.SegmentIndex;
        while (rear.active && ordinal < Main.maxNPCs)
        {
            IEaterofWorldsSegment link = EaterofWorldsHandler.TryGetSegment(rear);
            if (link is null || link.LocalHead != head || link.Previous != front || link.SegmentIndex != ++ordinal)
                break;
            if (!circular && rear.Anomaly.TimeFreeze_Remaining == 0)
            {
                Vector2 delta = front.Center - rear.Center;
                float distance = delta.Length();
                float spacing = EaterofWorldsHead.LinkSpacing(front, rear);
                if (emerging && rear.Distance(owner.CoilingCenter) < 60f)
                    spacing = Math.Min(spacing, distance);
                if (distance > 0.001f)
                {
                    // Limit the angle of the joint, including when the head doubles back past its neck.
                    float bend = MathHelper.WrapAngle(delta.ToRotation() - heading);
                    heading += Math.Clamp(bend, -0.57f, 0.57f);
                }
                rear.Center = front.Center - heading.ToRotationVector2() * spacing;
                rear.rotation = heading + MathHelper.PiOver2;
            }
            else
                heading = rear.rotation - MathHelper.PiOver2;
            // Terraria adds netOffset to individual sprites when drawing. Use one correction
            // across the local chain so an incoming head snapshot cannot open a visual seam.
            if (Main.netMode == NetmodeID.MultiplayerClient)
                rear.netOffset = head.netOffset;
            front = rear;
            rear = link.Next;
        }
    }
}
