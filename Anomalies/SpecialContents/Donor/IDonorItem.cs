namespace Anomalies.SpecialContents.Donor;

public interface IDonorItem
{
    int DonorID { get; }

    static void AddDonorItemIdentifier(Mod mod, List<TooltipLine> tooltips, int index) => tooltips.Insert(index, new TooltipLine(mod, "Tooltip_DonorItemIdentifier", Language.GetTextValue(AnomalySharedData.ModLocalizationPrefix + "SpecialContents.DonorItemIdentifier")) { OverrideColor = AnomalySharedData.MainColor });
    static void AddDonorItemOwnerIdentifier(Mod mod, List<TooltipLine> tooltips, int index, string owner) => tooltips.Insert(index, new TooltipLine(mod, "Tooltip_DonorItemOwnerIdentifier", Language.GetTextValue(AnomalySharedData.ModLocalizationPrefix + "SpecialContents.DonorItemOwnerIdentifier", owner)) { OverrideColor = AnomalySharedData.MainColor });
}
