using Transoceanic.Runtime.Publicizing.Wrappers;

namespace Transoceanic.Graphics.Rendering;

public static partial class LightweightBloom
{
    // Corresponds to KL DrawSystem.DownSampler / UpSampler and ReColor.BloomBlend.
    // Only the requested layer is captured; no render hooks or per-world-layer request queues.
    private static void DrawPyramid<TLayer>(SpriteBatch batch, Effect effect, in BloomSettings settings,
        TLayer layer, RenderTargetBinding[] previous, Viewport viewport) where TLayer : struct, IBloomLayer
    {
        GraphicsDevice device = batch.GraphicsDevice;
        int levels = Math.Clamp(settings.PyramidLevels, 2, 7);
        RenderTargetDescriptor full = RenderTargetDescriptor.Default with { Format = SurfaceFormat.Vector4 };
        RenderTargetDescriptor half = full with { Format = SurfaceFormat.HalfVector4 };
        using RenderTargetLease source = RenderTargetPool.Shared.Rent(device, viewport.Width, viewport.Height, full);
        // Full-resolution extraction is reused for the floating scene composite after filtering.
        using RenderTargetLease scene = RenderTargetPool.Shared.Rent(device, viewport.Width, viewport.Height, full);
        RenderTargetLease[] down = new RenderTargetLease[levels];
        RenderTargetLease up = null;
        RenderTargetPreserver.PreserveBindings(previous);
        if (previous.Length == 0)
            device.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;
        try
        {
            device.SetRenderTarget(source.Target);
            device.Clear(Color.Transparent);
            layer.Draw(batch);
            if (new SpriteBatch_Publicizer(batch).beginCalled)
                throw new InvalidOperationException("Bloom layers must end their SpriteBatch.");

            effect.Parameters["uSourceIntensity"].SetValue(PositiveFinite(settings.SourceIntensity));
            effect.Parameters["uThreshold"].SetValue(PositiveFinite(settings.Threshold));
            Filter(source.Target, scene.Target, "Extract");
            Texture2D input = scene.Target;
            for (int i = 0; i < levels; i++)
            {
                down[i] = RenderTargetPool.Shared.Rent(device, Math.Max(1, viewport.Width >> i),
                    Math.Max(1, viewport.Height >> i), half);
                effect.Parameters["uStrength"].SetValue(i == 0 ? 1.2f : 1f);
                Filter(input, down[i].Target, "PyramidDown");
                input = down[i].Target;
            }
            for (int i = levels - 2; i >= 0; i--)
            {
                RenderTargetLease next = RenderTargetPool.Shared.Rent(device, down[i].Target.Width, down[i].Target.Height, half);
                try
                {
                    // KL uses up index (5 - i): Lerp(.5, .2, index / 5).
                    effect.Parameters["uStrength"].SetValue(MathHelper.Lerp(0.5f, 0.2f, (5 - i) / 5f));
                    Filter(input, next.Target, "PyramidUp", down[i].Target);
                }
                catch
                {
                    device.SetRenderTargets(previous);
                    next.Dispose();
                    throw;
                }
                up?.Dispose();
                up = next;
                input = up.Target;
            }

            device.SetRenderTarget(scene.Target);
            device.Clear(Color.Transparent);
            // A scene target can be sampled before we bind it again. The backbuffer path
            // follows KL's foreground-only composite because the background is unavailable.
            bool hasScene = previous.Length > 0 && previous[0].RenderTarget is RenderTarget2D;
            if (hasScene)
            {
                Texture2D background = (RenderTarget2D)previous[0].RenderTarget;
                batch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp,
                    DepthStencilState.None, RasterizerState.CullNone);
                batch.Draw(background, new Rectangle(0, 0, viewport.Width, viewport.Height),
                    new Rectangle(viewport.X, viewport.Y, viewport.Width, viewport.Height), Color.White);
                batch.End();
            }
            effect.Parameters["uSourceIntensity"].SetValue(PositiveFinite(settings.SourceIntensity));
            batch.Begin(SpriteSortMode.Immediate, settings.AdditiveBody ? AdditiveBlend : BlendState.AlphaBlend,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone);
            effect.CurrentTechnique.Passes["Body"].Apply();
            for (int i = 0; i < Math.Clamp(settings.BodyPasses, 0, 2); i++)
                batch.Draw(source.Target, Vector2.Zero, Color.White);
            batch.End();

            device.SetRenderTargets(previous);
            device.Viewport = viewport;
            effect.Parameters["uStrength"].SetValue(PositiveFinite(settings.Strength));
            batch.Begin(SpriteSortMode.Immediate, hasScene ? BlendState.Opaque : BlendState.AlphaBlend,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone);
            device.Textures[1] = scene.Target;
            device.SamplerStates[1] = SamplerState.LinearClamp;
            effect.CurrentTechnique.Passes["PyramidComposite"].Apply();
            batch.Draw(up.Target, new Rectangle(0, 0, viewport.Width, viewport.Height), Color.White);
            batch.End();
        }
        finally
        {
            try
            {
                if (new SpriteBatch_Publicizer(batch).beginCalled)
                    batch.End();
            }
            finally
            {
                device.SetRenderTargets(previous);
                // Clear sampled leases before returning them to the pool.
                device.Textures[0] = null;
                device.Textures[1] = null;
                up?.Dispose();
                foreach (RenderTargetLease target in down)
                    target?.Dispose();
            }
        }

        void Filter(Texture2D texture, RenderTarget2D target, string pass, Texture2D fine = null)
        {
            device.SetRenderTarget(target);
            device.Clear(Color.Transparent);
            effect.Parameters["uImageSize"].SetValue(new Vector2(texture.Width, texture.Height));
            batch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp,
                DepthStencilState.None, RasterizerState.CullNone);
            device.Textures[1] = fine;
            device.SamplerStates[1] = SamplerState.LinearClamp;
            effect.CurrentTechnique.Passes[pass].Apply();
            batch.Draw(texture, new Rectangle(0, 0, target.Width, target.Height), Color.White);
            batch.End();
        }
    }
}
