using Anomalies.Assets.Effects;
using Anomalies.Visuals.Overlays;
using Anomalies.Visuals.Starburst;
using static Anomalies.GameContents.LegendaryNight.StarfallKnockerGeometry;

namespace Anomalies.GameContents.LegendaryNight;

public sealed class StarfallKnockerBurst : AnomalyModProjectile, IStarburstImpactSource, IOverlaySource, IOverlayDrawable
{
    private const int Lifetime = StarburstCircleVisuals.EndAge;
    private float Age => Projectile.ai[0] - 1f;
    private StarburstVisuals.Burst Frame => new(Projectile.Center, BurstRadius, 0f, 0f, Age);
    private bool _SoundPlayed;

    public override string Texture => TOTextures.InvisibleTexturePath;
    public override string LocalizationCategory => "GameContents";
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => Age is >= 0f and < StarburstPresentation.DamageDuration ? null : false;
    public override bool CanHitPvp(Player target) => false;
    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 250;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 240;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Melee;
        Projectile.penetrate = -1;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        // 原生更新会在绘制前扣除 timeLeft；为 0..83 的完整动画保留一帧余量。
        Projectile.timeLeft = Lifetime + 1;
    }

    public override void AI()
    {
        Projectile.ai[0]++;
        if (Age >= Lifetime)
        {
            Projectile.Kill();
            return;
        }
        if (Main.dedServ)
            return;
        if (!_SoundPlayed)
        {
            _SoundPlayed = true;
            SoundEngine.PlaySound(StarburstPresentation.Sound, Projectile.Center);
        }
        Lighting.AddLight(Projectile.Center, new Vector3(1f, 0.65f, 0.18f) * Math.Max(0f, 1f - Age / Lifetime));
    }

    public override bool? CanHitNPC(NPC target) => ProjectileDamageBounds.Around(Projectile.Center, BurstRadius).CanHitNPC(Type, target);

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) =>
        new Circle(Projectile.Center, BurstRadius).Collides(targetHitbox);

    bool IStarburstImpactSource.TryGetImpactFrame(bool includeCross, out StarburstVisuals.Burst frame)
    {
        frame = Frame;
        return StarburstCircleAssets.IsLoaded && StarburstCircleVisuals.IsVisible(Age);
    }

    public override bool PreDraw(ref Color lightColor) => false;

    public void SubmitOverlays(OverlayQueue overlays)
    {
        if (Projectile.active && StarburstCircleAssets.IsLoaded && StarburstCircleVisuals.IsVisible(Age))
            overlays.Submit(this, OverlayPriority.Attack);
    }

    // 由 All 遮罩阶段调用，调用前后 SpriteBatch 均未开始。
    public void DrawOverlay(SpriteBatch spriteBatch)
    {
        if (!StarburstCircleAssets.IsLoaded)
            return;
        WorldView2D view = new(Main.screenPosition, Main.GameViewMatrix.TransformationMatrix,
            new Vector2(Main.screenWidth, Main.screenHeight));
        StarburstVisuals.Burst frame = Frame;
        Effect bloom = AnomalyClientConfig.Instance.EnableShaders ? AnomalyCommonEffects.LightweightBloom?.Asset.Value : null;
        StarburstPresentation.DrawBoundary(spriteBatch, frame);
        StarburstCircleDrawing.Draw(spriteBatch, bloom, StarburstCircleAssets.Textures, view, frame, Main.dayTime);
    }
}
