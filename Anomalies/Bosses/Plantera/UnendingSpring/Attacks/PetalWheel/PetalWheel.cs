using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace Anomalies.Bosses.Plantera.UnendingSpring;

public sealed partial class PetalWheel : ModProjectile
{
    private const int Rings = 6, Petals = 12, Layers = 16, GuideSteps = 3 * Layers;
    private const int LaserStart = 60;
    private const int FlowerStart = 120;
    internal const int SpinStart = FlowerStart + 60;
    private const float SpinDegreesPerSecond = 10.125f;
    // The first alignment after 10 seconds occurs at 105 degrees per color.
    private const float AlignedDegrees = 105f;
    internal static readonly int HoldStart = SpinStart + (int)MathF.Ceiling(AlignedDegrees * 60f / SpinDegreesPerSecond);
    private static readonly int _SpreadStart = HoldStart + 60;
    internal static readonly int Duration = _SpreadStart + 60;
    private const float RingSpacing = 18f, LaserWidth = 10f, HitRadius = 9.5f;
    private const float Inner = UnendingSpringPaths.PetalTipRadius;
    private const float Outer = Inner + (Rings - 1) * RingSpacing;
    private const float Pitch = MathHelper.TwoPi / Petals;
    private const int Stations = 2 * Layers;
    private const int FlowersPerPetal = Stations + 1; // One additional, opposite-color tip.
    private const float SpreadSpeed = 5f, MaxRadius = PlanteraArena.Radius + 480f;
    private const float MaxSpreadAngle = MathHelper.Pi / 12f;
    private readonly record struct Flower(Vector2 Offset, bool Pink);
    private Flower[] _Flowers;
    private Vector2[] _SpreadDirections;
    private Vector2[] _Positions;
    private Vector2[,,] _Guides;
    private Vector2[][] _FlowerLines;
    private Vector2 _Rotation = Vector2.UnitX;
    private int Age => (int)Projectile.ai[2] - 1;
    private int Seed => (int)Projectile.ai[1];
    private float Direction => (Seed & 2) == 0 ? 1f : -1f;
    private bool PinkFirst => (Seed & 1) == 0;
    private static Color Tint(bool pink) => pink ? DashVisuals.Pink : DashVisuals.Green;
    public override string Texture => "Anomalies/Bosses/Plantera/UnendingSpring/Assets/Flower";

    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = (int)MaxRadius + 80;
    public override bool ShouldUpdatePosition() => false;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 8;
        Projectile.hostile = true;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.netImportant = true;
        float minOutwardSpeed = SpreadSpeed * MathF.Cos(MaxSpreadAngle);
        Projectile.timeLeft = _SpreadStart + (int)MathF.Ceiling((MaxRadius - Outer) / minOutwardSpeed) + 3;
    }

    // Both sides start at the same point on the sixth ring and join at the arena boundary.
    public override void AI()
    {
        NPC boss = Main.npc[(int)Projectile.ai[0]];
        if (!boss.active || boss.type != NPCID.Plantera) { Projectile.Kill(); return; }
        // The stationary boss placed the wheel at its center. Keep that world
        // origin when the boss starts chasing again during the flowers' flight.
        Prepare();
        Projectile.ai[2]++;
        if (Age == Duration) BurstRings();
        if (Age < FlowerStart) return;
        float turn = Direction * MathHelper.ToRadians(Math.Clamp((Age - SpinStart) * SpinDegreesPerSecond / 60f, 0f, AlignedDegrees));
        _Rotation = turn.ToRotationVector2();
        float expansion = Math.Max(0, Age - _SpreadStart) * SpreadSpeed;
        bool any = false;
        for (int i = 0; i < _Flowers.Length; i++)
        {
            _Positions[i] = Rotate(_Flowers[i].Offset, _Flowers[i].Pink) + expansion * _SpreadDirections[i];
            if (InRange(i)) any = true;
            else if ((_Positions[i] - SpreadSpeed * _SpreadDirections[i]).LengthSquared() <= MaxRadius * MaxRadius)
                ClearParticles.Burst(Projectile.Center + _Positions[i], Tint(_Flowers[i].Pink), 3);
        }
        if (!any) Projectile.Kill();
    }
    private bool InRange(int i) => _Positions[i].LengthSquared() <= MaxRadius * MaxRadius;

    private float RingRadius(int ring)
    {
        int delay = ring * 5;
        float progress = Math.Clamp((Age - delay) / (59f - delay), 0f, 1f);
        return MathHelper.SmoothStep(0f, Inner + ring * RingSpacing, progress);
    }
    public override bool? CanDamage() => Age >= LaserStart ? null : false;
    public override bool? Colliding(Rectangle projHitbox, Rectangle target)
    {
        if (Age < LaserStart) return false;
        Vector2 center = Projectile.Center;
        float near = Vector2.Distance(center, Vector2.Clamp(center, target.TopLeft(), target.BottomRight()));
        float x = Math.Max(MathF.Abs(center.X - target.Left), MathF.Abs(center.X - target.Right));
        float y = Math.Max(MathF.Abs(center.Y - target.Top), MathF.Abs(center.Y - target.Bottom));
        float far = MathF.Sqrt(x * x + y * y);
        for (int i = 0; Age < Duration && i < Rings; i++)
        {
            float r = Inner + i * RingSpacing;
            if (near <= r + LaserWidth / 2f && far >= r - LaserWidth / 2f) return true;
        }
        if (Age < FlowerStart || _Positions == null) return false;
        for (int i = 0; i < _Positions.Length; i++)
        {
            if (!InRange(i)) continue;
            Vector2 p = center + _Positions[i];
            if (Vector2.DistanceSquared(p, Vector2.Clamp(p, target.TopLeft(), target.BottomRight())) <= HitRadius * HitRadius)
                return true;
        }
        return false;
    }

    private void BurstRings()
    {
        if (Main.netMode == NetmodeID.Server || Age < 0) return;
        for (int ring = 0; ring < Rings; ring++)
        {
            float radius = RingRadius(ring);
            if (radius <= 0f) continue;
            for (int n = 0; n < 32; n++)
                ClearParticles.Burst(Projectile.Center + (n * MathHelper.TwoPi / 32).ToRotationVector2() * radius,
                    Tint((ring % 2 == 0) == PinkFirst), 2);
        }
    }

    public override void OnKill(int timeLeft)
    {
        if (Main.netMode == NetmodeID.Server) return;
        if (Age < Duration) BurstRings();
        if (Age < FlowerStart || _Flowers == null) return;
        for (int i = 0; i < _Flowers.Length; i++)
            if (InRange(i)) ClearParticles.Burst(Projectile.Center + _Positions[i], Tint(_Flowers[i].Pink), 3);
    }
}
