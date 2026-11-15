namespace Anomalies.Assets.Effects;

/// <summary>
/// 指定统一加载器创建原生着色器数据时使用的 Pass；默认使用 Pass0。
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class ShaderPassAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}
