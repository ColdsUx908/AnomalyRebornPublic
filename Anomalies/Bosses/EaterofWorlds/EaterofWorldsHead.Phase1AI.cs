namespace Anomalies.Bosses.EaterofWorlds;

public sealed partial class EaterofWorldsHead
{
    // Rotate the lead pair after each full charge/foldback cycle. Slot order is never a combat role.
    private int RegularRank(int ordinal)
    {
        int count = RegularHeadCount;
        int group = (ordinal - 1) / (TotalSegmentCount / count);
        return (group - CycleIndex / 2 % count + count) % count;
    }

    private void Phase1CombatAI(List<NPC> heads)
    {
        if (heads.Count == 0)
            return;
        Timer1++;
        switch (CurrentBehavior)
        {
            case Behavior.Phase1_Hunt:
                Hunt();
                break;
            case Behavior.Phase1_Charge:
                Charge();
                break;
            case Behavior.Phase1_Foldback:
                Foldback();
                break;
            case Behavior.Phase1_Split:
                SplitThroughPortal(heads);
                break;
        }
        if (TONetworkEnvironment.NotClient && Timer1 % 45 == 0)
        {
            foreach (NPC head in heads)
                head.netUpdate = true;
        }

        void Hunt()
        {
            foreach (NPC head in heads)
            {
                EaterofWorldsHead worm = GetInstance(head);
                int rank = RegularRank(worm.SegmentIndex);
                if (rank != 0)
                    Flank(head, rank);
                else if (CurrentLocalPhase == 0)
                    PursueTarget(head);
                else
                    head.velocity = worm.LaunchDirection * 18f;
            }
            if (!TONetworkEnvironment.NotClient)
                return;
            if (CurrentLocalPhase == 0 && Timer1 >= HuntDuration)
            {
                foreach (NPC head in heads)
                {
                    EaterofWorldsHead worm = GetInstance(head);
                    if (RegularRank(worm.SegmentIndex) != 0)
                        continue;
                    Vector2 direction = (Target.Center - head.Center).SafeNormalize(new Vector2(AttackSide, -0.4f));
                    direction.Y = Math.Min(direction.Y, -0.28f);
                    worm.SetRoute(head.Center, direction.SafeNormalize(-Vector2.UnitY));
                }
                ChangeStep(1);
            }
            else if (CurrentLocalPhase == 1 && Timer1 >= UppercutDuration)
                ChangeAttack(CycleIndex % 2 == 0 ? Behavior.Phase1_Charge : Behavior.Phase1_Foldback);
        }

        void Charge()
        {
            // 0: take position and aim. 1: all bearings are frozen; the two heads launch on alternating beats.
            if (CurrentLocalPhase == 1 && Timer1 == 1 && !Main.dedServ)
                SoundEngine.PlaySound(SoundID.Item8, AttackCenter);
            bool inPosition = true;
            foreach (NPC head in heads)
            {
                EaterofWorldsHead worm = GetInstance(head);
                int rank = RegularRank(worm.SegmentIndex);
                if (rank >= 2)
                {
                    Flank(head, rank);
                    continue;
                }
                if (CurrentLocalPhase == 0)
                {
                    float bearing = RegularHeadCount == 1 ? 0f : (rank == 0 ? -0.36f : 0.36f);
                    Vector2 approach = new Vector2(AttackSide, -0.42f).SafeNormalize(Vector2.UnitX).RotatedBy(bearing);
                    Vector2 station = Target.Center - approach * ApproachDistance;
                    float distance = head.Distance(station);
                    inPosition &= distance <= 40f;
                    FlyTo(head, station, Math.Max(31f, distance * 0.12f));
                    continue;
                }
                int age = Timer1 - LockDuration - rank * ChargeDuration;
                if (age <= 0)
                {
                    head.Center = worm.LaunchOrigin;
                    head.velocity = Vector2.Zero;
                    head.rotation = worm.LaunchDirection.ToRotation() + MathHelper.PiOver2;
                }
                else if (age <= ChargeDuration)
                    head.velocity = worm.LaunchDirection * LockedChargeSpeed(worm.LaunchOrigin);
                else
                    head.velocity *= 0.93f;
            }
            if (!TONetworkEnvironment.NotClient)
                return;
            if (CurrentLocalPhase == 0 && Timer1 >= AimDuration && (inPosition || Timer1 >= 90))
            {
                AttackCenter = Target.Center;
                foreach (NPC head in heads)
                {
                    EaterofWorldsHead worm = GetInstance(head);
                    if (RegularRank(worm.SegmentIndex) >= 2)
                        continue;
                    Vector2 direction = (AttackCenter - head.Center).SafeNormalize(new Vector2(AttackSide, 0f));
                    worm.SetRoute(head.Center, direction);
                    head.velocity = Vector2.Zero;
                }
                ChangeStep(1);
            }
            else if (CurrentLocalPhase == 1 && Timer1 >= LockDuration + Math.Min(2, RegularHeadCount) * ChargeDuration + 12)
                FinishAttack();
        }

        void Foldback()
        {
            // 0: approach the two ends of the lower pass. 1: pass under, turn in an arc, return on the upper lane.
            foreach (NPC head in heads)
            {
                int rank = RegularRank(GetInstance(head).SegmentIndex);
                if (rank >= 2)
                {
                    Flank(head, rank);
                    continue;
                }
                int age = CurrentLocalPhase == 0 ? 0 : Timer1;
                if (RegularHeadCount > 1 && rank == 1)
                    age = Math.Max(FoldPassDuration, age);
                if (RegularHeadCount > 1 && rank == 0 && age > FoldPassDuration)
                {
                    head.velocity = Vector2.Lerp(head.velocity, new Vector2(AttackSide * 13f, 3f), 0.15f);
                    continue;
                }
                const float TurnRadius = 108f;
                Vector2 offset;
                if (age <= FoldPassDuration)
                    offset = new Vector2(MathHelper.Lerp(-480f, 320f, (float)age / FoldPassDuration), TurnRadius);
                else if (age <= FoldPassDuration + FoldTurnDuration)
                {
                    float angle = MathHelper.Pi * (age - FoldPassDuration) / FoldTurnDuration;
                    offset = new Vector2(320f + TurnRadius * MathF.Sin(angle), TurnRadius * MathF.Cos(angle));
                }
                else
                    offset = new Vector2(320f - (age - FoldPassDuration - FoldTurnDuration) * 22f, -TurnRadius);
                offset.X *= AttackSide;
                FlyTo(head, AttackCenter + offset, CurrentLocalPhase == 0 ? 34f : 27f);
            }
            if (CurrentLocalPhase == 0 && Timer1 >= 58)
                ChangeStep(1);
            else if (CurrentLocalPhase == 1 && Timer1 >= FoldPassDuration + FoldTurnDuration + FoldReturnDuration)
                FinishAttack();
        }

        void Flank(NPC head, int rank)
        {
            float side = rank == 1 ? -AttackSide : AttackSide;
            MoveTowards(head, Target.Center + new Vector2(side * 560f, 260f), 19f, 1.8f);
        }

        void FinishAttack()
        {
            if (!TONetworkEnvironment.NotClient)
                return;
            CycleIndex++;
            ChangeAttack(Behavior.Phase1_Hunt);
        }
    }
}
