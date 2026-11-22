using Anomalies.Bosses;
using Anomalies.Bosses.Skeletron.LegendaryNight;

namespace Anomalies.GameContents.LegendaryNight;

public sealed partial class AncientBattlefieldAltar
{
    /// <summary>
    /// 传说之夜祭坛激活音效。
    /// </summary>
    public static readonly SoundStyle LegendaryNightActivate = new(LegendaryNightHandler.LegendaryNightPath + "Summoning/LegendaryNightActivate");

    partial void AnimateRitual(ref int frame, ref bool ritualExists)
    {
        foreach (Projectile projectile in Projectile.ActiveProjectiles)
        {
            if (projectile.ModProjectile is LegendaryNightRitual ritual)
            {
                frame = Math.Clamp(ritual.Timer1 / 2, 0, FrameCount - 1);
                ritualExists = true;
                return;
            }
        }
    }

    partial void ActivateRitual(int i, int j)
    {
        // 祭坛始终加载；独立 Boss 开关关闭时不查询召唤条件，也不产生音效或仪式。
        if (!AnomalyBossChangeToggler.LegendaryNight)
            return;

        if (!Main.LocalPlayer.HasItem(ModContent.ItemType<MemoryofAstraeus>())) //没有召唤物品不允许召唤
            return;

        foreach (Projectile p in Projectile.ActiveProjectiles) //仪式已经存在，不允许重复召唤
        {
            if (p.ModProjectile is LegendaryNightRitual)
                return;
        }

        if ((!Main.remixWorld && Main.dayTime) || !Ultra) //白天或者非超凡世界不允许召唤
        {
            TOLocalizationUtils.ChatLocalizedText(AnomalySharedData.ModLocalizationPrefix + "GameContents.LegendaryNight.SummonFailed", LegendaryNightHandler.StarColor);
            return;
        }

        Tile tile = Main.tile[i, j];
        int left = i - tile.TileFrameX / 18;
        int top = j - tile.TileFrameY / 18;
        Vector2 ritualSpawnPosition = new Vector2(left, top).ToWorldCoordinates() + new Vector2(32f, -20f);

        SoundEngine.PlaySound(LegendaryNightActivate, ritualSpawnPosition);
        Projectile.NewProjectileAction<LegendaryNightRitual>(new EntitySource_WorldEvent(), ritualSpawnPosition, Vector2.Zero, 0, 0f, action: p =>
        {
            LegendaryNightRitual modP = p.GetModProjectile<LegendaryNightRitual>();
            modP.AltarTopLeftCoordinate = new Point(left, top);
        });
    }

    partial void CancelRitual()
    {
        Projectile.KillAll(projectile => projectile.ModProjectile is LegendaryNightRitual);
    }
}
