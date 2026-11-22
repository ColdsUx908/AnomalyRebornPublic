using Anomalies.Visuals.Starburst;
using Terraria.Graphics.CameraModifiers;

namespace Anomalies.Bosses.Skeletron.LegendaryNight;

/// <summary>
/// 按钱币种类产生不同形状的审判攻击。
/// </summary>
/// <remarks>
/// <para>ai[0]：金币种类，0铜、1银、2金、3铂金；ai[1]：锁定时的主体自转帧，机制五存放负的锁定音乐帧；ai[2]：主体 NPC 索引 + 1。</para>
/// <para>localAI：未使用；框架 Timer1..5：未使用；无实体专属 ExtraAI。</para>
/// </remarks>
public sealed class CoinJudgment : AnomalyModProjectile, IStarburstImpactSource
{
    bool IStarburstImpactSource.TryGetImpactFrame(bool includeCross, out StarburstVisuals.Burst frame)
    {
        frame = default;
        return (includeCross || (int)Projectile.ai[0] is 0 or 2) && TryGetBlurFrame(out frame);
    }

    /// <summary>
    /// 预警持续时间，单位为游戏帧。
    /// </summary>
    public const int WarningDuration = 150;
    /// <summary>
    /// 危险预警持续时间，单位为游戏帧。
    /// </summary>
    public const int DangerWarningDuration = 20;
    private const int BoundaryWarningDuration = 30;
    /// <summary>
    /// 危险预警淡入持续时间，单位为游戏帧。
    /// </summary>
    private const int DangerWarningFadeInDuration = 6;
    /// <summary>
    /// 危险预警最大透明度。
    /// </summary>
    private const float DangerWarningMaxOpacity = 0.22f;
    private const int OverlayFadeInDuration = 10;
    /// <summary>
    /// 遮罩保持持续时间，单位为游戏帧。
    /// </summary>
    public const int OverlayHoldDuration = LegendaryNightStarburstParameters.StarburstDamageDuration;
    /// <summary>
    /// 遮罩淡出持续时间，单位为游戏帧。
    /// </summary>
    public const int OverlayFadeOutDuration = LegendaryNightStarburstParameters.StarburstFadeDuration;
    /// <summary>
    /// 遮罩最大透明度。
    /// </summary>
    public const float OverlayMaxOpacity = 0.9f;
    private const int BoundaryFadeInDuration = 12;
    /// <summary>
    /// 环形最大半径，单位为世界像素。
    /// </summary>
    private const float RingMaxRadius = 320f;
    /// <summary>
    /// 圆半径，单位为世界像素。
    /// </summary>
    public const float NormalCircleRadius = 960f;
    /// <summary>
    /// 十字半宽，单位为世界像素。
    /// </summary>
    public const float NormalCrossHalfWidth = 320f;
    private const float Phase2CrossHalfWidth = 240f;
    private const float CrossScreenshakeStrength = 67f;
    private const int CrossScreenshakeDuration = 16;
    private const float CrossScreenshakeFrequency = 38f;
    /// <summary>
    /// 金币外层半径，单位为世界像素。
    /// </summary>
    public const float GoldOuterRadius = 3200f;
    internal static float CrossRotation(int coinType) => coinType == 3 ? MathHelper.PiOver4 : 0f;

    /// <summary>
    /// 爆炸音效。
    /// </summary>
    public static readonly SoundStyle ExplosionSound = new(LegendaryNightHandler.LegendaryNightPath + "Attacks/Coins/CoinJudgmentExplosion") { Volume = 1.5f };
    /// <summary>
    /// 预警音效。
    /// </summary>
    public static readonly SoundStyle WarningSound = SoundID.Item4 with { Volume = 0.65f, Pitch = -0.35f };

