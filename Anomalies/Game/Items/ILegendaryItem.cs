namespace Anomalies.GameContents;

public interface ILegendaryItem
{
    /// <summary>
    /// 传奇物品的更新方法。
    /// <br/>处理传奇武器的“随游戏进度成长”特性。
    /// </summary>
    void LegendaryUpdate();
    /// <summary>
    /// 传奇物品的更新方法。
    /// <br/>处理传奇饰品的“随游戏进度成长”特性，并同时更新玩家的相关状态。
    /// </summary>
    /// <param name="player"></param>
    void LegendaryUpdate(Player player);

    static void AddLegendaryItemIdentifier(Mod mod, List<TooltipLine> tooltips, int index) => tooltips.Insert(index, new TooltipLine(mod, "Tooltip_CALegendaryItemIdentifier", Language.GetTextValue(AnomalySharedData.ModLocalizationPrefix + "GameContents.LegendaryItemIdentifier")) { OverrideColor = AnomalySharedData.RebornColor });
}