using Anomalies.Assets.Effects;

namespace Anomalies.Bosses.Skeletron;

/// <summary>所属项目的着色器引用，由统一加载器管理。</summary>
public sealed class SkeletronEffects : AnomalyEffectProvider
{
    public override string EffectDirectory => "Skeletron";

    public static MiscShaderContainer SkeletronArenaExterior;

    public static MiscShaderContainer SkeletronArenaMist;

    public static MiscShaderContainer SkeletronFinalCharge;

    public static MiscShaderContainer SkeletronForwardBeam;

    public static MiscShaderContainer SkeletronPhase3Warning;
}
