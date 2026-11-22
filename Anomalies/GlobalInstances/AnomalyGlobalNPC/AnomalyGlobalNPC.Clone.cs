namespace Anomalies.Common;

public sealed partial class AnomalyGlobalNPC
{
    partial void CloneExtraData(AnomalyGlobalNPC clone)
    {
        clone._Network_ProjectileReferenceCache = (ProjectileNetReference[])_Network_ProjectileReferenceCache.Clone();
        clone.BrainOfCthulhu_AfterImages = [];
        clone.EaterOfWorlds_Segments = [.. EaterOfWorlds_Segments];
        clone.Skeletron_HandBloodDust = null;
        clone.Skeletron_LegendaryNightHalfFieldHits = 0;
        clone.Depth_DrawAnchor = null;
        clone.Visual_Particles = null;
#if SPECIAL_CONTENTS
        clone.EventideReunion_DeathHitOwner = -1;
        clone.EventideReunion_DeathMarkTime = 0;
        clone.EventideReunion_DeathMarkProgress = 0f;
        clone.EventideReunion_LifeMarkTime = 0;
        clone.EventideReunion_DeathOwner = -1;
        clone.EventideReunion_SamsaraRecord = new();
        clone.EventideReunion_SamsaraOwner = -1;
        clone.EventideReunion_SamsaraStartTick = 0;
        clone.EventideReunion_SamsaraVisualTime = 0;
#endif
    }
}
