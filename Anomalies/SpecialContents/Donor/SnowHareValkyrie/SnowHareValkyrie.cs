namespace Anomalies.SpecialContents.Donor.SnowHareValkyrie;

public sealed partial class SnowHareValkyrie : AnomalyModItem, IDonorItem
{
    public const string LocalizationCategoryForSnowHareValkyrie = "SpecialContents.Donor.SnowHareValkyrie";

    private static readonly Vector2 _Grip = new(13f, 50f);
    private static readonly Vector2 _Muzzle = new(84f, 41f);

    public int DonorID => 4;

    public override string LocalizationCategory => "SpecialContents.Donor";

    public override bool IsLoadingEnabled(Mod mod) => AnomalySpecialContentToggler.SnowHareValkyrie;

    public override void SetDefaults()
    {
        Item.width = 84;
        Item.height = 64;
        Item.DamageType = DamageClass.Ranged;
        Item.damage = 20;
        Item.knockBack = 3f;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.useTime = Item.useAnimation = 20;
        Item.autoReuse = true;
        Item.noMelee = true;
        Item.useAmmo = AmmoID.Snowball;
        Item.shoot = ModContent.ProjectileType<CarrotMissile>();
        Item.shootSpeed = 15f;
        Item.UseSound = SoundID.Item11;
        Item.rare = ModContent.RarityType<SnowHareValkyrieRarity>();
        Item.value = Item.buyPrice(gold: 10);
    }

    public override Vector2? HoldoutOffset() => new Vector2(-_Grip.X, 0f);

    public override bool AltFunctionUse(Player player) => true;

    public override float UseSpeedMultiplier(Player player) => player.altFunctionUse == 2 ? 0.2f : 1f;

    public override void UseStyle(Player player, Rectangle heldItemFrame)
    {
        Vector2 forward = player.itemRotation.ToRotationVector2() * player.direction;
        Vector2 hand = GetGripPosition(player, forward, out float armRotation);
        player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRotation);
        // 原版 Shoot 绘制以贴图半高为纵向原点，并在 itemLocation 上加回半高。
        player.itemLocation = hand - GetTextureDown(player, forward)
            * ((_Grip.Y - heldItemFrame.Height / 2f) * player.GetAdjustedItemScale(Item));
        player.itemLocation.Y -= heldItemFrame.Height / 2f;
    }

    private static Vector2 GetGripPosition(Player player, Vector2 forward, out float armRotation)
    {
        armRotation = new Vector2(forward.X, forward.Y * player.gravDir).ToRotation() - MathHelper.PiOver2;
        Vector2 hand = player.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, armRotation);
        return player.MountedCenter + (hand - player.MountedCenter) * new Vector2(1f, player.gravDir);
    }

    private static Vector2 GetTextureDown(Player player, Vector2 forward) =>
        new Vector2(-forward.Y, forward.X) * player.direction * player.gravDir;

    public override bool CanConsumeAmmo(Item ammo, Player player) => !Main.remixWorld || !Main.rand.NextBool();

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        Vector2 forward = velocity.SafeNormalize(Vector2.UnitX * player.direction);
        Vector2 localForward = forward.RotatedBy(-player.fullRotation);
        Vector2 hand = GetGripPosition(player, localForward, out _);
        Vector2 grip = position + (hand - player.MountedCenter).RotatedBy(player.fullRotation);
        Vector2 offset = _Muzzle - _Grip;
        Vector2 muzzle = grip + (offset.X * forward + offset.Y * GetTextureDown(player, forward)) * player.GetAdjustedItemScale(Item);
        Projectile.NewProjectileAction<CarrotMissile>(source, muzzle, velocity, damage, knockback, player.whoAmI,
            missile =>
            {
                if (player.altFunctionUse == 2)
                {
                    missile.ai[0] = 1f;
                    missile.penetrate = -1;
                    missile.netImportant = true;
                    missile.usesLocalNPCImmunity = true;
                    missile.localNPCHitCooldown = -1;
                }
            });
        return false;
    }
}
