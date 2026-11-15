using Transoceanic.Graphics.Drawing;
using Transoceanic.Graphics.Rendering;
using static Anomalies.Bosses.Skeletron.LegendaryNight.LegendStarburstVisuals;

namespace Anomalies.Bosses.Skeletron.LegendaryNight;

/// <summary>铜币的实心铸纹与金币的空心铸纹：共用流光、同心刻线和向外飞散的金属星屑。</summary>
internal static class CoinRadialVisuals
{
    public const int StartAge = -8;
    public const int EndAge = 84;
    public const int WarningHandoffAge = -2;
    private const int SparkCount = 96;
    private const int SegmentCount = 96;

    public readonly record struct Frame(Vector2 Center, float OuterRadius, float InnerRadius, float Age, int CoinType);

    public static bool IsVisible(float age) => age > StartAge && age < EndAge;
    public static bool CoversWarning(float age) => age >= WarningHandoffAge;
    public static float Envelope(float age) => Smooth(Clamp((age - StartAge) / -StartAge))
        * (1f - Smooth(Clamp((age - 26f) / 58f)));

    public static void Draw(SpriteBatch batch, Effect effect, in CoinCrossVisuals.Textures textures,
        in WorldView2D view, in Frame frame, BlendState blend)
    {
        if (!IsVisible(frame.Age) || frame.OuterRadius <= frame.InnerRadius)
            return;
        CoinJudgmentPalette palette = CoinJudgmentPalette.For(frame.CoinType);
        float envelope = Envelope(frame.Age);
        if (effect is not null)
        {
            effect.Parameters["uInverseViewMatrix"].SetValue(Matrix.Invert(view.Transform));
            effect.Parameters["uScreenPosition"].SetValue(view.ScreenPosition);
            effect.Parameters["uResolution"].SetValue(view.Resolution);
            effect.Parameters["uCenter"].SetValue(frame.Center);
            effect.Parameters["uOuterRadius"].SetValue(frame.OuterRadius);
            effect.Parameters["uInnerRadius"].SetValue(frame.InnerRadius);
            effect.Parameters["uTime"].SetValue(frame.Age);
            effect.Parameters["uEnvelope"].SetValue(envelope);
            effect.Parameters["uWave"].SetValue(new Vector2(OutCubic(Clamp((frame.Age + 1f) / 16f)),
                1f - Smooth(Clamp((frame.Age - 12f) / 16f))));
            effect.Parameters["uShade"].SetValue(palette.Shade.ToVector3());
            effect.Parameters["uColor"].SetValue(palette.Body.ToVector3());
            effect.Parameters["uLight"].SetValue(palette.Light.ToVector3());
            effect.Parameters["uFlow"].SetValue(textures.Flow);
            effect.Parameters["uStreak"].SetValue(textures.Streak);
            effect.Parameters["uNoise"].SetValue(textures.Warp);
            batch.Begin(SpriteSortMode.Immediate, blend, SamplerState.LinearClamp,
                DepthStencilState.None, RasterizerState.CullNone);
            effect.CurrentTechnique.Passes["Pass0"].Apply();
            batch.Draw(textures.Flow, new Rectangle(0, 0, (int)view.Resolution.X, (int)view.Resolution.Y), Color.White);
            batch.End();
        }
        else
        {
            // 无着色器时用闭合的光带保留内孔，不用实心精灵覆盖金币的安全区。
            batch.Begin(SpriteSortMode.Deferred, blend, SamplerState.LinearClamp,
                DepthStencilState.None, RasterizerState.CullNone, null, view.Transform);
            if (frame.InnerRadius <= 0f)
                SpriteDrawing.DrawSized(batch, LegendStarburstCircleAssets.Textures.ExplosionDisc,
                    frame.Center - view.ScreenPosition, new Vector2(frame.OuterRadius * 2f), palette.Body, opacity: envelope * 0.4f);
            for (int edge = 0; edge < 2; edge++)
            {
                float radius = edge == 0 ? frame.OuterRadius : frame.InnerRadius;
                if (radius <= 0f)
                    continue;
                for (int i = 0; i < SegmentCount; i++)
                {
                    float angle = i * MathHelper.TwoPi / SegmentCount;
                    Vector2 direction = new(MathF.Cos(angle), MathF.Sin(angle));
                    SpriteDrawing.DrawSized(batch, textures.ShotLine, frame.Center - view.ScreenPosition + direction * radius,
                        new Vector2(radius * MathHelper.TwoPi / SegmentCount * 1.7f, 35f), palette.Light,
                        angle + MathHelper.PiOver2, envelope);
                }
            }
            batch.End();
        }

        batch.Begin(ReferenceEquals(blend, LightweightBloom.AdditiveBlend) ? SpriteSortMode.Texture : SpriteSortMode.Deferred,
            blend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, view.Transform);
        float age = frame.Age;
        float sparkEnvelope = Smooth(Clamp(age / 4f)) * (1f - Smooth(Clamp((age - 30f) / 54f)));
        float band = frame.OuterRadius - frame.InnerRadius;
        for (int i = 0; i < SparkCount; i++)
        {
            float phase = (i * 0.61803398875f + 0.21f) % 1f;
            float angle = i * MathHelper.TwoPi / SparkCount + age * (i % 2 == 0 ? 0.0018f : -0.0018f);
            Vector2 direction = new(MathF.Cos(angle), MathF.Sin(angle));
            float radius = frame.InnerRadius + band * (0.12f + 0.82f * phase);
            float travel = Math.Max(0f, age - i % 7) * (0.6f + phase * 1.4f);
            radius = Math.Min(frame.OuterRadius - 8f, radius + travel);
            float size = (i % 8 == 0 ? 32f : 16f) * (0.65f + phase * 0.65f);
            float alpha = sparkEnvelope * (0.7f + 0.3f * MathF.Sin(i * 2.1f + age * 0.35f));
            Vector2 position = frame.Center - view.ScreenPosition + direction * radius;
            SpriteDrawing.DrawSized(batch, textures.Tail, position - direction * (9f + travel * 0.12f),
                new Vector2(22f + travel * 0.24f, 3f), palette.Body, angle, alpha * 0.7f);
            SpriteDrawing.DrawSized(batch, textures.Star, position, new Vector2(size), palette.Spark, angle, alpha);
        }
        batch.End();
    }
}
