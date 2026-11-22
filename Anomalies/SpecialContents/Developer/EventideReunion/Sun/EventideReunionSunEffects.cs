using Anomalies.Visuals.Overlays;
using Terraria.Graphics.Effects;

namespace Anomalies.SpecialContents.Developer.EventideReunion;

public sealed class EventideReunionSunExplosion : AnomalyModProjectile
{
    public override bool IsLoadingEnabled(Mod mod) => AnomalySpecialContentToggler.EventideReunion;

    public override string LocalizationCategory => EventideReunion.LocalizationCategoryForEventideReunion;
    private const int Duration = 32;
    public float FlashOpacity => Math.Max(0f, 1f - Age / 10f) * 0.2f;
    private int Age => Duration - Projectile.timeLeft;
    public override string Texture => EventideReunionRuneAssets.SunPath;
    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 512;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 2;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Melee;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.penetrate = -1;
        Projectile.timeLeft = Duration;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => Age < 2 ? null : false;
    public override void AI()
    {
        Lighting.AddLight(Projectile.Center, 1.5f, 0.8f, 0.15f);
        if (Age != 0 || Main.dedServ)
            return;
        SoundEngine.PlaySound(EventideReunionSunAssets.SunDashHit, Projectile.Center);
        float proximity = MathHelper.Clamp(1f - Main.LocalPlayer.Distance(Projectile.Center) / 1200f, 0f, 1f);
        if (proximity > 0f)
            Main.LocalPlayer.SetScreenshake(10f * proximity);
        // 仅使用项目自身的粒子系统与本地资源。
        ParticleHandler.SpawnParticle(new BloomParticle(Projectile.Center, Vector2.Zero, Color.OrangeRed,
            0.2f, 2.7f, 32, lifeEndRatio: 0.1f, useLargeTexture: true));
        ParticleHandler.SpawnParticle(new BloomParticle(Projectile.Center, Vector2.Zero, Color.LightGoldenrodYellow,
            0.1f, 1.6f, 20, lifeEndRatio: 0.05f, useLargeTexture: true));
        ParticleHandler.SpawnParticle(new PulseRing(Projectile.Center, Vector2.Zero,
            Color.LightGoldenrodYellow, 0.05f, 2.7f, 20));
        ParticleHandler.SpawnParticle(new PulseRing(Projectile.Center, Vector2.Zero,
            Color.DarkOrange, 0.05f, 3.55f, 28));
        for (int i = 0; i < 48; i++)
        {
            Vector2 direction = (MathHelper.TwoPi * i / 48f + Main.rand.NextFloat(-0.04f, 0.04f)).ToRotationVector2();
            ParticleHandler.SpawnParticle(new PointingParticle(
                Projectile.Center + direction * Main.rand.NextFloat(42f, 76f),
                direction * Main.rand.NextFloat(6f, 20f), false,
                Main.rand.Next(26, 50), Main.rand.NextFloat(0.6f, 1.35f),
                i % 4 == 0 ? Color.LightYellow : Color.Lerp(Color.Gold, Color.OrangeRed, Main.rand.NextFloat())));
        }
        for (int i = 0; i < 12; i++)
        {
            Vector2 direction = Main.rand.NextVector2Unit();
            ParticleHandler.SpawnParticle(new MediumMistParticle(
                Projectile.Center + direction * Main.rand.NextFloat(12f, 64f), direction * Main.rand.NextFloat(8f, 26f),
                Color.Gold, Color.OrangeRed, Main.rand.NextFloat(0.9f, 1.8f), 180f, 0.05f));
        }
    }
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        => new Circle(Projectile.Center, EventideReunionSunSkill.BlastRadius).Collides(targetHitbox);
    public override void ModifyHitNPC(NPC targetNPC, ref NPC.HitModifiers modifiers)
        => modifiers.SourceDamage *= Main.dayTime ? EventideReunionDamageConstants.SunDayDamageMultiplier : EventideReunionDamageConstants.SunNightDamageMultiplier;
    public override void OnHitNPC(NPC targetNPC, NPC.HitInfo hit, int damageDone) => targetNPC.AddBuff(BuffID.OnFire, 180);
    public override bool PreDraw(ref Color lightColor)
    {
        float progress = MathHelper.Clamp(Age / (float)Duration, 0f, 1f);
        float expansion = 1f - MathF.Pow(1f - progress, 4f);
        float opacity = 1f - MathHelper.SmoothStep(0f, 1f, progress);
        Texture2D glow = BloomParticle.BloomCircleLargeEmission;
        Vector2 origin = glow.Size() * 0.5f;
        // 拉伸柔光形成尖细的日芒，长短交错并缓慢转动；零 Alpha 在现有批次中叠加发光。
        Color gold = new Color(255, 165, 35, 0) * opacity;
        Color core = new Color(255, 245, 190, 0) * opacity;
        const int RayCount = 24;
        for (int i = 0; i < RayCount; i++)
        {
            float angle = MathHelper.TwoPi * i / RayCount + Projectile.identity * 0.37f + progress * 0.18f;
            Vector2 direction = angle.ToRotationVector2();
            float length = MathHelper.Lerp(70f, i % 3 == 0 ? 480f : i % 2 == 0 ? 340f : 230f, expansion);
            float width = MathHelper.Lerp(i % 3 == 0 ? 28f : 18f, 5f, progress);
            Vector2 center = Projectile.Center + direction * (30f + expansion * 45f + length * 0.5f);
            Vector2 scale = new(length / glow.Width, width / glow.Height);
            Main.spriteBatch.Draw(glow, center - Main.screenPosition, null, gold, angle, origin,
                scale * new Vector2(1f, 2.5f), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(glow, center - Main.screenPosition, null, core, angle, origin,
                scale, SpriteEffects.None, 0f);
        }
        // 先点燃光球，日冕随即张开；收矛时光球收缩，喷出的金线继续向外运动。
        float ignition = MathHelper.SmoothStep(0f, 1f, Age / 6f);
        float cooling = MathHelper.SmoothStep(0f, 1f, (Age - 9f) / (Duration - 10f));
        float diskRadius = EventideReunionSunSkill.BlastRadius * (0.30f + ignition * 0.55f) * (1f - cooling * 0.65f);
        EventideReunionEffects.DrawPhotosphere(Projectile.Center, diskRadius * 3.5f,
            Age * 0.055f + Projectile.identity % 17, (1f - ignition) * 0.8f, 1f - cooling, solar: true);
        return false;
    }
}

public sealed class EventideReunionSunFlash : CustomOverlay
{
    public override bool IsLoadingEnabled(Mod mod) => AnomalySpecialContentToggler.EventideReunion;

    public override void Draw(SpriteBatch batch, RenderLayers layer, bool beginSpriteBatch)
    {
        if (Main.dedServ || Main.gameMenu || layer != RenderLayers.All)
            return;
        float opacity = 0f;
        foreach (Projectile active in Main.ActiveProjectiles)
        {
            if (active.ModProjectile is EventideReunionSunExplosion explosion)
                opacity = Math.Max(opacity, explosion.FlashOpacity * MathHelper.Clamp(1f - Main.LocalPlayer.Distance(active.Center) / 1200f, 0f, 1f));
        }
        if (opacity <= 0f)
            return;
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
            DepthStencilState.None, Main.Rasterizer, null, Matrix.Identity);
        batch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.White * opacity);
        batch.End();
    }
}
