using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace Anomalies.Bosses.Plantera.UnendingSpring;

public sealed class DashVisuals : ModSystem
{
    private static Texture2D _Sector, _Ray, _Orb;
    private static readonly Texture2D[] _OrbitWarnings = new Texture2D[2];
    internal static readonly Color Pink = SeedSprites.EdgeColor(true);
    internal static readonly Color Green = SeedSprites.EdgeColor(false);

    private static Texture2D Make(int width, int height, Func<int, int, Color> pixel)
    {
        Color[] colors = new Color[width * height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++) colors[y * width + x] = pixel(x, y);
        Texture2D result = new Texture2D(Main.instance.GraphicsDevice, width, height);
        result.SetData(colors);
        return result;
    }

    private static float RayOpening(int age, int pair)
    {
        const int SpreadTicks = 36;
        const int PairDelay = 4;
        float progress = (age - pair * PairDelay) / (SpreadTicks - 1f - 4 * PairDelay);
        return PlanteraUnending.HalfFanAngle * MathHelper.SmoothStep(0f, 1f, Math.Clamp(progress, 0f, 1f));
    }

    internal static void DrawTelegraph(Vector2 origin, float angle, int age)
    {
        const int Width = 512, Height = 256;
        float reach = DashFan.Range;
        float opening = RayOpening(age, 0);
        _Sector ??= Make(Width, Height, (x, y) =>
        {
            float u = (x + 0.5f) / Width, v = 2f * (y + 0.5f) / Height - 1f;
            float side = v * MathF.Tan(PlanteraUnending.HalfFanAngle);
            if (MathF.Abs(v) > u || u * u + side * side > 1f) return Color.Transparent;
            return Color.Lerp(Pink, Green, 0.5f + 0.5f * v / u);
        });
        _Ray ??= Make(256, 1, (x, y) => Color.Lerp(Pink, Green, x / 255f));
        Vector2 at = origin - Main.screenPosition;
        if (opening > 0f)
            Main.spriteBatch.Draw(_Sector, at, null, Color.White * 0.23f, angle,
                new Vector2(0f, Height / 2f), new Vector2(reach / Width,
                2f * reach * MathF.Tan(opening) / Height), SpriteEffects.None, 0f);
        for (int i = 0; i < 10; i++)
            Main.spriteBatch.Draw(_Ray, at, null, Color.White * 0.85f,
                angle + (i % 2 == 0 ? -1f : 1f) * RayOpening(age, i / 2), new Vector2(0f, 0.5f),
                new Vector2(reach / _Ray.Width, 2.8f), SpriteEffects.None, 0f);
    }

    internal static void DrawOrb(Vector2 screenPosition, int age)
    {
        _Orb ??= Make(64, 64, (x, y) =>
        {
            float radius = new Vector2(x - 31.5f, y - 31.5f).Length() / 32f;
            float alpha = Math.Clamp((1f - radius) * 2f, 0f, 1f);
            return Color.Lerp(Color.White, Green, Math.Clamp(radius * 1.7f, 0f, 1f)) * alpha;
        });
        Main.EntitySpriteDraw(_Orb, screenPosition, null, Color.White, 0f, new Vector2(32f),
            1f + 0.08f * MathF.Sin(age * 0.35f), SpriteEffects.None);
    }

    internal static void DrawOrbitWarning(Vector2 center, float axis, int parity, Color tint, float brightness)
    {
        const int Size = 512;
        _OrbitWarnings[parity] ??= Make(Size, 2 * Size, (x, y) =>
        {
            float u = (x + 0.5f) / Size, v = (y + 0.5f - Size) / Size;
            float r = MathF.Sqrt(u * u + v * v);
            float sideDistance = 0.8660254f * u - 0.5f * MathF.Abs(v);
            if (r >= 1f || sideDistance < 0f) return Color.Transparent;
            float band = r * ParadoxSector.Bands;
            if (!ParadoxSector.Selected((int)band, parity)) return Color.Transparent;
            float fraction = band - (int)band;
            bool edge = sideDistance < 1.2f / Size || Math.Min(fraction, 1f - fraction) < 0.019f;
            return Color.White * (edge ? 0.78f : 0.26f);
        });
        Main.EntitySpriteDraw(_OrbitWarnings[parity], center - Main.screenPosition, null, tint * brightness,
            axis, new Vector2(0, Size), ParadoxSector.Reach / Size, SpriteEffects.None);
    }

    internal static void Line(Vector2 a, Vector2 b, Color tint, float width)
    {
        Vector2 delta = b - a;
        float length = delta.Length();
        if (length < 0.001f) return;
        Main.EntitySpriteDraw(TextureAssets.MagicPixel.Value, a, new Rectangle(0, 0, 1, 1), tint,
            delta.ToRotation(), new Vector2(0f, 0.5f), new Vector2(length + 0.75f, width), SpriteEffects.None);
    }

    public override void Unload()
    {
        for (int i = 0; i < _OrbitWarnings.Length; i++)
        {
            Texture2D texture = _OrbitWarnings[i];
            if (texture != null) Main.QueueMainThreadAction(texture.Dispose);
            _OrbitWarnings[i] = null;
        }
        foreach (Texture2D texture in new[] { _Sector, _Ray, _Orb })
            if (texture != null) Main.QueueMainThreadAction(texture.Dispose);
        _Sector = _Ray = _Orb = null;
    }
}