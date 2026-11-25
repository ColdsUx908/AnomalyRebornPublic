namespace Anomalies.Bosses.Plantera.UnendingSpring;

public sealed partial class PlanteraUnending
{
    private Vector2 ToWorld(Vector2 local) => new(FallSide * local.Y, -FallSide * local.X);
    private Vector2 Rim => ParadoxCenter - Vector2.UnitX * (FallSide * PlanteraArena.Radius);
    private Vector2 SideTip(int side) => new(side * (HookOffset + Math.Min(MaxHookShift, HookLeadTicks * Steer)), TipDepth);
    private Vector2 VinePoint(int side, float t) =>
        2f * (1f - t) * t * new Vector2(side * 92f, 154f) + t * t * SideTip(side);
    private static float Extension(int time) => MathHelper.SmoothStep(0f, 1f,
        Math.Clamp((time - 44f) / 75f, 0f, 1f));

    private static float StrandSpeed(float modulation, int strand) =>
        (0.8f - (1f - PassageRatio) * (0.8f - MathF.Abs(modulation)) + strand * modulation) * ParadoxScale;

    // With four ticks between shots the 5-degree clock samples angles spaced by 20 degrees.
    private static readonly float _PeakModulation = 0.25f * MathF.Sin(MathHelper.ToRadians(80f));
    private static readonly float _InnerSpeed = StrandSpeed(_PeakModulation, -1);
    private static readonly float _OuterSpeed = StrandSpeed(_PeakModulation, 1);
    private static readonly float _CrossingSpeed = StrandSpeed(0f, 1);

    private float BlindSpotAim(Player player)
    {
        Vector2 offset = player.Center - ParadoxCenter;
        if (offset.LengthSquared() > PlanteraArena.Radius * PlanteraArena.Radius) return 0f;
        float playerX = FallSide * offset.X + PlanteraArena.Radius - ParadoxInset - TipDepth;
        float height = MathF.Abs(offset.Y);
        float halfX = (player.width + ParadoxSeed.SeedSize) * 0.5f;
        float halfY = (player.height + ParadoxSeed.SeedSize) * 0.5f;
        float x = 0f, vx = ForwardLaunch;
        int aimAge = 0;
        // A column near the launch edge can be crossed twice. Check both passes
        // against neutral trajectories, independently of any current adjustment.
        for (int age = 1; x <= 2f * PlanteraArena.Radius; age++)
        {
            vx = ParadoxSeed.NextForwardSpeed(vx);
            x += vx;
            if (age < ParadoxSeed.Warmup || MathF.Abs(x - playerX) > halfX) continue;
            float inner = HookOffset + _InnerSpeed * age - halfY;
            float outer = HookOffset + _OuterSpeed * age + halfY;
            if (height >= inner && height <= outer) return 0f;
            if (height > outer) aimAge = age;
        }
        if (aimAge == 0) return 0f; // Inside the normal central passage.
        // Put the zero-phase crossing of this chain through the target. A small
        // part of the correction moves the visible tip; the rest changes launch speed.
        float extra = height - (HookOffset + _CrossingSpeed * aimAge);
        float shift = Math.Min(MaxHookShift, HookLeadTicks * extra / (aimAge + HookLeadTicks));
        return (extra - shift) / aimAge;
    }

    private void AdvanceParadoxSteer() => Steer += Math.Clamp(SteerGoal - Steer, -0.32f, 0.32f);

    private void UpdateParadoxSteer(NPC boss, int tick)
    {
        float aim = 0f;
        if (boss.HasPlayerTarget && boss.HasValidTarget && !Main.player[boss.target].dead && !Main.player[boss.target].ghost)
            aim = BlindSpotAim(Main.player[boss.target]);
        if (BlindPlayer != boss.target || aim == 0f) BlindTicks = 0;
        BlindPlayer = boss.target;
        if (aim > 0f) BlindTicks++;
        SteerGoal = BlindTicks >= BlindSpotDelay ? aim : 0f;
        AdvanceParadoxSteer();
        if ((LastSyncedGoal == 0f) != (SteerGoal == 0f) ||
            (tick % 8 == 0 && MathF.Abs(SteerGoal - LastSyncedGoal) > 0.01f))
        {
            LastSyncedGoal = SteerGoal;
            boss.netUpdate = true;
        }
    }

    private void PlaceParadoxBoss(NPC boss, int time)
    {
        if (time < 0 || time >= ParadoxEnd) return;
        float progress = Math.Clamp((time) / 89f, 0f, 1f);
        boss.Center = Vector2.SmoothStep(ParadoxStart, Rim + ToWorld(new Vector2(0, ParadoxInset)), progress);
        boss.rotation = FallSide * MathHelper.PiOver2;
        boss.velocity = Vector2.Zero;
    }

    private bool PlaceParadoxHook(NPC boss, NPC hook, int time)
    {
        int i = (int)hook.ai[3] - 1;
        if (i < 0 || time < 0 || time >= ParadoxEnd) return false;
        int age = time;
        if (i == 0)
            hook.Center = Vector2.SmoothStep(AnomalyNPC.AnomalyAI64[4].GetValue<Vector2>(), Rim, Math.Clamp(age / 119f, 0f, 1f));
        else if (age < 44)
            hook.Center = Vector2.SmoothStep(AnomalyNPC.AnomalyAI64[4 + i].GetValue<Vector2>(), boss.Center, age / 44f);
        else
            hook.Center = boss.Center + ToWorld(VinePoint(i == 1 ? -1 : 1, Extension(time)));
        int side = i == 1 ? -1 : 1;
        Vector2 facing = i == 0 || age <= 44 ? boss.Center - hook.Center :
            ToWorld(VinePoint(side, Math.Max(0, Extension(time) - 0.01f)) - VinePoint(side, Extension(time)));
        hook.rotation = facing.ToRotation() - MathHelper.PiOver2;
        hook.velocity = Vector2.Zero;
        hook.localAI[3] = 0f;
        return true;
    }

    // Draw the stock vine texture along the deployed arm, including its downward bend.
}
