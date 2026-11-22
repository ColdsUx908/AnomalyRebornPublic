namespace Anomalies.SpecialContents.Developer.EventideReunion;

internal static class EventideReunionLifeAssets
{
    public const string AssetPath = EventideReunionAssets.AssetPath + "Life/";

    [LoadTexture("Anomalies/Assets/Textures/Starburst/GlowDot")]
    public static Asset<Texture2D> GlowDot;

    [LoadTexture(AssetPath + "LifePetal1")]
    public static Asset<Texture2D> LifePetal1;

    [LoadTexture(AssetPath + "LifePetal2")]
    public static Asset<Texture2D> LifePetal2;

    [LoadTexture(AssetPath + "LifePetal3")]
    public static Asset<Texture2D> LifePetal3;

    [LoadTexture(AssetPath + "TreeOfLife")]
    public static Asset<Texture2D> TreeOfLife;
}
