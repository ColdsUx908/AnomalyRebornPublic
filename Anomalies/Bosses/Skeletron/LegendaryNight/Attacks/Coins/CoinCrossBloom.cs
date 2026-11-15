using Anomalies.Assets.Effects;
using Transoceanic.Graphics.Rendering;

namespace Anomalies.Bosses.Skeletron.LegendaryNight;

/// <summary>十字的光能脉冲；Bloom 使用与其他爆炸相同的框架默认设置。</summary>
internal static class CoinCrossBloom
{
    internal static float EmissionIntensity => 1f;

    public static void Draw(SpriteBatch batch, Effect body, bool enabled, in CoinCrossVisuals.Textures textures,
        in WorldView2D view, in CoinCrossVisuals.Frame frame, bool dayTime)
    {
        if (!CoinCrossVisuals.IsVisible(frame.Age))
            return;
        Effect bloom = enabled && !Main.dedServ
            ? AnomalyEffects.Overlays.LegendaryNightBloom?.Asset.Value : null;
        bool useBloom = body is not null && bloom is not null;
        LegendaryNightBloomSettings settings = Settings(frame);
        Layer layer = new(useBloom ? body : null, textures, view, frame,
            useBloom || !dayTime ? LegendaryNightBloom.AdditiveBlend : BlendState.AlphaBlend);
        using CoinCrossVisuals.DrawingBindings bindings = new(batch.GraphicsDevice);
        using SpriteBatchScope scope = new(batch, false);
        LegendaryNightBloom.Draw(batch, useBloom ? bloom : null, settings, layer);
    }

    private static LegendaryNightBloomSettings Settings(in CoinCrossVisuals.Frame frame)
    {
        float pulse = (frame.Age - 3f) / 3.4f;
        return LegendaryNightBloomSettings.Default with
        {
            // 源图沿用 RGB/8；这里仅解码一次，并应用起爆的光能脉冲。
            SourceIntensity = 8f * (1f + 2.2f * MathF.Exp(-pulse * pulse)) * EmissionIntensity
        };
    }

    private readonly record struct Layer(Effect Body, CoinCrossVisuals.Textures Textures,
        WorldView2D View, CoinCrossVisuals.Frame Frame, BlendState Blend) : IBloomLayer
    {
        public void Draw(SpriteBatch spriteBatch) => CoinCrossVisuals.Draw(spriteBatch, Body, Textures, View, Frame, Blend);
    }
}
