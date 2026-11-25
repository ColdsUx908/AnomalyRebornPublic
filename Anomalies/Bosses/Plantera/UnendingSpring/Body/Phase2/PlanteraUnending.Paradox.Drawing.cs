namespace Anomalies.Bosses.Plantera.UnendingSpring;

public sealed partial class PlanteraUnending
{
    internal bool DrawParadoxVine(NPC boss, NPC hook)
    {
        int time = (int)Timer;
        if (CurrentBehavior != Behavior.ParadoxSector || time <= 44 || time >= ParadoxEnd) return false;
        int index = (int)hook.ai[3] - 1;
        if (index < 1) return false;
        int side = index == 1 ? -1 : 1;
        float extension = Extension(time);
        Texture2D vine = TextureAssets.Chain26.Value;
        Vector2 origin = new(vine.Width * 0.5f, 0f);
        Vector2 previous = boss.Center;
        int segments = Math.Max(1, (int)MathF.Ceiling(18f * extension));
        for (int i = 1; i <= segments; i++)
        {
            Vector2 next = i == segments ? hook.Center : boss.Center + ToWorld(VinePoint(side, extension * i / segments));
            Vector2 d = next - previous;
            Main.spriteBatch.Draw(vine, previous - Main.screenPosition, null, Color.White, d.ToRotation() - MathHelper.PiOver2,
                origin, new Vector2(1f, (d.Length() + 0.5f) / vine.Height), SpriteEffects.None, 0f);
            previous = next;
        }
        return true;
    }

}
