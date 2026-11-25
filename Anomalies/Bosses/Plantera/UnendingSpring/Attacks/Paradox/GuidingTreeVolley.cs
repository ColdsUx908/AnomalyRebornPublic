using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Anomalies.Bosses.Plantera.UnendingSpring;

// One synchronized projectile carries an entire forward volley from the rear hook.
public sealed class GuidingTreeVolley : ModProjectile
{
    private const int StartTick = 60, OrbitWays = 6;
    private const float Interval = 230.4f;
    private const int Warmup = 8, FadeTicks = 24;
    private const int BoostTicks = 150, BrakeTicks = 60;
    private const float JitterDegrees = 45f, Speed = 2.8f, OrbitRadius = 128f;
    private const float NinthBandMidpoint = ParadoxSector.Reach * 8.5f / ParadoxSector.Bands;
    private const float ThornHeight = 54f, SeedHeight = 30f;
    private const float ThornHitRadius = 5.85f, SeedHitRadius = 2.34f;
    private static readonly Color[] _SeedColors = { new(202, 128, 255), new(102, 235, 112), new(255, 222, 78), new(255, 90, 105) };
    private static readonly Color _ThornColor = new(255, 90, 105);
    private Vector2[] _Rays = Array.Empty<Vector2>();
    private int[] _FadeStart = Array.Empty<int>();
    private int _Ways;
    private readonly Vector2[] _OrbitOffsets = new Vector2[OrbitWays * 2];
    private readonly float[] _OrbitAngles = new float[OrbitWays * 2];
    private bool _Prepared;
    private float _Distance;
    private int Age => (int)Projectile.ai[2] - 1;
    private int Side => Math.Sign(Projectile.ai[1]);
    private Color SeedColor => _SeedColors[((int)MathF.Abs(Projectile.ai[1]) - 1) % _SeedColors.Length];
    public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.ThornBall}";

    private static int WaveAt(int tick) => (int)MathF.Round((tick - StartTick) / Interval);
    internal static bool IsDue(int tick) => tick >= StartTick &&
        tick == StartTick + (int)MathF.Round(WaveAt(tick) * Interval);

    internal static void Fire(NPC boss, Vector2 muzzle, int tick, int side, ref float gridPhase)
    {
        int height = boss.HasPlayerTarget && boss.HasValidTarget
            ? Main.player[boss.target].height : PlanteraUnending.StandingPlayerHeight;
        // Next whole pixel strictly above 1.5 player heights, even for integral thresholds.
        float clearance = (3L * height) / 2 + 1;
        float step = MathHelper.ToDegrees(2f * MathF.Asin(Math.Min(1f, (2f * OrbitRadius + clearance) / (2f * NinthBandMidpoint))));
        int wave = WaveAt(tick);
        if (wave % 3 == 0) gridPhase = Main.rand.NextFloat();
        // Only choose grid lanes inside the aim sector, including its partial edge lanes.
        float phase = ((gridPhase + (wave % 3) / 3f) % 1f) * Math.Min(step, JitterDegrees);
        int lane = Main.rand.Next(1 + (int)MathF.Floor((JitterDegrees - phase) / step));
        float jitter = phase + lane * step - JitterDegrees * 0.5f;
        Vector2 aim = new Vector2(side, 0).RotatedBy(MathHelper.ToRadians(jitter));
        // This stationary controller uses its synchronized vector for aim and angular spacing.
        Projectile.NewProjectileAction(boss.GetSource_FromAI(), muzzle, aim * step,
            ModContent.ProjectileType<GuidingTreeVolley>(), Math.Max(1, boss.defDamage / 2), 0f,
            Main.myPlayer, p => { p.ai[0] = boss.whoAmI; p.ai[1] = side * (1 + wave); });
    }

    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] =
        (int)(2f * PlanteraArena.Radius + OrbitRadius + 160f);
    public override bool ShouldUpdatePosition() => false;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 8;
        Projectile.hostile = true;
        Projectile.penetrate = -1;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = false;
        Projectile.netImportant = true;
        Projectile.timeLeft = 2;
    }

    private void Prepare()
    {
        if (_Prepared) return;
        Vector2 fromCenter = new(-Side * PlanteraArena.Radius, 0);
        Vector2 forward = new(Side, 0);
        float jitter = MathHelper.ToDegrees(MathHelper.WrapAngle(Projectile.velocity.ToRotation() - forward.ToRotation()));
        float step = Projectile.velocity.Length();
        // Clip the equally spaced grid at both tangents so edge lanes have the same spacing.
        int firstLane = (int)MathF.Ceiling((-90f - jitter) / step);
        int endLane = (int)MathF.Ceiling((90f - jitter) / step);
        _Ways = endLane - firstLane;
        _Rays = new Vector2[_Ways];
        _FadeStart = new int[_Ways];
        for (int i = 0; i < _Ways; i++)
        {
            float offset = jitter + (firstLane + i) * step;
            Vector2 ray = forward.RotatedBy(MathHelper.ToRadians(offset));
            _Rays[i] = ray;
            float dot = Vector2.Dot(fromCenter, ray);
            float exit = -dot + MathF.Sqrt(dot * dot + PlanteraArena.Radius * PlanteraArena.Radius - fromCenter.LengthSquared());
            int low = 0, high = (int)MathF.Ceiling(2f * PlanteraArena.Radius / Speed) + 1;
            while (high - low > 1)
            {
                int mid = (low + high) / 2;
                float travelled = DistanceAt(mid);
                if (travelled >= PlanteraArena.Radius && travelled > exit) high = mid;
                else low = mid;
            }
            _FadeStart[i] = high;
        }
        _Prepared = true;
    }

    public override void AI()
    {
        NPC boss = Main.npc[(int)Projectile.ai[0]];
        if (!boss.active || boss.type != NPCID.Plantera) { Projectile.Kill(); return; }
        Prepare();
        Projectile.ai[2]++;
        _Distance = DistanceAt(Age);
        UpdateOrbit();
        bool any = false;
        for (int i = 0; i < _Ways; i++)
        {
            int end = _FadeStart[i] + FadeTicks - 1;
            if (Age < end) any = true;
            else if (Age == end && !Main.dedServ) Burst(Position(i));
        }
        if (!any) { Projectile.Kill(); return; }
        Projectile.timeLeft = 2;
    }

    private static float DistanceAt(int age)
    {
        int fast = Math.Clamp(age, 0, BoostTicks);
        int braking = Math.Clamp(age - BoostTicks, 0, BrakeTicks);
        int cruise = Math.Max(0, age - BoostTicks - BrakeTicks);
        // Sum the linear speed ramp. Reconstructing from age also handles late joins.
        return Speed * (2f * fast + 2f * braking - braking * (braking + 1f) / (2 * BrakeTicks) + cruise);
    }
    private Vector2 Position(int i) => Projectile.Center + _Rays[i] * _Distance;
    private bool Visible(int i) => Age < _FadeStart[i] + FadeTicks - 1;
    private void UpdateOrbit()
    {
        float radius = OrbitRadius * MathF.Sin(MathHelper.ToRadians(Math.Min(90, Age + 1)));
        for (int j = 0; j < _OrbitOffsets.Length; j++)
        {
            int sign = j % 2 == 0 ? 1 : -1;
            float degrees = -Side * 90f + (j / 2) * (360f / OrbitWays) + sign * 0.215475f * (Age + 1);
            _OrbitAngles[j] = MathHelper.ToRadians(degrees);
            _OrbitOffsets[j] = _OrbitAngles[j].ToRotationVector2() * radius;
        }
    }
    private static bool Hits(Vector2 p, Rectangle target, float radius) =>
        Vector2.DistanceSquared(p, Vector2.Clamp(p, target.TopLeft(), target.BottomRight())) <= radius * radius;

    public override bool? Colliding(Rectangle projHitbox, Rectangle target)
    {
        if (!_Prepared || Age < Warmup) return false;
        for (int i = 0; i < _Ways; i++)
        {
            if (!Visible(i)) continue;
            Vector2 p = Position(i);
            if (Hits(p, target, ThornHitRadius)) return true;
            if (!Hits(p, target, OrbitRadius + SeedHitRadius)) continue;
            for (int j = 0; j < OrbitWays * 2; j++)
                if (Hits(p + _OrbitOffsets[j], target, SeedHitRadius)) return true;
        }
        return false;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        if (!_Prepared) return false;
        float warmup = Math.Min(1f, (Age + 1) / (float)Warmup);
        for (int i = 0; i < _Ways; i++)
        {
            if (!Visible(i)) continue;
            Vector2 p = Position(i) - Main.screenPosition;
            const float Margin = OrbitRadius + SeedHeight;
            if (p.X < -Margin || p.Y < -Margin || p.X > Main.screenWidth + Margin || p.Y > Main.screenHeight + Margin) continue;
            float opacity = 1f - Math.Max(0, Age - _FadeStart[i] + 1) / (float)FadeTicks;
            SeedSprites.DrawSpellShot(ProjectileID.ThornBall, _ThornColor, Age, p, _Rays[i].ToRotation() + Age * 0.035f,
                ThornHeight, opacity * warmup);
            for (int j = 0; j < OrbitWays * 2; j++)
            {
                Vector2 seed = p + _OrbitOffsets[j];
                SeedSprites.DrawSpellShot(ProjectileID.SeedPlantera, SeedColor, Age, seed, _OrbitAngles[j] + MathHelper.Pi, SeedHeight, warmup);
            }
        }
        return false;
    }

    private void Burst(Vector2 p)
    {
        ClearParticles.Burst(p, _ThornColor);
        for (int j = 0; j < OrbitWays * 2; j++)
            ClearParticles.Burst(p + _OrbitOffsets[j], SeedColor, 3);
    }
    public override void OnKill(int timeLeft)
    {
        if (Main.dedServ || !_Prepared) return;
        for (int i = 0; i < _Ways; i++)
            if (Visible(i)) Burst(Position(i));
    }
}