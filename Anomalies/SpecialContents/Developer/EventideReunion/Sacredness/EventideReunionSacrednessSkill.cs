namespace Anomalies.SpecialContents.Developer.EventideReunion;

public sealed partial class EventideReunionSacrednessSkill : AnomalyModProjectile
{
    private const int Cooldown = 1080;
    private const int DashStart = 30;
    private const int ThrowStart = DashStart + 10;
    private const int BeamStart = ThrowStart + 20;
    private const int RecallStart = BeamStart + 60;
    private const int End = RecallStart + EventideReunionRecall.Duration;
    private const float DashSpeed = 64f;
    private const float SpearLength = 220f;
    private const float BeamWidth = 256f;
    private const int BeamHalfHeight = 4096;
    private const float BeamCapLength = BeamWidth * 0.5f;
    // 与惠惠法杖一致，每个目标每三帧可被光柱命中一次。
    private const int BeamHitInterval = 3;

    private int Age => (int)Projectile.ai[0];
    internal bool IsDashing => Age is >= DashStart and < ThrowStart;
    private bool IsBeaming => Age is >= BeamStart and < RecallStart;
    private Vector2 Aim => Projectile.velocity.SafeNormalize(Vector2.UnitX * Projectile.Owner.direction);
    private bool _DashLocked;
    private Vector2 _DashOrigin;
    private Vector2 _DashEnd;
    private Vector2 _Impact;
    private bool _PulseSent;

    public override bool IsLoadingEnabled(Mod mod) => AnomalySpecialContentToggler.EventideReunion;
    public override string Texture => EventideReunionAssets.WeaponPath;
    public override string LocalizationCategory => EventideReunion.LocalizationCategoryForEventideReunion;

