using Transoceanic.Graphics.Rendering;
using Transoceanic.Runtime.Publicizing.Wrappers;

namespace Anomalies.Bosses.Skeletron.LegendaryNight;

internal static partial class LegendaryNightBloom
{
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
            DrawPyramid(spriteBatch, effect, settings, layer, previous, viewport);
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

}
