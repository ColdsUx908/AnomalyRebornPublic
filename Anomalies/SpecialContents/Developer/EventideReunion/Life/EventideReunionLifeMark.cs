namespace Anomalies.SpecialContents.Developer.EventideReunion;

public sealed class EventideReunionLifeMarkBehavior : AnomalyGlobalNPCBehavior
{
    public override bool IsLoadingEnabled(Mod mod) => AnomalySpecialContentToggler.EventideReunion;

    public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter writer)
        => writer.Write((ushort)npc.Anomaly.EventideReunion_LifeMarkTime);

    public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader reader)
        => npc.Anomaly.EventideReunion_LifeMarkTime = reader.ReadUInt16();

    public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers)
    {
        NPC root = npc.realLife >= 0 && npc.realLife < Main.maxNPCs ? Main.npc[npc.realLife] : npc;
        if (root.active && root.Anomaly.EventideReunion_LifeMarkTime > 0)
            modifiers.FinalDamage *= EventideReunionDamageConstants.LifeMarkIncomingDamageMultiplier;
    }
}

public sealed class EventideReunionLifeMarkSystem : ModSystem
{
    public override bool IsLoadingEnabled(Mod mod) => AnomalySpecialContentToggler.EventideReunion;

    public override void PostUpdateNPCs()
    {
        foreach (NPC npc in Main.ActiveNPCs)
        {
            if (npc.Anomaly.EventideReunion_LifeMarkTime <= 0)
                continue;
            if (--npc.Anomaly.EventideReunion_LifeMarkTime == 0 && Main.netMode != NetmodeID.MultiplayerClient)
                npc.netUpdate = true;
            if (!Main.dedServ && Main.GameUpdateCount % 12 == (ulong)npc.whoAmI % 12)
                Dust.NewDustPerfectAction(npc.Center + Main.rand.NextVector2Circular(npc.width * 0.5f, npc.height * 0.5f),
                    DustID.GrassBlades, d => { d.noGravity = true; d.velocity = new Vector2(0f, -1f); d.scale = 0.8f; });
        }
    }
}
