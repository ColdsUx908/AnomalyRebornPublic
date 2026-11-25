namespace Anomalies.Bosses.Plantera.UnendingSpring;

public sealed partial class PlanteraUnending
{
    public override bool PreAI()
    {
        NPC.TargetClosest(false);
        if (!NPC.HasValidTarget || !NPC.HasPlayerTarget)
        {
            if ((uint)NPC.target < Main.maxPlayers)
                UpdatePursuit(Main.player[NPC.target]);
            NPC.EncourageDespawn(10);
            return false;
        }
        NPC.defense = NPC.life > NPC.lifeMax / 2 ? 36 : 10;
        NPC.damage = NPC.GetAttackDamage_ScaledByStrength(NPC.life > NPC.lifeMax / 2 ? 50f : 70f);
        EnsureHooks();
        Player target = Main.player[NPC.target];
        if (TONetworkEnvironment.NotClient && Vector2.DistanceSquared(target.Center, NPC.Center) < 14400f * 14400f)
            NPC.timeLeft = Math.Max(NPC.timeLeft, NPC.activeTime);
        if (CurrentPhase == Phase.Initialize)
            EnterBehavior(Phase.Phase1, Behavior.Opening);
        if (CurrentPhase == Phase.Phase1 && NPC.life <= NPC.lifeMax / 2 + 1 && TONetworkEnvironment.NotClient)
            BeginPhaseChange();

        bool moving = true;
        float suppression = 0f;
        if (CurrentPhase == Phase.PhaseChange_1To2)
        {
            if (!TryRunPhaseChange(out moving, out suppression))
                return false;
        }
        else if (CurrentPhase == Phase.Phase1)
            RunPhaseOne(target, out moving, out suppression);
        else if (CurrentPhase == Phase.Phase2)
            RunPhaseTwo(out moving, out suppression);

        if (moving)
            UpdatePursuit(target);
        else
        {
            if (CurrentBehavior != Behavior.ParadoxSector && (CurrentBehavior != Behavior.DashFan || CurrentDashStage == DashStage.ApproachEdge))
                NPC.rotation = (target.Center - NPC.Center).ToRotation() + 1.57f;
            NPC.velocity = Vector2.Zero;
        }
        if (TONetworkEnvironment.NotClient)
        {
            if (FindOwned<PlanteraArena>() == null)
                Spawn<PlanteraArena>(Vector2.Zero);
            UpdateRose(suppression);
        }
        Timer++;
        return false;
    }

    private int AttackDamage => Math.Max(1, NPC.defDamage / 2);

    private Projectile Spawn<T>(Vector2 velocity, float argument = 0f, float mode = 0f) where T : ModProjectile
    {
        Projectile result = null;
        Projectile.NewProjectileAction(SourceAI, NPC.Center, velocity, ModContent.ProjectileType<T>(), AttackDamage,
            0f, Main.myPlayer, p =>
            {
                p.ai[0] = NPC.whoAmI;
                p.ai[1] = argument;
                p.ai[2] = mode;
                result = p;
            });
        return result;
    }

    private Projectile FindOwned<T>() where T : ModProjectile
    {
        foreach (Projectile projectile in Main.ActiveProjectiles)
            if (projectile.ai[0] == NPC.whoAmI && projectile.ModProjectile is T)
                return projectile;
        return null;
    }

    private void UpdateRose(float suppression)
    {
        Projectile rose = FindOwned<RosePattern>();
        if (rose == null)
            Spawn<RosePattern>(Vector2.Zero, Main.rand.Next(360), suppression);
        else if (rose.ai[2] != suppression)
        {
            rose.ai[2] = suppression;
            rose.netUpdate = true;
        }
    }

    // ai[3] 的低四位保存本组剩余招式，高四位保存上一招编号加一。
    private Behavior DrawAttack(Behavior first, int count)
    {
        int state = (int)NPC.ai[3];
        int remaining = state & 15;
        int eligible = remaining;
        if (remaining == 0)
        {
            remaining = (1 << count) - 1;
            int previous = (state >> 4) - 1;
            eligible = previous >= 0 ? remaining & ~(1 << previous) : remaining;
        }
        int index;
        do
            index = Main.rand.Next(count);
        while ((eligible & (1 << index)) == 0);
        NPC.ai[3] = (remaining & ~(1 << index)) | ((index + 1) << 4);
        return (Behavior)((int)first + index);
    }
}
