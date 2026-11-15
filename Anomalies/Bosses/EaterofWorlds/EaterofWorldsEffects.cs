using Anomalies.Assets.Effects;

namespace Anomalies.Bosses.EaterofWorlds;

/// <summary>所属项目的着色器引用，由统一加载器管理。</summary>
public sealed class EaterofWorldsEffects : AnomalyEffectProvider
{
    public override string EffectDirectory => "EaterofWorlds";

    public static MiscShaderContainer EaterDistortionFog;

    public static MiscShaderContainer EaterDistortionWinds;

    public static MiscShaderContainer EaterMetaballEdge;

    public static MiscShaderContainer EaterPortal;

    public static MiscShaderContainer EaterRealityCrack;
}
