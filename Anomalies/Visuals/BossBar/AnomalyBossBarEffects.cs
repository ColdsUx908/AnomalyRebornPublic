using Anomalies.Assets.Effects;

namespace Anomalies.Visuals.BossBar;

/// <summary>所属项目的着色器引用，由统一加载器管理。</summary>
public sealed class AnomalyBossBarEffects : AnomalyEffectProvider
{
    public override string EffectDirectory => "BossBars";

    public static MiscShaderContainer BrainofCthulhu;

    public static MiscShaderContainer EaterofWorlds;

    public static MiscShaderContainer EyeofCthulhu;

    public static MiscShaderContainer KingSlime;

    public static MiscShaderContainer QueenBee;

    public static MiscShaderContainer QueenSlime;

    public static MiscShaderContainer RainbowJewel;

    public static MiscShaderContainer Skeletron;
}
