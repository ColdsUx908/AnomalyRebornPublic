using Anomalies.Assets.Effects;

namespace Anomalies.Common.Items;

internal static class GoldItemNameDrawing
{
    public static bool PreDrawTooltipLine(DrawableTooltipLine line)
    {
        if (Main.dedServ || !AnomalyClientConfig.Instance.EnableShaders || AnomalyCommonEffects.GoldText is null)
            return true;
        return !TODrawUtils.TryDrawItemNameWithCustomShader(line, l => AnomalyCommonEffects.GoldText.Data
            .SetCustomParameter("uScreenResolution", Main.ScreenSize.ToVector2() * Math.Max(Main.UIScale, 1f) / 2f)
            .SetCustomParameter("uScreenRatio", TODrawUtils.ScreenRatio)
            .SetCustomParameter("uPosition", new Vector2(l.X, l.Y))
            .Apply());
    }
}
