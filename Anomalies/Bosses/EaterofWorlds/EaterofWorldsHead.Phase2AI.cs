namespace Anomalies.Bosses.EaterofWorlds;

public sealed partial class EaterofWorldsHead
{
    private static int ShortIndex(int ordinal) => (ordinal - UltraLongWormSegmentCount - 1) / UltraShortWormSegmentCount;

    private int WaveDelay(int index) => CurrentPhase == Phase.Phase2_Relay && index >= UltraShortWormCount / 2 ? RelayDelay : 0;
    private int WaveAge(int index) => Timer1 - WaveLockDuration - WaveDelay(index);

    private bool StartHuntingCircle(Vector2 center)
    {
        if (!TryGetCircularChain(out List<NPC> chain))
            return false;
        float length = 0f;
        for (int i = 1; i < chain.Count; i++)
            length += Main.getGoodWorld ? 62f : chain[i].width * chain[i].scale;
        float radius = Math.Max(700f, length / MathHelper.ToRadians(200f));
        int turn = CycleIndex % 2 == 0 ? 1 : -1;
        float openingAngle = Target.velocity.SafeNormalize(Vector2.UnitX).ToRotation();
        float headAngle = openingAngle + MathHelper.Pi + turn * length / (2f * radius);
        return StartCircularMovement(center, radius, headAngle, turn * 12f / radius);
    }

    private void PrepareShortWorms(List<NPC> heads)
    {
        AttackCenter = Target.Center;
        bool relay = CurrentPhase == Phase.Phase2_Relay;
        int groupSize = relay ? UltraShortWormCount / 2 : UltraShortWormCount;
        float angle = (CycleIndex % 4) switch { 1 => 0.52f, 2 => -0.50f, 3 => MathHelper.PiOver2, _ => 0f };
        foreach (NPC head in heads)
        {
            if (head == NPC)
                continue;
            EaterofWorldsHead worm = GetInstance(head);
            int index = ShortIndex(worm.SegmentIndex);
            int group = index / groupSize;
            int lane = index % groupSize - groupSize / 2;
            Vector2 direction = Vector2.UnitY.RotatedBy(angle + group * 0.24f);
            Vector2 across = new(direction.Y, -direction.X);
            float width = Math.Abs(across.X) * head.width + Math.Abs(across.Y) * head.height;
            float side = lane < 0 ? -1f : 1f;
            float shoulder = side * (72f + width * 0.5f) + (lane < 0 ? lane + 1 : lane) * width;
            Vector2 gate = AttackCenter + across * (group * 52f * AttackSide);
            worm.SetRoute(gate + across * shoulder - direction * WaveApproachDistance, direction);
        }
    }

    private void Phase2CombatAI(List<NPC> heads)
    {
        Timer1++;
        if (CurrentLocalPhase == 1 && !Main.dedServ
            && (Timer1 == 1 || CurrentPhase == Phase.Phase2_Relay && Timer1 == RelayDelay + 1))
            SoundEngine.PlaySound(SoundID.Item8, AttackCenter);
        foreach (NPC head in heads)
        {
            if (head == NPC)
                continue;
            EaterofWorldsHead worm = GetInstance(head);
            switch (CurrentLocalPhase)
            {
                case 0: // Re-form two shoulders around the marked gap.
                    FlyTo(head, worm.LaunchOrigin, 34f);
                    break;
                case 1: // First wave, then the second group on a fixed beat in the relay.
                    int age = WaveAge(ShortIndex(worm.SegmentIndex));
                    if (age <= 0)
                    {
                        head.Center = worm.LaunchOrigin;
                        head.velocity = Vector2.Zero;
                        head.rotation = worm.LaunchDirection.ToRotation() + MathHelper.PiOver2;
                    }
                    else if (age <= WaveTravelDuration)
                        head.velocity = worm.LaunchDirection * WaveSpeed;
                    else
                        head.velocity *= 0.86f;
                    break;
                case 2: // Vulnerable recovery along the two shoulders, within reach of the gap.
                    FlyTo(head, worm.LaunchOrigin + worm.LaunchDirection * (WaveApproachDistance + 40f), 20f);
                    break;
                case 3: // Show the new center before moving the open arc.
                    head.velocity *= 0.86f;
                    break;
            }
        }
        if (!TONetworkEnvironment.NotClient)
            return;
        switch (CurrentLocalPhase)
        {
            case 0 when Timer1 >= FormationDuration:
                ChangeStep(1);
                break;
            case 1 when Timer1 >= WaveLockDuration + WaveTravelDuration + WaveDelay(UltraShortWormCount - 1) + 8:
                ChangeStep(2);
                break;
            case 2 when Timer1 >= WaveRecoveryDuration:
                CycleIndex++;
                if (CurrentPhase == Phase.Phase2 && NPC.LifeRatio <= RelayLifeRatio)
                    CurrentPhase = Phase.Phase2_Relay;
                if (Target.Distance(CircularCenter) > CircularRadius + 200f && !RetargetInsideCircle())
                {
                    AttackCenter = Target.Center;
                    SpawnPortal(AttackCenter, false, 60);
                    ChangeStep(3);
                }
                else
                    BeginWave();
                break;
            case 3 when Timer1 == 60:
                if (!StartHuntingCircle(AttackCenter))
                    RemoveEncounter();
                break;
            case 3 when Timer1 >= 84:
                BeginWave();
                break;
        }

        void BeginWave()
        {
            PrepareShortWorms(heads);
            ChangeStep(0);
        }

        bool RetargetInsideCircle()
        {
            foreach (Player player in Main.ActivePlayers)
            {
                if (!player.dead && player.Distance(CircularCenter) <= CircularRadius)
                {
                    NPC.target = player.whoAmI;
                    return true;
                }
            }
            return false;
        }
    }
}