    private bool _Judged;
    private bool _WarningSoundPlayed;
    private int _Age = -1;
    private bool IsMechanic5 => Projectile.ai[1] < 0f;
    private int ImpactAge => IsMechanic5
        ? SkeletronLegendHead.Mechanic5Parameters.JudgmentInterval - CoinRoulette.FirstLockFrame
        : WarningDuration;

    private float? ArenaInnerRadius => IsMechanic5
        && LegendaryNightHandler.TryGetLegendaryNightHead((int)Projectile.ai[2], out SkeletronLegendHead head, oneBased: true)
        ? head.Phase2Arena?.MainCircleRadius : null;
    internal float CrossHalfWidth => IsMechanic5 ? Phase2CrossHalfWidth : NormalCrossHalfWidth;
    internal float EffectiveCopperRadius => ArenaInnerRadius ?? NormalCircleRadius;
    internal float EffectiveGoldInnerRadius => ArenaInnerRadius ?? NormalCircleRadius;
    internal bool UsesRadialVisuals => (int)Projectile.ai[0] is 0 or 2 && CoinCrossAssets.IsLoaded;
    internal bool UsesCrossVisuals => (int)Projectile.ai[0] is 1 or 3 && CoinCrossAssets.IsLoaded;

    public float OverlayOpacity => GetOverlayOpacity(_Age - ImpactAge + WarningDuration);
    private bool IsWarningCovered => UsesCrossVisuals && CoinCrossAssets.BodyEffect is not null
        && CoinCrossVisuals.CoversWarning(_Age - ImpactAge + CoinCrossVisuals.PeakAge)
        || UsesRadialVisuals && CoinCrossAssets.RadialEffect is not null
        && CoinRadialVisuals.CoversWarning(_Age - ImpactAge);
    public float WarningOverlayOpacity => IsMechanic5 || IsWarningCovered
        ? 0f : GetWarningOverlayOpacity(_Age - (WarningDuration - DangerWarningDuration), DangerWarningDuration);
    public float BoundaryOpacity => IsMechanic5 || IsWarningCovered ? 0f : _Age < WarningDuration
        ? MathHelper.Clamp((_Age - (WarningDuration - BoundaryWarningDuration)) / (float)BoundaryFadeInDuration, 0f, 1f)
        : OverlayOpacity / OverlayMaxOpacity;
    public static float GetWarningOverlayOpacity(float age, int duration)
    {
        if (age < 0f || age >= duration)
            return 0f;
        return DangerWarningMaxOpacity * MathHelper.Clamp(age / DangerWarningFadeInDuration, 0f, 1f);
    }
    public static float GetOverlayOpacity(float timer)
    {
        float age = timer - WarningDuration;
        if (age < -OverlayFadeInDuration)
            return 0f;
        float fadeIn = MathHelper.Clamp((age + OverlayFadeInDuration + 1f) / (OverlayFadeInDuration + 1f), 0f, 1f);
        float fadeOut = MathHelper.Clamp((OverlayHoldDuration + OverlayFadeOutDuration - age) / OverlayFadeOutDuration, 0f, 1f);
        return OverlayMaxOpacity * fadeIn * fadeOut;
    }

    internal bool TryGetRadialFrame(out CoinRadialVisuals.Frame frame)
    {
        frame = default;
        int age = _Age - ImpactAge;
        if (!UsesRadialVisuals || !CoinRadialVisuals.IsVisible(age))
            return false;
        int coinType = (int)Projectile.ai[0];
        frame = coinType == 0
            ? new(Projectile.Center, EffectiveCopperRadius, 0f, age, coinType)
            : new(Projectile.Center, GoldOuterRadius, EffectiveGoldInnerRadius, age, coinType);
        return true;
    }

    internal bool TryGetBlurFrame(out StarburstVisuals.Burst frame)
    {
        frame = default;
        int age = _Age - ImpactAge;
        if ((int)Projectile.ai[0] is < 0 or > 3 || age < 0 || age >= StarburstVisuals.BlurDuration)
            return false;
        // 四种币共享一个短时 Filter；以实际审判时刻为零，不因十字提前撑开而提早模糊。
        frame = new(Projectile.Center, (int)Projectile.ai[0] == 2 ? GoldOuterRadius : EffectiveCopperRadius, 0f, 0f, age);
        return true;
    }

