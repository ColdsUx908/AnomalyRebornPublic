namespace Anomalies.Bosses.EaterofWorlds;

public sealed partial class EaterofWorldsHead
{
    // ai[0]: owner/local head/previous/next; ai[1]: immutable global ordinal;
    // ai[2]: Phase + Behavior; ai[3]: LocalPhase. Extra slots below are encounter state.
    public const int TeleportInterval = 600;
    public const int FormationTimeout = 150;
    public const int PortalChargeDuration = 180;
    public const float PortalRetreatDistance = 1700f;
    public const float PortalSafeDistance = 1100f;
    public const float PortalExitDistance = 600f;
    public const float OutwardChargeSpeed = 34f;
    public const float OutwardChargeDistance = 1200f;
    public const float SplitSpread = MathHelper.Pi / 3.6f;
    public const int HeadContactDamage = 108;
    public const int BodyContactDamage = 66;
    public const int TailContactDamage = 54;

    public int CycleIndex
    {
        get => AnomalyNPC.AnomalyAI32[4].I;
        set { AnomalyNPC.AnomalyAI32[4].I = value; AnomalyNPC.AIChanged32[4] = true; }
    }
    public int AttackSide => CycleIndex % 2 == 0 ? 1 : -1;
    public Vector2 AttackCenter
    {
        get => AnomalyNPC.AnomalyAI64[1].GetValue<Vector2>();
        set { AnomalyNPC.AnomalyAI64[1].SetValue(value); AnomalyNPC.AIChanged64[1] = true; }
    }
    // Locked entrance/exit direction for the portal transition.
    public Vector2 LaunchDirection
    {
        get => AnomalyNPC.AnomalyAI64[3].GetValue<Vector2>();
        set { AnomalyNPC.AnomalyAI64[3].SetValue(value); AnomalyNPC.AIChanged64[3] = true; }
    }

    public bool ContactSuppressed => CurrentPhase == Phase.Initialize || CurrentBehavior == Behavior.Despawn
        || CurrentBehavior is Behavior.Phase1_Split or Behavior.Phase1_Combine && CurrentLocalPhase < 3;

    private void ChangeStep(int step)
    {
        if (!TONetworkEnvironment.NotClient)
            return;
        CurrentLocalPhase = step;
        Timer1 = 0;
        NPC.netUpdate = true;
    }

    private void ChangeAttack(Behavior behavior)
    {
        if (!TONetworkEnvironment.NotClient)
            return;
        CurrentBehavior = behavior;
        CurrentLocalPhase = 0;
        Timer1 = Timer2 = 0;
        AttackCenter = Target.Center;
        NPC.netUpdate = true;
    }

    private static void MoveTowards(NPC head, Vector2 point, float speed, float acceleration = 0.65f)
    {
        Vector2 delta = point - head.Center;
        Vector2 desired = delta.SafeNormalize(Vector2.UnitY) * Math.Min(speed, delta.Length() * 0.12f);
        head.velocity = Vector2.Lerp(head.velocity, desired, acceleration / Math.Max(speed, 1f));
    }

