namespace Anomalies.Bosses.Plantera.UnendingSpring;

public sealed class UnendingSpringName : GlobalNPC
{
    public override bool AppliesToEntity(NPC entity, bool lateInstantiation) => entity.type == NPCID.Plantera;

    public override void ModifyTypeName(NPC npc, ref string typeName)
    {
        if (PlanteraUnending.GetInstance(npc).ShouldProcess)
            typeName = Language.GetTextValue("Mods.Anomalies.Bosses.Plantera.UnendingSpring.DisplayName");
    }
}
