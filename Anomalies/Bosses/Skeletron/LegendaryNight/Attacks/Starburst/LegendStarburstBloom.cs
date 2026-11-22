using Anomalies.Visuals.Starburst;

namespace Anomalies.Bosses.Skeletron.LegendaryNight;

internal static class LegendStarburstBloom
{
    public static BloomSettings GetBloomSettings(float outerRadius, in WorldView2D view, bool dayTime, bool ring)
    {
        if (!ring)
            return StarburstCircleDrawing.GetBloomSettings(outerRadius, view, dayTime);
        // 圆形校准已共用，圆环保留原有运算顺序。
        const float OutputScale = 1.5f;
        float haloScale = OutputScale * view.Scale * outerRadius / LegendStarburstRingVisuals.ReferenceRadius;
        BloomSettings defaults = BloomSettings.Default;
        return StarburstVisuals.WithBody(defaults with
        {
            NearRadius = defaults.NearRadius * haloScale,
            FarRadius = defaults.FarRadius * haloScale
        }, dayTime);
    }

}
