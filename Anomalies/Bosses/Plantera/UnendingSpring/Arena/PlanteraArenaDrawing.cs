using Anomalies.Assets.Effects;

namespace Anomalies.Bosses.Plantera.UnendingSpring;

public sealed class PlanteraArenaDrawing : ModSystem
{
    private Asset<Texture2D> _Wave;
    public override void Load()
    {
        if (!Main.dedServ)
            _Wave = Mod.Assets.Request<Texture2D>("Bosses/Plantera/UnendingSpring/Assets/PlanteraArenaWave");
    }

    public override void PostDrawTiles()
    {
        if (!AnomalyClientConfig.Instance.EnableShaders || _Wave is not { IsLoaded: true } ||
            UnendingSpringEffects.UnendingSpringArenaEdge is not MiscShaderContainer shader)
            return;
        foreach (Projectile p in Main.ActiveProjectiles)
        {
            if (p.ModProjectile is not PlanteraArena arena || arena.Opacity <= 0f)
                continue;
            GraphicsDevice device = Main.instance.GraphicsDevice;
            Texture previousTexture = device.Textures[1];
            SamplerState previousSampler = device.SamplerStates[1];
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap,
                DepthStencilState.None, Main.Rasterizer, null, Matrix.Identity);
            shader.Data.UseImage1(_Wave)
                .SetCustomParameter("uScreenPosition", Main.screenPosition)
                .SetCustomParameter("uInverseViewMatrix", Matrix.Invert(Main.GameViewMatrix.TransformationMatrix))
                .SetCustomParameter("uCenter", p.Center)
                .SetCustomParameter("uRadius", arena.VisibleRadius)
                .SetCustomParameter("uFlowTime", arena.FlowTime)
                .UseColor(new Color(255, 110, 240)).UseOpacity(arena.Opacity).Apply();
            device.SamplerStates[1] = SamplerState.LinearWrap;
            Main.spriteBatch.Draw(TOTextures.FillerTexture, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), new Color(255, 85, 255));
            Main.spriteBatch.End();
            device.Textures[1] = previousTexture;
            device.SamplerStates[1] = previousSampler;
        }
    }
}
