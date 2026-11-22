using Anomalies.Assets.Effects;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;

namespace Anomalies.SpecialContents.Developer.EventideReunion;

public sealed class EventideReunionStarLens : ModSystem
{
    private Projectile _Source;
    public override bool IsLoadingEnabled(Mod mod) => AnomalySpecialContentToggler.EventideReunion;
    public override void PostSetupContent()
    {
        if (!Main.dedServ && EventideReunionEffects.EventideStarLens?.GetShader() is AnomalyScreenShaderData data)
            data.BeforeApply = Apply;
    }

    public override void PostUpdateEverything()
    {
        Filter filter = EventideReunionEffects.EventideStarLens;
        if (Main.dedServ || filter is null)
            return;
        _Source = null;
        float closest = float.MaxValue;
        if (!Main.gameMenu && AnomalyClientConfig.Instance.EnableShaders)
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.ModProjectile is not EventideReunionStarSkill || projectile.ai[0] is < EventideReunionStarSkill.Arrival or >= (EventideReunionStarSkill.Burst + EventideReunionStarSkill.WaveDuration))
                    continue;
                float age = projectile.ai[0] - EventideReunionStarSkill.Burst;
                if (age is >= -12f and < 0f)
                    continue;
                float distance = MathF.Abs(Main.LocalPlayer.Distance(projectile.Center) - (age >= 0f ? EventideReunionStarSkill.WaveRadius(age) : 0f));
                if (distance < closest)
                {
                    closest = distance;
                    _Source = projectile;
                }
            }
        if (_Source is null)
        {
            if (filter.IsActive())
                filter.Deactivate();
        }
        else if (!filter.IsActive())
            Filters.Scene.Activate("Anomalies:EventideReunion/EventideStarLens", _Source.Center);
    }

    private void Apply(ScreenShaderData data)
    {
        if (_Source is not { active: true })
        {
            data.UseIntensity(0f);
            return;
        }
        float progress = MathHelper.Clamp((_Source.ai[0] - EventideReunionStarSkill.Arrival) / 72f, 0f, 1f);
        WorldView2D view = new(Main.screenPosition, Main.GameViewMatrix.TransformationMatrix, new Vector2(Main.screenWidth, Main.screenHeight));
        data.UseDirection(view.WorldToUv(_Source.Center));
        float elapsed = _Source.ai[0] - EventideReunionStarSkill.Burst;
        bool wave = elapsed >= 0f;
        data.Shader.Parameters["uWave"].SetValue(wave ? 1f : 0f);
        data.Shader.Parameters["uZoomScale"].SetValue(Main.GameViewMatrix.Zoom.X);
        data.UseProgress(wave ? elapsed / EventideReunionStarSkill.WaveDuration : progress);
        data.Shader.Parameters["uViewportSize"].SetValue(new Vector2(Main.screenWidth, Main.screenHeight));
        data.Shader.Parameters["uRadius"].SetValue((wave ? EventideReunionStarSkill.WaveRadius(elapsed) : 180f - 80f * progress) * Main.GameViewMatrix.Zoom.X);
        data.UseIntensity(wave ? 1f - MathHelper.SmoothStep(0f, 1f, elapsed / EventideReunionStarSkill.WaveDuration) : MathF.Sin(MathHelper.Pi * progress) * 0.8f);
    }

    public override void OnWorldUnload()
    {
        _Source = null;
        EventideReunionEffects.EventideStarLens?.Deactivate();
    }
}
