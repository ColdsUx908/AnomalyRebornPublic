using Anomalies.Visuals.Starburst;

namespace Anomalies.Bosses.Skeletron.LegendaryNight;

internal static class LegendStarburstRingAssets
{
    private const string TexturePath = "Anomalies/Bosses/Skeletron/LegendaryNight/Attacks/Starburst/Textures/";
    [LoadTexture(TexturePath + "RingMarkGradient")]
    private static Asset<Texture2D> _RingMarkGradient;

    public static bool IsLoaded => !Main.dedServ && AnomalyClientConfig.Instance.EnableShaders
        && StarburstCircleAssets.IsLoaded && _RingMarkGradient?.IsLoaded == true
        && LegendaryNightEffects.LegendStarburstRingMark?.Asset.IsLoaded == true;

    public static LegendStarburstRingVisuals.Textures Textures
    {
        get
        {
            StarburstCircleVisuals.Textures shared = StarburstCircleAssets.Textures;
            return new(_RingMarkGradient.Value, shared.ShotLine, shared.GlowDot, shared.Star4, shared.StarFlare);
        }
    }
}
