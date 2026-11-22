using Anomalies.Assets.Effects;

namespace Anomalies.Bosses.Skeletron.LegendaryNight;

/// <summary>
/// 钱币十字的 LDR 双尺度柔光，不使用浮点渲染目标。
/// </summary>
internal static class CoinCrossBloom
{
    public static void Draw(SpriteBatch batch, Effect body, bool enabled, in CoinCrossVisuals.Textures textures,
        in WorldView2D view, in CoinCrossVisuals.Frame frame, bool dayTime)
    {
        if (!CoinCrossVisuals.IsVisible(frame.Age))
            return;
        Effect bloom = enabled && !Main.dedServ
            ? AnomalyCommonEffects.LightweightBloom?.Asset.Value : null;
        bool useBloom = body is not null && bloom is not null;
        BloomSettings settings = BloomSettings.Default with { SkipFilterClear = true };
        Layer layer = new(useBloom ? body : null, useBloom ? CoinCrossAssets.ParticleEffect : null, textures, view, frame,
            useBloom || !dayTime ? LightweightBloom.AdditiveBlend : BlendState.AlphaBlend);
        using CoinCrossVisuals.DrawingBindings bindings = new(batch.GraphicsDevice);
        using SpriteBatchScope scope = new(batch, false);
        LightweightBloom.Draw(batch, useBloom ? bloom : null, settings, layer);
    }

    private readonly record struct Layer(Effect Body, Effect Particles, CoinCrossVisuals.Textures Textures,
        WorldView2D View, CoinCrossVisuals.Frame Frame, BlendState Blend) : IBloomLayer
    {
        public void Draw(SpriteBatch spriteBatch) => CoinCrossVisuals.Draw(spriteBatch, Body, Particles, Textures, View, Frame, Blend);
    }
}
