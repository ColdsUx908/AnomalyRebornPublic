namespace Anomalies.Bosses.Plantera.UnendingSpring;

internal static class UnendingSpringPaths
{
    internal const float PetalTipRadius = 152f;
    internal const float DashSpeed = 36f;
    private const int RosePeriod = 180;

    private static float RoseAngle(float initialDegrees, int frame) =>
        initialDegrees + 6f * (frame % RosePeriod);

    private static Vector2 RosePoint(float angle) =>
        MathHelper.ToRadians(angle).ToRotationVector2() *
        (PetalTipRadius * MathF.Cos(MathHelper.ToRadians(angle * 4f / 3f)));

    internal static Vector2 NeedleOffset(float initialDegrees, int birthFrame, int age)
    {
        float angle = RoseAngle(initialDegrees, birthFrame);
        int movingFrames = Math.Max(0, age - 3);
        int acceleratingFrames = Math.Min(250, movingFrames);
        float distance = 2f * 1.03275f * (0.005f * acceleratingFrames * (acceleratingFrames + 1)
            + 2.5f * (movingFrames - acceleratingFrames));
        return RosePoint(angle) - MathHelper.ToRadians(angle).ToRotationVector2() * distance;
    }

    internal static void SampleRoseTrail(List<Vector2> points, float initialDegrees, int frame)
    {
        points.Clear();
        Vector2 point = RosePoint(RoseAngle(initialDegrees, frame));
        points.Add(point);
        float remaining = 120f;
        for (int offset = 1; offset <= Math.Min(frame, 90) && remaining > 0f; offset++)
        {
            Vector2 next = RosePoint(RoseAngle(initialDegrees, frame - offset));
            Vector2 delta = next - point;
            float length = delta.Length();
            if (length < 0.001f)
                continue;

            points.Add(point + delta * (Math.Min(length, remaining) / length));
            remaining -= length;
            point = next;
        }
    }

    internal static Vector2 LoopPoint(Vector2 axis, int steps, float progress, int branch)
    {
        float height = branch * 42f * MathF.Sin(MathHelper.TwoPi * progress);
        return axis * (progress * steps * DashSpeed) + new Vector2(-axis.Y, axis.X) * height;
    }

    internal static bool HitsTrail(List<Vector2> offsets, Vector2 origin, float side, float width, Rectangle target)
    {
        for (int i = 1; i < offsets.Count; i++)
        {
            Vector2 start = origin + side * offsets[i - 1];
            Vector2 end = origin + side * offsets[i];
            if (start != end && RotatedRectangle.FromLineSegment(start, end, width).Collides(target))
                return true;
        }
        return false;
    }
}