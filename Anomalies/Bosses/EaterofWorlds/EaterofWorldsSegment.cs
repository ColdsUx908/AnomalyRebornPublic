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

    protected int GetContactDamage()
    {
        if (!ValidHead)
            return 0;
        EaterofWorldsHead owner = HeadBehavior;
        if (owner.ContactSuppressed || !ValidLocalHead || !owner.ChainCanHit(EaterofWorldsHead.GetInstance(LocalHead).SegmentIndex))
            return 0;
        if (IsUnderCoilMask)
            return 0;
        if (PortalOpacity < 1f)
            return 0;
        return SegmentType switch
        {
            EaterofWorldsSegmentType.Head when owner.UltraCombat && !IsFirstSegment => EaterofWorldsHead.BodyContactDamage,
            EaterofWorldsSegmentType.Head => EaterofWorldsHead.HeadContactDamage,
            EaterofWorldsSegmentType.Body => EaterofWorldsHead.BodyContactDamage,
            _ => EaterofWorldsHead.TailContactDamage,
        };
    }

    protected float PortalOpacity
    {
        get
        {
            if (!ValidHead)
                return 1f;
            EaterofWorldsHead owner = HeadBehavior;
            if (owner.InUltraTransition)
            {
                if (owner.CurrentLocalPhase == 1)
                    return Utils.GetLerpValue(0f, 24f, owner.Timer1, true);
                return 1f - Utils.GetLerpValue(10f, 80f, owner.Timer1, true);
            }
            if (owner.CurrentBehavior != EaterofWorldsHead.Behavior.Phase1_Split)
                return 1f;
            if (owner.CurrentLocalPhase == 1)
            {
                int offset = ValidLocalHead ? SegmentIndex - EaterofWorldsHead.GetInstance(LocalHead).SegmentIndex : 0;
                return 1f - Utils.GetLerpValue(offset * 1.5f + 4f, offset * 1.5f + 16f, owner.Timer1, true);
            }
            if (owner.CurrentLocalPhase == 3)
            {
                // Visibility follows actual emergence, including FTW spacing and scaled segments.
                return Utils.GetLerpValue(0f, 60f, NPC.Distance(owner.CoilingCenter), true);
            }
            return 1f;
        }
    }

    protected bool IsUnderCoilMask => ValidHead && HeadBehavior.CoilMaskStrength > 0f
        && NPC.Distance(HeadBehavior.CoilingCenter) < 180f;

    protected void UpdateCoilDR()
    {
        NPC.dontTakeDamage = ValidHead && HeadBehavior.InUltraTransition;
        NPC.Opacity = PortalOpacity;
        AnomalyNPC.DR = PortalOpacity < 1f || IsUnderCoilMask ? 0.95f : 0f;
    }

    protected void BodyAndTailMovementAI()
    {
        NPC.damage = GetContactDamage();
        NPC.defense = 8;
        if (ValidLocalHead && EaterofWorldsHead.GetInstance(LocalHead).CircularMovementActive)
        {
            // The encounter owner places the entire local chain on its common circle.
            NPC.velocity = Vector2.Zero;
            return;
        }
        if (ValidHead && ValidLocalHead && SegmentIndex > EaterofWorldsHead.UltraLongWormSegmentCount
            && (HeadBehavior.UltraCombat || HeadBehavior.InUltraTransition && HeadBehavior.CurrentLocalPhase == 1))
        {
            // A three-link lane stays straight, including its tail while staging or recovering.
            EaterofWorldsHead local = EaterofWorldsHead.GetInstance(LocalHead);
            float linkSpacing = Main.getGoodWorld ? 62f : NPC.width * NPC.scale;
            NPC.Center = LocalHead.Center - local.LaunchDirection * ((SegmentIndex - local.SegmentIndex) * linkSpacing);
            NPC.rotation = local.LaunchDirection.ToRotation() + MathHelper.PiOver2;
            NPC.velocity = Vector2.Zero;
            return;
        }
        Vector2 delta = Previous.Center - NPC.Center;
        float spacing = Main.getGoodWorld ? 62f : NPC.width * NPC.scale;
        Vector2 direction = delta.SafeNormalize(Vector2.UnitY);
        // Pull only: pushing compressed segments apart unfolds a coil instantly, even with a stationary head.
        NPC.velocity = direction * Math.Max(0f, delta.Length() - spacing);
        if (HeadBehavior.CurrentBehavior == EaterofWorldsHead.Behavior.Phase1_Split
            && delta.Length() <= spacing + Previous.velocity.Length() + 8f)
        {
            // Follow the predecessor's travelled arc, not a chord cutting across its tight orbit.
            // Otherwise the inner segments stop moving and the distant tail can never enter the coil.
            Vector2 point = Previous.Center;
            float remaining = spacing;
            foreach (Vector2 oldPosition in Previous.oldPos)
            {
                Vector2 past = oldPosition + Previous.Size * 0.5f;
                float distance = Vector2.Distance(point, past);
                if (distance >= remaining && distance > 0.001f)
                {
                    Vector2 destination = Vector2.Lerp(point, past, remaining / distance);
                    Vector2 movement = destination - NPC.Center;
                    float speed = movement.Length();
                    NPC.velocity = movement.SafeNormalize(Vector2.Zero) * Math.Min(speed, 40f);
                    break;
                }
                remaining -= distance;
                point = past;
            }
        }
        NPC.rotation += MathHelper.WrapAngle(direction.ToRotation() + MathHelper.PiOver2 - NPC.rotation) * 0.28f;
    }
}
