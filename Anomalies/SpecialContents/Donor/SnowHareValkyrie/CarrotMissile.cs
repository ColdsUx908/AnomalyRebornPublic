namespace Anomalies.SpecialContents.Donor.SnowHareValkyrie;

public sealed partial class CarrotMissile : AnomalyModProjectile
{
    private const int Lifetime = 240;
    private const float DrawScale = 1f;
    private const float BodyWidthScale = 0.8f;
    private const int FadeDuration = 18;
    private const float MinTurnRate = 0.10f;
    private const float SeekRange = 640f;

    private bool IsMarkerMissile => Projectile.ai[0] == 1f;

    public override string LocalizationCategory => SnowHareValkyrie.LocalizationCategoryForSnowHareValkyrie;
    public override bool IsLoadingEnabled(Mod mod) => AnomalySpecialContentToggler.SnowHareValkyrie;

    public override void SetStaticDefaults() => Main.projFrames[Type] = 3;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 12;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.penetrate = 1;
        Projectile.timeLeft = Lifetime;
    }

    public override void AI()
    {
        if (IsMarkerMissile && !Main.dedServ)
            Lighting.AddLight(Projectile.Center, new Vector3(1f, 0.36f, 0.06f) * Projectile.Opacity);
        if (IsAttached)
        {
            UpdateAttached();
            return;
        }
        Projectile.frame = (int)Projectile.ai[0];
        NPC target = NPC.GetNPCFromIndex((int)Projectile.ai[1] - 1);
        if (Projectile.IsOnOwnerClient && (Lifetime - Projectile.timeLeft) % 8 == 0
            && (target is null || !target.CanBeChasedBy(Projectile) || Projectile.Distance(target.Center) > SeekRange))
        {
            target = TOKinematicUtils.GetNPCTarget(Projectile.Center, SeekRange, ignoreTiles: false);
            int encoded = (target?.whoAmI ?? -1) + 1;
            if (Projectile.ai[1] != encoded)
            {
                Projectile.ai[1] = encoded;
                Projectile.netUpdate = true;
            }
        }

        if (target is not null && target.CanBeChasedBy(Projectile))
        {
            Vector2 offset = target.Center - Projectile.Center;
            float speed = Projectile.velocity.Length();
            float distance = offset.Length();
            // 近距离提高角速度，使转弯半径小于目标距离，避免沿切线绕行。
            float turnRate = Math.Max(MinTurnRate, 2f * MathF.Atan2(speed, distance));
            float angle = Projectile.velocity.ToRotation().AngleTowards(offset.ToRotation(), turnRate);
            Projectile.velocity = angle.ToRotationVector2() * Math.Min(speed, distance);
        }
        Projectile.Opacity = MathHelper.SmoothStep(0f, 1f, Math.Min(1f, Projectile.timeLeft / (float)FadeDuration));
        Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
    }

    // 长半轴 27.5 加上黄色导弹的半像素中心偏移，覆盖任意旋转。
    private ProjectileDamageBounds DamageBounds => ProjectileDamageBounds.Around(Projectile.Center, 28f * DrawScale);

    public override bool? CanHitNPC(NPC target) => DamageBounds.CanHitNPC(Type, target);

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        if (targetHitbox.Width <= 0 || targetHitbox.Height <= 0 || DamageBounds.Excludes(targetHitbox))
            return false;

        // 两色弹身按可见像素边缘取半轴，黄色贴图中心比红色高半个像素。
        bool gold = Projectile.ai[0] == 1f;
        Vector2 center = Projectile.Center;
        if (gold)
            center += new Vector2(0f, -0.5f * DrawScale).RotatedBy(Projectile.rotation);
        Ellipse body = new(center, (gold ? 14f : 15f) * BodyWidthScale * DrawScale,
            (gold ? 27.5f : 27f) * DrawScale, Projectile.rotation);
        return body.Collides(targetHitbox);
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (IsMarkerMissile && Projectile.IsOnOwnerClient)
            Attach(target, Projectile.Center);
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        if (!IsMarkerMissile)
            return true;
        if (Projectile.IsOnOwnerClient)
            Attach(null, Projectile.Center + Projectile.velocity);
        return false;
    }

    public override void OnKill(int timeLeft)
    {
        if (!IsAttached && timeLeft <= 0 && !Main.dedServ)
        {
            Color color = Projectile.ai[0] == 1f ? Color.Gold : Color.LightCoral;
            for (int i = 0; i < 6; i++)
            {
                Dust.NewDustPerfectAction(Projectile.Center, DustID.TintableDustLighted, dust =>
                {
                    dust.noGravity = true;
                    dust.color = color;
                    dust.velocity = Projectile.velocity * 0.12f + Main.rand.NextVector2Circular(2f, 2f);
                    dust.scale = 0.85f;
                });
            }
        }
        if (!IsMarkerMissile && Projectile.IsOnOwnerClient && timeLeft > 0)
            CarrotBlast.Spawn(Projectile);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        if (IsAttached)
            DrawRing();
        DrawMissile(Projectile.Center, Projectile.rotation, (int)Projectile.ai[0], IsAttached ? 10 : Lifetime - Projectile.timeLeft, Projectile.Opacity);
        return false;
    }

    internal static void DrawMissile(Vector2 position, float rotation, int frame, int age, float opacity)
    {
        Texture2D texture = TextureAssets.Projectile[ModContent.ProjectileType<CarrotMissile>()].Value;
        Vector2 center = position - Main.screenPosition;
        if (frame == 1)
        {
            Texture2D glow = BloomParticle.BloomCircleLargeEmission;
            Main.EntitySpriteDraw(glow, center, null, new Color(255, 107, 16, 0) * opacity, rotation,
                glow.Size() * 0.5f, new Vector2(72f / glow.Width, 108f / glow.Height), SpriteEffects.None);
        }
        Color tint = Color.White * opacity;
        Vector2 bodyScale = new Vector2(BodyWidthScale, 1f) * DrawScale;
        Rectangle leaf = texture.Frame(1, 3, 0, 2);
        Vector2 leafOrigin = leaf.Size() * 0.5f;
        // 原作三枚米弹以 10 度为步长，每步向弹尾移动约 3 个贴图像素。
        float distance = 3f * Math.Clamp(age, 0, 10) * DrawScale;
        for (int i = -1; i <= 1; i++)
        {
            float leafRotation = rotation - i * MathHelper.ToRadians(10f);
            Main.EntitySpriteDraw(texture, center + (Vector2.UnitY * distance).RotatedBy(leafRotation), leaf, tint, leafRotation, leafOrigin,
                new Vector2(0.5f, 1f) * DrawScale, SpriteEffects.None);
        }
        Rectangle body = texture.Frame(1, 3, 0, frame);
        Main.EntitySpriteDraw(texture, center, body, tint, rotation, body.Size() * 0.5f,
            bodyScale, SpriteEffects.None);
    }
}
