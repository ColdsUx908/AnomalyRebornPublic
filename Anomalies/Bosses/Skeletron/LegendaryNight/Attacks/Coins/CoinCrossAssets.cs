using Anomalies.Assets.Effects;

namespace Anomalies.Bosses.Skeletron.LegendaryNight;

internal sealed class CoinCrossAssets : ILoadable
{
    private const string TexturePath = "Anomalies/Bosses/Skeletron/LegendaryNight/Attacks/Coins/Textures/";
    [LoadTexture(TexturePath + "CrossFlow")]
    private static Asset<Texture2D> _Flow;
    [LoadTexture(TexturePath + "CrossOuter")]
    private static Asset<Texture2D> _Outer;
    [LoadTexture(TexturePath + "CrossStar")]
    private static Asset<Texture2D> _Star;
    [LoadTexture(TexturePath + "CrossStreak")]
    private static Asset<Texture2D> _Streak;
    [LoadTexture(TexturePath + "CrossWarp")]
    private static Asset<Texture2D> _Warp;

    internal static bool IsLoaded => !Main.dedServ && LegendStarburstCircleAssets.IsLoaded
        && _Outer?.IsLoaded == true && _Flow?.IsLoaded == true && _Streak?.IsLoaded == true
        && _Warp?.IsLoaded == true && _Star?.IsLoaded == true;
    internal static Effect BodyEffect => AnomalyClientConfig.Instance.EnableShaders
        && AnomalyEffects.Primitives.LegendCoinCross?.Asset.IsLoaded == true
        ? AnomalyEffects.Primitives.LegendCoinCross.Asset.Value : null;
    internal static CoinCrossVisuals.Textures Textures => new(_Outer.Value, _Flow.Value, _Streak.Value, _Warp.Value,
        _Star.Value, LegendStarburstCircleAssets.Textures.SparkTail,
        LegendStarburstCircleAssets.Textures.ShotLine);

    public void Load(Mod mod) { }

    public void Unload() { }
}
