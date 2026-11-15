using Transoceanic.Graphics.Drawing;
using Transoceanic.Graphics.Rendering;
using Transoceanic.Primitives.Mathematics;
using static Anomalies.Bosses.Skeletron.LegendaryNight.LegendStarburstVisuals;

namespace Anomalies.Bosses.Skeletron.LegendaryNight;

/// <summary>钱币十字的光流、束身消融和独立星尘；游戏与 FNA 预览共用。</summary>
internal static class CoinCrossVisuals
{
    public const int EndAge = 72;
    public const int PeakAge = 8;
    public const float ReferenceWidth = 120f;
    // 使用属性方法体，便于热重载；束身、发射口和延迟收束范围共用宽度倍率。
    internal static float VisualWidthMultiplier => 2.3f;
    // 统一调整星点尺寸和拖尾粗细，不改变发射位置、飞行速度及尾迹长度。
    internal static float ParticleSizeMultiplier => 2f;
    // 每帧读取倍率；同步作用于轴向位移、历史尾迹和预计离场时间。
    internal static float ParticleSpeedMultiplier => 2f;
    // 只乘粒子的 RGB 光能，Alpha 仍由寿命控制；超过 1 的颜色交给统一 HDR Bloom。
    internal static float ParticleColorIntensity => 2f;
    private static readonly Color _StarColor = new(132, 111, 58);
    private static readonly Color _TailColor = new(62, 42, 12);
    private static readonly Star[] _Stars = new Star[400];
    // 只缓存确定性配方，不持有实体、时钟或网络状态。
    private static readonly float[] _StarEnds = new float[400];
    private static float _CachedLength;
    private static float _CachedVisualWidthMultiplier;
    private static float _CachedParticleSizeMultiplier;
    private static float _CachedParticleSpeedMultiplier;

    public readonly record struct Textures(Texture2D Outer, Texture2D Flow, Texture2D Streak,
        Texture2D Warp, Texture2D Star, Texture2D Tail, Texture2D ShotLine);
    // Width 是原伤害判定的完整世界宽度；视觉宽度另乘 VisualWidthMultiplier。
    public readonly record struct Frame(Vector2 Center, float Width, float Length, float Rotation, float Age);
    internal readonly record struct StarState(Vector2 Position, Vector2 Tail, float Velocity,
        float Size, float Opacity, bool Bright);
    private readonly record struct Star(float X, float Y, float Speed, float Phase, float Size,
        bool Bright, float Born, float Life);

    static CoinCrossVisuals()
    {
        Mulberry32Random random = new(71831);
        for (int i = 0; i < _Stars.Length; i++)
            _Stars[i] = new((float)(((i * 0.61803398875 + 0.21) % 1) * 2 - 1),
                (i % 2 == 0 ? -1f : 1f) * Lerp(0.16f, 0.36f, random.NextSingle()),
                Lerp(1f, 2.4f, random.NextSingle()), random.NextSingle() * MathHelper.TwoPi,
                Lerp(1.3f, 2.3f, random.NextSingle()), i % 16 == 0,
                (float)(1 + 45 * ((i * 0.75487766625 + 0.13) % 1)), Lerp(26f, 38f, random.NextSingle()));
    }

    public static bool IsVisible(float age) => age > 0f && age < EndAge;

    public static float Envelope(float age) => !IsVisible(age) ? 0f : age < PeakAge ? OutCubic(age / PeakAge)
        : age < 28f ? Lerp(1f, 0.26f, Smooth((age - PeakAge) / 20f))
        : Lerp(0.26f, 0.012f, OutCubic(Clamp((age - 28f) / 34f)));
    private static float VisualEnvelope(float age) => Envelope(age) * Math.Max(0f, VisualWidthMultiplier);
    private static float ParticleSpeed => float.IsFinite(ParticleSpeedMultiplier) ? Math.Max(0f, ParticleSpeedMultiplier) : 0f;
    internal static float Expansion(float age) => Lerp(0.78f, 1f, OutCubic(Clamp(age / 6f)));
    internal static int StarCount(float length) => Math.Clamp((int)MathF.Ceiling(length / 24f), 32, _Stars.Length);

