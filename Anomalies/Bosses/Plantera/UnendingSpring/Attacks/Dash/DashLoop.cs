using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Anomalies.Bosses.Plantera.UnendingSpring;

public sealed class DashLoop : ModProjectile
{
    private const int Segments = 32, Hold = 150;
    private int _Age;
    private int Steps => Math.Max(1, Math.Abs((int)Projectile.ai[2]));
    private float Extent => Math.Min(1f, _Age / (float)Steps);
    public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.SeedPlantera;
    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] =
        (int)(2 * PlanteraUnending.LoopTicks * PlanteraUnending.DashSpeed) + 64;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 8;
        Projectile.hostile = true;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.netImportant = true;
        Projectile.timeLeft = 240;
    }
    public override bool ShouldUpdatePosition() => false;
    public override void SendExtraAI(BinaryWriter writer) => writer.Write((ushort)_Age);
    public override void ReceiveExtraAI(BinaryReader reader) => _Age = reader.ReadUInt16();
    private Vector2 Point(float u, int branch)
    {
        return Projectile.Center + UnendingSpringPaths.LoopPoint(Projectile.ai[1].ToRotationVector2(), Steps, u, branch);
    }
    public override void AI()
    {
        if (!DashFan.ActiveOwner(Projectile)) { Projectile.Kill(); return; }
        if (++_Age < Steps + Hold) return;
        if (Main.netMode != NetmodeID.MultiplayerClient)
        {
            NPC boss = Main.npc[(int)Projectile.ai[0]];
            if (boss.HasValidTarget && boss.HasPlayerTarget)
            {
                if (Projectile.ai[2] < 0f) FireSeed(boss, 0f);
                FireSeed(boss, 0.5f);
                FireSeed(boss, 1f);
            }
        }
        Projectile.Kill();
    }
    public override void OnKill(int timeLeft)
    {
        if (_Age <= 0) return;
        for (int i = 0; i <= Segments; i += 2)
        {
            float u = i / (float)Segments;
            if (u > Extent) break;
            ClearParticles.Burst(Point(u, 1), DashVisuals.Pink, 2);
            if (i % (Segments / 2) != 0) ClearParticles.Burst(Point(u, -1), DashVisuals.Pink, 2);
        }
    }

    private void FireSeed(NPC boss, float u)
    {
        Vector2 at = Point(u, 1);
        Vector2 direction = (Main.player[boss.target].Center - at).SafeNormalize(Vector2.UnitY);
        float spread = Main.rand.NextFloat(-MathHelper.Pi / 18f, MathHelper.Pi / 18f);
        Projectile.NewProjectileAction(Projectile.GetSource_FromThis(), at, direction.RotatedBy(spread) * 7.15f,
            ModContent.ProjectileType<DashPinkSeed>(), Projectile.damage, 0f, Main.myPlayer, p => p.ai[0] = boss.whoAmI);
    }
    // The clipped segment at the leading edge follows the boss while it writes the loop.
    private bool Trace(Rectangle? target)
    {
        float extent = Extent;
        for (int branch = -1; branch <= 1; branch += 2)
        {
            Vector2 a = Projectile.Center;
            for (int n = 1; n <= Segments; n++)
            {
                float u = Math.Min(n / (float)Segments, extent);
                Vector2 b = Point(u, branch);
                if (target.HasValue)
                {
                    if (RotatedRectangle.FromLineSegment(a, b, 8f).Collides(target.Value))
                        return true;
                }
                else
                {
                    Vector2 start = a - Main.screenPosition, end = b - Main.screenPosition;
                    DashVisuals.Line(start, end, DashVisuals.Pink * 0.35f, 13f);
                    DashVisuals.Line(start, end, DashVisuals.Pink, 7f);
                    DashVisuals.Line(start, end, new Color(255, 223, 241), 2.5f);
                }
                if (u >= extent) break;
                a = b;
            }
        }
        return false;
    }
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) =>
        _Age > 0 && Vector2.DistanceSquared(targetHitbox.Center.ToVector2(), Projectile.Center) <
        MathF.Pow(Steps * PlanteraUnending.DashSpeed + targetHitbox.Width + targetHitbox.Height + 64f, 2) && Trace(targetHitbox);
    public override bool PreDraw(ref Color lightColor)
    {
        if (_Age > 0) Trace(null);
        return false;
    }
}

public sealed class DashPinkSeed : ModProjectile
{
    public override void OnKill(int timeLeft) => ClearParticles.Burst(Projectile.Center, DashVisuals.Pink);
    public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.SeedPlantera;
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Type] = 9;
        ProjectileID.Sets.TrailingMode[Type] = 2;
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 15;
        Projectile.hostile = true;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 300;
    }
    public override void AI()
    {
        int owner = (int)Projectile.ai[0];
        if ((uint)owner >= Main.maxNPCs || !Main.npc[owner].active || Main.npc[owner].type != NPCID.Plantera)
        {
            Projectile.Kill();
            return;
        }
        Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
    }
    public override bool PreDraw(ref Color lightColor)
    {
        SeedSprites.DrawTrail(Projectile, DashVisuals.Pink, width: 16f);
        SeedSprites.DrawSpellShot(ProjectileID.SeedPlantera, DashVisuals.Pink, 300 - Projectile.timeLeft,
            Projectile.Center - Main.screenPosition, Projectile.rotation, 26.25f, 1f);
        return false;
    }
}