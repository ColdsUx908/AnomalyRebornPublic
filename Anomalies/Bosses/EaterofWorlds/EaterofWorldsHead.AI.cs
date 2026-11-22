namespace Anomalies.Bosses.EaterofWorlds;

public sealed partial class EaterofWorldsHead
{
    public static event Action<EaterofWorldsHead> OnRunningPreAI;

    public override bool PreAI()
    {
        using BossNetUpdateScope netUpdate = new(NPC);
        NPC.alpha = 0;
        NPC.noGravity = true;
        NPC.noTileCollide = true;
        NPC.boss = IsFirstSegment;
        if (!IsFirstSegment)
        {
            if (!ValidHead)
            {
                NPC.damage = 0;
                if (TONetworkEnvironment.NotClient)
                    NPC.active = false;
                return false;
            }
            NPC.realLife = Head.whoAmI;
            NPC.lifeMax = Head.lifeMax;
            NPC.life = Head.life;
            NPC.target = Head.target;
            UpdateCoilDR();
            // The owner drives all heads once, so NPC slot order cannot advance the attack twice.
            NPC.damage = GetContactDamage();
            if (CircularMovementActive)
                NPC.velocity = Vector2.Zero;
            else
                SmoothHeadRotation(NPC);
            OnRunningPreAI?.Invoke(this);
            return false;
        }

        if (CurrentBehavior != Behavior.Despawn && !NPC.TargetClosestIfInvalid(true, DespawnDistance))
        {
            if (TONetworkEnvironment.NotClient)
            {
                CurrentBehavior = Behavior.Despawn;
                Timer1 = 0;
            }
            else
            {
                NPC.damage = 0;
                return false;
            }
        }
        if (CurrentBehavior == Behavior.Despawn)
        {
            StopEncounterCircularMovement();
            NPC.damage = 0;
            NPC.velocity = Vector2.Lerp(NPC.velocity, new Vector2(0f, 18f), 0.04f);
            if (++Timer1 >= 90 && TONetworkEnvironment.NotClient)
                RemoveEncounter();
            return false;
        }

        if (TONetworkEnvironment.NotClient && ShouldEnterUltra)
        {
            if (CurrentBehavior == Behavior.Phase1_Split)
            {
                foreach (Projectile portal in Main.ActiveProjectiles)
                {
                    if (portal.ModProjectile is EaterEntryPortal or EaterTeleportRift && portal.ai[0] == NPC.whoAmI + 1)
                        portal.Kill();
                }
            }
            CurrentPhase = Phase.PhaseChange_1To2;
            ChangeAttack(Behavior.UltraTransition);
            Timer2 = Math.Clamp(NPC.life, 1, UltraEntryLife);
            foreach (NPC segment in AllSegments)
            {
                segment.damage = 0;
                segment.dontTakeDamage = true;
            }
            CoilingCenter = Target.Center - new Vector2(0f, 200f);
            SpawnPortal(CoilingCenter, false, UltraGatherDuration);
        }
        else if (TONetworkEnvironment.NotClient && RegularPhase && CurrentBehavior != Behavior.Phase1_Split
            && RegularPhaseAtCurrentLife > CurrentPhase)
        {
            CurrentPhase = RegularPhaseAtCurrentLife;
            CycleIndex = 0;
            ChangeAttack(Behavior.Phase1_Split);
        }

        switch (CurrentPhase)
        {
            case Phase.Initialize:
                InitializeEncounter();
                break;
            case Phase.Phase1:
            case Phase.Phase1_Duet:
            case Phase.Phase1_Trio:
                Phase1CombatAI(GetEncounterHeads());
                break;
            case Phase.PhaseChange_1To2:
                UltraTransitionAI(GetEncounterHeads());
                break;
            case Phase.Phase2:
            case Phase.Phase2_Relay:
                Phase2CombatAI(GetEncounterHeads());
                break;
        }
        NPC.damage = GetContactDamage();
        UpdateCoilDR();
        SpawnCoilDust();
        SmoothHeadRotation(NPC);
        OnRunningPreAI?.Invoke(this);
        UpdateCircularMovement();
        return false;

        void InitializeEncounter()
        {
            NPC.damage = 0;
            if (!TONetworkEnvironment.NotClient)
                return;
            Head = LocalHead = NPC;
            SegmentIndex = 1;
            Previous = Next = null;
            NPC.realLife = -1;
            AllSegments.Clear();
            AllSegments.Add(NPC);
            const float SpawnRadius = 140f;
            Vector2 coilCenter = NPC.Center - new Vector2(SpawnRadius, 0f);
            CoilingCenter = coilCenter;
            float coilAngle = 0f;
            NPC.velocity = Vector2.Zero;
            for (int i = 1; i < TotalSegmentCount; i++)
            {
                int ordinal = i;
                NPC.NewNPCAction(SourceAI, NPC.Center,
                    i == TotalSegmentCount - 1 ? NPCID.EaterofWorldsTail : NPCID.EaterofWorldsBody,
                    action: segment =>
                    {
                        IEaterofWorldsSegment behavior = EaterofWorldsHandler.TryGetSegment(segment);
                        behavior.Head = behavior.LocalHead = NPC;
                        behavior.SegmentIndex = ordinal + 1;
                        segment.realLife = NPC.whoAmI;
                        segment.lifeMax = NPC.lifeMax;
                        segment.life = NPC.life;
                        segment.damage = 0;
                        float spacing = Main.getGoodWorld ? 62f : segment.width * segment.scale;
                        coilAngle -= 2f * MathF.Asin(Math.Clamp(spacing / (2f * SpawnRadius), 0f, 1f));
                        segment.Center = coilCenter + new Vector2(SpawnRadius, 0f).RotatedBy(coilAngle);
                        segment.rotation = coilAngle + MathHelper.Pi;
                        segment.velocity = Vector2.Zero;
                        AllSegments.Add(segment);
                    });
            }
            if (TransformToWorms(1).Count != 1)
            {
                RemoveEncounter();
                return;
            }
            CurrentPhase = Phase.Phase1;
            CoilingStartAngle = (NPC.Center - Target.Center).ToRotation();
            ChangeAttack(Behavior.Phase1_Hunt);
            Phase1CombatAI(GetEncounterHeads());
        }
    }

    private void RemoveEncounter()
    {
        if (!TONetworkEnvironment.NotClient)
            return;
        foreach (NPC segment in AllSegments)
        {
            if (segment != NPC && EaterofWorldsHandler.TryGetSegment(segment)?.Head != NPC)
                continue;
            segment.active = false;
            if (Main.netMode == NetmodeID.Server)
                NetMessage.SendData(MessageID.SyncNPC, number: segment.whoAmI);
        }
        NPC.active = false;
    }

    // Rebuilt from synced ownership on clients (including late join); server preserves original order.
    private List<NPC> GetEncounterHeads()
    {
        List<NPC> heads = [];
        for (int i = 0; i < Main.maxNPCs; i++)
        {
            NPC candidate = Main.npc[i];
            if (candidate.active && candidate.type == NPCID.EaterofWorldsHead
                && (candidate == NPC || GetInstance(candidate).Head == NPC))
                heads.Add(candidate);
        }
        heads.Sort((a, b) => GetInstance(a).SegmentIndex.CompareTo(GetInstance(b).SegmentIndex));
        return heads;
    }
}
