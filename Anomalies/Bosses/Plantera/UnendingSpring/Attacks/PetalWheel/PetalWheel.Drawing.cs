namespace Anomalies.Bosses.Plantera.UnendingSpring;

public sealed partial class PetalWheel
{
    public override bool PreDraw(ref Color lightColor)
    {
        if (Age < 0 || _Guides == null) return false;
        Vector2 origin = Projectile.Center - Main.screenPosition;
        for (int ring = 0; Age < Duration && ring < Rings; ring++)
        {
            float radius = RingRadius(ring);
            if (radius <= 0f) continue;
            Color color = Tint((ring % 2 == 0) == PinkFirst);
            Vector2 a = origin + new Vector2(radius, 0f);
            for (int i = 1; i <= 96; i++)
            {
                Vector2 b = origin + (i * MathHelper.TwoPi / 96).ToRotationVector2() * radius;
                if (Age >= LaserStart)
                {
                    DashVisuals.Line(a, b, color, LaserWidth);
                    DashVisuals.Line(a, b, Color.Lerp(color, Color.White, 0.65f), 3f);
                }
                else DashVisuals.Line(a, b, color * 0.8f, 2.5f);
                a = b;
            }
        }
        if (Age < LaserStart) return false;
        if (Age < SpinStart) DrawWarningPetals(origin);
        else if (Age < HoldStart) DrawFlowerLines(origin);
        if (Age >= FlowerStart)
        {
            for (int i = 0; i < _Flowers.Length; i++)
                if (InRange(i))
                    Main.EntitySpriteDraw(Projectile.Texture, origin + _Positions[i], null,
                        Tint(_Flowers[i].Pink), (Age >= _SpreadStart ? _SpreadDirections[i] : _Positions[i]).ToRotation() + MathHelper.PiOver2,
                        new Vector2(35f), 0.5f, SpriteEffects.None);
        }
        return false;
    }
    private void DrawWarningPetals(Vector2 origin)
    {
        float length = GuideSteps * Math.Clamp((Age - LaserStart + 1) / 60f, 0f, 1f);
        for (int petal = 0; petal < Petals; petal++)
            for (int side = 0; side < 2; side++)
                for (int i = 0; i < length; i++)
                {
                    Vector2 a = _Guides[petal, side, i];
                    Vector2 b = Vector2.Lerp(a, _Guides[petal, side, i + 1], Math.Min(length - i, 1f));
                    Color color = Color.Lerp(DashVisuals.Pink, DashVisuals.Green, (i + 0.5f) / GuideSteps);
                    DashVisuals.Line(origin + a, origin + b, color * 0.72f, 2.5f);
                }
    }
    private void DrawFlowerLines(Vector2 origin)
    {
        for (int petal = 0; petal < Petals; petal++)
            for (int parity = 0; parity < 2; parity++)
            {
                int first = petal * FlowersPerPetal + parity;
                bool pink = _Flowers[first].Pink;
                Color color = Tint(pink) * 0.65f;
                Vector2[] path = _FlowerLines[petal * 2 + parity];
                Vector2 a = origin + Rotate(path[^1], pink);
                foreach (Vector2 point in path)
                {
                    Vector2 b = origin + Rotate(point, pink);
                    DashVisuals.Line(a, b, color, 2.5f);
                    a = b;
                }
            }
    }
}