    public static void Draw(SpriteBatch batch, Effect body, in Textures textures,
        in WorldView2D view, in Frame frame, BlendState blend)
    {
        if (!IsVisible(frame.Age) || frame.Width <= 0f || frame.Length <= 0f)
            return;
        using DrawingBindings bindings = new(batch.GraphicsDevice);
        float scale = frame.Width / ReferenceWidth;
        float referenceLength = frame.Length / scale;
        float length = referenceLength * Expansion(frame.Age);
        float envelope = VisualEnvelope(frame.Age);
        Vector2 center = frame.Center - view.ScreenPosition;
        EnsureStarEnds(referenceLength);
        for (int beam = 0; beam < 2; beam++)
        {
            float rotation = frame.Rotation + beam * MathHelper.PiOver2;
            Matrix local = Matrix.CreateScale(scale) * Matrix.CreateRotationZ(rotation)
                * Matrix.CreateTranslation(center.X, center.Y, 0f) * view.Transform;
            if (body is not null && frame.Age < 64f)
            {
                body.Parameters["uOuter"].SetValue(textures.Outer);
                body.Parameters["uFlow"].SetValue(textures.Flow);
                body.Parameters["uStreak"].SetValue(textures.Streak);
                body.Parameters["uNoise"].SetValue(textures.Warp);
                body.Parameters["uTime"].SetValue(frame.Age);
                // 在 CPU 计算时间包络，兼容项目的 XNA effect preshader 编译器。
                body.Parameters["uRise"].SetValue(Smooth(Clamp(frame.Age / 2f)));
                body.Parameters["uThin"].SetValue(Smooth(Clamp((frame.Age - 24f) / 20f)));
                body.Parameters["uRelease"].SetValue(Smooth(Clamp((frame.Age - 38f) / 26f)));
                body.Parameters["uLength"].SetValue(length);
                batch.Begin(SpriteSortMode.Immediate, blend, SamplerState.LinearClamp,
                    DepthStencilState.None, RasterizerState.CullNone, null, local);
                body.CurrentTechnique.Passes["Pass0"].Apply();
                SpriteDrawing.DrawSized(batch, textures.Outer, Vector2.Zero,
                    new Vector2(length, ReferenceWidth * envelope), Color.White);
                batch.End();
            }
            if (body is null && frame.Age < 64f)
            {
                batch.Begin(SpriteSortMode.Deferred, blend, SamplerState.LinearClamp,
                    DepthStencilState.None, RasterizerState.CullNone, null, local);
                float opacity = Smooth(Clamp(frame.Age / 2f)) * (1f - Smooth(Clamp((frame.Age - 38f) / 26f)));
                SpriteDrawing.DrawSized(batch, textures.ShotLine, Vector2.Zero,
                    new Vector2(length, ReferenceWidth * envelope), new Color(255, 205, 70), opacity: opacity);
                batch.End();
            }
            float colorIntensity = float.IsFinite(ParticleColorIntensity) ? Math.Max(0f, ParticleColorIntensity) : 0f;
            if (colorIntensity <= 0f)
                continue;
            batch.Begin(SpriteSortMode.Immediate, blend, SamplerState.LinearClamp,
                DepthStencilState.None, RasterizerState.CullNone, null, local);
            if (body is not null)
            {
                body.Parameters["uParticleColorIntensity"].SetValue(colorIntensity);
                body.CurrentTechnique.Passes["Particles"].Apply();
            }
            for (int i = 0; i < StarCount(frame.Length); i++)
            {
                StarState state = GetStarState(i, frame.Age, referenceLength);
                if (state.Opacity <= 0.002f)
                    continue;
                DrawParticle(textures.Tail, (state.Position + state.Tail) * 0.5f,
                    new Vector2(Vector2.Distance(state.Position, state.Tail), state.Size * 0.16f),
                    _TailColor, state.Velocity < 0f ? MathHelper.Pi : 0f, state.Opacity * 0.75f);
                float size = state.Size * (state.Bright ? 4f : 2.8f);
                DrawParticle(textures.Star, state.Position, new Vector2(size),
                    _StarColor, 0f, state.Opacity);
            }
            batch.End();

            void DrawParticle(Texture2D texture, Vector2 position, Vector2 size, Color color, float angle, float opacity)
            {
                if (body is not null)
                    SpriteDrawing.DrawSized(batch, texture, position, size, color, angle, opacity);
                else
                    SpriteDrawing.DrawGlow(batch, texture, position, size, color, angle,
                        opacity * Math.Min(colorIntensity, 64f));
            }
        }
    }

