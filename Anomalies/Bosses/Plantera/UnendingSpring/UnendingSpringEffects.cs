using Anomalies.Assets.Effects;

namespace Anomalies.Bosses.Plantera.UnendingSpring;

public sealed class UnendingSpringEffects : AnomalyEffectProvider
{
    public override string EffectDirectory => "UnendingSpring";

    public static MiscShaderContainer UnendingSpringArenaEdge;
}