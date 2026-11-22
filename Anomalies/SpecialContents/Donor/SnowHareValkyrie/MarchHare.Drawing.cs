namespace Anomalies.SpecialContents.Donor.SnowHareValkyrie;

public sealed partial class MarchHare
{
    internal const float WorldScale = 0.8f;
    internal const float MovementScale = 0.625f;
    // 按同屏带叶胡萝卜的全长校准兔形。
    private const float SpriteScale = 1f;
    private const float BodyRadius = 25f * SpriteScale;
    private const int EarStart = 5;
    private const int FrameSize = 64;

    private static int BodyStart(bool red) => red ? 2 : 1;

    // 原作 Additive (Soft) 的源色为预乘 RGB，黑色边缘不遮挡背景。
    internal static BlendState CreateLightBlend() => new()
    {
        ColorSourceBlend = Blend.One,
        ColorDestinationBlend = Blend.InverseSourceColor,
        ColorWriteChannels = ColorWriteChannels.Red | ColorWriteChannels.Green | ColorWriteChannels.Blue
    };

    private static (Vector2 Center, float Rotation) EarPose(Vector2 center, float heading, int age, int side)
    {
        float steps = Math.Clamp(age - EarStart, 0, 30);
        float distance = 2f * steps - steps * (steps + 1f) / 30f;
        float rotation = heading + side * MathHelper.ToRadians(22.5f);
        Vector2 backward = new(MathF.Sin(rotation), -MathF.Cos(rotation));
        return (center + backward * (distance * SpriteScale), rotation);
    }

    internal static void DrawHare(SpriteBatch batch, Texture2D atlas, Vector2 center, float heading, int age, bool red, float opacity = 1f)
    {
        if (age < BodyStart(red))
            return;

        int column = red ? FrameSize : 0;
        Color tint = (red ? new Color(255, 230, 230) : Color.White) * opacity;
        Vector2 origin = new(FrameSize / 2f);
        Rectangle body = new(column, 0, FrameSize, FrameSize);
        batch.Draw(atlas, center, body, tint, heading, origin, SpriteScale, SpriteEffects.None, 0f);
        if (age < EarStart)
            return;
        Rectangle ear = new(column, FrameSize, FrameSize, FrameSize);
        for (int side = -1; side <= 1; side += 2)
        {
            (Vector2 position, float rotation) = EarPose(center, heading, age, side);
            batch.Draw(atlas, position, ear, tint, rotation, origin, SpriteScale, SpriteEffects.None, 0f);
        }
    }

    internal static void DrawMist(SpriteBatch batch, Texture2D atlas, Vector2 center, int age, bool red, bool ears)
    {
        // 原作雾化图约 72 像素；弹身组 22 的倍率 1.5，米弹组 4 的倍率 1。
        float size = (red ? 73f : 72f) / FrameSize;
        Vector2 scale = new(size, 72f / FrameSize);
        scale *= (1.5f - 0.025f * age) * (ears ? 1f : 1.5f) * SpriteScale;
        Rectangle frame = new(red ? FrameSize : 0, 2 * FrameSize, FrameSize, FrameSize);
        batch.Draw(atlas, center, frame, Color.White, 0f, new Vector2(FrameSize / 2f), scale, SpriteEffects.None, 0f);
    }
}
