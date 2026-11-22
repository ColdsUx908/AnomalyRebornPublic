using Anomalies.Common.Items;
using Terraria.Enums;
using Terraria.ObjectData;

namespace Anomalies.GameContents.LegendaryNight;

/// <summary>
/// 传说之夜召唤内容。
/// </summary>
/// <remarks>
/// <para>数据槽位：不使用 NPC/Projectile 的 ai、localAI 或框架计时槽；状态由物品/物块原版字段及调用的仪式实体管理。</para>
/// </remarks>
public sealed class AncientBattlefieldAltarItem : AnomalyModItem
{
    public override string Texture => AnomalySharedData.ModPath + "GameContents/LegendaryNight/AncientBattlefieldAltarItem";

    public override string LocalizationCategory => "GameContents.LegendaryNight";

    public override void SetStaticDefaults()
    {
        AnomalySharedData.DontApplyAnomalyRarityShaderItems[Type] = true;
    }

    public override void SetDefaults()
    {
        Item.DefaultToPlaceableTile(ModContent.TileType<AncientBattlefieldAltar>());
        Item.rare = ModContent.RarityType<AnomalyUltramundaneRarity>();
        Item.value = AnomalyRarity.AnomalyPrice;
    }

    public override void ModifyResearchSorting(ref ContentSamples.CreativeHelper.ItemGroup itemGroup) => itemGroup = ContentSamples.CreativeHelper.ItemGroup.CraftingObjects;

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ModContent.ItemType<FallenStarEssence>())
            .AddIngredient(ItemID.Bone, 20)
            .AddIngredient(ItemID.StoneBlock, 50)
            .AddIngredient(ItemID.GoldBar, 10)
            .AddTile(TileID.DemonAltar)
            .Register();
    }

    public override bool PreDrawTooltipLine(DrawableTooltipLine line, ref int yOffset)
        => GoldItemNameDrawing.PreDrawTooltipLine(line);
}

/// <summary>
/// 传说之夜召唤内容。
/// </summary>
/// <remarks>
/// <para>数据槽位：不使用 NPC/Projectile 的 ai、localAI 或框架计时槽；状态由物品/物块原版字段及调用的仪式实体管理。</para>
/// </remarks>
public sealed partial class AncientBattlefieldAltar : ModTile
{
    public override string Texture => AnomalySharedData.ModPath + "GameContents/LegendaryNight/AncientBattlefieldAltar";

    public override string LocalizationCategory => "GameContents.LegendaryNight";

    /// <summary>
    /// 宽度。
    /// </summary>
    public const int Width = 5;
    /// <summary>
    /// 高度。
    /// </summary>
    public const int Height = 5;
    /// <summary>
    /// 动画帧数量。
    /// </summary>
    public const int FrameCount = 7;

    public override void SetStaticDefaults()
    {
        Main.tileFrameImportant[Type] = true;
        Main.tileNoAttach[Type] = true;
        Main.tileLavaDeath[Type] = false;

        // Various data sets to protect this tile from unintentional interactions
        TileID.Sets.PreventsTileRemovalIfOnTopOfIt[Type] = true;
        TileID.Sets.PreventsTileHammeringIfOnTopOfIt[Type] = true;
        TileID.Sets.PreventsSandfall[Type] = true;
        TileID.Sets.DisableSmartCursor[Type] = true;

        TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
        TileObjectData.newTile.Width = Width;
        TileObjectData.newTile.Height = Height;
        TileObjectData.newTile.Origin = new Point16(2, 3);
        TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop | AnchorType.SolidSide, TileObjectData.newTile.Width, 0);
        TileObjectData.newTile.CoordinateWidth = 16;
        TileObjectData.newTile.CoordinateHeights = [16, 16, 16, 16, 16];
        TileObjectData.newTile.DrawYOffset = 0;
        TileObjectData.newTile.StyleHorizontal = true;
        TileObjectData.newTile.LavaDeath = false;
        TileObjectData.addTile(Type);
        AddMapEntry(Color.Gold, ModContent.GetModItem<AncientBattlefieldAltarItem>().DisplayName);

        AnimationFrameHeight = Height * 18;
    }

    public override bool CanExplode(int i, int j) => false;

    public override bool CreateDust(int i, int j, ref int type)
    {
        type = DustID.YellowStarDust;
        return true;
    }

    public override void AnimateTile(ref int frame, ref int frameCounter)
    {
        bool ritualExists = false;
        AnimateRitual(ref frame, ref ritualExists);
        if (!ritualExists && ++frameCounter >= 3)
        {
            frameCounter = 0;
            frame = Math.Max(0, frame - 1);
        }
    }

    public override void MouseOver(int i, int j) => DisplayIcon(i, j);
    public override void MouseOverFar(int i, int j) => DisplayIcon(i, j);
    public static void DisplayIcon(int i, int j)
    {
        Main.LocalPlayer.cursorItemIconID = ModContent.ItemType<MemoryofAstraeus>();
        Main.LocalPlayer.noThrow = 2;
        Main.LocalPlayer.cursorItemIconEnabled = true;
    }

    public override bool RightClick(int i, int j)
    {
        ActivateRitual(i, j);
        return true;
    }

    public override void KillMultiTile(int i, int j, int frameX, int frameY) => CancelRitual();

    partial void ActivateRitual(int i, int j);
    partial void AnimateRitual(ref int frame, ref bool ritualExists);
    partial void CancelRitual();
}
