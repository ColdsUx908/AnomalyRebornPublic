using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Anomalies.Bosses.Plantera.UnendingSpring;

public sealed class ParadoxSeed : ModProjectile
{
    internal const int SeedSize = 20;
    internal const int Warmup = 8;
    private bool Pink => Projectile.ai[2] > 0f;
    private int Age => (int)Math.Abs(Projectile.ai[2]) - 1;
    private int Sprite => Pink ? ProjectileID.SeedPlantera : ProjectileID.PoisonSeedPlantera;
    public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.SeedPlantera}";

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailingMode[Type] = 0;
        ProjectileID.Sets.TrailCacheLength[Type] = 12;
        ProjectileID.Sets.DrawScreenCheckFluff[Type] = 720;
    }

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = SeedSize;
        Projectile.scale = 2f;
        Projectile.hostile = true;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.netImportant = true;
    }
    internal static float NextForwardSpeed(float speed) => Math.Min(10f * PlanteraUnending.ParadoxScale,
        speed + 0.05f * PlanteraUnending.ParadoxScale);
    public override bool? CanDamage() => Age < Warmup ? false : null;
    public override void AI()
    {
        float side = Projectile.ai[0];
        Projectile.velocity.X = side * NextForwardSpeed(side * Projectile.velocity.X);
        Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        Projectile.frame = Age / 6 % Main.projFrames[Sprite];
        Projectile.ai[2] += Pink ? 1 : -1;
        // The rotated bottom tangent is fixed at launch, even after the arena moves again.
        float pastEdge = side * (Projectile.Center.X + Projectile.velocity.X - Projectile.ai[1]);
        Projectile.timeLeft = pastEdge > Projectile.width / 2f ? 1 : 2;
    }
    public override bool PreDraw(ref Color lightColor)
    {
        float opacity = Math.Clamp((Age + 1f) / 8f, 0.25f, 1f);
        SeedSprites.DrawTrail(Projectile, SeedSprites.EdgeColor(Pink), opacity, width: 24f, maxStep: 64f, connectHead: true);
        SeedSprites.Draw(Projectile, Pink, lightColor, opacity, border: 3, spriteType: Sprite);
        return false;
    }
    public override void OnKill(int timeLeft) => ClearParticles.Burst(Projectile.Center, SeedSprites.EdgeColor(Pink));
}