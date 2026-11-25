namespace Anomalies.Bosses.Plantera.UnendingSpring;

public sealed class HookWarningRing : ModProjectile
{
    private const int Duration = 12;
    private const int Segments = 40;
    private const float RadiusInPlayerHeights = 2.2f;
    private static readonly Color _RingColor = new(103, 255, 143);

    public override string Texture => "Terraria/Images/MagicPixel";

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 2;
        Projectile.timeLeft = Duration;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
    }

    public override bool ShouldUpdatePosition() => false;

    public override bool? CanDamage() => false;

    public override void AI()
    {
        int targetIndex = (int)Projectile.ai[0];
        if ((uint)targetIndex >= Main.maxPlayers || !Main.player[targetIndex].active || Main.player[targetIndex].dead)
        {
            Projectile.Kill();
            return;
        }

        Projectile.Center = Main.player[targetIndex].Center;
        Projectile.ai[1]++;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        int targetIndex = (int)Projectile.ai[0];
        if ((uint)targetIndex >= Main.maxPlayers)
            return false;

        Player player = Main.player[targetIndex];
        float progress = MathHelper.Clamp(Projectile.ai[1] / (Duration - 1f), 0f, 1f);
        float radius = player.height * RadiusInPlayerHeights * progress;
        Vector2 center = player.Center - Main.screenPosition;
        Vector2 previous = new(radius, 0f);
        Color tint = _RingColor * (1f - 0.45f * progress);
        for (int i = 1; i <= Segments; i++)
        {
            Vector2 next = (MathHelper.TwoPi * i / Segments).ToRotationVector2() * radius;
            Vector2 delta = next - previous;
            Main.EntitySpriteDraw(TextureAssets.MagicPixel.Value, center + previous, new Rectangle(0, 0, 1, 1),
                tint, delta.ToRotation(), new Vector2(0f, 0.5f), new Vector2(delta.Length() + 0.5f, 2f),
                SpriteEffects.None);
            previous = next;
        }
        return false;
    }
}