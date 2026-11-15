using Anomalies.Assets.Effects;
using Terraria.GameContent.ItemDropRules;

namespace Anomalies.GameContents.AnomalyItems;

public sealed class CoinBag : AnomalyModItem
{
    public override string LocalizationCategory => "GameContents.AnomalyItems";

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
    {
        if (Main.dedServ || !AnomalyClientConfig.Instance.EnableShaders || AnomalyCommonEffects.GoldText is null)
            return true;

        if (TODrawUtils.TryDrawItemNameWithCustomShader(line, l => AnomalyCommonEffects.GoldText.Data
            .SetCustomParameter("uScreenResolution", Main.ScreenSize.ToVector2() * Math.Max(Main.UIScale, 1f) / 2f)
            .SetCustomParameter("uScreenRatio", TODrawUtils.ScreenRatio)
            .SetCustomParameter("uPosition", new Vector2(l.X, l.Y))
            .Apply()))
        {
            return false;
        }

        return true;
    }

    public override void ModifyItemLoot(ItemLoot itemLoot)
    {
        itemLoot.Add(ItemDropRule.Common(ItemID.PlatinumCoin, 1, 591, 591));
        itemLoot.Add(ItemDropRule.Common(ItemID.GoldCoin, 1, 60, 60));
        itemLoot.Add(ItemDropRule.Common(ItemID.SilverCoin, 1, 15, 15));
        itemLoot.Add(ItemDropRule.Common(ItemID.CopperCoin, 1, 3, 3));
    }
}
