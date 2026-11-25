namespace Anomalies.Bosses.Plantera.UnendingSpring;

public sealed partial class PlanteraUnending
{
    private void BeginPhaseChange()
    {
        foreach (Projectile p in Main.ActiveProjectiles)
        {
            if (p.ai[0] == NPC.whoAmI && p.ModProjectile is AimedNeedle or SeedShot or SeekingRingSeed or BurstThorn or ButterflyFlowers)
                p.Kill();
        }
        EnterBehavior(Phase.PhaseChange_1To2, Behavior.TransitionSpell);
    }

    private bool TryRunPhaseChange(out bool moving, out float suppression)
    {
        moving = true;
        suppression = 0f;
        NPC.dontTakeDamage = true;
        NPC.life = HeldLife;
        NPC.lifeRegenCount = 0;
        if (Timer >= TransitionSpell.FirstRingTick && NPC.localAI[2] == 0f)
        {
            NPC.localAI[2] = 1f;
            if (!Main.dedServ)
            {
                for (int type = 378; type <= 380; type++)
                {
                    Vector2 position = NPC.position + new Vector2(Main.rand.Next(NPC.width), Main.rand.Next(NPC.height));
                    Gore.NewGoreAction(SourceAI, position, NPC.velocity, type, gore => gore.scale = NPC.scale);
                }
            }
        }
        if (Timer == 0 && TONetworkEnvironment.NotClient && Spawn<TransitionSpell>(Vector2.Zero, Main.rand.Next(1 << 23)) == null)
            return false;
        if (Timer >= TransitionSpell.StartActingTick && TONetworkEnvironment.NotClient)
        {
            NPC.ai[3] = 0f;
            NPC.dontTakeDamage = false;
            EnterBehavior(Phase.Phase2, Behavior.Standoff);
        }
        else
        {
            moving = false;
            suppression = RosePattern.ClearNeedles;
        }
        return true;
    }
}
