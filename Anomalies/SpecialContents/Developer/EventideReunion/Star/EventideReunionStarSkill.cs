using Anomalies.Visuals.Starburst;

namespace Anomalies.SpecialContents.Developer.EventideReunion;

public sealed partial class EventideReunionStarSkill : AnomalyModProjectile, IStarburstImpactSource
{
    private static readonly Vector2 _WeaponGrip = new(144f, 156f);
    private static readonly Vector2 _WeaponTip = new(299f, 0f);
    private static Vector2 TipOffset(float angle) => (_WeaponTip - _WeaponGrip).RotatedBy(angle + MathHelper.PiOver4);

    internal const int Launch = 72;
    private const int FlightDuration = 30;
    private const float FlightSpeed = 32f;
    internal const int Arrival = Launch + FlightDuration + 1;
    internal const int Burst = Arrival + 84;
    internal const int WaveDuration = 150;
    internal static float WaveRadius(float elapsed)
    {
        // 先快速展开，再以连续速度滑向外场。
        float t = Math.Clamp(elapsed / WaveDuration, 0f, 1f);
        return 1600f * (1f - MathF.Pow(1f - t, 3f));
    }

    internal const int Recovery = Burst + EventideReunionRecall.Duration;
    private const int End = Burst + 240;
    private Vector2 _Origin;
    private bool _Locked;
    private Vector2 _PreviousCenter;
    private int Age => (int)Projectile.ai[0];
    private Vector2 Direction => new Vector2(Projectile.ai[1], Projectile.ai[2]).SafeNormalize(Vector2.UnitX * Projectile.Owner.direction);

    public override bool IsLoadingEnabled(Mod mod) => AnomalySpecialContentToggler.EventideReunion;
    public override string Texture => EventideReunionAssets.WeaponPath;
    public override string LocalizationCategory => EventideReunion.LocalizationCategoryForEventideReunion;
    public override LocalizedText DisplayName => ModContent.GetModItem<EventideReunion>().DisplayName;
    internal static bool IsBlockingActions(Player player)
    {
        if (player.ownedProjectileCounts[ModContent.ProjectileType<EventideReunionStarSkill>()] == 0)
            return false;
        foreach (Projectile active in Main.ActiveProjectiles)
            if (active.owner == player.whoAmI && active.ModProjectile is EventideReunionStarSkill { Age: < Recovery })
                return true;
        return false;
    }

