using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace Anomalies.Bosses.Plantera.UnendingSpring;

// One invocation of handleButterflies in SoHW/St6/boss_fin/boss_ns4.dnh.
// Positions and speeds below use the original field's pixels and 60 Hz clock.
public sealed class ButterflyFlowers : ModProjectile
{
    private const float WorldScale = 2f;
    private const int RangeMultiplier = 11;
    private const int MotherReturnTick = 10 * 60;
    private const float MotherSpeed = 2.5f;
    private const float ChildSpeedMultiplier = 1.2f;
    private const int TrailTicks = 91;
    private const float GhostOpacity = 0.2f;
    // Original 384 x 448 field, boss at (192,140), with 32 pixels of offscreen margin.
    private static readonly Rectangle _OriginalField = new(-224, -172, 448, 512);
    private const float SpriteScale = 0.5f;
    // In the supplied 70 x 68 image the central disk spans pixels 16..53 on both axes.
    private static readonly Vector2 _SpriteOrigin = new(35f, 35f);
    private const float HitRadius = 19f * SpriteScale;
    private static readonly Color _Pink = new(255, 133, 188);
    private static readonly Color _Green = new(112, 255, 127);
    private readonly List<Flower> _Flowers = new(640);
    private int _NextFrame;
    private uint? _LastUpdate;
    private int _MothersRemaining;
    private int _EffectsProcessedThrough = -1;

    private struct Flower
    {
        public Vector2 Position;
        public float Angle;
        public int Born;
        public Vector3[] Trail;
        public Vector2 RangeOrigin;
        public bool Mother;
        public bool Pink;
        public bool Ghost;
        public int Delay => Mother ? 10 : 30;
    }

    public override string Texture => "Anomalies/Bosses/Plantera/UnendingSpring/Assets/Flower";
    public override void SetStaticDefaults() =>
        ProjectileID.Sets.DrawScreenCheckFluff[Type] = 1600 + (int)(_OriginalField.Height * WorldScale * RangeMultiplier);
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

    public override void AI()
    {
        NPC boss = Main.npc[(int)Projectile.ai[0]];
        if (!boss.active || boss.type != NPCID.Plantera)
        {
            Projectile.Kill();
            return;
        }
        UpdateFlowers(boss);
    }

    // The boss steps its flowers before deciding whether to move or fire. Its
    // projectile also runs AI later in the tick, and predicts the pattern on clients.
    internal void UpdateFlowers(NPC boss)
    {
        if (_LastUpdate == Main.GameUpdateCount) return;
        _LastUpdate = Main.GameUpdateCount;
        // Plantera holds position during this attack. Shots use the launch point as
        // their origin; returning mothers steer towards the body in that same space.
        Vector2 bossOffset = (boss.Center - Projectile.Center) / WorldScale;
        int frame = (int)Projectile.ai[2];
        if (frame < _NextFrame)
        {
            _NextFrame = 0;
            _Flowers.Clear();
        }
        while (_NextFrame <= frame)
        {
            int tick = _NextFrame++;
            bool playBurst = Main.netMode != NetmodeID.Server && tick > _EffectsProcessedThrough && frame - tick <= 1;
            Advance(tick, playBurst, bossOffset);
        }
        _EffectsProcessedThrough = Math.Max(_EffectsProcessedThrough, frame);
        Projectile.ai[2] = _NextFrame;
        if (_Flowers.Count == 0)
        {
            Projectile.Kill();
            return;
        }
        Projectile.timeLeft = 2;
        if (Main.netMode == NetmodeID.Server && _NextFrame % 120 == 0) Projectile.netUpdate = true;
    }

