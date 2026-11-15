using Terraria.Graphics.Shaders;

namespace Anomalies.Assets.Effects;

/// <summary>
/// 普通着色器资产及其原生参数模板，由统一加载器管理。
/// </summary>
public sealed record MiscShaderContainer(Asset<Effect> Asset, MiscShaderData Data);
