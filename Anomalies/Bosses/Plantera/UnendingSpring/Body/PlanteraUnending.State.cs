namespace Anomalies.Bosses.Plantera.UnendingSpring;

public sealed partial class PlanteraUnending
{
    public enum Phase : byte
    {
        Initialize,
        Phase1,
        PhaseChange_1To2,
        Phase2,
    }

    public enum Behavior : byte
    {
        Opening,
        Standoff,
        CrossSeeds,
        SeekingRing,
        ButterflyFlowers,
        TransitionSpell,
        HookLightning,
        DashFan,
        PetalWheel,
        ParadoxSector,
    }

    // ai[0] 的前两个字节保存阶段与行为，ai[1] 保存局部步骤，ai[2] 保存当前行为帧数。
    public Phase CurrentPhase => (Phase)AI_Union_0.Byte0;
    public Behavior CurrentBehavior => (Behavior)AI_Union_0.Byte1;
    public int CurrentLocalPhase => (int)NPC.ai[1];
    private ref float Timer => ref NPC.ai[2];

    private void EnterBehavior(Phase phase, Behavior behavior)
    {
        if (!TONetworkEnvironment.NotClient)
            return;

        Union32 state = AI_Union_0;
        state.Byte0 = (byte)phase;
        state.Byte1 = (byte)behavior;
        AI_Union_0 = state;
        NPC.ai[1] = 0f;
        Timer = 0f;
        NPC.netUpdate = true;
    }
}