namespace Anomalies.Common;

/// <summary>
/// 弹幕伤害形状的保守外界。边缘接触必须交给实体的精确碰撞。
/// </summary>
internal readonly struct ProjectileDamageBounds(float left, float top, float right, float bottom)
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ProjectileDamageBounds Around(Vector2 center, float radius) =>
        new(center.X - radius, center.Y - radius, center.X + radius, center.Y + radius);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ProjectileDamageBounds Swept(Vector2 start, Vector2 end, float padding) =>
        new(Math.Min(start.X, end.X) - padding, Math.Min(start.Y, end.Y) - padding,
            Math.Max(start.X, end.X) + padding, Math.Max(start.Y, end.Y) + padding);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Excludes(Rectangle target) =>
        target.Left > right || target.Right < left || target.Top > bottom || target.Bottom < top;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool? CanHitNPC(int projectileType, NPC target)
    {
        if (!Excludes(target.Hitbox) || target.netOffset != Vector2.Zero || target.type == NPCID.SolarCrawltipedeTail)
            return null;
        AnomalyGlobalNPC data = target.Anomaly;
        if (data.Z != 0f || data.Depth_DrawAnchor.HasValue || BehaviorQueries.MayOverrideProjectileCollision(projectileType))
            return null;
        return false;
    }
}
