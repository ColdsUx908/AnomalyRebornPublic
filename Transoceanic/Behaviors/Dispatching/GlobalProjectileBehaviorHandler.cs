using Transoceanic.Behaviors.Collections;
using Terraria.ModLoader.Core;

namespace Transoceanic.Behaviors.Dispatching;

/// <summary>
/// 将 tModLoader 实体回调转发到适用行为，按行为集合维护优先级、方法分组和实体绑定。
/// </summary>
internal sealed partial class GlobalProjectileBehaviorHandler : GlobalProjectile
{
    /// <inheritdoc/>
    /// <remarks>
    /// 返回 true，为每个实体分配独立的框架实例数据。
    /// </remarks>
    public override bool InstancePerEntity => true;

    /// <summary>
    /// 获取当前 Handler 用于查询和分发的行为集合；由框架在内容加载和卸载时填充、清理。
    /// </summary>
    internal static readonly GlobalEntityBehaviorSet<Projectile, GlobalProjectileBehavior> _BehaviorSet = new();

    private static readonly SimpleEntityBehaviorSet<Projectile, GlobalProjectileBehavior>.DispatchGroup _CollisionGroup =
        _BehaviorSet.GetDispatch(nameof(Colliding));

    private static bool[] _PossibleCollisionOverrides = [];

    internal static void IndexCollisionHooks()
    {
        GlobalHookList<GlobalProjectile> hooks = GlobalHookList<GlobalProjectile>.Create<Func<Projectile, Rectangle, Rectangle, bool?>>(g => g.Colliding);
        bool[] overrides = new bool[ProjectileLoader.ProjectileCount];
        for (int type = 0; type < overrides.Length; type++)
        {
            foreach (GlobalProjectile global in hooks.Enumerate(type))
            {
                if (global is not GlobalProjectileBehaviorHandler || HasBehaviorCandidate(type))
                {
                    overrides[type] = true;
                    break;
                }
            }
        }
        _PossibleCollisionOverrides = overrides;

        static bool HasBehaviorCandidate(int type)
        {
            foreach (GlobalProjectileBehavior behavior in _CollisionGroup.Behaviors)
            {
                if (!behavior.HasFixedCollisionTypes || behavior.HasCollisionHandler(type))
                    return true;
            }
            return false;
        }
    }

    internal static void ClearCollisionHooks() => _PossibleCollisionOverrides = [];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool MayOverrideCollision(int type) =>
        (uint)type >= (uint)_PossibleCollisionOverrides.Length || _PossibleCollisionOverrides[type];

    public override bool? Colliding(Projectile projectile, Rectangle projHitbox, Rectangle targetHitbox)
    {
        foreach (GlobalProjectileBehavior behavior in _CollisionGroup.Behaviors)
        {
            if (!behavior.HasCollisionHandler(projectile.type))
                continue;
            using EntityBehavior<Projectile>.EntityLock entityLock = behavior.LockEntity();
            if (behavior.ShouldProcess && behavior.Colliding(projectile, projHitbox, targetHitbox) is bool result)
                return result;
        }
        return base.Colliding(projectile, projHitbox, targetHitbox);
    }
}
