using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Anomalies.Bosses.Plantera.UnendingSpring;

public sealed class RosePattern : ModProjectile
{
    public override string Texture => "Anomalies/Bosses/Plantera/UnendingSpring/Assets/Needle";
    private const float Scale = 2f; // World pixels per source pixel.
    private const float PetalRadius = 76f;
    internal const float TipRadius = Scale * PetalRadius;
    internal const float NeedleDrawScale = 1.84f;
    private const float DelayDrawScale = 1.6f;
    private const float NeedleHitRadius = 3.2f;
    private const int DegreesPerTick = 6;
    private const int PathPeriod = 1080 / DegreesPerTick;
    private const int NeedleLife = 480;
    internal const float ClearNeedles = 2f;
    private const int NeedlesPerPeriod = 85833;
    private const long EmissionPeriod = 50_000_000_000L * 20;
    internal static readonly Color Pink = new(255, 115, 185);
    internal static readonly Color Green = new(114, 255, 149);
    private bool _Mirrored;
    private readonly List<(Vector2 offset, bool active, float rotation)> _Needles = new();
    private readonly List<Vector2> _Trail = new();
    private readonly Queue<int> _Births = new();
    private long _EmissionCredit;
    private int _TipsSinceShot;
    private int _Tick;

    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 2600;

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

