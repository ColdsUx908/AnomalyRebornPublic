namespace Anomalies.SpecialContents.Developer.EventideReunion;

internal static class EventideReunionRecall
{
    internal const int Duration = 48;

    // 三束光分别直行或沿两侧弧线归体，终点跟随当前玩家。
    internal static Vector2 Position(Vector2 origin, Vector2 hand, int elapsed, int strand, out float progress)
    {
        progress = Math.Clamp((elapsed - strand * 7f) / (24f + strand * 5f), 0f, 1f);
        Vector2 normal = (hand - origin).SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2);
        return Vector2.Lerp(origin, hand, progress) + normal * (MathF.Sin(progress * MathHelper.Pi) * (strand - 1) * 85f);
    }
}