    internal static void Release(Player player, Vector2 direction)
    {
        Item weapon = player.HeldItem;
        if (player.whoAmI != Main.myPlayer || player.dead || player.CCed || player.noItems
            || weapon.type != ModContent.ItemType<EventideReunion>() || player.Anomaly.EventideReunion_StarCooldown > 0 || IsBlockingActions(player))
            return;
        Projectile.NewProjectileAction<EventideReunionStarSkill>(player.GetSource_ItemUse(weapon), player.MountedCenter,
            Vector2.Zero, player.GetWeaponDamage(weapon), 0f, player.whoAmI, p =>
            {
                p.ai[1] = direction.X;
                p.ai[2] = direction.Y;
                player.Anomaly.EventideReunion_StarCooldown = EventideReunionResonancePlayer.Release(player, EventideReunionAspect.Star, 1320);
                p.originalDamage = p.damage = player.GetWeaponDamage(weapon);
            });
    }

    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 5000;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 24;
        Projectile.friendly = true;
        Projectile.penetrate = -1;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.ignoreWater = Projectile.netImportant = true;
        Projectile.tileCollide = false;
        Projectile.DamageType = DamageClass.Melee;
        Projectile.timeLeft = End + 600;
    }

    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => Projectile.IsOnOwnerClient && Age > Launch && Age < Arrival ? null : false;
    public override bool CanHitPvp(Player target) => false;
    public override bool? CanHitNPC(NPC target) =>
        CanDamage() != false && target.CanBeChasedBy(Projectile) ? null : false;

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) =>
        new Circle(Projectile.Center, 12f).Collides(targetHitbox) || _PreviousCenter != Projectile.Center
        && RotatedRectangle.FromLineSegment(_PreviousCenter, Projectile.Center, 24f).Collides(targetHitbox);

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) =>
        modifiers.SourceDamage *= EventideReunionDamageConstants.StarPierceDamageMultiplier;

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (Projectile.IsOnOwnerClient && Age > Launch && Age < Arrival)
            StopFlight();
    }

    private void StopFlight()
    {
        Projectile.ai[0] = Arrival;
        Projectile.netUpdate = true;
    }

    private Vector2 FlightPosition(float elapsed) =>
        _Origin + TipOffset(Direction.ToRotation()) + Direction * (FlightSpeed * elapsed);
    protected override void SendAnomalyExtraAI(BinaryWriter writer)
    {
        writer.Write(_Locked);
        writer.WriteVector2(_Origin);
    }
    protected override void ReceiveAnomalyExtraAI(BinaryReader reader)
    {
        _Locked = reader.ReadBoolean();
        _Origin = reader.ReadVector2();
    }

    bool IStarburstImpactSource.TryGetImpactFrame(bool includeCross, out StarburstVisuals.Burst frame) =>
        StarburstPostProcessing.TryCreateShockwaveFrame(Projectile.Center, 180f, Age - Burst, 0.55f, 18, out frame);

    public override void AI()
    {
        Player player = Projectile.Owner;
        if (!player.active || player.dead || Age < Launch && (player.CCed || player.noItems || player.HeldItem.type != ModContent.ItemType<EventideReunion>()))
        {
            Projectile.Kill();
            return;
        }
        // ai[0] 为动作时钟，ai[1..2] 为瞄准方向；蓄满时锁定方向和出手位置。
        if (Age <= Launch && !_Locked)
        {
            _Origin = player.MountedCenter;
            if (Projectile.owner == Main.myPlayer)
            {
                Vector2 aim = (Main.MouseWorld - _Origin).SafeNormalize(Direction);
                Projectile.ai[1] = aim.X;
                Projectile.ai[2] = aim.Y;
                _Locked = Age == Launch;
                Projectile.netUpdate |= Age % 8 == 0 || _Locked;
            }
            if (Age == Launch && !_Locked)
                return;
        }

        if (Age < Launch)
            Projectile.Center = player.MountedCenter;
        else if (Age < Arrival)
        {
            int elapsed = Age - Launch;
            if (Projectile.IsOnOwnerClient && (elapsed >= FlightDuration || EventideReunionInput.StopPressed(player)))
            {
                if (elapsed == 0)
                    Projectile.Center = FlightPosition(0f);
                StopFlight();
            }
            else
            {
                _PreviousCenter = FlightPosition(elapsed);
                Projectile.Center = FlightPosition(Math.Min(elapsed + 1, FlightDuration));
            }
        }
        if (Age < Recovery)
        {
            player.heldProj = Projectile.whoAmI;
            player.itemTime = player.itemAnimation = 2;
            if (Age < Launch && Direction.X != 0f)
                player.ChangeDir(Direction.X > 0f ? 1 : -1);
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, SpearAngle - MathHelper.PiOver2);
        }
        if (Projectile.IsOnOwnerClient && Age == Burst)
        {
            Projectile.NewProjectileAction<EventideReunionStarStrike>(Projectile.GetSource_FromAI(), Projectile.Center, Vector2.Zero,
                (int)Math.Clamp(Projectile.damage * (double)EventideReunionDamageConstants.StarWaveDamageMultiplier, 1d, int.MaxValue),
                0f, Projectile.owner);
        }
        if (!Main.dedServ)
            PlayCues();
        if (Age >= End)
            Projectile.Kill();
        else if (Projectile.IsOnOwnerClient || Age != Arrival - 1)
            Projectile.ai[0]++;
    }
}

public sealed class EventideReunionStarPlayer : AnomalyPlayerBehavior
{
    public override bool IsLoadingEnabled(Mod mod) => AnomalySpecialContentToggler.EventideReunion;
    public override void PostUpdate()
    {
        if (AnomalyPlayer.EventideReunion_StarCooldown > 0)
            AnomalyPlayer.EventideReunion_StarCooldown--;
    }
    public override void UpdateDead() => PostUpdate();
    public override void SetControls()
    {
        if (EventideReunionStarSkill.IsBlockingActions(Player))
            Player.controlUseItem = Player.controlUseTile = false;
    }
}
