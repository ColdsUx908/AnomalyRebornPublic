namespace Anomalies.Bosses.KingSlime;

public sealed class SlimeGel : AnomalyModProjectile
{
    /* 数组使用约定
     * 
     * Projectile.ai
     * [0] 当前已碰撞次数
     * [1] 是否启用彩虹色（大于 0 时启用）
     */

    public override string LocalizationCategory => "Bosses.KingSlime";

    public bool IsRainbow => Projectile.ai[1] > 0f;

    public override void SetDefaults()
    {
        Projectile.width = 50;
        Projectile.height = 50;
        Projectile.aiStyle = -1;
        Projectile.hostile = true;
        Projectile.friendly = false;
        Projectile.timeLeft = 450;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = true;
        Projectile.penetrate = -1;
        Projectile.scale = 0.5f;
    }

    public override bool OnTileCollide(Vector2 oldVelocity) => SlimeGelMotion.Gel_OnTileCollide(Projectile, oldVelocity, DustID.Skyware, d =>
    {
        d.velocity = Main.rand.NextPolarVector2(1f, 5f);
        d.noGravity = true;
    });

    public override void AI() => SlimeGelMotion.Gel_AI(Projectile);

    public override Color? GetAlpha(Color lightColor)
    {
        if (IsRainbow)
            return Main.DiscoColor * Projectile.Opacity;

        return null;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = Projectile.Texture;
        Color color = Projectile.GetAlpha(lightColor);
        Main.spriteBatch.DrawFromCenter(texture, Projectile.Center - Main.screenPosition, null, color, Projectile.rotation, Projectile.scale, SpriteEffects.None, 0f);
        return false;
    }

    private float DamageRadius => 32f * Projectile.scale;

    public override bool? CanHitNPC(NPC target) => target.friendly || Projectile.friendly
        ? ProjectileDamageBounds.Around(Projectile.Center, DamageRadius).CanHitNPC(Type, target) : null;

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => new Circle(Projectile.Center, DamageRadius).Collides(targetHitbox);
}

