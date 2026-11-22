namespace Anomalies.Visuals.Starburst;

/// <summary>
/// 普通奇数次星爆的完整 Bloom 提交；Boss 和武器使用同一图层及校准。
/// </summary>
internal static class StarburstCircleDrawing
{
    public static BloomSettings GetBloomSettings(float radius, in WorldView2D view, bool dayTime)
    {
        float haloScale = 1.5f * view.Scale * (radius / StarburstCircleVisuals.ReferenceRadius) / 1.1f;
        BloomSettings defaults = BloomSettings.Default;
        return StarburstVisuals.WithBody(defaults with
        {
            NearRadius = defaults.NearRadius * haloScale,
            FarRadius = defaults.FarRadius * haloScale
        }, dayTime);
    }

    // 调用前后均未开始 SpriteBatch；不改写动画时间，不附加额外强度缩放。
    public static void Draw(SpriteBatch batch, Effect bloom, in StarburstCircleVisuals.Textures textures,
        in WorldView2D view, in StarburstVisuals.Burst frame, bool dayTime)
    {
        BlendState blend = bloom is not null || !dayTime ? LightweightBloom.AdditiveBlend : BlendState.AlphaBlend;
        Layer layer = new(null, 0, frame, textures, view, blend);
        LightweightBloom.Draw(batch, bloom, GetBloomSettings(frame.OuterRadius, view, dayTime), layer);
    }

    public static void Draw(SpriteBatch batch, Effect bloom, in StarburstCircleVisuals.Textures textures,
        in WorldView2D view, StarburstVisuals.Burst[] frames, int count, float radius, bool dayTime)
    {
        BlendState blend = bloom is not null || !dayTime ? LightweightBloom.AdditiveBlend : BlendState.AlphaBlend;
        Layer layer = new(frames, count, default, textures, view, blend);
        LightweightBloom.Draw(batch, bloom, GetBloomSettings(radius, view, dayTime), layer);
    }

    private readonly record struct Layer(StarburstVisuals.Burst[] Frames, int Count, StarburstVisuals.Burst Frame,
        StarburstCircleVisuals.Textures Textures, WorldView2D View, BlendState Blend) : IBloomLayer
    {
        public void Draw(SpriteBatch spriteBatch)
        {
            if (Frames is null)
                StarburstCircleVisuals.Draw(spriteBatch, Textures, View, Frame, Blend, drawWarning: false);
            else
                for (int i = 0; i < Count; i++)
                    if (Frames[i].InnerRadius == 0f)
                        StarburstCircleVisuals.Draw(spriteBatch, Textures, View, Frames[i], Blend, drawWarning: false);
        }
    }
}