    internal bool TryGetCrossFrame(in WorldView2D view, out CoinCrossVisuals.Frame frame)
    {
        frame = default;
        // 预览的第 8 帧为最大宽度，严格对齐原有的单帧伤害结算。
        float age = _Age - ImpactAge + CoinCrossVisuals.PeakAge;
        if (!UsesCrossVisuals || !CoinCrossVisuals.IsVisible(age))
            return false;
        float width = CrossHalfWidth * 2f;
        float reach = 800f * width / CoinCrossVisuals.ReferenceWidth;
        Matrix inverse = Matrix.Invert(view.Transform);
        // 无限十字的末端必须在视口外；正常战斗使用固定的设计长度，不随镜头抖动。
        ReadOnlySpan<Vector2> corners = [Vector2.Zero, new(view.Resolution.X, 0f), new(0f, view.Resolution.Y), view.Resolution];
        foreach (Vector2 corner in corners)
            reach = Math.Max(reach, (Vector2.Transform(corner, inverse) + view.ScreenPosition - Projectile.Center).Length() / 0.6f);
        frame = new(Projectile.Center, width, reach * 2f, CrossRotation((int)Projectile.ai[0]), age, (int)Projectile.ai[0]);
        return true;
    }

    public override string Texture => TOTextures.InvisibleTexturePath;
    public override string LocalizationCategory => LegendaryNightHandler.LocalizationCategory;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 2;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.penetrate = -1;
        Projectile.netImportant = true;
        Projectile.timeLeft = WarningDuration + Math.Max(OverlayHoldDuration + OverlayFadeOutDuration,
            Math.Max(CoinCrossVisuals.EndAge - CoinCrossVisuals.PeakAge, CoinRadialVisuals.EndAge)) + 2;
    }

    // 伤害由本机玩家的 Hurt 路径结算，避免常规敌对弹幕检测提前被无敌帧拦截或重复命中。
    public override bool? CanDamage() => false;
    public override bool ShouldUpdatePosition() => false;
    public override bool PreDraw(ref Color lightColor)
    {
        // 蓝圈与复制币固定在锁定位置，共用审判计时。
        if (_Age >= 0 && _Age < ImpactAge)
        {
            float radiusFactor = 1f - _Age / (float)ImpactAge;
            Vector2 center = Projectile.Center - Main.screenPosition;
            Texture2D ring = ParticleHandler.GetTexture<PulseRing>();
            Main.spriteBatch.DrawFromCenter(ring, center, null,
                (CoinJudgmentPalette.For((int)Projectile.ai[0]).Light with { A = 0 }), scale: RingMaxRadius * radiusFactor / PulseRing.TextureRadius);
            CoinRoulette.DrawCoin(center, (int)Projectile.ai[0], 0, 1f, 0.8f);
        }
        return false;
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        Vector2 center = Projectile.Center;
        int coinType = (int)Projectile.ai[0];
        return coinType switch
        {
            0 => new Circle(center, EffectiveCopperRadius).Collides(targetHitbox),
            1 or 3 => CrossCollides(),
            2 => new Annulus(center, EffectiveGoldInnerRadius, GoldOuterRadius).Collides(targetHitbox),
            _ => false
        };

        bool CrossCollides()
        {
            // 将条带延伸至目标碰撞箱之外，检测时等价于无限延伸的十字。
            Vector2 offset = new Vector2(targetHitbox.Center.X, targetHitbox.Center.Y) - center;
            // 两条无限长条带的法向投影。整像素余量覆盖奇数尺寸中心取整和浮点误差；
            // 临界位置仍交给原有的旋转矩形判定。
            float halfWidth = CrossHalfWidth;
            if (coinType == 1)
            {
                if (MathF.Abs(offset.X) > halfWidth + targetHitbox.Width * 0.5f + 1f &&
                    MathF.Abs(offset.Y) > halfWidth + targetHitbox.Height * 0.5f + 1f)
                    return false;
            }
            else
            {
                float limit = halfWidth * MathF.Sqrt(2f) + (targetHitbox.Width + targetHitbox.Height) * 0.5f + 2f;
                if (MathF.Abs(offset.X + offset.Y) > limit && MathF.Abs(offset.X - offset.Y) > limit)
                    return false;
            }
            float reach = offset.Length() + targetHitbox.Width + targetHitbox.Height + 1f;
            Vector2 axis = Vector2.UnitX.RotatedBy(CrossRotation(coinType)) * reach;
            Vector2 perpendicular = new(-axis.Y, axis.X);
            return RotatedRectangle.FromLineSegment(center - axis, center + axis, CrossHalfWidth * 2f).Collides(targetHitbox)
                || RotatedRectangle.FromLineSegment(center - perpendicular, center + perpendicular, CrossHalfWidth * 2f).Collides(targetHitbox);
        }
    }

    public override void AI()
    {
        if (!LegendaryNightHandler.TryGetLegendaryNightHead((int)Projectile.ai[2], out SkeletronLegendHead head, oneBased: true)
            || (IsMechanic5 ? !head.Mechanic5Active : head.CurrentPhase != SkeletronLegendHead.Phase.Phase1
                || head.CurrentSpell != SkeletronLegendHead.Spell.PlatinumCoin))
        {
            _Age = -1;
            if (TONetworkEnvironment.NotClient)
                Projectile.Kill();
            return;
        }

        // 第二次结算所在帧已切回常态；从常态计时接续冲击余光。
        int spinTime = head.CurrentBehavior == SkeletronLegendHead.Behavior.Spin
            ? head.Timer1
            : CoinRoulette.RotationDuration + head.Timer1;
        int timer = IsMechanic5 ? head.Phase2MusicElapsedTime + (int)Projectile.ai[1] : spinTime - (int)Projectile.ai[1];
        UpdateVisuals(timer);

        if (timer != ImpactAge || _Judged)
            return;

        _Judged = true;
        if (!IsMechanic5 && TONetworkEnvironment.NotClient)
            head.RecordPhase1CoinJudgment((int)Projectile.ai[0]);
        if (Main.dedServ)
            return;

        SoundEngine.PlaySound(ExplosionSound);

        Player player = Main.LocalPlayer;
        if (!player.active || player.dead || player.ghost || Colliding(Projectile.Hitbox, player.Hitbox) != true)
            return;

        LegendaryNightDamage.HurtLocalPlayer(Projectile, Projectile.damage, true, knockback: 4.5f);
    }

    // 本地预览只推进表现；正式 AI 仍独立执行一次性的伤害与战斗记录。
    internal void UpdateVisuals(int timer)
    {
        // 激光最大宽度与审判结算同帧；共享视觉入口也覆盖本地预览，重复更新不重复触发。
        if (!Main.dedServ && timer == ImpactAge && _Age != timer && (int)Projectile.ai[0] is 1 or 3)
            Main.instance.CameraModifiers.Add(new PunchCameraModifier(Projectile.Center,
                CrossRotation((int)Projectile.ai[0]).ToRotationVector2(), CrossScreenshakeStrength,
                CrossScreenshakeFrequency, CrossScreenshakeDuration, uniqueIdentity: "Anomalies:CoinCross"));
        _Age = timer;
        if (!IsMechanic5 && !_WarningSoundPlayed && timer >= WarningDuration - BoundaryWarningDuration && timer < WarningDuration && !Main.dedServ)
        {
            _WarningSoundPlayed = true;
            SoundEngine.PlaySound(WarningSound);
        }
    }
}
