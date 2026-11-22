using Anomalies.BaseContents;

namespace Anomalies.GameContents.MusicBoxes;

public sealed class DivineLamentMusicBox : BaseMusicBox<DivineLamentMusicBoxTile>
{
    public override string MusicFileName => "Gods Weep — Divine Lament";
}

public sealed class DivineLamentMusicBoxTile : BaseMusicBoxTile;