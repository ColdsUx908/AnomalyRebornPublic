using Anomalies.Visuals.BossBar;

namespace Anomalies.Bosses.EaterofWorlds;

public sealed partial class EaterofWorldsHead : EaterofWorldsSegment<EaterofWorldsHead>
{
    protected override int AI32Capacity => 6;

    protected override int AI64Capacity => 5;

    #region 数据
    public enum Phase : byte
    {
        Initialize,
        Phase1,
        Phase1_Duet,
        Phase1_Trio,
        PhaseChange_1To2,
        Phase2,
        Phase2_Relay,
    }

    public enum Behavior : byte
    {
        None = 0,
        Phase1_Hunt = 1,
        Phase1_Charge = 2,
        Phase1_Split = 3,
        Phase1_Foldback = 4,
        UltraTransition,
        ShortWormFormation,
        Despawn = byte.MaxValue,
    }

    public const string AnomalyEaterofWorldsPath = AnomalySharedData.ModPath + "Bosses/EaterofWorlds/";

    public const float DespawnDistance = 8000f;

    public const int UltraLongWormSegmentCount = 60;
    public const int UltraShortWormCount = 10;
    public const int UltraShortWormSegmentCount = 3;

    public static int BodyCount => Ultra ? 88 : 58;
    public static int TotalSegmentCount => BodyCount + 2;

    public List<NPC> AllSegments => AnomalyNPC.EaterOfWorlds_Segments;

    public Phase CurrentPhase
    {
        get => (Phase)AI_Union_2.Byte0;
        set
        {
            if (CurrentPhase != value && Main.netMode != NetmodeID.MultiplayerClient)
                NPC.netUpdate = true;
            Union32 union = AI_Union_2;
            union.Byte0 = (byte)value;
            AI_Union_2 = union;
        }
    }

    public Behavior CurrentBehavior
    {
        get => (Behavior)AI_Union_2.Byte1;
        set
        {
            if (CurrentBehavior != value && Main.netMode != NetmodeID.MultiplayerClient)
                NPC.netUpdate = true;
            Union32 union = AI_Union_2;
            union.Byte1 = (byte)value;
            AI_Union_2 = union;
        }
    }

    public int CurrentLocalPhase
    {
        get => (int)NPC.ai[3];
        set
        {
            if (NPC.ai[3] != value)
            {
                NPC.ai[3] = value;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    NPC.netUpdate = true;
            }
        }
    }

    public bool CurrentlySplit
    {
        get => AnomalyNPC.AnomalyAI32[0].Bits[0];
        set
        {
            if (AnomalyNPC.AnomalyAI32[0].Bits[0] != value)
            {
                AnomalyNPC.AnomalyAI32[0].Bits[0] = value;
                AnomalyNPC.AIChanged32[0] = true;
            }
        }
    }

    public float CoilingStartAngle
    {
        get => AnomalyNPC.AnomalyAI32[1].F;
        set
        {
            float temp = TOMathUtils.NormalizeWithPeriod(value);
            if (AnomalyNPC.AnomalyAI32[1].F != temp)
            {
                AnomalyNPC.AnomalyAI32[1].F = temp;
                AnomalyNPC.AIChanged32[1] = true;
            }
        }
    }

    public Vector2 CoilingCenter
    {
        get => AnomalyNPC.AnomalyAI64[0].GetValue<Vector2>();
        set
        {
            if (AnomalyNPC.AnomalyAI64[0].GetValue<Vector2>() != value)
            {
                AnomalyNPC.AnomalyAI64[0].SetValue(value);
                AnomalyNPC.AIChanged64[0] = true;
            }
        }
    }
    #endregion 数据

    public override EaterofWorldsSegmentType SegmentType => EaterofWorldsSegmentType.Head;

    public override bool AllowCalamityLogic(CalamityLogicType_NPCBehavior type) => type switch
    {
        CalamityLogicType_NPCBehavior.VanillaOverrideAI => false,
        _ => true,
    };

    public override void SetDefaultsFinal()
    {
        NPC.lifeMax = CalamityEnabled ? 12000 : 9000;
        BridgeUtils.ApplyCalamityHealthBoost(NPC);
        NPC.defense = 8;
    }

