using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Anomalies.Bosses.Plantera.UnendingSpring;

// SoHW/St6/boss_fin/boss_sc4.dnh, normal route, Lunatic (difficulty index 3).
public sealed class TransitionSpell : ModProjectile
{
    internal const int FirstRingTick = 235;
    private const int RingInterval = 3;
    // Cumulative rounding of the previous ten rings at 120% gives 445 flowers.
    private static readonly int[] _RingWays = { 44, 45, 44, 46, 44, 46, 43, 46, 43, 44 };
    private static int LastRingTick => FirstRingTick + (_RingWays.Length - 1) * RingInterval;
    internal static int StartActingTick => LastRingTick + 180;
    private const int ChargeStart = 160, ChargeWays = 25;
    private const float ChargeRadius = 480f, ChargeScale = 0.48f, Scale = 2f;
    private const int RangeMultiplier = 10;
    private static readonly Rectangle _OriginalField = new(-224, -136, 448, 512);
    private struct Shot
    {
        public int Born, Color;
        public Vector2 Position, Velocity;
        public float Angle, DistanceLeft, Speed;
    }
    private static readonly Color[] _Palette = {
        new(255,75,84), new(255,157,72), new(255,234,94), new(188,246,81),
        new(104,241,125), new(79,232,202), new(106,223,251), new(94,160,255),
        new(97,108,237), new(163,107,245), new(218,126,244), new(255,139,199)
    };
    private readonly List<Shot> _Shots = new(445);
    private int _NextFrame, _LastParticleFrame = -1;
    private float _OpeningAngle;
    public override string Texture => "Anomalies/Bosses/Plantera/UnendingSpring/Assets/TransitionShots";
    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 1100 * RangeMultiplier;
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
    public override void AI()
    {
        NPC boss = Main.npc[(int)Projectile.ai[0]];
        int frame = (int)Projectile.ai[2];
        if (!boss.active || boss.type != NPCID.Plantera)
        {
            Projectile.Kill();
            return;
        }
        if (frame < _NextFrame) _NextFrame = 0;
        while (_NextFrame <= frame)
        {
            int tick = _NextFrame++;
            Advance(tick, tick == frame && tick > _LastParticleFrame && Main.netMode != NetmodeID.Server);
        }
        _LastParticleFrame = Math.Max(_LastParticleFrame, frame);
        Projectile.ai[2] = _NextFrame;
        if (frame >= LastRingTick && _Shots.Count == 0)
        {
            Projectile.Kill();
            return;
        }
        Projectile.timeLeft = 2;
        if (Main.netMode == NetmodeID.Server && _NextFrame % 120 == 0) Projectile.netUpdate = true;
    }
    private static Vector2 Unit(float degrees) => MathHelper.ToRadians(degrees).ToRotationVector2();

    private static float StraightRange(Vector2 p, Vector2 direction)
    {
        float x = direction.X == 0f ? float.PositiveInfinity :
            ((direction.X > 0f ? _OriginalField.Right : _OriginalField.Left) - p.X) / direction.X;
        float y = direction.Y == 0f ? float.PositiveInfinity :
            ((direction.Y > 0f ? _OriginalField.Bottom : _OriginalField.Top) - p.Y) / direction.Y;
        return Math.Min(x, y);
    }

