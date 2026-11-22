namespace Anomalies.GameContents.LegendaryNight;

internal static class StarfallKnockerGeometry
{
    public const int ChargeDuration = 300;
    public const int SwingDuration = 18;
    public const int ImpactPause = 5;
    public const int RecoveryDuration = 21;
    public const float HeadRadius = 38f;
    public const float BurstRadius = 800f;
    public const float ChargeAngle = MathHelper.Pi - MathHelper.Pi / 9f;
    public const float HeadTurnDuration = 4f;
    public static readonly Vector2 TextureGrip = new(38f, 149f);
    // 装饰星星定义原有挥动轴；黄色锤面是独立的命中和特效锚点。
    public static readonly Vector2 TextureAxisEnd = new(128f, 64f);
    public static readonly Vector2 TextureHead = new(158f, 98f);

    // 下砸覆盖前方 -90..45 度，上挑覆盖 -45..90 度，重叠区优先下砸。
    public static float SelectSwingSide(Vector2 aim, float gravity) =>
        MathF.Atan2(aim.Y * gravity, Math.Abs(aim.X)) <= MathHelper.PiOver4 ? 1f : -1f;

    public static Vector2 ChargeTarget(int facing, float gravity) =>
        new Vector2(MathF.Cos(ChargeAngle) * facing, MathF.Sin(ChargeAngle) * gravity) * (TextureAxisEnd - TextureGrip).Length();

    public static Vector2 ChargeStep(Vector2 previous, Vector2 target) => Vector2.Lerp(previous, target, 0.19f);
    public static Vector2 SwingVector(int facing, float gravity, float side, float elapsed)
    {
        float progress = Math.Clamp(elapsed / SwingDuration, 0f, 1f);
        // 下砸经背后向上抡起；上挑直接从后拖位置向前下方抡出。
        float initialAngle = side > 0f ? ChargeAngle - MathHelper.TwoPi : ChargeAngle;
        float angle = MathHelper.Lerp(initialAngle, MathHelper.PiOver4 * side, SwingProgress(progress));
        return new Vector2(MathF.Cos(angle) * facing, MathF.Sin(angle) * gravity) * (TextureAxisEnd - TextureGrip).Length();
    }
    // 落日遗逢式短暂压住、快速爆发、末端收势。
    public static float SwingProgress(float progress)
    {
        if (progress < 0.18f)
            return 0.04f * MathHelper.SmoothStep(0f, 1f, progress / 0.18f);
        if (progress < 0.78f)
        {
            float burst = (progress - 0.18f) / 0.6f;
            return 0.04f + 0.92f * (1f - MathF.Pow(1f - burst, 3f));
        }
        return 0.96f + 0.04f * MathHelper.SmoothStep(0f, 1f, (progress - 0.78f) / 0.22f);
    }
    public static float HeadHandedness(float side, float elapsed) => side > 0f ? 1f
        : MathF.Cos(MathHelper.Pi * MathHelper.SmoothStep(0f, 1f, Math.Clamp(elapsed / HeadTurnDuration, 0f, 1f)));

    // 纹理握点 -> 锤柄局部轴 -> 2D 缩放/翻面 -> 世界坐标；不含深度或透视。
    public static Matrix TextureTransform(Vector2 grip, Vector2 vector, float scale, float handedness)
    {
        Vector2 textureAxis = TextureAxisEnd - TextureGrip;
        float textureAngle = MathF.Atan2(textureAxis.Y, textureAxis.X);
        float angle = vector.LengthSquared() > 0.0001f ? MathF.Atan2(vector.Y, vector.X) : 0f;
        return Matrix.CreateTranslation(-TextureGrip.X, -TextureGrip.Y, 0f)
            * Matrix.CreateRotationZ(-textureAngle)
            * Matrix.CreateScale(vector.Length() / textureAxis.Length() * scale, 1.2f * handedness * scale, 1f)
            * Matrix.CreateRotationZ(angle)
            * Matrix.CreateTranslation(grip.X, grip.Y, 0f);
    }

    public static Vector2 HeadCenter(Vector2 grip, Vector2 vector, float scale, float handedness) =>
        Vector2.Transform(TextureHead, TextureTransform(grip, vector, scale, handedness));
}
