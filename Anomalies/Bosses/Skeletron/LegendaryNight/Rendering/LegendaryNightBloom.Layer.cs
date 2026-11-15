using Transoceanic.Graphics.Rendering;
using Transoceanic.Runtime.Publicizing.Wrappers;

namespace Anomalies.Bosses.Skeletron.LegendaryNight;

internal static partial class LegendaryNightBloom
{
    /// <summary>
    /// 使用调用者持有的 Effect 处理图层；不取得 Effect 的所有权。null 表示直接执行回调。
    /// 调用前 SpriteBatch 必须未开始，回调自行 Begin/End；异常时关闭遗留批次并恢复设备状态。
    /// 须在图形主线程调用，不允许在回调内重建设备或释放正在使用的资源。
    /// </summary>
    public static void Draw(SpriteBatch spriteBatch, Effect effect, in LegendaryNightBloomSettings settings, Action drawLayer)
    {
        ArgumentNullException.ThrowIfNull(drawLayer);
        Draw(spriteBatch, effect, settings, new DelegateLayer(drawLayer));
    }

    /// <summary>
    /// 使用值类型图层避免逐帧闭包。Effect 为 null 时不租用目标，仍恢复设备状态。
    /// 按当前视口大小捕获并合成回原视口，支持非零视口原点；零尺寸视口不执行图层。
    /// 原 RT 的保留策略会持久设为 PreserveContents。恢复目标、视口、裁剪、混合/深度/光栅状态及纹理/采样槽 0、1；
    /// 其他槽、顶点和索引绑定由调用者负责。不得在回调内更改目标绑定。
    /// </summary>
    public static void Draw<TLayer>(SpriteBatch spriteBatch, Effect effect, in LegendaryNightBloomSettings settings, TLayer layer)
        where TLayer : struct, IBloomLayer
    {
        ValidateBatch(spriteBatch);
        GraphicsDevice device = spriteBatch.GraphicsDevice;
        if (effect is not null)
            ValidateResource(device, effect, nameof(effect));
        Viewport viewport = device.Viewport;
        if (viewport.Width <= 0 || viewport.Height <= 0)
            return;
        Rectangle scissor = device.ScissorRectangle;
        RenderTargetBinding[] previous = device.GetRenderTargets();
        RenderTargetUsage backbufferUsage = device.PresentationParameters.RenderTargetUsage;
        BlendState blend = device.BlendState;
        DepthStencilState depth = device.DepthStencilState;
        RasterizerState rasterizer = device.RasterizerState;
        SamplerState sampler = device.SamplerStates[0];
        Texture texture = device.Textures[0];
        SamplerState auxiliarySampler = device.SamplerStates[1];
        Texture auxiliaryTexture = device.Textures[1];
        try
        {
            using SpriteBatchScope batchScope = new(spriteBatch, false);
            if (effect is null)
            {
                layer.Draw(spriteBatch);
                if (new SpriteBatch_Publicizer(spriteBatch).beginCalled)
                    throw new InvalidOperationException("Bloom layers must end their SpriteBatch.");
                return;
            }
            if (settings.PyramidLevels != 0)
            {
                DrawPyramid(spriteBatch, effect, settings, layer, previous, viewport);
                return;
            }
            RenderTargetDescriptor descriptor = RenderTargetDescriptor.Default with { Format = SurfaceFormat.HalfVector4 };
            using RenderTargetLease source = RenderTargetPool.Shared.Rent(device, viewport.Width, viewport.Height, descriptor);
            using RenderTargetLease nearScratch = RenderTargetPool.Shared.Rent(device, Math.Max(1, viewport.Width / 2), Math.Max(1, viewport.Height / 2), descriptor);
            using RenderTargetLease near = RenderTargetPool.Shared.Rent(device, nearScratch.Target.Width, nearScratch.Target.Height, descriptor);
            using RenderTargetLease farScratch = settings.WideHalo
                ? RenderTargetPool.Shared.Rent(device, Math.Max(1, viewport.Width / 4), Math.Max(1, viewport.Height / 4), descriptor) : null;
            using RenderTargetLease far = settings.WideHalo
                ? RenderTargetPool.Shared.Rent(device, farScratch.Target.Width, farScratch.Target.Height, descriptor) : null;
            RenderTargetPreserver.PreserveBindings(previous);
            if (previous.Length == 0)
                device.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;
            try
            {
                device.SetRenderTarget(source.Target);
                device.Clear(Color.Transparent);
                layer.Draw(spriteBatch);
                if (new SpriteBatch_Publicizer(spriteBatch).beginCalled)
                    throw new InvalidOperationException("Bloom layers must end their SpriteBatch.");
                BlurEmission(spriteBatch, effect, source.Target, nearScratch.Target, near.Target, settings.NearRadius, settings);
                if (settings.WideHalo)
                    BlurEmission(spriteBatch, effect, source.Target, farScratch.Target, far.Target, settings.FarRadius, settings);
                device.SetRenderTargets(previous);
                device.Viewport = viewport;
                device.ScissorRectangle = scissor;
                Composite(spriteBatch, effect, source.Target, near.Target, far?.Target,
                    new Rectangle(0, 0, viewport.Width, viewport.Height), settings);
            }
            finally
            {
                try
                {
                    if (new SpriteBatch_Publicizer(spriteBatch).beginCalled)
                        spriteBatch.End();
                }
                finally
                {
                    // 解除绑定后才离开租约作用域，包含回调与滤波抛错的路径。
                    device.SetRenderTargets(previous);
                }
            }
        }
        finally
        {
            // 不无条件重绑原目标，避免 DiscardContents 被再次触发。
            device.Viewport = viewport;
            device.ScissorRectangle = scissor;
            device.PresentationParameters.RenderTargetUsage = backbufferUsage;
            device.BlendState = blend;
            device.DepthStencilState = depth;
            device.RasterizerState = rasterizer;
            device.SamplerStates[0] = sampler;
            device.Textures[0] = texture;
            device.SamplerStates[1] = auxiliarySampler;
            device.Textures[1] = auxiliaryTexture;
        }
    }

    private readonly struct DelegateLayer(Action drawLayer) : IBloomLayer
    {
        public void Draw(SpriteBatch spriteBatch) => drawLayer();
    }
}
