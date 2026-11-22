namespace Anomalies.SpecialContents.Contributor.MeguminStaff;

public sealed class MeguminExhaustion : ModBuff
{
    public override bool IsLoadingEnabled(Mod mod) => AnomalySpecialContentToggler.MeguminStaff;

    public override string LocalizationCategory => MeguminStaff.LocalizationCategoryForMeguminStaff;

    public override void SetStaticDefaults()
    {
        Main.debuff[Type] = true;
        Main.buffNoSave[Type] = true;
    }

    public override void Update(Player player, ref int buffIndex)
    {
        player.manaRegenDelay = Math.Max(player.manaRegenDelay, 30);
    }
}
