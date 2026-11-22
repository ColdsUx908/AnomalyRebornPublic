using Anomalies.BaseContents;

namespace Anomalies.Bosses.EaterofWorlds;

public sealed class CorruptionShockwave : BaseShockwaveProjectile
{
    public override string LocalizationCategory => "Bosses.EaterofWorlds";
    public override int LifeTime => 150;
    public override float FinalScale => 3.5f;
    public override bool UseHDTexture => true;

    public override bool? CanDamage() => false;

    public override void AI()
    {
        if (Timer1 == 0 && !Main.dedServ)
            SoundEngine.PlaySound(SoundID.ForceRoar, Projectile.Center);
        base.AI();
    }

    public override Color? GetAlpha(Color lightColor) => new Color(172, 76, 244);
}
