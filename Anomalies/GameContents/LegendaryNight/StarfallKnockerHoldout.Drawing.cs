using Anomalies.Visuals.Starburst;
using static Anomalies.GameContents.LegendaryNight.StarfallKnockerGeometry;

namespace Anomalies.GameContents.LegendaryNight;

public sealed partial class StarfallKnockerHoldout
{
    private float FlameStrength => CurrentPhase == Phase.Charging
        ? MathHelper.Lerp(0.18f, 0.85f, Timer / ChargeDuration)
        : CurrentPhase == Phase.Swinging ? 1f : Math.Clamp(1f - Timer / 16f, 0f, 1f);

    // 黄色锤面上的火流根部，与武器共用变换，支持转身、翻面和反重力。
    private static readonly Vector2[] _FlameRoots =
    [
        new(148f, 92f), new(155f, 83f), new(168f, 79f),
        new(175f, 87f), new(167f, 98f), new(156f, 109f), new(152f, 100f)
    ];

    private void DrawHeadFlames()
    {
        float strength = FlameStrength;
        if (strength <= 0f)
            return;
        Texture2D bloom = ParticleHandler.GetTexture<BloomParticle>();
        Matrix transform = WeaponTransform;
        Vector2 head = CurrentHeadCenter;
        Vector2 backward = -_HeadMotion.SafeNormalize(new Vector2(Facing, 0f));
        Vector2 normal = new(-backward.Y, backward.X);
        float speed = Math.Clamp(_HeadMotion.Length() / 25f, 0f, 1f);
        float reach = (45f + 65f * speed) * Projectile.scale * strength;
        for (int strand = 0; strand < _FlameRoots.Length; strand++)
        {
            Vector2 root = Vector2.Transform(_FlameRoots[strand], transform);
            Vector2 previous = root;
            float phase = strand * 1.71f - _FlameTime * 0.23f;
            const int Segments = 18;
            for (int segment = 1; segment <= Segments; segment++)
            {
                float t = segment / (float)Segments;
                // 多点起焰，向后汇聚；末端保留细小火舌摆动。
                float wave = MathF.Sin(t * 9f + phase) + 0.35f * MathF.Sin(t * 21f - phase * 1.3f);
                Vector2 point = Vector2.Lerp(root, head, MathHelper.SmoothStep(0f, 1f, t))
                    + backward * (reach * t * (0.8f + strand * 0.045f))
                    + normal * (wave * 9f * Projectile.scale * MathF.Sin(t * MathHelper.Pi));
                float width = (3f + 9f * (1f - t)) * Projectile.scale;
                float opacity = strength * (1f - t) * 0.55f;
                Color color = Color.Lerp(new Color(255, 244, 165, 0), new Color(255, 85, 12, 0), t);
                DrawFireSegment(bloom, previous, point, width, color * opacity);
                previous = point;
            }
        }
        Main.spriteBatch.DrawFromCenter_VectorScale(bloom, head - Main.screenPosition, null,
            new Color(255, 185, 45, 0) * (strength * 0.25f), 0f,
            new Vector2(75f * Projectile.scale / bloom.Width));
    }

    private static void DrawFireSegment(Texture2D bloom, Vector2 start, Vector2 end, float width, Color color)
    {
        Vector2 delta = end - start;
        float length = delta.Length();
        if (length < 0.001f)
            return;
        Main.spriteBatch.DrawFromCenter_VectorScale(bloom, (start + end) * 0.5f - Main.screenPosition,
            null, color, delta.ToRotation(), new Vector2((length + width) / bloom.Width, width / bloom.Height));
    }

    private void DrawHeadTrail()
    {
        if (_HeadTrail.Count < 2)
            return;
        Texture2D bloom = ParticleHandler.GetTexture<BloomParticle>();
        float fade = CurrentPhase == Phase.Recovering ? Math.Clamp(1f - Timer / 12f, 0f, 1f) : 1f;
        for (int i = 1; i < _HeadTrail.Count; i++)
        {
            float strength = i / (float)_HeadTrail.Count;
            Vector2 segment = _HeadTrail[i] - _HeadTrail[i - 1];
            float length = segment.Length();
            if (length < 0.01f)
                continue;
            Vector2 center = (_HeadTrail[i] + _HeadTrail[i - 1]) * 0.5f - Main.screenPosition;
            float diameter = MathHelper.Lerp(18f, 120f, strength) * Projectile.scale;
            float intensity = strength * strength * fade * Math.Min(1f, length / 3f);
            Main.spriteBatch.DrawFromCenter_VectorScale(bloom, center, null,
                new Color(255, 90, 10, 0) * (intensity * 0.12f), segment.ToRotation(),
                new Vector2((diameter + length * 2f) / bloom.Width, diameter / bloom.Height));
            Main.spriteBatch.DrawFromCenter_VectorScale(bloom, center, null,
                new Color(255, 225, 100, 0) * (intensity * 0.18f), segment.ToRotation(),
                new Vector2((diameter * 0.45f + length * 2f) / bloom.Width, diameter * 0.45f / bloom.Height));
            // 三股流火沿宽尾蜿蜒；亮度随段长归一，避免密采样过曝。
            Vector2 normal = new(-segment.Y / length, segment.X / length);
            for (int strand = 0; strand < 3; strand++)
            {
                float phase = strength * 18f - _FlameTime * 0.24f + strand * MathHelper.TwoPi / 3f;
                float offset = MathF.Sin(phase) * 22f * Projectile.scale * (1f - strength);
                Vector2 strandCenter = center + Main.screenPosition + normal * offset;
                Color color = Color.Lerp(new Color(255, 75, 8, 0), new Color(255, 240, 145, 0), strength);
                DrawFireSegment(bloom, strandCenter - segment * 0.5f, strandCenter + segment * 0.5f,
                    (8f + strength * 18f) * Projectile.scale, color * (intensity * 0.2f));
            }
        }
    }
    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = TextureAssets.Projectile[Type].Value;
        DrawHeadTrail();
        Main.spriteBatch.End();
        try
        {
            Matrix transform = WeaponTransform
                * Matrix.CreateTranslation(-Main.screenPosition.X, -Main.screenPosition.Y, 0f)
                * Main.GameViewMatrix.TransformationMatrix;
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, RasterizerState.CullNone, null, transform);
            Main.spriteBatch.Draw(texture, Vector2.Zero, Color.Lerp(lightColor, Color.White, 0.4f));
            Main.spriteBatch.End();
        }
        finally
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        }
        DrawHeadFlames();
        if (CurrentPhase == Phase.Charging && StarburstCircleAssets.IsLoaded)
        {
            float charge = Timer / ChargeDuration;
            float pulse = charge >= 1f ? 1f + 0.12f * MathF.Sin(Main.GlobalTimeWrappedHourly * 5f) : 1f;
            Texture2D star = StarburstCircleAssets.Textures.StarFlare;
            Main.spriteBatch.Draw(star, Projectile.Center - Main.screenPosition, null,
                new Color(255, 210, 110, 0) * charge, 0f, star.Size() / 2f,
                64f / star.Width * pulse, SpriteEffects.None, 0f);
        }
        return false;
    }
}
