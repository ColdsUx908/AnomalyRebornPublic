using Anomalies.Assets.Effects;

namespace Anomalies.SpecialContents.Developer.EventideReunion;

/// <summary>
/// 所属项目的着色器引用，由统一加载器管理。
/// </summary>
public sealed class EventideReunionEffects : AnomalyEffectProvider
{
    public override string EffectDirectory => "EventideReunion";

    public override bool IsLoadingEnabled(Mod mod) => AnomalySpecialContentToggler.EventideReunion;

    public static MiscShaderContainer EventideDeathMark;

    public static MiscShaderContainer EventideDeathSweep;

    public static MiscShaderContainer EventideDeathVignette;

    public static MiscShaderContainer EventideLifeHeart;

    public static MiscShaderContainer EventideReunion;

    public static MiscShaderContainer EventideSacredLight;

    public static MiscShaderContainer EventideSamsaraMark;

    public static MiscShaderContainer EventideSamsaraThread;

    public static MiscShaderContainer EventideStarCore;

    public static Terraria.Graphics.Effects.Filter EventideStarLens;

    public static MiscShaderContainer EventideStarNebula;

    public static MiscShaderContainer EventideStarShockwave;

    public static MiscShaderContainer EventideSweep;

    internal static void DrawPhotosphere(Vector2 center, float diameter, float time, float collapse, float opacity, bool solar)
    {
        if (!AnomalyClientConfig.Instance.EnableShaders || EventideReunionEffects.EventideStarCore is not { } shader)
            return;
        using SpriteBatchScope scope = new(Main.spriteBatch, SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp);
        shader.Data.SetCustomParameter("uNoiseTexture", AnomalyTextures.Noise.Perlin2)
            .SetCustomParameter("uTime", time)
            .SetCustomParameter("uCollapse", collapse)
            .Apply();
        shader.Asset.Value.CurrentTechnique.Passes[solar ? "SolarFlare" : "Pass0"].Apply();
        Texture2D pixel = TOTextures.FillerTexture;
        Main.spriteBatch.DrawFromCenter(pixel, center - Main.screenPosition, null, Color.White * opacity, scale: diameter / pixel.Width);
    }
}