    public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment) =>
        AnomalyUtils.ApplyAnomalyBossDifficultyAndPlayerScaling(NPC, bossAdjustment);

    public override bool CheckActive() => false;

    public override bool CheckDead()
    {
        if (!base.CheckDead())
            return false;
        if (!IsFirstSegment || !TONetworkEnvironment.NotClient)
            return true;
        // Remove children before vanilla loot/progression examines the remaining Eater segments.
        foreach (NPC segment in AllSegments)
        {
            if (segment == NPC || !segment.active)
                continue;
            segment.life = 0;
            segment.active = false;
            if (Main.netMode == NetmodeID.Server)
                NetMessage.SendData(MessageID.SyncNPC, number: segment.whoAmI);
        }
        return true;
    }

    public override void BossHeadSlot(ref int index)
    {
        if (PortalOpacity <= 0f)
            index = -1;
    }

    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        spriteBatch.DrawFromCenter(NPC.Texture, NPC.Center - screenPos, NPC.frame,
            drawColor * PortalOpacity, NPC.rotation, NPC.scale);
        return false;
    }

    public override void ApplyCustomMainBossBarShader(BossHealthBar newBar, SpriteBatch spriteBatch, Rectangle destinationRentangle)
    {
        AnomalyBossBarEffects.EaterofWorlds.Data
            .UseImage1(AnomalyTextures.Noise._Smear)
            .UseImage2(AnomalyTextures.Noise._Vein)
            .SetCustomParameter("uScreenResolution", Main.ScreenSize.ToVector2() * Math.Max(Main.UIScale, 1f) / 2f)
            .SetCustomParameter("uPosition", destinationRentangle.TopLeft())
            .Apply();
    }

    /// <summary>
    /// 将 Boss 转换为指定数量的独立蠕虫。
    /// </summary>
    /// <param name="amount">
    /// 要转换的独立蠕虫数量。
    /// <br/>若 amount 为 1，则该方法将 Boss 转换为一个完整蠕虫。
    /// </param>
    /// <returns>由所有转换后的独立蠕虫的头部组成的列表，按顺序排列。</returns>
    public List<NPC> TransformToWorms(int amount)
    {
        if (!TONetworkEnvironment.NotClient || !IsFirstSegment)
            return [];
        return ApplyWormTopology(EaterofWorldsTopology.Create(AllSegments.Count, amount));
    }

    /// <summary>
    /// 将 Ultra 的 90 个体节重组为一条 60 节长虫及十条 3 节短虫；节数包含头尾。
    /// 仅转换拓扑，不改变阶段、位置或运动；调用方负责安排转换时机与出场。
    /// </summary>
    /// <returns>长虫头在首位，其后为按全局体节顺序排列的短虫头；无法转换时返回空列表且不修改实体。</returns>
    public List<NPC> TransformToUltraWorms()
    {
        if (!Ultra || !TONetworkEnvironment.NotClient || !IsFirstSegment)
            return [];
        int[] lengths = new int[UltraShortWormCount + 1];
        lengths[0] = UltraLongWormSegmentCount;
        Array.Fill(lengths, UltraShortWormSegmentCount, 1, UltraShortWormCount);
        return ApplyWormTopology(EaterofWorldsTopology.Create(AllSegments.Count, lengths));
    }

    private List<NPC> ApplyWormTopology(EaterofWorldsTopology.Link[] links)
    {
        if (!TONetworkEnvironment.NotClient || !IsFirstSegment) //只有服务端的真正头部能执行转换操作
            return [];

        NPC owner = NPC;
        List<NPC> segments = AllSegments;
        int count = segments.Count;
        // Validate the entire graph before mutating any entity; a partial conversion cannot be recovered safely.
        if (count != TotalSegmentCount || links.Length != count
            || segments.Any(s => !s.active || !s.EoW))
            return [];

        for (int i = 0; i < count; i++)
        {
            IEaterofWorldsSegment segment = EaterofWorldsHandler.TryGetSegment(segments[i]);
            if (segment.Head != owner || segment.SegmentIndex != i + 1)
                return [];
        }

        StopEncounterCircularMovement();
        bool split = links.Any(link => link.Head != 0);
        List<NPC> heads = [];
        for (int i = 0; i < count; i++)
        {
            NPC segment = segments[i];
            EaterofWorldsTopology.Link link = links[i];
            NPC localHead = segments[link.Head];
            int type = link.Previous < 0 ? NPCID.EaterofWorldsHead
                : link.Next < 0 ? NPCID.EaterofWorldsTail : NPCID.EaterofWorldsBody;
            // Do not SetDefaults: it would reset health, global data and the shared-life relationship.
            segment.type = segment.netID = type;
            IEaterofWorldsSegment behavior = EaterofWorldsHandler.TryGetSegment(segment);
            behavior.Head = owner;
            behavior.LocalHead = localHead;
            behavior.SegmentIndex = i + 1;
            behavior.Previous = link.Previous < 0 ? null : segments[link.Previous];
            behavior.Next = link.Next < 0 ? null : segments[link.Next];
            segment.realLife = i == 0 ? -1 : owner.whoAmI;
            segment.target = owner.target;
            if (i != 0)
            {
                segment.lifeMax = owner.lifeMax;
                segment.life = owner.life;
                segment.boss = false;
            }
            segment.ai[2] = owner.ai[2];
            segment.ai[3] = owner.ai[3];
            segment.Timer1 = owner.Timer1;
            segment.Timer2 = owner.Timer2;
            segment.Timer3 = owner.Timer3;
            segment.Anomaly.AnomalyAI32[0].Bits[0] = split;
            segment.Anomaly.AnomalyAI32[0].Bits[1] = true; // initialized trajectory, independent of form
            segment.Anomaly.AIChanged32[0] = true;
            segment.damage = 0;
            segment.defense = 8;
            Array.Fill(segment.oldPos, segment.position);
            Array.Fill(segment.oldRot, segment.rotation);
            segment.netUpdate = true;
            if (link.Previous < 0)
                heads.Add(segment);
        }
        CurrentlySplit = split;
        return heads;
    }
}
