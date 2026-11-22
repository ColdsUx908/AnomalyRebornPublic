namespace Anomalies.SpecialContents.Developer.EventideReunion;

// 波前使用独立的近战弹幕，每个敌人只结算一次。
public sealed class EventideReunionStarStrike : AnomalyModProjectile
{
    private int Elapsed => (int)Projectile.localAI[0];

    public override bool IsLoadingEnabled(Mod mod) => AnomalySpecialContentToggler.EventideReunion;
    public override string Texture => EventideReunionAssets.WeaponPath;
    public override string LocalizationCategory => EventideReunion.LocalizationCategoryForEventideReunion;
    public override LocalizedText DisplayName => ModContent.GetModItem<EventideReunion>().DisplayName;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 24;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Melee;
        Projectile.penetrate = -1;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.hide = true;
        Projectile.timeLeft = EventideReunionStarSkill.WaveDuration + 2;
    }

    public override bool ShouldUpdatePosition() => false;
    public override bool CanHitPvp(Player target) => false;
    public override bool? CanHitNPC(NPC target) => target.CanBeChasedBy(Projectile) ? null : false;

    public override void AI()
    {
        if (!Projectile.Owner.active || Projectile.Owner.dead || Elapsed > EventideReunionStarSkill.WaveDuration)
        {
            Projectile.Kill();
            return;
        }
        Projectile.localAI[0]++;
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        int frame = Elapsed - 1;
        return new Annulus(Projectile.Center, EventideReunionStarSkill.WaveRadius(frame - 1),
            EventideReunionStarSkill.WaveRadius(frame)).Collides(targetHitbox);
    }
}