    private void Advance(int frame, bool playBurst, Vector2 bossOffset)
    {
        if (frame == 0)
        {
            _MothersRemaining = 8;
            for (int i = 0; i < 4; i++)
            {
                AddMother(Projectile.ai[1] + i * MathHelper.PiOver2, true);
                AddMother(Projectile.ai[1] - i * MathHelper.PiOver2, false);
            }
        }
        for (int i = _Flowers.Count - 1; i >= 0; i--)
        {
            Flower f = _Flowers[i];
            int age = frame - f.Born;
            if (age < f.Delay) continue;
            float speed;
            if (f.Mother)
            {
                if (age >= MotherReturnTick)
                {
                    Vector2 toBoss = bossOffset - f.Position;
                    if (toBoss.LengthSquared() <= MotherSpeed * MotherSpeed)
                    {
                        f.Position = bossOffset;
                        if (playBurst) Burst(f);
                        _Flowers.RemoveAt(i);
                        if (--_MothersRemaining == 0)
                        {
                            if (playBurst) BurstChildren();
                            _Flowers.Clear();
                            return;
                        }
                        continue;
                    }
                    f.Angle = f.Angle.AngleLerp(toBoss.ToRotation(), 0.08f);
                }
                // 67 volleys per 900 flight ticks. Both lanes start with a solid flower.
                int phase = (age - f.Delay) * 67;
                if (phase % 900 < 67)
                {
                    // Children born beyond the old field use a boss-centered field to
                    // measure their baseline range in the direction they are fired.
                    Vector2 rangeOrigin = OutsideOriginalField(f.Position) ? Vector2.Zero : f.Position;
                    bool ghost = (phase / 900) % 2 == 1;
                    _Flowers.Add(new Flower { Position = f.Position, RangeOrigin = rangeOrigin, Angle = f.Angle - MathHelper.PiOver2, Born = frame, Pink = true, Ghost = ghost });
                    _Flowers.Add(new Flower { Position = f.Position, RangeOrigin = rangeOrigin, Angle = f.Angle + MathHelper.PiOver2, Born = frame, Ghost = ghost });
                }
                if (age < MotherReturnTick)
                {
                    float turn = age < 40 ? 3f : age < 130 ? 1.4f : -0.6f;
                    f.Angle += MathHelper.ToRadians(f.Pink ? -turn : turn);
                }
                speed = MotherSpeed;
            }
            else
            {
                float gain = Math.Max(0, age - 90 + 1) * 0.02f;
                speed = Math.Min(2f, 0.2f + gain) * ChildSpeedMultiplier;
            }
            Vector2 step = f.Angle.ToRotationVector2() * speed;
            f.Position += step;
            if (f.Trail != null)
                f.Trail[age % TrailTicks] = new Vector3(f.Position, f.Angle);
            if (!f.Mother)
            {
                f.RangeOrigin += step / RangeMultiplier;
                if (OutsideOriginalField(f.RangeOrigin))
                {
                    if (playBurst) ClearParticles.Burst(Projectile.Center + f.Position * WorldScale, f.Pink ? _Pink : _Green);
                    _Flowers.RemoveAt(i);
                    continue;
                }
            }
            _Flowers[i] = f;
        }
    }

    private static bool OutsideOriginalField(Vector2 p) => p.X < _OriginalField.Left || p.X > _OriginalField.Right ||
        p.Y < _OriginalField.Top || p.Y > _OriginalField.Bottom;

    private void AddMother(float angle, bool pink) => _Flowers.Add(new Flower
    {
        Position = angle.ToRotationVector2() * 20f,
        Angle = angle,
        Mother = true,
        Pink = pink,
        Trail = Main.netMode == NetmodeID.Server ? null : new Vector3[TrailTicks]
    });

    public override void OnKill(int timeLeft)
    {
        if (Main.netMode == NetmodeID.Server || _Flowers.Count == 0) return;
        foreach (Flower f in _Flowers)
            if (f.Mother) ClearParticles.Burst(Projectile.Center + f.Position * WorldScale, f.Pink ? _Pink : _Green, 32);
        BurstChildren();
    }