    private void Advance(int frame, bool particles)
    {
        if (frame == 0)
        {
            _Shots.Clear();
            _OpeningAngle = (float)new Random((int)Projectile.ai[1]).NextDouble() * 360f;
        }
        if (particles && frame == FirstRingTick)
            ClearParticles.Burst(Projectile.Center, _Palette[11], ChargeWays);
        int elapsed = frame - FirstRingTick;
        if (elapsed >= 0 && frame <= LastRingTick && elapsed % RingInterval == 0)
        {
            int ring = elapsed / RingInterval;
            int ways = _RingWays[ring];
            float speed = (3f - ring * 0.2f) * 1.3f * 1.4f * 1.2f;
            for (int i = 0; i < ways; i++)
            {
                float angle = _OpeningAngle + i * 360f / ways + ring * 3f;
                Vector2 direction = Unit(angle);
                Vector2 start = direction * 20f;
                _Shots.Add(new Shot
                {
                    Born = frame,
                    Color = ring,
                    Position = start,
                    Angle = angle,
                    Speed = speed,
                    Velocity = direction * speed,
                    DistanceLeft = StraightRange(start, direction) * RangeMultiplier
                });
            }
        }
        for (int i = _Shots.Count - 1; i >= 0; i--)
        {
            Shot s = _Shots[i];
            if (frame - s.Born < 10) continue;
            s.Position += s.Velocity;
            s.DistanceLeft -= s.Speed;
            if (s.DistanceLeft <= 0f)
            {
                if (particles) Burst(s);
                _Shots.RemoveAt(i);
            }
            else _Shots[i] = s;
        }
    }
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        foreach (Shot s in _Shots)
        {
            if (_NextFrame - 1 - s.Born < 10) continue;
            Vector2 p = Projectile.Center + s.Position * Scale;
            float x = p.X - MathHelper.Clamp(p.X, targetHitbox.Left, targetHitbox.Right);
            float y = p.Y - MathHelper.Clamp(p.Y, targetHitbox.Top, targetHitbox.Bottom);
            const float Radius = 9.5f;
            if (x * x + y * y <= Radius * Radius) return true;
        }
        return false;
    }
    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D flower = ModContent.Request<Texture2D>("Anomalies/Bosses/Plantera/UnendingSpring/Assets/Flower").Value;
        Vector2 origin = Projectile.Center - Main.screenPosition;
        int frame = _NextFrame - 1;
        if (frame >= ChargeStart && frame < FirstRingTick)
            DrawCharge(flower, origin, frame - ChargeStart);
        foreach (Shot s in _Shots)
        {
            int age = _NextFrame - 1 - s.Born;
            float fade = Math.Min(1f, age / 10f);
            Vector2 p = origin + s.Position * Scale;
            if (p.X < -72 || p.Y < -72 || p.X > Main.screenWidth + 72 || p.Y > Main.screenHeight + 72) continue;
            Color color = _Palette[s.Color] * fade;
            float angle = MathHelper.ToRadians(s.Angle) + MathHelper.PiOver2;
            Main.EntitySpriteDraw(flower, p, null, color, angle, new Vector2(35f), 0.5f * (2f - fade), SpriteEffects.None);
        }
        return false;
    }

    public override void OnKill(int timeLeft)
    {
        if (Main.netMode == NetmodeID.Server) return;
        foreach (Shot shot in _Shots) Burst(shot);
        int frame = _NextFrame - 1;
        if (frame >= ChargeStart && frame < FirstRingTick)
            for (int i = 0; i < ChargeWays; i++)
                ClearParticles.Burst(Projectile.Center + ChargeOffset(i, frame - ChargeStart), _Palette[11], 3);
    }

    private void Burst(Shot shot) =>
        ClearParticles.Burst(Projectile.Center + shot.Position * Scale, _Palette[shot.Color], 3);

    private static Vector2 ChargeOffset(int way, int age)
    {
        float t = age / (float)(FirstRingTick - ChargeStart - 1);
        return Unit(way * (360f / ChargeWays) + t * 90f) * (ChargeRadius * (1f - t));
    }

    private static void DrawCharge(Texture2D flower, Vector2 origin, int age)
    {
        Texture2D outline = ModContent.Request<Texture2D>("Anomalies/Bosses/Plantera/UnendingSpring/Assets/ChargeFlowerOutline").Value;
        Vector2 flowerOrigin = new(35f);
        Color tint = _Palette[11];
        float opacity = Math.Min(1f, (age + 1) / 8f);
        for (int i = 0; i < ChargeWays; i++)
        {
            for (int lag = 12; lag >= 2; lag -= 2)
            {
                if (age < lag) continue;
                float strength = 1f - lag / 14f;
                Main.EntitySpriteDraw(flower, origin + ChargeOffset(i, age - lag), null, tint * (0.48f * strength),
                    0f, flowerOrigin, ChargeScale * (0.4f + 0.6f * strength), SpriteEffects.None);
            }
            Vector2 p = origin + ChargeOffset(i, age);
            Main.EntitySpriteDraw(outline, p, null, new Color(255, 202, 229) * opacity, 0f,
                flowerOrigin + new Vector2(3f), ChargeScale, SpriteEffects.None);
            Main.EntitySpriteDraw(flower, p, null, tint * opacity, 0f, flowerOrigin, ChargeScale, SpriteEffects.None);
        }
    }
}