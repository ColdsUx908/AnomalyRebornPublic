using Anomalies.Assets.Effects;

namespace Anomalies.Bosses.Skeletron.LegendaryNight;

public sealed partial class LegendaryNightOverlay
{
    // Normalize attack envelopes before merging them per pixel.
    private const float PeakSlapFlameStrength = 1f;
    private const float Unbounded = 1_000_000f;
    private BlendState SlapUnion { get; }
    // Shape packs outer radius, inner radius, upper projection limit, lower projection limit.
    private readonly List<(Vector2 Center, Vector4 Shape, Vector2 Axis, float Strength)> _Slaps = [];

    public LegendaryNightOverlay()
    {
        if (Main.dedServ)
            return;

        // Max coverage merges the silhouettes independently of submission order.
        SlapUnion = new BlendState
        {
            ColorBlendFunction = BlendFunction.Max,
            ColorSourceBlend = Blend.One,
            ColorDestinationBlend = Blend.One
        };
        TOCleanup.Register(SlapUnion.Dispose);
    }

    internal static bool UseSlapFlame => AnomalyClientConfig.Instance.EnableShaders
        && LegendaryNightEffects.LegendSlapFlame?.Asset.IsLoaded == true;

    private void DrawSlapFlame(SpriteBatch spriteBatch, LegendaryNightPhase2Arena arena)
    {
        if (!UseSlapFlame || arena is null)
            return;

        _Slaps.Clear();
        SkeletronLegendHead head = arena?.MasterBehavior;
        if (head is { Mechanic2Active: true } && head.Mechanic2HandPattern != 0)
        {
            for (int strike = 0; strike < SkeletronLegendHead.Mechanic2Parameters.StrikeCount; strike++)
            {
                int age = head.Mechanic2Age - SkeletronLegendHead.Mechanic2Parameters.GetStrikeTime(strike);
                float opacity = SkeletronLegendHead.Mechanic2Parameters.HalfFieldOpacity(age);
                if (opacity > 0f)
                    _Slaps.Add((arena.Projectile.Center, new Vector4(arena.Radius, -Unbounded, Unbounded, 0f),
                        Vector2.UnitX * head.Mechanic2StrikeSide(strike),
                        opacity * (PeakSlapFlameStrength / SkeletronLegendHead.Mechanic2Parameters.HalfFieldMaxOpacity)));
            }
        }

        if (_Slaps.Count == 0)
            return;

        Effect effect = LegendaryNightEffects.LegendSlapFlame.Asset.Value;
        Matrix inverseView = Matrix.Invert(Main.GameViewMatrix.TransformationMatrix);
        Rectangle screen = new(0, 0, Main.screenWidth, Main.screenHeight);
        effect.Parameters["uInverseViewMatrix"].SetValue(inverseView);
        effect.Parameters["uScreenPosition"].SetValue(Main.screenPosition);
        effect.Parameters["uResolution"].SetValue(new Vector2(screen.Width, screen.Height));
        using RenderTargetLease lease = RenderTargetPool.Shared.Rent(Main.instance.GraphicsDevice,
            screen.Width, screen.Height, RenderTargetDescriptor.Default);
        using (lease.Scope(clearColor: Color.Transparent))
        {
            spriteBatch.Begin(SpriteSortMode.Immediate, SlapUnion, SamplerState.LinearClamp,
                DepthStencilState.None, Main.Rasterizer, null, Matrix.Identity);
            foreach ((Vector2 center, Vector4 shape, Vector2 axis, float strength) in _Slaps)
            {
                effect.Parameters["uCenter"].SetValue(center);
                effect.Parameters["uShape"].SetValue(shape);
                effect.Parameters["uAxis"].SetValue(axis);
                effect.Parameters["uStrength"].SetValue(strength);
                effect.CurrentTechnique.Passes["Coverage"].Apply();
                spriteBatch.Draw(TOTextures.FillerTexture, screen, Color.White);
            }
            spriteBatch.End();
        }

        effect.Parameters["uStarColor"].SetValue(LegendaryNightHandler.StarColor.ToVector3());
        effect.Parameters["uFlowTime"].SetValue((float)Main.timeForVisualEffects / 60f);
        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp,
            DepthStencilState.None, Main.Rasterizer, null, Matrix.Identity);
        effect.CurrentTechnique.Passes["Pass0"].Apply();
        spriteBatch.Draw(lease.Target, screen, Color.White);
        spriteBatch.End();

    }
}
