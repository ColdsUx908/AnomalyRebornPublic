using Transoceanic.Graphics.Rendering;

namespace Anomalies.Bosses.Skeletron.LegendaryNight;

/// <summary>仅用于传说之夜爆炸的五级 HDR 光能配置。</summary>
internal readonly record struct LegendaryNightBloomSettings
{
    public LegendaryNightBloomSettings() { }
    public float SourceIntensity { get; init; } = 1f;
    public float Strength { get; init; } = 1f;
    public float Threshold { get; init; } = 1f;
    public static LegendaryNightBloomSettings Default => new();
}

/// <summary>传说之夜专用 HDR 合成；借用框架目标池与混合状态，不改变公共 Bloom。</summary>
internal static partial class LegendaryNightBloom
{
    public static BlendState AdditiveBlend => LightweightBloom.AdditiveBlend;

    private static float PositiveFinite(float value) => float.IsFinite(value) ? Math.Max(value, 0f) : 0f;

    private static void ValidateResource(GraphicsDevice device, GraphicsResource resource, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(resource, parameterName);
        ObjectDisposedException.ThrowIf(resource.IsDisposed, resource);
        if (!ReferenceEquals(device, resource.GraphicsDevice))
            throw new ArgumentException("Bloom resources must belong to the SpriteBatch graphics device.", parameterName);
    }

    private static void ValidateBatch(SpriteBatch spriteBatch)
    {
        ArgumentNullException.ThrowIfNull(spriteBatch);
        if (new Transoceanic.Runtime.Publicizing.Wrappers.SpriteBatch_Publicizer(spriteBatch).beginCalled)
            throw new InvalidOperationException("Bloom requires an inactive SpriteBatch.");
    }
}
