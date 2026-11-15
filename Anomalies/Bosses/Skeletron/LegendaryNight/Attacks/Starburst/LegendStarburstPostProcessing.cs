using Anomalies.Assets.Effects;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Transoceanic.Graphics.Drawing;

namespace Anomalies.Bosses.Skeletron.LegendaryNight;

/// <summary>星爆、钱币审判和冲击波的短时场景反馈；由当前有效实体推导，攻击结束后自动失活。</summary>
public sealed class LegendStarburstPostProcessing : ModSystem
{
    private const string FilterKey = "Anomalies:LegendaryNight/LegendStarburstRadialBlur";
    private static Filter _Filter;
    private static AnomalyScreenShaderData _Data;

    public override void PostSetupContent()
    {
        Filter filter = LegendaryNightEffects.LegendStarburstRadialBlur;
        if (Main.dedServ || filter?.GetShader() is not AnomalyScreenShaderData data)
            return;
        TOCleanup.Register(Clear);
        _Filter = filter;
        _Data = data;
        data.BeforeApply = ApplyParameters;
    }

    public override void PostUpdateEverything()
    {
        if (_Filter is null)
            return;
        if (AnomalyClientConfig.Instance.EnableShaders && TryGetBurst(out LegendStarburstVisuals.Burst frame))
        {
            // Arm capture at age zero, before the first nonzero blur sample. FilterManager's
            // scene fade must not gate this short impulse or omit frames after reactivation.
            _Data.UseIntensity(LegendStarburstVisuals.BlurStrength(frame.Age) * frame.BlurScale);
            if (!_Filter.IsActive())
                Filters.Scene.Activate(FilterKey, frame.Center);
            _Filter.Opacity = 1f;
            _Data.UseGlobalOpacity(1f);
        }
        else
            Deactivate();
    }

    public override void ModifyScreenPosition()
    {
        if (TryGetBurst(out LegendStarburstVisuals.Burst frame, includeCross: false))
            Main.screenPosition -= LegendStarburstVisuals.ShakeOffset(frame.Age) * frame.ShakeScale;
    }

    private static bool TryGetBurst(out LegendStarburstVisuals.Burst frame, bool includeCross = true)
    {
        frame = default;
        if (Main.dedServ || Main.gameMenu)
            return false;
        bool found = false;
        foreach (Projectile projectile in Projectile.ActiveProjectiles)
        {
            LegendStarburstVisuals.Burst candidate = default;
            bool visible = projectile.ModProjectile switch
            {
                LegendStellarShockwave wave => wave.TryGetImpactFrame(out candidate),
                LegendShockwave wave => wave.TryGetImpactFrame(out candidate),
                LegendStarburst burst => burst.TryGetVisualFrame(out candidate),
                CoinJudgment judgment when includeCross || (int)projectile.ai[0] is 0 or 2 => judgment.TryGetBlurFrame(out candidate),
                _ => false
            };
            if (visible && candidate.Age < LegendStarburstVisuals.BlurDuration
                && (!found || FeedbackStrength(candidate, includeCross) > FeedbackStrength(frame, includeCross)
                    || FeedbackStrength(candidate, includeCross) == FeedbackStrength(frame, includeCross) && candidate.Age < frame.Age))
            {
                // 选择当前较强反馈，强度相同时取较新项；重叠爆发不叠加全屏效果。
                frame = candidate;
                found = true;
            }
        }
        return found;
    }

    private static float FeedbackStrength(in LegendStarburstVisuals.Burst frame, bool blur) => blur
        ? LegendStarburstVisuals.BlurStrength(frame.Age) * frame.BlurScale
        : LegendStarburstVisuals.ShakeOffset(frame.Age).LengthSquared() * frame.ShakeScale * frame.ShakeScale;

    internal static bool TryCreateShockwaveFrame(Vector2 center, float radius, int age, float strength,
        int duration, out LegendStarburstVisuals.Burst frame)
    {
        frame = default;
        return !Main.dedServ && !Main.gameMenu
            && LegendStarburstVisuals.TryCreateShockwaveFrame(center, radius, Main.LocalPlayer.Distance(center),
                age, strength, duration, out frame);
    }

    public override void OnWorldUnload() => Deactivate();

    internal static void Deactivate()
    {
        if (_Data is not null)
        {
            _Data.UseIntensity(0f);
            _Data.UseGlobalOpacity(0f);
        }
        _Filter?.Deactivate();
        if (_Filter is not null)
            _Filter.Opacity = 0f;
    }

    private static void Clear()
    {
        Deactivate();
        if (_Data is not null)
            _Data.BeforeApply = null;
        _Data = null;
        _Filter = null;
    }

    private static void ApplyParameters(ScreenShaderData data)
    {
        // Read the same live music age as drawing, after the camera shake and zoom.
        if (AnomalyClientConfig.Instance.EnableShaders && TryGetBurst(out LegendStarburstVisuals.Burst frame))
        {
            WorldView2D view = new(Main.screenPosition, Main.GameViewMatrix.TransformationMatrix,
                new Vector2(Main.screenWidth, Main.screenHeight));
            data.UseDirection(view.WorldToUv(frame.Center));
            data.UseIntensity(LegendStarburstVisuals.BlurStrength(frame.Age) * frame.BlurScale);
        }
        else
            data.UseIntensity(0f);
    }
}
