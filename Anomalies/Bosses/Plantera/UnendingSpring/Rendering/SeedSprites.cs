using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace Anomalies.Bosses.Plantera.UnendingSpring;

public sealed class SeedSprites : ModSystem
{
    private static readonly Dictionary<(int type, bool pink, Color? outlineOnly, int border, Color? tint), Texture2D> _Cache = new();

    internal static Color EdgeColor(bool pink) => pink ? new Color(255, 142, 193) : new Color(102, 244, 70);

    internal static void DrawTrail(Projectile projectile, Color color, float opacity = 1f, float width = 4f,
        float maxStep = 32f, bool connectHead = false)
    {
        Vector2[] path = projectile.oldPos;
        Vector2 offset = projectile.Size / 2f - Main.screenPosition;
        for (int i = path.Length - 1; i >= (connectHead ? 0 : 1); i--)
        {
            Vector2 next = i == 0 ? projectile.position : path[i - 1];
            if (path[i] == Vector2.Zero || next == Vector2.Zero) continue;
            Vector2 segment = next - path[i];
            float length = segment.Length();
            if (length < 0.01f || length > maxStep) continue;
            float strength = connectHead ? 1f - i / (float)path.Length : 1f - (i - 1f) / (path.Length - 1);
            Main.EntitySpriteDraw(TextureAssets.MagicPixel.Value, path[i] + offset, new Rectangle(0, 0, 1, 1),
                color * (0.65f * strength * opacity), segment.ToRotation(), new Vector2(0f, 0.5f),
                new Vector2(length + 0.5f, width * strength * projectile.scale), SpriteEffects.None);
        }
    }

    internal static void Draw(Projectile projectile, bool pink, Color lightColor, float opacity = 1f,
        Color? outlineOnly = null, float? drawScale = null, int border = 1, int? spriteType = null)
    {
        int type = spriteType ?? projectile.type;
        Texture2D sprite = Get(type, pink, outlineOnly, border);
        Rectangle frame = sprite.Frame(1, Main.projFrames[type], 0, projectile.frame);
        Main.EntitySpriteDraw(sprite, projectile.Center - Main.screenPosition, frame,
            Color.Lerp(lightColor, Color.White, 0.8f) * opacity, projectile.rotation,
            frame.Size() / 2f, drawScale ?? projectile.scale, SpriteEffects.None);
    }

    internal static void DrawSpellShot(int type, Color tint, int age, Vector2 position, float rotation,
        float height, float opacity, float verticalScale = 1f)
    {
        int border = type == ProjectileID.ThornBall ? 3 : 1;
        Texture2D sprite = Get(type, false, null, border, tint);
        int frames = Main.projFrames[type];
        Rectangle frame = sprite.Frame(1, frames, 0, age / 6 % frames);
        // Extra outline pixels extend outward without shrinking the ball inside.
        float originalHeight = frame.Height - 2 * (border - 1);
        Vector2 scale = new Vector2(1f, System.MathF.Abs(verticalScale)) * (height / originalHeight);
        Main.EntitySpriteDraw(sprite, position, frame, Color.White * opacity, rotation, frame.Size() / 2f,
            scale, verticalScale < 0f ? SpriteEffects.FlipVertically : SpriteEffects.None);
    }

    private static Texture2D Get(int type, bool pink, Color? outlineOnly, int border = 1, Color? tint = null)
    {
        (int, bool, Color?, int, Color?) key = (type, pink, outlineOnly, border, tint);
        if (_Cache.TryGetValue(key, out Texture2D sprite)) return sprite;
        if (type < Terraria.ID.ProjectileID.Count) Main.instance.LoadProjectile(type);
        Texture2D source = TextureAssets.Projectile[type].Value;
        int width = source.Width, frames = Main.projFrames[type], height = source.Height / frames;
        Color[] original = new Color[width * source.Height];
        source.GetData(original);
        Color[] pixels = ColorizeFrames(original, width, height, frames, pink, outlineOnly, border, tint);
        sprite = new Texture2D(source.GraphicsDevice, width + 2 * border, (height + 2 * border) * frames);
        sprite.SetData(pixels);
        _Cache.Add(key, sprite);
        return sprite;
    }

    private static Color[] ColorizeFrames(Color[] original, int width, int height, int frames, bool pink,
        Color? outlineOnly, int border, Color? tint)
    {
        Color edge = outlineOnly ?? tint ?? EdgeColor(pink);
        Color shadow = pink ? new Color(111, 34, 78) : new Color(29, 69, 32);
        Color highlight = pink ? new Color(255, 207, 232) : new Color(209, 255, 152);
        if (tint is Color pigment)
        {
            shadow = Color.Lerp(Color.Black, pigment, 0.32f);
            highlight = Color.Lerp(pigment, Color.White, 0.48f);
        }
        // Pad each animation frame separately, so the outline cannot bleed into its neighbour.
        int stride = width + 2 * border, frameHeight = height + 2 * border;
        Color[] pixels = new Color[stride * frameHeight * frames];
        for (int frame = 0; frame < frames; frame++)
        {
            int src = frame * width * height, dst = frame * frameHeight * stride;
            bool Opaque(int x, int y) => (uint)x < width && (uint)y < height && original[src + y * width + x].A > 0;
            bool AtEdge(int x, int y)
            {
                for (int dy = -border; dy <= border; dy++)
                    for (int dx = -border; dx <= border; dx++)
                        if (dx * dx + dy * dy <= border * border && Opaque(x + dx, y + dy)) return true;
                return false;
            }
            for (int y = -border; y < height + border; y++)
                for (int x = -border; x < width + border; x++)
                {
                    int i = dst + (y + border) * stride + x + border;
                    if (Opaque(x, y))
                    {
                        Color pixel = original[src + y * width + x];
                        // Source RGB is premultiplied. Preserve its shading and fractional coverage.
                        float value = (0.299f * pixel.R + 0.587f * pixel.G + 0.114f * pixel.B) / pixel.A;
                        pixels[i] = outlineOnly.HasValue ? pixel :
                            Color.Lerp(shadow, highlight, MathHelper.Clamp(value, 0f, 1f)) * (pixel.A / 255f);
                    }
                    else if (AtEdge(x, y))
                        pixels[i] = edge;
                }
        }
        return pixels;
    }

    public override void Unload()
    {
        foreach (Texture2D sprite in _Cache.Values) Main.QueueMainThreadAction(sprite.Dispose);
        _Cache.Clear();
    }
}