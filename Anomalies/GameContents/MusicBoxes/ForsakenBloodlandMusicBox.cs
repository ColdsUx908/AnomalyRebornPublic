using Anomalies.BaseContents;

namespace Anomalies.GameContents.MusicBoxes;

public sealed class ForsakenBloodlandMusicBox : BaseMusicBox<ForsakenBloodlandMusicBoxTile>
{
    public override string MusicFileName => "Godforsaken — Forsaken Bloodland";
}

public sealed class ForsakenBloodlandMusicBoxTile : BaseMusicBoxTile;