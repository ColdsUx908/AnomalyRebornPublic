namespace Anomalies.Bosses.Plantera.UnendingSpring;

public sealed partial class PlanteraUnending
{
    private void RunPhaseOne(Player target, out bool moving, out float suppression)
    {
        if (CurrentBehavior == Behavior.Opening && Timer >= OpeningTicks)
            EnterBehavior(Phase.Phase1, Behavior.Standoff);
        if (CurrentBehavior == Behavior.Standoff && Timer >= StandoffTicks && TONetworkEnvironment.NotClient)
            EnterBehavior(Phase.Phase1, DrawAttack(Behavior.CrossSeeds, 3));

        moving = CurrentBehavior is Behavior.Opening or Behavior.Standoff;
        suppression = moving && CurrentBehavior != Behavior.Opening && Timer >= 0 ? 0f : 1f;
        if (Timer < 0)
            moving = false;
        if (CurrentBehavior == Behavior.SeekingRing)
        {
            suppression = Timer < RingRelease ? 1f : 0f;
            moving = Timer > RingRelease + (RingCount - 1) * RingSpacing;
        }
        if (!TONetworkEnvironment.NotClient)
            return;

        switch (CurrentBehavior)
        {
            case Behavior.CrossSeeds:
                int fireTick = (int)Timer - SilenceTicks;
                if (fireTick >= 0 && fireTick <= 120 && fireTick % 40 == 0)
                {
                    int volley = fireTick / 40;
                    int count = 21 + 6 * volley;
                    float start = Main.rand.NextFloat(MathHelper.TwoPi);
                    for (int i = 0; i < count; i++)
                        SeedShot.Fire(SourceAI, NPC.Center, (start + i * MathHelper.TwoPi / count).ToRotationVector2() * 6.381375f,
                            AttackDamage, NPC.whoAmI, volley);
                }
                if (fireTick == 195)
                {
                    for (int i = 0; i < 4; i++)
                        Spawn<BurstThorn>((i * MathHelper.PiOver2).ToRotationVector2() * 6.125f);
                }
                if (fireTick > 195 && FindOwned<BurstThorn>() == null)
                {
                    EnterBehavior(Phase.Phase1, Behavior.Standoff);
                    Timer = 1 - SilenceTicks;
                }
                break;
            case Behavior.ButterflyFlowers:
                if (Timer == SilenceTicks)
                    Spawn<ButterflyFlowers>(Vector2.Zero, (target.Center - NPC.Center).ToRotation());
                if (Timer >= SilenceTicks)
                {
                    Projectile controller = FindOwned<ButterflyFlowers>();
                    if (controller?.ModProjectile is ButterflyFlowers flowers)
                        flowers.UpdateFlowers(NPC);
                    if (controller == null || !controller.active)
                    {
                        EnterBehavior(Phase.Phase1, Behavior.Standoff);
                        moving = true;
                        suppression = 0f;
                    }
                }
                break;
            case Behavior.SeekingRing:
                if (Timer == 0)
                {
                    float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                    float step = MathHelper.TwoPi / RingCount * (Main.rand.NextBool() ? 1 : -1);
                    for (int i = 0; i < RingCount; i++)
                        Spawn<SeekingRingSeed>(Vector2.Zero, angle + i * step, -(RingRelease + i * RingSpacing));
                }
                if (Timer >= RingRelease + RingCount * RingSpacing)
                {
                    EnterBehavior(Phase.Phase1, Behavior.Standoff);
                    Timer = RingCount * RingSpacing;
                }
                break;
        }
    }
}