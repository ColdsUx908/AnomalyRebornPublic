using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Anomalies.Bosses.Plantera.UnendingSpring;

public class AimedNeedle : ModProjectile
{
    internal const float LaunchSpeed = 4f;
    private const float CruiseSpeed = 15.6f;
    internal const float WheelSpeed = CruiseSpeed * 0.7f;
    private const int SlowTicks = 30;
    private const int AccelerationTicks = 30;
    public override string Texture => "Anomalies/Bosses/Plantera/UnendingSpring/Assets/Needle";
    // Match the main pink in the original needle sprite.
    protected virtual Color Outline => new(255, 114, 184);

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailingMode[Type] = 0;
        ProjectileID.Sets.TrailCacheLength[Type] = 13;
    }

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 6;
        Projectile.hostile = true;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = 240;
    }

    public override void AI()
    {
        NPC boss = Main.npc[(int)Projectile.ai[0]];
        if (!boss.active || boss.type != NPCID.Plantera)
        {
            Projectile.Kill();
            return;
        }
        if (Projectile.ai[2] == 0f && Projectile.ai[1] < SlowTicks + AccelerationTicks)
        {
            float acceleratingTicks = ++Projectile.ai[1] - SlowTicks;
            if (acceleratingTicks > 0)
            {
                float speed = MathHelper.Lerp(LaunchSpeed, CruiseSpeed, acceleratingTicks / AccelerationTicks);
                Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY) * speed;
                if (acceleratingTicks == AccelerationTicks && Main.netMode == NetmodeID.Server)
                    Projectile.netUpdate = true;
            }
        }
        Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
    }

    public override void OnKill(int timeLeft) => ClearParticles.Burst(Projectile.Center, Outline);

    public override bool PreDraw(ref Color lightColor)
    {
        SeedSprites.DrawTrail(Projectile, Outline);
        SeedSprites.Draw(Projectile, true, Color.White, outlineOnly: Outline, drawScale: 1.84f);
        return false;
    }
}

public sealed class GreenAimedNeedle : AimedNeedle
{
    public override string Texture => "Anomalies/Bosses/Plantera/UnendingSpring/Assets/GreenNeedle";
    protected override Color Outline => new Color(114, 255, 149);
}