    private void BurstChildren()
    {
        // Reserve a share of the dust budget for each visible flower, including ghosts.
        Vector2 near = Main.Camera.ScaledPosition - new Vector2(96f);
        Vector2 far = Main.Camera.ScaledPosition + Main.Camera.ScaledSize + new Vector2(96f);
        bool Visible(Flower f)
        {
            Vector2 p = Projectile.Center + f.Position * WorldScale;
            return p.X >= near.X && p.X <= far.X && p.Y >= near.Y && p.Y <= far.Y;
        }
        List<Flower> visible = _Flowers.FindAll(f => !f.Mother && Visible(f));
        if (visible.Count == 0) return;
        int count = Math.Clamp(600 / visible.Count, 1, 48);
        foreach (Flower f in visible) Burst(f, count);
    }

    private void Burst(Flower f, int count = 48)
    {
        Vector2 center = Projectile.Center + f.Position * WorldScale;
        for (int i = 0; i < count; i++)
        {
            float angle = (i + Main.rand.NextFloat()) * MathHelper.TwoPi / count;
            Vector2 velocity = angle.ToRotationVector2() * Main.rand.NextFloat(1.4f, 6.2f);
            Color color = f.Pink ? _Pink : _Green;
            Dust.NewDustPerfectAction(center, DustID.RainbowMk2, dust =>
            {
                dust.velocity = velocity;
                dust.color = color;
                dust.scale = Main.rand.NextFloat(0.9f, 1.6f);
                dust.noGravity = true;
            });
        }
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        foreach (Flower f in _Flowers)
        {
            if (f.Ghost || _NextFrame - 1 - f.Born < f.Delay) continue;
            Vector2 p = Projectile.Center + f.Position * WorldScale;
            float dx = p.X - MathHelper.Clamp(p.X, targetHitbox.Left, targetHitbox.Right);
            float dy = p.Y - MathHelper.Clamp(p.Y, targetHitbox.Top, targetHitbox.Bottom);
            if (dx * dx + dy * dy <= HitRadius * HitRadius) return true;
        }
        return false;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = Projectile.Texture;
        Vector2 origin = Projectile.Center - Main.screenPosition;
        foreach (Flower f in _Flowers)
        {
            Vector2 p = origin + f.Position * WorldScale;
            float padding = f.Mother ? 52f + (TrailTicks - 1) * MotherSpeed * WorldScale : 52f;
            if (p.X < -padding || p.Y < -padding || p.X > Main.screenWidth + padding || p.Y > Main.screenHeight + padding) continue;
            int age = _NextFrame - 1 - f.Born;
            float fade = MathHelper.Clamp(age / (float)f.Delay, 0f, 1f);
            Color tint = f.Pink ? _Pink : _Green;
            float scale = SpriteScale * (2f - fade);
            float rotation = f.Angle + MathHelper.PiOver2;
            if (f.Mother)
            {
                for (int lag = TrailTicks - 1; lag >= 2; lag -= 2)
                {
                    if (age - lag < f.Delay) continue;
                    Vector3 stamp = f.Trail[(age - lag) % TrailTicks];
                    float strength = 1f - lag / (float)TrailTicks;
                    Main.EntitySpriteDraw(texture, origin + new Vector2(stamp.X, stamp.Y) * WorldScale,
                        null, tint * (0.45f * strength), stamp.Z + MathHelper.PiOver2, _SpriteOrigin,
                        SpriteScale * (0.3f + 0.7f * strength), SpriteEffects.None);
                }
                for (int i = 0; i < 8; i++)
                {
                    Vector2 offset = (MathHelper.TwoPi * i / 8).ToRotationVector2() * 2f;
                    Main.EntitySpriteDraw(texture, p + offset, null, tint * fade, rotation, _SpriteOrigin, scale, SpriteEffects.None);
                }
            }
            Main.EntitySpriteDraw(texture, p, null, tint * (fade * (f.Ghost ? GhostOpacity : 1f)),
                rotation, _SpriteOrigin, scale, SpriteEffects.None);
        }
        return false;
    }
}