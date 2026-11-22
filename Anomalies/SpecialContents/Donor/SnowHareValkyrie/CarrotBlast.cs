namespace Anomalies.SpecialContents.Donor.SnowHareValkyrie;

public sealed class CarrotBlast : AnomalyModProjectile
{
    private const int Lifetime = 80;
    private const float MaxCoreDiameter = 145.2f;
    private const float HaloScale = 1.14f;
    private const float DamageMultiplier = 0.75f;

    internal static void Spawn(Projectile missile) =>
        Projectile.NewProjectileAction<CarrotBlast>(missile.GetSource_Death(), missile.Center, Vector2.Zero,
            Math.Max(1, (int)(missile.damage * DamageMultiplier)), missile.knockBack, missile.owner);

    private float Progress => (Lifetime - Projectile.timeLeft) / (float)Lifetime;
    private float CoreDiameter => MathHelper.Lerp(28f, MaxCoreDiameter, 1f - MathF.Pow(1f - Progress, 4f));
    private float HaloDiameter => CoreDiameter * HaloScale;

    public override string LocalizationCategory => SnowHareValkyrie.LocalizationCategoryForSnowHareValkyrie;
    public override bool IsLoadingEnabled(Mod mod) => AnomalySpecialContentToggler.SnowHareValkyrie;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = (int)MathF.Ceiling(MaxCoreDiameter * HaloScale);
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.penetrate = -1;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.timeLeft = Lifetime;
    }

    public override bool? CanHitNPC(NPC target) =>
        ProjectileDamageBounds.Around(Projectile.Center, MaxCoreDiameter * HaloScale * 0.5f).CanHitNPC(Type, target);

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        Vector2 center = Projectile.Center;
        Vector2 closest = Vector2.Clamp(center, targetHitbox.TopLeft(), targetHitbox.BottomRight());
        float radius = HaloDiameter * 0.5f;
        return Vector2.DistanceSquared(center, closest) <= radius * radius;
    }

    public override void AI()
    {
        if (!Main.dedServ && Projectile.timeLeft == Lifetime)
            SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D atlas = TextureAssets.Projectile[Type].Value;
        Vector2 position = Projectile.Center - Main.screenPosition;
        float age = Lifetime - Projectile.timeLeft;
        float progress = Progress;
        DrawLayer(1, HaloDiameter, 1f - MathHelper.Clamp((age - 65f) / 15f, 0f, 1f), 0f);
        float coreOpacity = 1f - MathHelper.Clamp((age - 40f) / 40f, 0f, 1f);
        if (AnomalyClientConfig.Instance.EnableShaders && SnowHareValkyrieEffects.CarrotBlast is not null)
        {
            using (Main.spriteBatch.Scope(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp,
                DepthStencilState.None, RasterizerState.CullNone, transformMatrix: Main.GameViewMatrix.TransformationMatrix, effectIsNull: true))
            {
                SnowHareValkyrieEffects.CarrotBlast.Data.SetCustomParameter("uProgress", progress).Apply();
                DrawLayer(0, CoreDiameter, coreOpacity, progress * 0.9f);
            }
        }
        else
            DrawLayer(0, CoreDiameter, coreOpacity, progress * 0.9f);
        return false;

        void DrawLayer(int row, float size, float opacity, float rotation)
        {
            Rectangle frame = atlas.Frame(1, 2, 0, row);
            Main.EntitySpriteDraw(atlas, position, frame, Color.White * opacity, rotation, frame.Size() * 0.5f,
                size / frame.Width, SpriteEffects.None);
        }
    }
}