    internal static void Release(Player player, Vector2 direction)
    {
        Item weapon = player.HeldItem;
        if (player.whoAmI != Main.myPlayer || player.dead || player.noItems || player.CCed
            || weapon.type != ModContent.ItemType<EventideReunion>() || player.Anomaly.EventideReunion_SacrednessCooldown > 0
            || player.ownedProjectileCounts[ModContent.ProjectileType<EventideReunionSacrednessSkill>()] > 0)
            return;
        Projectile.NewProjectileAction<EventideReunionSacrednessSkill>(player.GetSource_ItemUse(weapon), player.MountedCenter,
            direction.SafeNormalize(Vector2.UnitX * player.direction), player.GetWeaponDamage(weapon),
            player.GetWeaponKnockback(weapon, weapon.knockBack), player.whoAmI, p =>
            {
                player.Anomaly.EventideReunion_SacrednessCooldown = EventideReunionResonancePlayer.Release(
                    player, EventideReunionAspect.Sacredness, Cooldown);
                p.originalDamage = p.damage = player.GetWeaponDamage(weapon);
            });
    }

    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = BeamHalfHeight + (int)BeamCapLength;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 16;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Melee;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = Projectile.netImportant = true;
        Projectile.penetrate = -1;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.timeLeft = End + 600;
    }

    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => Projectile.IsOnOwnerClient && (IsDashing || IsBeaming) ? null : false;
    public override bool CanHitPvp(Player target) => false;
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        if (IsBeaming)
            return RotatedRectangle.FromLineSegment(_Impact - Vector2.UnitY * BeamHalfHeight,
                _Impact + Vector2.UnitY * BeamHalfHeight, BeamWidth).Collides(targetHitbox);
        return IsDashing && RotatedRectangle.FromLineSegment(DashPosition(Age - 1),
            DashPosition(Age) + Aim * SpearLength, 32f).Collides(targetHitbox);
    }

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) => modifiers.SourceDamage *=
        IsDashing ? EventideReunionDamageConstants.SacredDashDamageMultiplier : EventideReunionDamageConstants.SacredBeamDamageMultiplier;

    private Vector2 DashPosition(int age) => Vector2.Lerp(_DashOrigin, _DashEnd,
        Math.Clamp((age - DashStart + 1f) / (ThrowStart - DashStart), 0f, 1f));

    protected override void SendAnomalyExtraAI(BinaryWriter writer)
    {
        writer.Write(_DashLocked);
        if (!_DashLocked)
            return;
        writer.WriteVector2(_DashOrigin);
        writer.WriteVector2(_DashEnd);
        writer.WriteVector2(_Impact);
    }

    protected override void ReceiveAnomalyExtraAI(BinaryReader reader)
    {
        _DashLocked = reader.ReadBoolean();
        if (!_DashLocked)
            return;
        _DashOrigin = reader.ReadVector2();
        _DashEnd = reader.ReadVector2();
        _Impact = reader.ReadVector2();
    }

    public override void AI()
    {
        Player player = Projectile.Owner;
        if (!player.active || player.dead || player.noItems || player.CCed || player.HeldItem.type != ModContent.ItemType<EventideReunion>())
        {
            Projectile.Kill();
            return;
        }

        // 持有者一次锁定整段扫掠和落点。远端等快照到达，不从自己的光标或图格另选路径。
        if (Age == DashStart - 1 && !_DashLocked)
        {
            if (!Projectile.IsOnOwnerClient)
                return;
            Projectile.velocity = (Main.MouseWorld - player.MountedCenter).SafeNormalize(Aim);
            LockPath(player);
            Projectile.netUpdate = true;
        }

        Projectile.ai[0]++;
        if (Age >= End)
        {
            Projectile.Kill();
            return;
        }
        player.itemTime = player.itemAnimation = 2;
        // 光柱单独在世界弹幕层绘制，施法者离开画面后依然可见。
        if (!IsBeaming)
            player.heldProj = Projectile.whoAmI;
        if (Age <= ThrowStart && Aim.X != 0f)
            player.ChangeDir(Aim.X > 0f ? 1 : -1);
        player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, ArmAngle(player) - MathHelper.PiOver2);

        if (Age < DashStart)
            Projectile.Center = player.MountedCenter;
        else if (IsDashing)
        {
            Projectile.Center = DashPosition(Age);
            if (Projectile.IsOnOwnerClient)
            {
                player.Center = Projectile.Center;
                player.velocity = Vector2.Zero;
                player.fallStart = (int)(player.position.Y / 16f);
                if (Main.netMode == NetmodeID.MultiplayerClient && (Age % 3 == 0 || Age == ThrowStart - 1))
                    NetMessage.SendData(MessageID.PlayerControls, number: player.whoAmI);
            }
        }
        else
            Projectile.Center = _Impact;

        if (Age == BeamStart)
        {
            Array.Clear(Projectile.localNPCImmunity);
            Projectile.localNPCHitCooldown = BeamHitInterval;
        }
        if (Age >= RecallStart && !_PulseSent && Main.netMode != NetmodeID.MultiplayerClient)
        {
            _PulseSent = true;
            EventideReunionSacrednessPulse.Release(player);
        }
        if (!Main.dedServ)
            PlayCues();
    }

    private void LockPath(Player player)
    {
        player.RemoveAllGrapplingHooks();
        if (player.mount.Active)
            player.mount.Dismount(player);
        _DashOrigin = player.Center;
        Vector2 position = player.position;
        Vector2 step = Aim * 8f;
        for (int i = 0; i < (ThrowStart - DashStart) * DashSpeed / 8f; i++)
        {
            Vector2 next = position + step;
            if (!WorldGen.InWorld((int)next.X / 16, (int)next.Y / 16, 10)
                || !WorldGen.InWorld((int)(next.X + player.width) / 16, (int)(next.Y + player.height) / 16, 10)
                || Collision.TileCollision(position, step, player.width, player.height, gravDir: (int)player.gravDir) != step
                || Collision.SolidCollision(next, player.width, player.height))
                break;
            position = next;
        }
        _DashEnd = position + player.Size * 0.5f;
        // 地形只定位投矛焦点、十字光和归体起点。光柱贯穿焦点上下两侧。
        Vector2 probe = _DashEnd - Vector2.UnitX;
        Vector2 down = Vector2.UnitY * 8f;
        for (int i = 0; i < 80 && probe.Y + 10f < Main.maxTilesY * 16f; i++)
        {
            Vector2 allowed = Collision.TileCollision(probe, down, 2, 2);
            probe += allowed;
            if (allowed != down)
                break;
        }
        _Impact = probe + new Vector2(1f, 2f);
        _DashLocked = true;
    }
}
