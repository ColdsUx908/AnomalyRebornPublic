namespace Anomalies.Bosses.Plantera.UnendingSpring;

public sealed partial class PlanteraUnending
{
    private void RunPhaseTwo(out bool moving, out float suppression)
    {
        if (CurrentBehavior == Behavior.Standoff && Timer >= StandoffTicks && TONetworkEnvironment.NotClient)
        {
            ClearAimedNeedles();
            EnterBehavior(Phase.Phase2, DrawAttack(Behavior.HookLightning, 4));
        }

        moving = CurrentBehavior == Behavior.Standoff;
        suppression = moving ? 0f : RosePattern.ClearNeedles;
        if (CurrentBehavior == Behavior.HookLightning && TONetworkEnvironment.NotClient)
        {
            UpdateHookFormation();
            if (LightningTick >= HookDuration + SilenceTicks)
            {
                EnterBehavior(Phase.Phase2, Behavior.Standoff);
                moving = true;
                suppression = 0f;
            }
        }
        if (CurrentBehavior == Behavior.ParadoxSector)
        {
            if (TONetworkEnvironment.NotClient)
            {
                moving = !UpdateParadox(NPC, (int)Timer);
                suppression = moving ? 0f : RosePattern.ClearNeedles;
            }
            else
            {
                PlaceParadoxBoss(NPC, (int)Timer);
                if (Timer >= SilenceTicks && Timer < ParadoxEnd)
                    AdvanceParadoxSteer();
            }
        }
        if (CurrentBehavior == Behavior.DashFan)
        {
            bool silent = UpdateDashRound(NPC, TONetworkEnvironment.NotClient);
            moving = !silent || CurrentDashStage == DashStage.Recovery;
            suppression = silent ? RosePattern.ClearNeedles : 0f;
        }
        if (CurrentBehavior == Behavior.PetalWheel && TONetworkEnvironment.NotClient)
        {
            if (Timer == SilenceTicks)
            {
                Vector2 center = FindOwned<PlanteraArena>()?.Center ?? NPC.Center;
                Projectile.NewProjectileAction(SourceAI, center, Vector2.Zero, ModContent.ProjectileType<PetalWheel>(),
                    AttackDamage, 0f, Main.myPlayer, p =>
                    {
                        p.ai[0] = NPC.whoAmI;
                        p.ai[1] = Main.rand.Next(1 << 23);
                    });
            }
            if (Timer == SilenceTicks + PetalWheel.HoldStart)
                ClearAimedNeedles();
            if (Timer >= SilenceTicks + PetalWheel.Duration + SilenceTicks)
            {
                EnterBehavior(Phase.Phase2, Behavior.Standoff);
                moving = true;
                suppression = 0f;
            }
        }
    }

    private void ClearAimedNeedles()
    {
        foreach (Projectile p in Main.ActiveProjectiles)
        {
            if (p.ai[0] == NPC.whoAmI && p.ModProjectile is AimedNeedle)
                p.Kill();
        }
    }
}