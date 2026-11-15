namespace Anomalies.Assets.Effects;

/// <summary>
/// 提供一个资源目录及其静态着色器容器。
/// </summary>
public interface IAnomalyEffectProvider
{
    public abstract string EffectDirectory { get; }
}

/// <summary>
/// 通过原生内容加载流程筛选提供者，复用统一资源加载和清理。
/// </summary>
public abstract class AnomalyEffectProvider : ILoadable, IAnomalyEffectProvider
{
    public abstract string EffectDirectory { get; }

    public virtual bool IsLoadingEnabled(Mod mod) => true;

    void ILoadable.Load(Mod mod)
    {
        if (!Main.dedServ && AnomalyClientConfig.Instance.EnableShaders)
            AnomalyEffects.Load(mod, this);
    }

    void ILoadable.Unload() { }
}
