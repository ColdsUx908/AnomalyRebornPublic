namespace Anomalies.Bosses.Plantera;

public sealed partial class Plantera : AnomalyNPCBehavior<Plantera>
{
    public override int ApplyingType => NPCID.Plantera;

    public override bool ShouldProcess => base.ShouldProcess && AnomalyBossChangeToggler.Plantera && AnomalyNPC.CustomAIType != 1;

    public override bool PreAI()
    {
        return true;
    }
}
