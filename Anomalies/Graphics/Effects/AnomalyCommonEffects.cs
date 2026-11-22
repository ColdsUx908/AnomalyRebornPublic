using Terraria.Graphics.Effects;

namespace Anomalies.Assets.Effects;

/// <summary>
/// 所属项目的着色器引用，由统一加载器管理。
/// </summary>
public sealed class AnomalyCommonEffects : AnomalyEffectProvider
{
    public override string EffectDirectory => "Common";

    public static MiscShaderContainer AnomalyTitle;

    public static MiscShaderContainer AnomalyUltramundaneTitle;

    public static MiscShaderContainer CircularAoETelegraph;

    public static MiscShaderContainer GlowCircle;

    public static MiscShaderContainer GlowLine;

    public static MiscShaderContainer GoldText;

    public static MiscShaderContainer LightweightBloom;

    public static Filter RadialBlur;

    public static MiscShaderContainer Shockwave;

    public static MiscShaderContainer SolidColor;

    public static MiscShaderContainer SolidColorMask;

    public static Filter StarburstRadialBlur;
}