    private void PursueTarget(NPC head)
    {
        // Calamity EaterOfWorldsAI's inTiles steering, always enabled: no terrain scan or falling branch.
        // Shared health replaces its remaining-body ratio; Ultra selects Death movement parameters.
        float lifeRatio = NPC.LifeRatio;
        bool death = Ultra;
        float segmentVelocity = 12f + (death ? 4.8f : 2.4f) * (1f - lifeRatio);
        float segmentAcceleration = 0.15f + (death ? 0.06f : 0.03f) * (1f - lifeRatio);
        int speedStage = death && lifeRatio < 0.05f ? 4
            : death && lifeRatio < 0.1f ? 3
            : lifeRatio < (death ? 0.5f : 0.2f) ? 2
            : lifeRatio < 0.4f || death ? 1 : 0;
        segmentVelocity += speedStage * 0.6f;
        segmentAcceleration += speedStage * 0.03f;
        if (death)
        {
            segmentVelocity += head.justHit ? 8f : 2f;
            segmentAcceleration += head.justHit ? 0.16f : 0.04f;
        }
        if (Main.getGoodWorld)
        {
            segmentVelocity += 4f;
            segmentAcceleration += 0.05f;
        }
        Vector2 destination = Target.Center + (speedStage == 4 ? Target.velocity * 20f : Vector2.Zero);
        float targetPosX = (int)(destination.X / 16f) * 16 - (int)(head.Center.X / 16f) * 16;
        float targetPosY = (int)(destination.Y / 16f) * 16 - (int)(head.Center.Y / 16f) * 16;
        float absoluteTargetX = Math.Abs(targetPosX);
        float absoluteTargetY = Math.Abs(targetPosY);
        float targetDistance = MathF.Sqrt(targetPosX * targetPosX + targetPosY * targetPosY);
        // Preserve momentum when the snapped positions coincide instead of dividing by zero.
        if (targetDistance <= 0f)
            return;
        targetPosX *= segmentVelocity / targetDistance;
        targetPosY *= segmentVelocity / targetDistance;
        if ((head.velocity.X > 0f && targetPosX > 0f) || (head.velocity.X < 0f && targetPosX < 0f) || (head.velocity.Y > 0f && targetPosY > 0f) || (head.velocity.Y < 0f && targetPosY < 0f))
        {
            if (head.velocity.X < targetPosX)
                head.velocity.X += segmentAcceleration;
            else if (head.velocity.X > targetPosX)
                head.velocity.X -= segmentAcceleration;
            if (head.velocity.Y < targetPosY)
                head.velocity.Y += segmentAcceleration;
            else if (head.velocity.Y > targetPosY)
                head.velocity.Y -= segmentAcceleration;

            if (Math.Abs(targetPosY) < segmentVelocity * 0.2 && ((head.velocity.X > 0f && targetPosX < 0f) || (head.velocity.X < 0f && targetPosX > 0f)))
            {
                if (head.velocity.Y > 0f)
                    head.velocity.Y += segmentAcceleration * 2f;
                else
                    head.velocity.Y -= segmentAcceleration * 2f;
            }

            if (Math.Abs(targetPosX) < segmentVelocity * 0.2 && ((head.velocity.Y > 0f && targetPosY < 0f) || (head.velocity.Y < 0f && targetPosY > 0f)))
            {
                if (head.velocity.X > 0f)
                    head.velocity.X += segmentAcceleration * 2f;
                else
                    head.velocity.X -= segmentAcceleration * 2f;
            }
        }
        else if (absoluteTargetX > absoluteTargetY)
        {
            if (head.velocity.X < targetPosX)
                head.velocity.X += segmentAcceleration * 1.1f;
            else if (head.velocity.X > targetPosX)
                head.velocity.X -= segmentAcceleration * 1.1f;

            if ((Math.Abs(head.velocity.X) + Math.Abs(head.velocity.Y)) < segmentVelocity * 0.5)
            {
                if (head.velocity.Y > 0f)
                    head.velocity.Y += segmentAcceleration;
                else
                    head.velocity.Y -= segmentAcceleration;
            }
        }
        else
        {
            if (head.velocity.Y < targetPosY)
                head.velocity.Y += segmentAcceleration * 1.1f;
            else if (head.velocity.Y > targetPosY)
                head.velocity.Y -= segmentAcceleration * 1.1f;

            if ((Math.Abs(head.velocity.X) + Math.Abs(head.velocity.Y)) < segmentVelocity * 0.5)
            {
                if (head.velocity.X > 0f)
                    head.velocity.X += segmentAcceleration;
                else
                    head.velocity.X -= segmentAcceleration;
            }
        }
    }

    internal Vector2 CoilLaunchDirection(int group, bool combine) =>
        (Target.Center - CoilingCenter).SafeNormalize(Vector2.UnitY).RotatedBy(combine ? 0f : (group - 1) * SplitSpread);

    internal static void SmoothHeadRotation(NPC head)
    {
        Vector2 facing = head.velocity;
        if (facing.LengthSquared() > 0.1f)
            head.rotation += MathHelper.WrapAngle(facing.ToRotation() + MathHelper.PiOver2 - head.rotation) * 0.2f;
    }

}
