using Terraria;

namespace AnomaliesMusic.Scenes;

public sealed class CorruptionMusicScene : BiomeMusicScene
{
    protected override string TrackName => "Gods Weep — Divine Lament";

    protected override bool IsEnabled(AnomaliesMusicConfig config) => config.DivineLament;

    protected override bool IsBiomeActive(Player player) => player.ZoneCorrupt &&
        (player.ZoneOverworldHeight || player.ZoneDirtLayerHeight || player.ZoneRockLayerHeight);
}
