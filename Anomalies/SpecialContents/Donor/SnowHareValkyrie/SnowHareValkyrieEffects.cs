using Anomalies.Assets.Effects;

namespace Anomalies.SpecialContents.Donor.SnowHareValkyrie;

public sealed class SnowHareValkyrieEffects : AnomalyEffectProvider
{
    public override string EffectDirectory => "SnowHareValkyrie";

    public override bool IsLoadingEnabled(Mod mod) => AnomalySpecialContentToggler.SnowHareValkyrie;

    public static MiscShaderContainer CarrotBlast;

    public static MiscShaderContainer DonorName;

    public static MiscShaderContainer SnowHareName;
}
