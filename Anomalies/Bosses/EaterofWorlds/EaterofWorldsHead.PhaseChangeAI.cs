namespace Anomalies.Bosses.EaterofWorlds;

public sealed partial class EaterofWorldsHead
{
    private void UltraTransitionAI(List<NPC> heads)
    {
        Timer1++;
        switch (CurrentLocalPhase)
        {
            case 0: // The old chains disappear into the rift. Its burst marks the first healing frame.
                foreach (NPC head in heads)
                    FlyTo(head, CoilingCenter, 30f);
                if (Timer1 < UltraGatherDuration || !TONetworkEnvironment.NotClient)
                    break;
                bool released = false;
                Projectile.NewProjectileAction<CorruptionShockwave>(SourceAI, CoilingCenter, Vector2.Zero, 0, 0f,
                    action: wave =>
                    {
                        wave.scale = 0f;
                        released = true;
                    });
                if (!released)
                    break;
                CycleIndex = 0;
                List<NPC> transformed = TransformToUltraWorms();
                if (transformed.Count != UltraShortWormCount + 1 || !StartHuntingCircle(Target.Center))
                {
                    RemoveEncounter();
                    return;
                }
                PrepareShortWorms(transformed);
                foreach (NPC head in transformed.Skip(1))
                {
                    head.Center = GetInstance(head).LaunchOrigin;
                    head.velocity = Vector2.Zero;
                    head.netUpdate = true;
                }
                ChangeStep(1);
                break;
            case 1: // The owner heals from one saved value; a snapshot is sufficient to resume the curve.
                foreach (NPC head in heads)
                {
                    if (head != NPC)
                        GetInstance(head).SwimBesideLane(Timer1, -32f, 12f);
                }
                int healedLife = TransitionLife(Timer1);
                NPC.life = healedLife;
                if (!Main.dedServ && Timer1 % 15 == 0 && Timer1 <= UltraHealDuration)
                {
                    int gained = healedLife - TransitionLife(Timer1 - 15);
                    if (gained > 0)
                        NPC.HealEffect(gained, false);
                }
                if (!TONetworkEnvironment.NotClient)
                    break;
                NPC.netUpdate |= Timer1 % 15 == 0;
                if (Timer1 >= UltraHealDuration)
                {
                    CurrentPhase = Phase.Phase2;
                    ChangeAttack(Behavior.ShortWormFormation);
                    PrepareShortWorms(heads);
                }
                break;
        }

        int TransitionLife(int age)
        {
            int targetLife = Math.Max(1, (int)(NPC.lifeMax * UltraHealRatio));
            return age >= UltraHealDuration ? targetLife
                : (int)MathHelper.Lerp(Timer2, targetLife, TOMathUtils.Interpolation.LogarithmicEaseOut((float)age / UltraHealDuration));
        }
    }
}
