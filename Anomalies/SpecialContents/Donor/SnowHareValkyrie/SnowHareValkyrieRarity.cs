namespace Anomalies.SpecialContents.Donor.SnowHareValkyrie;

public sealed class SnowHareValkyrieRarity : ModRarity
{
    public override bool IsLoadingEnabled(Mod mod) => AnomalySpecialContentToggler.SnowHareValkyrie;

    public override Color RarityColor => Color.Lerp(Color.SkyBlue, Color.White,
        0.5f + 0.5f * MathF.Sin(Main.GlobalTimeWrappedHourly * 1.5f));

    public override int GetPrefixedRarity(int offset, float valueMult) => Type;
}
