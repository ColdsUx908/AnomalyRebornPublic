namespace Transoceanic.Behaviors;

/// <summary>
/// 面向全部弹幕的行为基类，由全局弹幕调度器筛选并调用。
/// </summary>
public abstract partial class GlobalProjectileBehavior : GlobalEntityBehavior<Projectile>
{
    /// <summary>
    /// 该弹幕类型是否可能由本行为处理碰撞。按类型转发的适配器可用注册表排除无候选类型。
    /// 默认允许所有类型；此查询发生在绑定保护及 ShouldProcess 前，只能依据类型和注册信息。
    /// </summary>
    public virtual bool HasCollisionHandler(int projectileType) => true;

    /// <summary>
    /// 若注册信息在配方加载完成至模组卸载之间固定，可允许宿主预先记录各类型的碰撞候选。
    /// 默认将本行为视为潜在覆盖，以兼容运行中重新填充的单实体集合。
    /// </summary>
    public virtual bool HasFixedCollisionTypes => false;

    /// <inheritdoc cref="GlobalProjectile.CanCutTiles"/>
    public virtual bool? CanCutTiles(Projectile projectile) => null;

    /// <inheritdoc cref="GlobalProjectile.CanDamage"/>
    public virtual bool? CanDamage(Projectile projectile) => null;

    /// <inheritdoc cref="GlobalProjectile.CanUseGrapple"/>
    public virtual bool? CanUseGrapple(int type, Player player) => null;

    /// <inheritdoc cref="GlobalProjectile.UseGrapple"/>
    public virtual void UseGrapple(Player player, ref int type) { }
}
