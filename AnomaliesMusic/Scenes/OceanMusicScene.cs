using Terraria;

namespace AnomaliesMusic.Scenes;

public sealed class OceanMusicScene : BiomeMusicScene
{
    protected override string TrackName => "Deep Blue — Echoes of the Azure";

    protected override bool IsEnabled(AnomaliesMusicConfig config) => config.EchoesoftheAzure;

    protected override bool IsBiomeActive(Player player) => player.ZoneBeach;

    // Let evil biome music take precedence when the ocean overlaps with one.
    public override float GetWeight(Player player) => 0.4f;
}
