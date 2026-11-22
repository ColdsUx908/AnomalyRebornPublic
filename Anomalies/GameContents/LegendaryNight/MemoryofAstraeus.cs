using Anomalies.Common.Items;

namespace Anomalies.GameContents.LegendaryNight;

/// <summary>
/// 传说之夜召唤内容。
/// </summary>
/// <remarks>
/// <para>数据槽位：不使用 NPC/Projectile 的 ai、localAI 或框架计时槽；状态由物品/物块原版字段及调用的仪式实体管理。</para>
/// </remarks>
public class MemoryofAstraeus : AnomalyModItem
{
    public override string Texture => AnomalySharedData.ModPath + "GameContents/LegendaryNight/MemoryofAstraeus";

    public override string LocalizationCategory => "GameContents.LegendaryNight";

    public override void SetStaticDefaults()
    {
        ItemID.Sets.SortingPriorityBossSpawns[Type] = 19; // Celestial Sigil
        AnomalySharedData.DontApplyAnomalyRarityShaderItems[Type] = true;
    }

    public override void SetDefaults()
    {
        Item.width = 40;
        Item.height = 60;
        Item.rare = ModContent.RarityType<AnomalyUltramundaneRarity>();
        Item.value = AnomalyRarity.AnomalyPrice;
    }

    public override void ModifyResearchSorting(ref ContentSamples.CreativeHelper.ItemGroup itemGroup) => itemGroup = ContentSamples.CreativeHelper.ItemGroup.BossItem;

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ModContent.ItemType<FallenStarEssence>())
            .AddIngredient(ItemID.Bottle)
            .AddIngredient(ItemID.FallenStar, 5)
            .AddIngredient(ItemID.GoldBar, 5)
            .AddTile(TileID.DemonAltar)
            .Register();
    }

    public override bool PreDrawTooltipLine(DrawableTooltipLine line, ref int yOffset)
        => GoldItemNameDrawing.PreDrawTooltipLine(line);
}
