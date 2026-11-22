namespace Anomalies.Visuals.Starburst;

internal interface IStarburstImpactSource
{
    bool TryGetImpactFrame(bool includeCross, out StarburstVisuals.Burst frame);
}
