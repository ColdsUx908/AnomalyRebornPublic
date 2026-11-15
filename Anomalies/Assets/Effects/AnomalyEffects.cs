using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;

namespace Anomalies.Assets.Effects;

/// <summary>
/// 按提供者目录和嵌套容器路径统一加载着色器。
/// </summary>
public static class AnomalyEffects
{
    internal static void Load(Mod mod, IAnomalyEffectProvider provider)
    {
        LoadType(provider.GetType(), provider.EffectDirectory);

        void LoadType(Type type, string directory)
        {
            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly).OrderBy(field => field.Name, StringComparer.Ordinal))
            {
                if (field.FieldType != typeof(MiscShaderContainer) && field.FieldType != typeof(Filter))
                    continue;

                string relativePath = $"{directory}/{field.Name}";
                string key = $"{mod.Name}:{relativePath}";
                string pass = field.GetCustomAttribute<ShaderPassAttribute>()?.Name ?? "Pass0";
                // 登记先于资源请求，加载中途失败也能清理已经赋值的字段。
                TOCleanup.Register(() => field.SetValue(null, null));
                Asset<Effect> asset = mod.Assets.Request<Effect>($"Assets/Effects_Compiled/{relativePath}");
                if (field.FieldType == typeof(Filter))
                {
                    AnomalyScreenShaderData data = new(asset, pass);
                    Filter filter = new(data, EffectPriority.VeryHigh);
                    TOCleanup.Register(() =>
                    {
                        data.BeforeApply = null;
                        filter.Deactivate();
                    });
                    Filters.Scene[key] = filter;
                    filter.Load();
                    field.SetValue(null, filter);
                }
                else
                {
                    MiscShaderData data = new(asset, pass);
                    GameShaders.Misc[key] = data;
                    field.SetValue(null, new MiscShaderContainer(asset, data));
                }
            }

            foreach (Type nestedType in type.GetNestedTypes(BindingFlags.Public).Where(type => type.IsAbstract && type.IsSealed).OrderBy(type => type.Name, StringComparer.Ordinal))
                LoadType(nestedType, $"{directory}/{nestedType.Name}");
        }
    }
}
