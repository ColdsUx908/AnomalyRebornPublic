namespace Anomalies.ModIntegration;

public sealed class AnomaliesMusicHandler : IContentLoader
{
    public static int PathwaytoReverie { get; private set; } = -1;

    // Older music packs return null and keep the theme enabled.
    public static bool IsPathwaytoReverieEnabled => PathwaytoReverie > 0 &&
        AnomalyModReferences.AnomaliesMusic.Mod is Mod musicMod &&
        musicMod.Call("IsPathwaytoReverieEnabled") is not false;

    void IContentLoader.PostSetupContent()
    {
        int slot = AnomalyModReferences.AnomaliesMusic.Mod is Mod musicMod
            ? MusicLoader.GetMusicSlot(musicMod, "Music/Rainpath — Pathway to Reverie")
            : -1;
        PathwaytoReverie = slot > 0 ? slot : -1;
    }

    void IContentLoader.OnModUnload() => PathwaytoReverie = -1;
}
