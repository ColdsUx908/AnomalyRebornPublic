using Anomalies.Assets.Effects;

namespace Anomalies.SpecialContents.Developer.EventideReunion;

/// <summary>所属项目的着色器引用，由统一加载器管理。</summary>
public sealed class EventideReunionEffects : AnomalyEffectProvider
{
    public override string EffectDirectory => "EventideReunion";

    public override bool IsLoadingEnabled(Mod mod) => AnomalySpecialContentToggler.EventideReunion;

    public static MiscShaderContainer EventideDeathMark;

    public static MiscShaderContainer EventideDeathSweep;

    public static MiscShaderContainer EventideDeathVignette;

    public static MiscShaderContainer EventideReunion;

    public static MiscShaderContainer EventideSamsaraMark;

    public static MiscShaderContainer EventideSamsaraThread;

    public static MiscShaderContainer EventideSweep;
}
