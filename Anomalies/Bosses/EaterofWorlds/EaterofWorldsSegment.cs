namespace Anomalies.Bosses.EaterofWorlds;

public enum EaterofWorldsSegmentType : byte
{
    Head,
    Body,
    Tail
}

public interface IEaterofWorldsSegment
{
    NPC Head { get; set; }
    bool ValidHead { get; }
    NPC LocalHead { get; set; }
    bool ValidLocalHead { get; }
    NPC Previous { get; set; }
    bool ValidPrevious { get; }
    NPC Next { get; set; }
    bool ValidNext { get; }
    int SegmentIndex { get; set; }
    bool IsFirstSegment { get; }
    bool IsLastSegment { get; }
    EaterofWorldsSegmentType SegmentType { get; }
}


public abstract class EaterofWorldsSegment<TBehavior> : AnomalyNPCBehavior<TBehavior>, IEaterofWorldsSegment where TBehavior : EaterofWorldsSegment<TBehavior>, new()
{
    public override bool ShouldProcess => base.ShouldProcess && AnomalyBossChangeToggler.EaterofWorlds;

    //此处所有NPC索引均为whoAmI+1，0表示无效索引
    //这样做的目的是使默认值（0）为空NPC，确保真正的头部能在默认状态下正常工作

    public NPC Head
    {
        get => NPC.GetNPCFromIndex(AI_Union_0.Byte0 - 1);
        set
        {
            Union32 union = AI_Union_0;
            union.Byte0 = (byte)(value is null || value.whoAmI >= Main.maxNPCs ? 0 : value.whoAmI + 1);
            AI_Union_0 = union;
        }
    }
    public bool ValidHead => Head.active && Head.type == NPCID.EaterofWorldsHead;

    public EaterofWorldsHead HeadBehavior => EaterofWorldsHead.GetInstance(Head);

    public NPC LocalHead
    {
        get => NPC.GetNPCFromIndex(AI_Union_0.Byte1 - 1);
        set
        {
            Union32 union = AI_Union_0;
            union.Byte1 = (byte)(value is null || value.whoAmI >= Main.maxNPCs ? 0 : value.whoAmI + 1);
            AI_Union_0 = union;
        }
    }
    public bool ValidLocalHead => LocalHead.active && LocalHead.type == NPCID.EaterofWorldsHead;

    public NPC Previous
    {
        get => NPC.GetNPCFromIndex(AI_Union_0.Byte2 - 1);
        set
        {
            Union32 union = AI_Union_0;
            union.Byte2 = (byte)(value is null || value.whoAmI >= Main.maxNPCs ? 0 : value.whoAmI + 1);
            AI_Union_0 = union;
        }
    }
    public bool ValidPrevious => Previous.active && Previous.EoW;

    public NPC Next
    {
        get => NPC.GetNPCFromIndex(AI_Union_0.Byte3 - 1);
        set
        {
            Union32 union = AI_Union_0;
            union.Byte3 = (byte)(value is null || value.whoAmI >= Main.maxNPCs ? 0 : value.whoAmI + 1);
            AI_Union_0 = union;
        }
    }
    public bool ValidNext => Next.active && Next.EoW;

    public bool IsFirstSegment => SegmentIndex <= 1;
    public bool IsLastSegment => SegmentIndex == EaterofWorldsHead.TotalSegmentCount;

    public int SegmentIndex
    {
        get => AI_Union_1.I;
        set
        {
            Union32 union = AI_Union_1;
            union.I = value;
            AI_Union_1 = union;
        }
    }

    public abstract EaterofWorldsSegmentType SegmentType { get; }

    public sealed override int ApplyingType => SegmentType switch
    {
        EaterofWorldsSegmentType.Head => NPCID.EaterofWorldsHead,
        EaterofWorldsSegmentType.Body => NPCID.EaterofWorldsBody,
        EaterofWorldsSegmentType.Tail => NPCID.EaterofWorldsTail,
        _ => NPCID.None
    };

    private EaterofWorldsHead LifeOwner => ValidHead ? HeadBehavior : IsFirstSegment && SegmentType == EaterofWorldsSegmentType.Head
        ? EaterofWorldsHead.GetInstance(NPC) : null;

    public bool DeathLocked => LifeOwner?.LocksUltraLife == true;

    public override void Load() => BossDeathEffectRegistry.Register(ApplyingType,
        npc => GetInstance(npc) is { ShouldProcess: true, DeathLocked: true });

    public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers)
    {
        if (LifeOwner is EaterofWorldsHead { LocksUltraLife: true } owner)
            modifiers.SetMaxDamage(Math.Max(1, owner.NPC.life - owner.UltraEntryLife));
    }

    public override bool CheckDead()
    {
        if (!DeathLocked)
            return true;
        EaterofWorldsHead owner = LifeOwner;
        owner.NPC.life = Math.Max(owner.NPC.life, owner.UltraEntryLife);
        NPC.life = owner.NPC.life;
        NPC.active = true;
        if (TONetworkEnvironment.NotClient)
            owner.NPC.netUpdate = NPC.netUpdate = true;
        return false;
    }

    protected void UpdateCombatState()
    {
        // All links read one owner pose for visibility, protection and contact during this tick.
        EaterofWorldsHead owner = HeadBehavior;
        int localOrdinal = ValidLocalHead ? EaterofWorldsHead.GetInstance(LocalHead).SegmentIndex : 0;
        float opacity = GetOpacity();
        bool concealed = opacity < 1f || owner.CoilMaskStrength > 0f && NPC.Distance(owner.CoilingCenter) < 180f;
        NPC.Opacity = opacity;
        NPC.dontTakeDamage = owner.InUltraTransition;
        AnomalyNPC.DR = concealed ? 0.95f : 0f;
        NPC.damage = concealed || localOrdinal == 0 || !owner.NPC.HasValidTarget || owner.ContactSuppressed || !owner.ChainCanHit(localOrdinal)
            ? 0 : SegmentType switch
            {
                EaterofWorldsSegmentType.Head when owner.UltraCombat && !IsFirstSegment => EaterofWorldsHead.BodyContactDamage,
                EaterofWorldsSegmentType.Head => EaterofWorldsHead.HeadContactDamage,
                EaterofWorldsSegmentType.Body => EaterofWorldsHead.BodyContactDamage,
                _ => EaterofWorldsHead.TailContactDamage,
            };

        float GetOpacity()
        {
            if (owner.InUltraTransition)
            {
                return owner.CurrentLocalPhase == 1 ? Utils.GetLerpValue(0f, 24f, owner.Timer1, true)
                    : 1f - Utils.GetLerpValue(10f, 80f, owner.Timer1, true);
            }
            if (owner.CurrentBehavior != EaterofWorldsHead.Behavior.Phase1_Split)
                return 1f;
            if (owner.CurrentLocalPhase == 1)
            {
                int offset = localOrdinal == 0 ? 0 : SegmentIndex - localOrdinal;
                return 1f - Utils.GetLerpValue(offset * 1.5f + 4f, offset * 1.5f + 16f, owner.Timer1, true);
            }
            return owner.CurrentLocalPhase == 3 ? Utils.GetLerpValue(0f, 60f, NPC.Distance(owner.CoilingCenter), true) : 1f;
        }
    }

    protected void PrepareFollower()
    {
        UpdateCombatState();
        NPC.defense = 8;
        NPC.velocity = Vector2.Zero;
    }
}
