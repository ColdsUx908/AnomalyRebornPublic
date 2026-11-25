namespace Anomalies.Bosses.Plantera.UnendingSpring;

public sealed partial class PetalWheel
{
    private static Vector2 PetalPoint(float axis, float u, int side)
    {
        float bend = u == 1f ? 0f : MathF.Sin(MathHelper.Pi * u);
        // Open the flanks near the root so even two flowers on the same rotating
        // layer have space for their complete sprites, including the corners.
        float angle = axis + side * 0.34f * Pitch * MathF.Cbrt(bend);
        // A nearly uniform radial spacing leaves enough room between counter-rotating layers.
        return angle.ToRotationVector2() * MathHelper.Lerp(Outer, PlanteraArena.Radius, u + 0.02f * bend);
    }

    private void Prepare()
    {
        if (_Flowers != null) return;
        _Flowers = new Flower[Petals * FlowersPerPetal];
        _Positions = new Vector2[_Flowers.Length];
        _Guides = new Vector2[Petals, 2, GuideSteps + 1];
        _FlowerLines = new Vector2[Petals * 2][];
        float orientation = (Seed >> 2) / 2097152f * MathHelper.TwoPi;
        int index = 0;
        for (int petal = 0; petal < Petals; petal++)
        {
            float axis = orientation + petal * Pitch;
            for (int side = 0; side < 2; side++)
            {
                for (int step = 0; step <= GuideSteps; step++)
                    _Guides[petal, side, step] = PetalPoint(axis, step / (float)GuideSteps, side * 2 - 1);
            }
            // Traverse the closed outline once, assigning a single color at each station.
            for (int slot = 0; slot < Stations; slot++)
            {
                int side = slot <= Layers ? 0 : 1;
                int level = Math.Min(slot, Stations - slot);
                Vector2 point = _Guides[petal, side, level * GuideSteps / Layers];
                // Alternating radial layers are shared by all petals. Opposite
                // colors can cross the same angle without ever sharing a radius.
                bool pink = (level % 2 == 0) == PinkFirst;
                _Flowers[index++] = new Flower(point, pink);
            }
            Flower tip = _Flowers[petal * FlowersPerPetal + Layers];
            _Flowers[index++] = tip with { Pink = !tip.Pink };
            for (int parity = 0; parity < 2; parity++)
                _FlowerLines[petal * 2 + parity] = BuildFlowerLine(petal, parity);
        }
        // The synchronized wheel seed reproduces each straight flight path on late join.
        PrepareSpread();
    }

    private void PrepareSpread()
    {
        Vector2[] launch = new Vector2[_Flowers.Length];
        _SpreadDirections = new Vector2[_Flowers.Length];
        float finalTurn = Direction * MathHelper.ToRadians(AlignedDegrees);
        for (int i = 0; i < _Flowers.Length; i++)
        {
            Flower flower = _Flowers[i];
            launch[i] = flower.Offset.RotatedBy(flower.Pink ? finalTurn : -finalTurn);
            _SpreadDirections[i] = Vector2.Normalize(launch[i]);
        }
        Random random = new Random(Seed);
        for (int i = 0; i < _Flowers.Length; i++)
        {
            Vector2 radial = _SpreadDirections[i];
            float deviation = MathHelper.Lerp(-MaxSpreadAngle, MaxSpreadAngle, random.NextSingle());
            // Straight radial flight is already clear. Reduce a sampled deviation
            // only if the two swept sprites would cross while both are still in range.
            for (int attempt = 0; attempt < 16; attempt++, deviation *= 0.5f)
            {
                Vector2 candidate = radial.RotatedBy(deviation);
                if (!ClearFlight(i, candidate, launch)) continue;
                _SpreadDirections[i] = candidate;
                break;
            }
        }
    }

    private bool ClearFlight(int index, Vector2 candidate, Vector2[] launch)
    {
        const float ClearWidth = 50f;
        static bool Tip(int n) => n % FlowersPerPetal == Layers || n % FlowersPerPetal == Stations;
        bool isTip = Tip(index);
        float maxTravel = (MaxRadius - Outer) / MathF.Cos(MaxSpreadAngle) + SpreadSpeed;
        for (int j = 0; j < launch.Length; j++)
        {
            if (index == j) continue;
            if (isTip && Tip(j)) continue;
            Vector2 offset = launch[index] - launch[j];
            Vector2 relative = candidate - _SpreadDirections[j];
            float rate = relative.LengthSquared();
            if (rate < 1e-12f) continue;
            float closest = Math.Clamp(-Vector2.Dot(offset, relative) / rate, 0f, maxTravel);
            if ((offset + relative * closest).LengthSquared() < ClearWidth * ClearWidth) return false;
        }
        return true;
    }

    private Vector2[] BuildFlowerLine(int petal, int parity)
    {
        List<Vector2> nodes = new List<Vector2>(Layers + 2);
        for (int slot = 0; slot < Stations; slot++)
        {
            // Both colored outlines meet at the root and at the actual arena tip.
            // Each formation carries its own flower at the tip.
            if (slot % 2 == parity || slot == 0 || slot == Layers)
                nodes.Add(_Flowers[petal * FlowersPerPetal + slot].Offset);
        }
        const int Subdivisions = 6;
        Vector2[] path = new Vector2[nodes.Count * Subdivisions];
        for (int i = 0; i < nodes.Count; i++)
            for (int step = 0; step < Subdivisions; step++)
            {
                Vector2 point = Vector2.CatmullRom(nodes[(i + nodes.Count - 1) % nodes.Count], nodes[i],
                    nodes[(i + 1) % nodes.Count], nodes[(i + 2) % nodes.Count], step / (float)Subdivisions);
                if (point.LengthSquared() > PlanteraArena.Radius * PlanteraArena.Radius)
                    point = Vector2.Normalize(point) * PlanteraArena.Radius;
                path[i * Subdivisions + step] = point;
            }
        return path;
    }

    private Vector2 Rotate(Vector2 p, bool pink)
    {
        float sin = pink ? _Rotation.Y : -_Rotation.Y;
        return new Vector2(p.X * _Rotation.X - p.Y * sin, p.X * sin + p.Y * _Rotation.X);
    }

}
