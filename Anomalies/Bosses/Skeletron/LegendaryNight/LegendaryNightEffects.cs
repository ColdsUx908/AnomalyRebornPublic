using Anomalies.Assets.Effects;
using Terraria.Graphics.Effects;

namespace Anomalies.Bosses.Skeletron.LegendaryNight;

/// <summary>所属项目的着色器引用，由统一加载器管理。</summary>
public sealed class LegendaryNightEffects : AnomalyEffectProvider
{
    public override string EffectDirectory => "LegendaryNight";

    public static MiscShaderContainer LegendaryNightArenaEdge;

    public static MiscShaderContainer LegendCoinCross;

    public static MiscShaderContainer LegendCoinCrossParticles;

    public static MiscShaderContainer LegendCoinRadial;

    public static MiscShaderContainer LegendSlapFlame;

    public static Filter LegendStarburstRadialBlur;

    public static MiscShaderContainer LegendStarburstRingMark;

    public static MiscShaderContainer LegendStellarAccretion;

    public static MiscShaderContainer LegendStellarExplosion;

    public static MiscShaderContainer LegendStellarLaser;

    public static MiscShaderContainer LegendStellarLaserWarning;

    public static MiscShaderContainer LegendStellarLightOrb;

    public static MiscShaderContainer LegendStellarPhotosphere;

    public static MiscShaderContainer LegendStellarSphere;
}
