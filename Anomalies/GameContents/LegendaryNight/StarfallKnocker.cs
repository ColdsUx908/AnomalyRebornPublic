using Anomalies.Common.Items;

namespace Anomalies.GameContents.LegendaryNight;

public sealed class StarfallKnocker : AnomalyModItem
{
    public override string LocalizationCategory => "GameContents";

    public override void SetStaticDefaults() => AnomalySharedData.DontApplyAnomalyRarityShaderItems[Type] = true;

    public override bool PreDrawTooltipLine(DrawableTooltipLine line, ref int yOffset)
        => GoldItemNameDrawing.PreDrawTooltipLine(line);

    public override void SetDefaults()
    {
        Item.width = 192;
        Item.height = 186;
        Item.maxStack = 1;
        Item.damage = 500;
        Item.DamageType = DamageClass.Melee;
        Item.knockBack = 10f;
        Item.useTime = Item.useAnimation = 30;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.channel = true;
        Item.noMelee = true;
        Item.noUseGraphic = true;
        Item.shoot = ModContent.ProjectileType<StarfallKnockerHoldout>();
        Item.shootSpeed = 1f;
        Item.rare = ModContent.RarityType<AnomalyUltramundaneRarity>();
    }

    public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] == 0;

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
        Vector2 velocity, int type, int damage, float knockback)
    {
        Projectile.NewProjectileAction(source, player.MountedCenter,
            velocity.SafeNormalize(Vector2.UnitX * player.direction), type, damage, knockback, player.whoAmI);
        return false;
    }
}
