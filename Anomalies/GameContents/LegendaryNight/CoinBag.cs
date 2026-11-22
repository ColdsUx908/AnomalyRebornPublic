using Anomalies.Common.Items;
using Terraria.GameContent.ItemDropRules;

namespace Anomalies.GameContents.LegendaryNight;

public sealed class CoinBag : AnomalyModItem
{
    public override string LocalizationCategory => "GameContents.LegendaryNight";

    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = 10;
        AnomalySharedData.DontApplyAnomalyRarityShaderItems[Type] = true;
    }

    public override void SetDefaults()
    {
        Item.width = 40;
        Item.height = 48;
        Item.maxStack = Item.CommonMaxStack;
        Item.consumable = true;
        Item.rare = ModContent.RarityType<AnomalyUltramundaneRarity>();
    }

    public override bool CanRightClick() => true;

    public override bool PreDrawTooltipLine(DrawableTooltipLine line, ref int yOffset)
        => GoldItemNameDrawing.PreDrawTooltipLine(line);

    public override void ModifyItemLoot(ItemLoot itemLoot)
    {
        itemLoot.Add(ItemDropRule.Common(ItemID.PlatinumCoin, 1, 591, 591));
        itemLoot.Add(ItemDropRule.Common(ItemID.GoldCoin, 1, 60, 60));
        itemLoot.Add(ItemDropRule.Common(ItemID.SilverCoin, 1, 15, 15));
        itemLoot.Add(ItemDropRule.Common(ItemID.CopperCoin, 1, 3, 3));
    }
}
