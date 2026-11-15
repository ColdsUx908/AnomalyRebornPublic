namespace Anomalies.Bosses.Skeletron.LegendaryNight;

/// <summary>四种钱币共用的鲜亮金属层次；亮芯保留币色，不将所有审判染成金色。</summary>
internal readonly record struct CoinJudgmentPalette(Color Shade, Color Body, Color Light, Color Spark)
{
    public static CoinJudgmentPalette For(int coinType) => coinType switch
    {
        0 => new(new(210, 62, 30), new(255, 126, 55), new(255, 194, 124), new(255, 230, 190)),
        1 => new(new(96, 155, 188), new(220, 242, 244), new(248, 255, 244), new(255, 255, 250)),
        2 => new(new(210, 108, 8), new(255, 192, 28), new(255, 226, 110), new(255, 246, 204)),
        _ => new(new(62, 118, 210), new(150, 210, 255), new(212, 240, 255), new(242, 252, 255))
    };
}
