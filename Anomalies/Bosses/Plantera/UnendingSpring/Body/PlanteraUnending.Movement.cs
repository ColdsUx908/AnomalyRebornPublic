namespace Anomalies.Bosses.Plantera.UnendingSpring;

public sealed partial class PlanteraUnending
{
    // 原版 aiStyle 51 的逐轴转向。原作移除了钩爪重心限制和地形狂暴。
    private void UpdatePursuit(Player target)
    {
        bool retreat = target.dead || !target.active;
        float speed = NPC.life < NPC.lifeMax / 4 ? 7f : NPC.life < NPC.lifeMax / 2 ? 5f : 2.5f;
        float acceleration = NPC.life < NPC.lifeMax / 2 ? 0.05f : 0.025f;
        if (Main.expertMode)
        {
            speed = (speed + 1f) * 1.1f;
            acceleration = (acceleration + 0.01f) * 1.1f;
        }
        if (Main.getGoodWorld)
        {
            speed *= 1.15f;
            acceleration *= 1.15f;
        }
        if (retreat)
            speed += 8f;
        speed *= HasGreenLaser ? 1.1f * 0.9f * 0.85f : 1.1f;
        acceleration *= 1.1f;
        Vector2 offset = target.Center - NPC.Center;
        NPC.rotation = offset.ToRotation() + 1.57f;
        if (offset.LengthSquared() < speed * speed)
            return;

        Vector2 desired = offset.SafeNormalize(Vector2.Zero) * (retreat ? -speed : speed);
        NPC.velocity.X = SteerAxis(NPC.velocity.X, desired.X);
        NPC.velocity.Y = SteerAxis(NPC.velocity.Y, desired.Y);

        float SteerAxis(float velocity, float goal)
        {
            float step = Math.Sign(goal - velocity) * acceleration;
            velocity += step;
            if (step > 0f && velocity < 0f && goal > 0f || step < 0f && velocity > 0f && goal < 0f)
                velocity += 2f * step;
            return velocity;
        }
    }
}