    internal static StarState GetStarState(int index, float age, float referenceLength)
    {
        EnsureStarEnds(referenceLength);
        Star star = _Stars[index];
        float elapsed = age - star.Born;
        float velocity = (star.X < 0f ? -1f : 1f) * star.Speed * ParticleSpeed;
        Vector2 position = Position(star, age, referenceLength);
        Vector2 tail = position - new Vector2(velocity * Math.Clamp(elapsed, 0f, 7f), 0f);
        float twinkle = 0.8f + 0.2f * MathF.Pow(0.5f + 0.5f * MathF.Sin(elapsed * 0.25f + star.Phase), 2f);
        float birth = age < PeakAge ? Smooth(Clamp(Margin(star, age, referenceLength) / 2f)) : 1f;
        float opacity = elapsed <= 0f || elapsed >= star.Life || !IsVisible(age) ? 0f
            : Smooth(Clamp(elapsed / 2f)) * twinkle * Smooth(Clamp((_StarEnds[index] - age) / 12f)) * birth;
        return new(position, tail, velocity, star.Size * Math.Max(0f, ParticleSizeMultiplier), opacity, star.Bright);
    }

    private static Vector2 Position(in Star star, float age, float length) => new(
        star.X * length * 0.43f * Expansion(star.Born) + (star.X < 0f ? -1f : 1f) * star.Speed * ParticleSpeed * (age - star.Born),
        star.Y * ReferenceWidth * VisualEnvelope(star.Born) * MathF.Pow(1f - star.X * star.X, 0.65f) * 0.78f);

    private static float Margin(in Star star, float age, float length)
    {
        Vector2 position = Position(star, age, length);
        float radius = star.Size * Math.Max(0f, ParticleSizeMultiplier) * (star.Bright ? 4f : 2.8f) * 0.5f;
        // 粒子始终向外直飞；头部外缘必定比历史尾迹更靠近轴向边界。
        float extent = Math.Abs(position.X) + radius;
        float halfLength = length * 0.5f * Expansion(age);
        float taper = MathF.Pow(1f - Smooth(Clamp((extent / halfLength - 0.65f) / 0.30f)), 0.35f);
        float available = ReferenceWidth * VisualEnvelope(age <= PeakAge ? age : Math.Max(PeakAge, age - 18f)) * 0.40f * taper;
        return Math.Min(halfLength * 0.95f - extent, available - Math.Abs(position.Y) - radius);
    }

    private static void EnsureStarEnds(float length)
    {
        if (_CachedLength == length && _CachedVisualWidthMultiplier == VisualWidthMultiplier
            && _CachedParticleSizeMultiplier == ParticleSizeMultiplier && _CachedParticleSpeedMultiplier == ParticleSpeed)
            return;
        _CachedLength = length;
        _CachedVisualWidthMultiplier = VisualWidthMultiplier;
        _CachedParticleSizeMultiplier = ParticleSizeMultiplier;
        _CachedParticleSpeedMultiplier = ParticleSpeed;
        for (int i = 0; i < _Stars.Length; i++)
        {
            Star star = _Stars[i];
            float end = Math.Min(star.Born + star.Life, EndAge);
            for (float time = Math.Max(PeakAge, star.Born); time <= end; time += 0.5f)
            {
                if (Margin(star, time, length) > 0f)
                    continue;
                float low = Math.Max(star.Born, time - 0.5f);
                float high = time;
                for (int step = 0; step < 12; step++)
                {
                    float middle = (low + high) * 0.5f;
                    if (Margin(star, middle, length) > 0f)
                        low = middle;
                    else
                        high = middle;
                }
                end = low;
                break;
            }
            _StarEnds[i] = end;
        }
    }

    internal readonly struct DrawingBindings : IDisposable
    {
        private readonly GraphicsDevice _Device;
        private readonly Texture[] _Textures;
        private readonly SamplerState[] _Samplers;
        private readonly VertexBufferBinding[] _Vertices;
        private readonly IndexBuffer _Indices;

        public DrawingBindings(GraphicsDevice device)
        {
            _Device = device;
            _Textures = new Texture[7];
            _Samplers = new SamplerState[7];
            for (int slot = 0; slot < 7; slot++)
            {
                _Textures[slot] = device.Textures[slot];
                _Samplers[slot] = device.SamplerStates[slot];
            }
            _Vertices = device.GetVertexBuffers();
            _Indices = device.Indices;
        }

        public void Dispose()
        {
            for (int slot = 0; slot < 7; slot++)
            {
                _Device.Textures[slot] = _Textures[slot];
                _Device.SamplerStates[slot] = _Samplers[slot];
            }
            _Device.SetVertexBuffers(_Vertices);
            _Device.Indices = _Indices;
        }
    }
}
