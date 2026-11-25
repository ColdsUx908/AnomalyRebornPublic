using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace Anomalies.Bosses.Plantera.UnendingSpring;

public sealed class BeamRenderer : ModSystem
{
    private static readonly Dictionary<string, Asset<Texture2D>> _Textures = new();
    private static BasicEffect _Effect;
    private static readonly VertexPositionColorTexture[] _Vertices = new VertexPositionColorTexture[184];

    internal static Texture2D Image(string name)
    {
        if (!_Textures.TryGetValue(name, out Asset<Texture2D> asset))
            _Textures[name] = asset = ModContent.Request<Texture2D>("Anomalies/Bosses/Plantera/UnendingSpring/Assets/" + name,
                AssetRequestMode.ImmediateLoad);
        return asset.Value;
    }

    internal static void Draw(List<Vector2> points, Vector2 center, Color tint, float side = 1f)
    {
        if (points.Count < 2) return;
        GraphicsDevice device = Main.graphics.GraphicsDevice;
        _Effect ??= new BasicEffect(device) { TextureEnabled = true, VertexColorEnabled = true };
        _Effect.World = Main.GameViewMatrix.TransformationMatrix;
        _Effect.View = Matrix.Identity;
        _Effect.Projection = Matrix.CreateOrthographicOffCenter(0, device.Viewport.Width,
            device.Viewport.Height, 0, 0, 1);
        _Effect.Texture = Image("GrayBeam");
        device.BlendState = BlendState.AlphaBlend;
        device.RasterizerState = RasterizerState.CullNone;
        device.DepthStencilState = DepthStencilState.None;
        device.SamplerStates[0] = SamplerState.LinearClamp;
        float distance = 0;
        for (int i = 0; i < points.Count; i++)
        {
            if (i > 0) distance += Vector2.Distance(points[i], points[i - 1]);
            Vector2 tangent = points[Math.Min(i + 1, points.Count - 1)] - points[Math.Max(i - 1, 0)];
            Vector2 normal = new Vector2(-tangent.Y, tangent.X).SafeNormalize(Vector2.UnitX)
                * 14f;
            Vector2 p = center + points[i] * side;
            normal *= side;
            float v = MathHelper.Clamp(distance / 120f, 0, 1);
            _Vertices[2 * i] = new(new Vector3(p - normal, 0), tint, new Vector2(0, v));
            _Vertices[2 * i + 1] = new(new Vector3(p + normal, 0), tint, new Vector2(1, v));
        }
        foreach (EffectPass pass in _Effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            device.DrawUserPrimitives(PrimitiveType.TriangleStrip, _Vertices, 0, 2 * points.Count - 2);
        }
    }

    public override void Unload()
    {
        _Textures.Clear();
        BasicEffect oldEffect = _Effect;
        _Effect = null;
        if (oldEffect != null) Main.QueueMainThreadAction(oldEffect.Dispose);
    }
}