using Anomalies.Assets.Effects;

namespace Anomalies.SpecialContents.Contributor.MeguminStaff;

/// <summary>
/// 所属项目的着色器引用，由统一加载器管理。
/// </summary>
public sealed class MeguminStaffEffects : AnomalyEffectProvider
{
    public override string EffectDirectory => "MeguminStaff";

    public override bool IsLoadingEnabled(Mod mod) => AnomalySpecialContentToggler.MeguminStaff;

    public static MiscShaderContainer MeguminCircle;

    public static MiscShaderContainer MeguminLaser;

    public static MiscShaderContainer MeguminStaff;
}
