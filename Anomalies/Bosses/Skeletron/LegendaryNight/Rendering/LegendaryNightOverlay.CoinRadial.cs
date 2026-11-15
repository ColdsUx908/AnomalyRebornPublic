using Anomalies.Assets.Effects;

namespace Anomalies.Bosses.Skeletron.LegendaryNight;

public sealed partial class LegendaryNightOverlay
{
    private static void DrawCoinRadialImpacts(SpriteBatch spriteBatch)
    {
        if (!CoinCrossAssets.IsLoaded)
            return;
        Effect body = CoinCrossAssets.RadialEffect;
        Effect bloom = body is not null ? AnomalyCommonEffects.LightweightBloom?.Asset.Value : null;
        WorldView2D view = new(Main.screenPosition, Main.GameViewMatrix.TransformationMatrix,
            new Vector2(Main.screenWidth, Main.screenHeight));
        BlendState blend = bloom is not null || !Main.dayTime ? LightweightBloom.AdditiveBlend : BlendState.AlphaBlend;
        CoinCrossVisuals.Textures textures = CoinCrossAssets.Textures;
        foreach (Projectile projectile in Projectile.ActiveProjectiles)
        {
            if (projectile.ModProjectile is not CoinJudgment judgment
                || !judgment.TryGetRadialFrame(out CoinRadialVisuals.Frame frame))
                continue;
            CoinRadialLayer layer = new(body, textures, view, frame, blend);
            using CoinCrossVisuals.DrawingBindings bindings = new(spriteBatch.GraphicsDevice);
            using SpriteBatchScope scope = new(spriteBatch, false);
            BloomSettings settings = BloomSettings.Default with { SkipFilterClear = true };
            LightweightBloom.Draw(spriteBatch, bloom, settings, layer);
        }
    }

    private readonly record struct CoinRadialLayer(Effect Body, CoinCrossVisuals.Textures Textures,
        WorldView2D View, CoinRadialVisuals.Frame Frame, BlendState Blend) : IBloomLayer
    {
        public void Draw(SpriteBatch spriteBatch) => CoinRadialVisuals.Draw(spriteBatch, Body, Textures, View, Frame, Blend);
    }
}
