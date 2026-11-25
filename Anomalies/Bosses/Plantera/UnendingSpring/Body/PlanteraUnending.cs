namespace Anomalies.Bosses.Plantera.UnendingSpring;

public sealed partial class PlanteraUnending : AnomalyNPCBehavior<PlanteraUnending>
{
    private const int OpeningTicks = 180;
    private const int StandoffTicks = 510;
    private const int SilenceTicks = 120;
    private const int RingCount = 60;
    private const int RingSpacing = 5;
    private const int RingRelease = SeekingRingSeed.FormationTicks + 60;

    public override int ApplyingType => NPCID.Plantera;
    public override bool ShouldProcess => base.ShouldProcess && AnomalyBossChangeToggler.UnendingSpring && AnomalyNPC.CustomAIType == 1;
    public override bool AllowCalamityLogic(CalamityLogicType_NPCBehavior type) => type != CalamityLogicType_NPCBehavior.VanillaOverrideAI;
    internal bool HasGreenLaser => CurrentPhase == Phase.Phase2 || CurrentPhase == Phase.PhaseChange_1To2 && Timer >= TransitionSpell.FirstRingTick;
    internal bool PetalAiming => CurrentBehavior == Behavior.PetalWheel && Timer >= SilenceTicks + PetalWheel.SpinStart && Timer < SilenceTicks + PetalWheel.HoldStart;
    private bool DeathLocked => CurrentPhase != Phase.Phase2;
    private int HeldLife => Math.Max(1, NPC.lifeMax / 2 + (HasGreenLaser ? -1 : 1));

    public override void Load() => BossDeathEffectRegistry.Register(NPCID.Plantera,
        npc => GetInstance(npc) is { ShouldProcess: true, DeathLocked: true });

    public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers)
    {
        if (DeathLocked)
            modifiers.SetMaxDamage(Math.Max(1, NPC.life - HeldLife));
    }

    private bool CanDealContactDamage => !(CurrentPhase == Phase.PhaseChange_1To2 && HasGreenLaser)
        && !(CurrentBehavior == Behavior.DashFan && CurrentDashStage == DashStage.ApproachEdge)
        && !(CurrentBehavior == Behavior.ParadoxSector && Timer < SilenceTicks);

    public override bool CanHitPlayer(Player target, ref int cooldownSlot) => CanDealContactDamage;

    public override bool CanHitNPC(NPC target) => CanDealContactDamage;

    public override bool CheckDead()
    {
        if (!DeathLocked)
            return true;
        NPC.life = HeldLife;
        NPC.dontTakeDamage = true;
        NPC.netUpdate = true;
        return false;
    }

    internal int ArenaRecenterTicks => CurrentBehavior switch
    {
        Behavior.Standoff when CurrentPhase == Phase.Phase2 || Timer >= 0f => 0,
        Behavior.SeekingRing when Timer >= RingRelease => 0,
        Behavior.SeekingRing or Behavior.PetalWheel => SilenceTicks,
        Behavior.DashFan or Behavior.ParadoxSector => 0,
        _ => 180,
    };
}
