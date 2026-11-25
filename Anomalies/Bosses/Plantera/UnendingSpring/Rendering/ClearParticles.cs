using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace Anomalies.Bosses.Plantera.UnendingSpring;

internal static class ClearParticles
{
    internal static void Burst(Vector2 at, Color color, int count = 5)
    {
        if (Main.netMode == NetmodeID.Server) return;
        Vector2 screen = at - Main.Camera.ScaledPosition;
        if (screen.X < -64f || screen.Y < -64f || screen.X > Main.Camera.ScaledSize.X + 64f ||
            screen.Y > Main.Camera.ScaledSize.Y + 64f) return;
        float start = Main.rand.NextFloat(MathHelper.TwoPi);
        for (int i = 0; i < count; i++)
        {
            Vector2 v = (start + MathHelper.TwoPi * i / count).ToRotationVector2() * Main.rand.NextFloat(1.2f, 3.8f);
            Dust.NewDustPerfectAction(at, DustID.RainbowMk2, dust =>
            {
                dust.velocity = v;
                dust.color = color;
                dust.scale = 1.1f;
                dust.noGravity = true;
            });
        }
    }
}