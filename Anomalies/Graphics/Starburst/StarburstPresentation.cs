namespace Anomalies.Visuals.Starburst;

internal static class StarburstPresentation
{
    public const int DamageDuration = 24;
    public static readonly SoundStyle Sound = new("Anomalies/Assets/Sounds/Starburst") { MaxInstances = 0 };

    // 与 Boss 一样，前七帧显示完整伤害边界，再由扩张波覆盖。
    public static void DrawBoundary(SpriteBatch batch, in StarburstVisuals.Burst frame)
    {
        if (frame.Age is >= 0f and < StarburstCircleVisuals.ExpansionDuration)
            AnomalyDrawUtils.DrawGlowingCircle(batch, frame.Center, frame.OuterRadius,
                Color.Red, 0.22f * (1f - StarburstCircleVisuals.Expansion(frame.Age)), 3f);
    }
}
