namespace Anomalies.Bosses.Skeletron.LegendaryNight;

public sealed partial class LegendaryNightOverlay
{
    private static void DrawCoinCrossImpacts(SpriteBatch spriteBatch)
    {
        if (!CoinCrossAssets.IsLoaded)
            return;
        WorldView2D view = new(Main.screenPosition, Main.GameViewMatrix.TransformationMatrix,
            new Vector2(Main.screenWidth, Main.screenHeight));
        foreach (Projectile projectile in Projectile.ActiveProjectiles)
        {
            if (projectile.ModProjectile is not CoinJudgment judgment || !judgment.TryGetCrossFrame(view, out CoinCrossVisuals.Frame frame))
                continue;
            CoinCrossBloom.Draw(spriteBatch, CoinCrossAssets.BodyEffect, AnomalyClientConfig.Instance.EnableShaders,
                CoinCrossAssets.Textures, view, frame, Main.dayTime);
        }
    }
}
