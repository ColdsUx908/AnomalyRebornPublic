using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Anomalies.Bosses.Plantera.UnendingSpring;

// A fan shares one projectile slot. Its individual seeds have identical trajectories
// for drawing and damage, including on clients joining after the orb has vanished.
public sealed class DashFan : ModProjectile
{
    internal const float Range = 5500f;
    private const int OrbTicks = 60, Interval = 3, Waves = OrbTicks / Interval, Lanes = 29;
    private Vector2[] _Velocities;
    private float[] _Angles;
    private int _Age = -1;
    public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.PoisonSeedPlantera;
    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = (int)Range + 128;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 12;
        Projectile.hostile = true;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.netImportant = true;
        Projectile.timeLeft = 250;
    }
    public override bool ShouldUpdatePosition() => false;
    public override void SendExtraAI(BinaryWriter writer) => writer.Write((short)_Age);
    public override void ReceiveExtraAI(BinaryReader reader) => _Age = reader.ReadInt16();
    internal static bool ActiveOwner(Projectile p)
    {
        int owner = (int)p.ai[0];
        if ((uint)owner >= Main.maxNPCs) return false;
        NPC boss = Main.npc[owner];
        return boss.active && boss.type == NPCID.Plantera && PlanteraUnending.GetInstance(boss) is { ShouldProcess: true, CurrentBehavior: PlanteraUnending.Behavior.DashFan };
    }
    private void Prepare()
    {
        if (_Velocities != null) return;
        _Velocities = new Vector2[Waves * Lanes];
        _Angles = new float[_Velocities.Length];
        Random random = new Random((int)Projectile.ai[2]);
        float[] gaps = new float[Lanes - 1];
        for (int wave = 0; wave < Waves; wave++)
        {
            // Normalize irregular gaps so every volley still reaches both warning lines.
            float sum = 0f;
            for (int i = 0; i < gaps.Length; i++)
            {
                gaps[i] = 0.25f + 1.5f * random.NextSingle();
                sum += gaps[i];
            }
            float offset = 0f;
            for (int lane = 0; lane < Lanes; lane++)
            {
                float angle = Projectile.ai[1] + MathHelper.Lerp(-PlanteraUnending.HalfFanAngle,
                    PlanteraUnending.HalfFanAngle, offset / sum);
                int index = wave * Lanes + lane;
                _Angles[index] = angle;
                _Velocities[index] = angle.ToRotationVector2() * (30f + 12f * random.NextSingle());
                if (lane < gaps.Length) offset += gaps[lane];
            }
        }
    }
    public override void AI()
    {
        if (!ActiveOwner(Projectile)) { Projectile.Kill(); return; }
        Prepare();
        _Age++;
        if (Main.netMode != NetmodeID.Server)
        {
            if (_Age == OrbTicks) ClearParticles.Burst(Projectile.Center, DashVisuals.Green, 12);
            for (int wave = 0; wave < Waves; wave++)
            {
                int steps = _Age - wave * Interval + 1;
                if (steps * 42f <= Range || (steps - 1) * 30f > Range) continue;
                for (int i = wave * Lanes; i < (wave + 1) * Lanes; i++)
                    if (!Position(i, steps, out _) && Position(i, steps - 1, out Vector2 end))
                        ClearParticles.Burst(end, DashVisuals.Green, 3);
            }
        }
        if (_Age >= (Waves - 1) * Interval + (int)MathF.Ceiling(Range / 30f)) Projectile.Kill();
    }

    public override void OnKill(int timeLeft)
    {
        if (Main.netMode == NetmodeID.Server || _Velocities == null) return;
        if (_Age < OrbTicks) ClearParticles.Burst(Projectile.Center, DashVisuals.Green, 12);
        for (int wave = 0; wave < Waves && wave * Interval <= _Age; wave++)
        {
            int steps = _Age - wave * Interval + 1;
            for (int i = wave * Lanes; i < (wave + 1) * Lanes; i++)
                if (Position(i, steps, out Vector2 at)) ClearParticles.Burst(at, DashVisuals.Green, 3);
        }
    }
    private bool Position(int index, int steps, out Vector2 center)
    {
        Vector2 displacement = _Velocities[index] * steps;
        center = Projectile.Center + displacement;
        return displacement.LengthSquared() <= Range * Range;
    }
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        if (_Velocities == null) return false;
        for (int wave = 0; wave < Waves && wave * Interval <= _Age; wave++)
        {
            int steps = _Age - wave * Interval + 1;
            for (int i = wave * Lanes; i < (wave + 1) * Lanes; i++)
            {
                if (!Position(i, steps, out Vector2 at)) continue;
                Vector2 previous = at - _Velocities[i];
                if (RotatedRectangle.FromLineSegment(previous, at, 12.5f).Collides(targetHitbox)) return true;
            }
        }
        return false;
    }
    public override bool PreDraw(ref Color lightColor)
    {
        if (_Velocities == null) return false;
        Vector2 screen = Main.screenPosition;
        Vector2 halfView = Main.Camera.ScaledSize / 2f + new Vector2(130f);
        Vector2 viewCenter = screen + new Vector2(Main.screenWidth, Main.screenHeight) / 2f;
        for (int wave = 0; wave < Waves && wave * Interval <= _Age; wave++)
        {
            int steps = _Age - wave * Interval + 1;
            for (int i = wave * Lanes; i < (wave + 1) * Lanes; i++)
            {
                if (!Position(i, steps, out Vector2 at) || MathF.Abs(at.X - viewCenter.X) > halfView.X ||
                    MathF.Abs(at.Y - viewCenter.Y) > halfView.Y) continue;
                Vector2 tip = at - screen, trail = _Velocities[i] * Math.Min(steps, 2);
                DashVisuals.Line(tip - trail, tip - trail * 0.45f, DashVisuals.Green * 0.25f, 8f);
                DashVisuals.Line(tip - trail * 0.45f, tip, DashVisuals.Green * 0.65f, 14f);
                SeedSprites.DrawSpellShot(ProjectileID.PoisonSeedPlantera, DashVisuals.Green, steps,
                    tip, _Angles[i] + MathHelper.PiOver2, 26.25f, 1f);
            }
        }
        if (_Age < OrbTicks) DashVisuals.DrawOrb(Projectile.Center - screen, _Age);
        return false;
    }
}