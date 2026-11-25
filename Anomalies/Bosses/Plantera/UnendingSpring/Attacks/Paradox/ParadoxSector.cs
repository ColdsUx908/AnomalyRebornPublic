using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Anomalies.Bosses.Plantera.UnendingSpring;

// A round shares a projectile slot. Radius, speed and birth time are reproducible on late join.
public sealed class ParadoxSector : ModProjectile
{
    internal const int FirstTick = 180, Bands = 12;
    internal const float Reach = 2f * PlanteraArena.Radius;
    private const int Rise = 72, Fire = Rise + 48, FireTicks = 36;
    internal const int RoundTicks = Fire + FireTicks + 54;
    internal const int LastStart = FirstTick + 4 * RoundTicks;
    private const float Sweep = MathHelper.TwoPi / 3f, HitRadius = 6.25f;
    private sealed class Seed
    {
        public int Born, Flight;
        public float Radius, AngularSpeed;
        public Vector2 Offset, Previous;
    }
    private readonly List<Seed> _Seeds = new();
    private int _End;
    internal int CompletionAge
    {
        get { Prepare(); return _End; }
    }
    private int Age => (int)Projectile.ai[2] - 1;
    private int Key => (int)MathF.Abs(Projectile.ai[1]) - 1;
    private int Parity => Key & 1;
    private bool Green => (Key & 2) != 0;
    private int Direction => Green ? 1 : -1; // Positive angles turn clockwise on screen.
    private float Axis => Projectile.ai[1] > 0 ? 0f : MathHelper.Pi;
    private float Entry => Axis - Direction * Sweep / 2f;
    private Color Tint => Green ? DashVisuals.Green : DashVisuals.Pink;
    internal static bool Selected(int band, int parity) => band >= Bands - 3 || (band & 1) == parity;

    public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.SeedPlantera}";
    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = (int)Reach + 128;
    public override bool ShouldUpdatePosition() => false;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 8;
        Projectile.hostile = true;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.netImportant = true;
        Projectile.timeLeft = 2;
    }

    private void Prepare()
    {
        if (_Seeds.Count != 0) return;
        Random random = new Random(Key);
        for (int band = 0; band < Bands; band++)
        {
            if (!Selected(band, Parity)) continue;
            float inner = Math.Max(HitRadius, Reach * band / Bands + HitRadius);
            float outer = Reach * (band + 1) / Bands - HitRadius;
            for (int tick = Fire; tick < Fire + FireTicks; tick++)
            {
                int count = random.Next(3, 7);
                for (int j = 0; j < count; j++)
                {
                    float radius = MathHelper.Lerp(inner, outer, random.NextSingle());
                    float speed = (44f + 32f * random.NextSingle()) / radius;
                    int flight = (int)MathF.Ceiling(Sweep / speed);
                    _Seeds.Add(new Seed { Born = tick, Radius = radius, AngularSpeed = speed, Flight = flight });
                    _End = Math.Max(_End, tick + flight + 1);
                }
            }
        }
    }

    private float Angle(Seed seed, int steps) =>
        Entry + Direction * Math.Min(Sweep, seed.AngularSpeed * Math.Max(0, steps));
    private Vector2 Offset(Seed seed, int steps) => Angle(seed, steps).ToRotationVector2() * seed.Radius;

    public override void AI()
    {
        NPC boss = Main.npc[(int)Projectile.ai[0]];
        if (!boss.active || boss.type != NPCID.Plantera) { Projectile.Kill(); return; }
        Projectile.ai[2]++;
        Projectile.timeLeft = 2;
        if (Age < Fire) return;
        Prepare();
        foreach (Seed seed in _Seeds)
        {
            int steps = Age - seed.Born;
            if (steps == seed.Flight + 1 && !Main.dedServ)
                ClearParticles.Burst(Projectile.Center + Offset(seed, seed.Flight), Tint, 3);
            if (steps < 0 || steps > seed.Flight) continue;
            seed.Previous = Offset(seed, steps - 1);
            seed.Offset = Offset(seed, steps);
        }
        if (Age >= _End) Projectile.Kill();
    }

    public override bool? CanDamage() => Age < Fire ? false : null;
    public override bool? Colliding(Rectangle projHitbox, Rectangle target)
    {
        if (Age < Fire) return false;
        Vector2 center = Projectile.Center;
        Vector2 closest = Vector2.Clamp(center, target.TopLeft(), target.BottomRight());
        float near = Vector2.Distance(center, closest) - HitRadius;
        float far = Vector2.Distance(center, target.Center.ToVector2()) + target.Size().Length() / 2f + HitRadius + 1f;
        foreach (Seed seed in _Seeds)
        {
            int steps = Age - seed.Born;
            if (steps < 0 || steps > seed.Flight || seed.Radius < near || seed.Radius > far) continue;
            Vector2 point = center + seed.Offset;
            if (Vector2.DistanceSquared(point, Vector2.Clamp(point, target.TopLeft(), target.BottomRight())) <= HitRadius * HitRadius)
                return true;
            float start = Angle(seed, steps - 1), stop = Angle(seed, steps);
            // Short chords follow even the tight inner arcs, without tunnelling across players.
            int pieces = (int)MathF.Ceiling(Math.Abs(stop - start) / 0.15f);
            Vector2 a = center + seed.Previous;
            for (int i = 1; i <= pieces; i++)
            {
                Vector2 b = i == pieces ? point : center + MathHelper.Lerp(start, stop, i / (float)pieces).ToRotationVector2() * seed.Radius;
                if (RotatedRectangle.FromLineSegment(a, b, HitRadius * 2f).Collides(target)) return true;
                a = b;
            }
        }
        return false;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        if (Age < 0) return false;
        if (Age < Fire)
        {
            DashVisuals.DrawOrbitWarning(Projectile.Center, Axis, Parity, Tint, Math.Min(1f, Age / (float)Rise));
            return false;
        }
        Vector2 origin = Projectile.Center - Main.screenPosition;
        foreach (Seed seed in _Seeds)
        {
            int steps = Age - seed.Born;
            if (steps < 0 || steps > seed.Flight) continue;
            Vector2 p = origin + seed.Offset;
            if (p.X < -100 || p.Y < -100 || p.X > Main.screenWidth + 100 || p.Y > Main.screenHeight + 100) continue;
            Vector2 previous = origin + seed.Previous;
            if (steps > 0) DashVisuals.Line(previous, p, Tint * 0.6f, 11f);
            SeedSprites.DrawSpellShot(Green ? ProjectileID.PoisonSeedPlantera : ProjectileID.SeedPlantera, Tint,
                steps, p, Angle(seed, steps) + Direction * MathHelper.PiOver2 + MathHelper.PiOver2, 26f, 1f);
        }
        return false;
    }
    public override void OnKill(int timeLeft)
    {
        if (Main.dedServ) return;
        foreach (Seed seed in _Seeds)
            if (Age >= seed.Born && Age <= seed.Born + seed.Flight)
                ClearParticles.Burst(Projectile.Center + seed.Offset, Tint, 3);
    }
}