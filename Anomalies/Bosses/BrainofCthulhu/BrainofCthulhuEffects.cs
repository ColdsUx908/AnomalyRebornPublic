using Anomalies.Assets.Effects;
using Terraria.Graphics.Effects;

namespace Anomalies.Bosses.BrainofCthulhu;

/// <summary>
/// 所属项目的着色器引用，由统一加载器管理。
/// </summary>
public sealed class BrainofCthulhuEffects : AnomalyEffectProvider
{
    public override string EffectDirectory => "BrainofCthulhu";

    public static Filter BrainOfCthulhuForcefield;
}