    public override bool ShouldUpdatePosition() => false;
    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(_Tick);
        writer.Write(_EmissionCredit);
        writer.Write((byte)_Births.Count);
        foreach (int birth in _Births)
            writer.Write((ushort)(_Tick - birth));
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        _Tick = reader.ReadInt32();
        _EmissionCredit = reader.ReadInt64();
        int count = reader.ReadByte();
        _Births.Clear();
        for (int i = 0; i < count; i++)
            _Births.Enqueue(_Tick - reader.ReadUInt16());
    }


    public override void AI()
    {
        NPC boss = Main.npc[(int)Projectile.ai[0]];
        if (!boss.active || boss.type != NPCID.Plantera || !PlanteraUnending.GetInstance(boss).ShouldProcess)
        {
            Projectile.Kill();
            return;
        }

        Projectile.timeLeft = 2;
        if (Projectile.ai[2] == ClearNeedles)
        {
            BurstNeedles();
            _Births.Clear();
        }
        Projectile.Center = boss.Center;
        _Mirrored = PlanteraUnending.GetInstance(boss).HasGreenLaser;
        _Needles.Clear();
        _Trail.Clear();
        AddInner(_Tick);
        if (Main.netMode != NetmodeID.MultiplayerClient) UpdateAimedShot(boss);
        if (++_Tick % 300 == 0 && Main.netMode == NetmodeID.Server)
            Projectile.netUpdate = true;
    }

    private float AngleAt(int frame) => Projectile.ai[1] + DegreesPerTick * (frame % PathPeriod);

    private void UpdateAimedShot(NPC boss)
    {
        bool wheelShots = _Mirrored && PlanteraUnending.GetInstance(boss).PetalAiming;
        if (_Mirrored ? !wheelShots : Projectile.ai[2] != 0f)
        {
            _TipsSinceShot = 0;
            return;
        }
        // |cos(4a/3)| peaks every 135 degrees. Count the nearest rendered frame
        // once, choosing the earlier one if a tip falls halfway between frames.
        if (_Tick == 0 || (AngleAt(_Tick) + DegreesPerTick * 0.5f) % 135f >= DegreesPerTick)
            return;
        if (_TipsSinceShot < 2)
        {
            _TipsSinceShot++;
            return;
        }
        if (!boss.HasPlayerTarget || !boss.HasValidTarget) return;
        float speed = wheelShots ? AimedNeedle.WheelSpeed : AimedNeedle.LaunchSpeed;
        bool fired = FireAimed(boss, Projectile.Center + _Trail[0], ModContent.ProjectileType<AimedNeedle>(), speed, wheelShots);
        if (wheelShots)
            fired |= FireAimed(boss, Projectile.Center - _Trail[0], ModContent.ProjectileType<GreenAimedNeedle>(), speed, true);
        if (fired) _TipsSinceShot = 0;
    }

    private bool FireAimed(NPC boss, Vector2 head, int type, float speed, bool constantSpeed)
    {
        Vector2 direction = (Main.player[boss.target].Center - head).SafeNormalize(Vector2.UnitY);
        bool fired = false;
        Projectile.NewProjectileAction(Projectile.GetSource_FromAI(), head, direction * speed, type, Projectile.damage, 0f,
            Main.myPlayer, p =>
            {
                p.ai[0] = boss.whoAmI;
                p.ai[2] = constantSpeed ? 1f : 0f;
                fired = true;
            });
        return fired;
    }

    private void AddTrail(int frame) => UnendingSpringPaths.SampleRoseTrail(_Trail, Projectile.ai[1], frame);

    private void AddInner(int elapsed)
    {
        AddTrail(elapsed);
        while (_Births.Count > 0 && elapsed - _Births.Peek() > NeedleLife)
        {
            int birth = _Births.Dequeue();
            BurstNeedle(NeedleOffset(birth, NeedleLife));
        }
        // Apply the phase-two 15% increase exactly with a shared denominator of 20.
        long rate = NeedlesPerPeriod * (_Mirrored ? 56_749L * 23 : 100_000L * 20);
        if (Projectile.ai[2] == 0)
        {
            _EmissionCredit += rate;
            if (_EmissionCredit >= EmissionPeriod)
            {
                _Births.Enqueue(elapsed);
                _EmissionCredit -= EmissionPeriod;
            }
        }
        else _EmissionCredit = EmissionPeriod - rate; // Emit on the resume tick.

        foreach (int birth in _Births)
        {
            int age = elapsed - birth;
            float angle = AngleAt(birth);
            _Needles.Add((NeedleOffset(birth, age), age >= 3,
                MathHelper.ToRadians(angle + 270)));
        }
    }

    private Vector2 NeedleOffset(int birth, int age) => UnendingSpringPaths.NeedleOffset(Projectile.ai[1], birth, age);

    private void BurstNeedle(Vector2 offset)
    {
        ClearParticles.Burst(Projectile.Center + offset, Pink, 3);
        if (_Mirrored) ClearParticles.Burst(Projectile.Center - offset, Green, 3);
    }

    private void BurstNeedles()
    {
        foreach ((Vector2 offset, bool active, float rotation) needle in _Needles) BurstNeedle(needle.offset);
    }

    public override void OnKill(int timeLeft)
    {
        BurstNeedles();
        foreach (Vector2 point in _Trail) BurstNeedle(point);
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) =>
        HitsPath(targetHitbox, 1f) || (_Mirrored && HitsPath(targetHitbox, -1f));

    private bool HitsPath(Rectangle targetHitbox, float side)
    {
        foreach ((Vector2 offset, bool active, float rotation) needle in _Needles)
        {
            if (!needle.active) continue;
            Vector2 p = Projectile.Center + needle.offset * side;
            float dx = p.X - MathHelper.Clamp(p.X, targetHitbox.Left, targetHitbox.Right);
            float dy = p.Y - MathHelper.Clamp(p.Y, targetHitbox.Top, targetHitbox.Bottom);
            if (dx * dx + dy * dy <= NeedleHitRadius * NeedleHitRadius) return true;
        }
        return UnendingSpringPaths.HitsTrail(_Trail, Projectile.Center, side, 14f * Scale, targetHitbox);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Vector2 center = Projectile.Center - Main.screenPosition;
        if (_Trail.Count > 1)
        {
            Main.spriteBatch.End();
            BeamRenderer.Draw(_Trail, center, Pink);
            if (_Mirrored) BeamRenderer.Draw(_Trail, center, Green, -1f);
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }
        DrawNeedles(false);
        if (_Mirrored) DrawNeedles(true);
        return false;
    }

    private void DrawNeedles(bool green)
    {
        foreach ((Vector2 offset, bool active, float rotation) needle in _Needles)
        {
            Vector2 p = Projectile.Center - Main.screenPosition + (green ? -needle.offset : needle.offset);
            if (p.X < -40 || p.Y < -40 || p.X > Main.screenWidth + 40 || p.Y > Main.screenHeight + 40)
                continue;
            Texture2D texture = BeamRenderer.Image(needle.active ? (green ? "GreenNeedle" : "Needle") : "Delay");
            Main.EntitySpriteDraw(texture, p, null, needle.active ? Color.White : (green ? Green : Pink),
                needle.rotation + (green ? MathHelper.Pi : 0f), texture.Size() / 2,
                needle.active ? NeedleDrawScale : DelayDrawScale, SpriteEffects.None);
        }
    }
}