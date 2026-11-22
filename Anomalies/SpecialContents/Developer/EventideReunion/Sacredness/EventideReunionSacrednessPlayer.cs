namespace Anomalies.SpecialContents.Developer.EventideReunion;

public sealed class EventideReunionSacrednessPlayer : AnomalyPlayerBehavior
{
    public override bool IsLoadingEnabled(Mod mod) => AnomalySpecialContentToggler.EventideReunion;

    private bool IsCasting => Player.ownedProjectileCounts[ModContent.ProjectileType<EventideReunionSacrednessSkill>()] > 0;

    private bool IsDashing()
    {
        if (IsCasting)
            foreach (Projectile p in Main.ActiveProjectiles)
                if (p.owner == Player.whoAmI && p.ModProjectile is EventideReunionSacrednessSkill { IsDashing: true })
                    return true;
        return false;
    }

    public override bool ImmuneTo(PlayerDeathReason damageSource, int cooldownCounter, bool dodgeable) => IsDashing();

    public override void SetControls()
    {
        if (!IsCasting)
            return;
        Player.controlUseItem = Player.controlUseTile = false;
        if (IsDashing())
        {
            Player.controlLeft = Player.controlRight = Player.controlUp = Player.controlDown = false;
            Player.controlJump = Player.controlHook = Player.controlMount = false;
        }
    }

    public override void PreUpdateMovement()
    {
        if (IsDashing())
        {
            Player.gravity = 0f;
            Player.velocity = Vector2.Zero;
            Player.noFallDmg = true;
        }
    }

    public override void PostUpdate()
    {
        if (AnomalyPlayer.EventideReunion_SacrednessCooldown > 0)
            AnomalyPlayer.EventideReunion_SacrednessCooldown--;
    }

    public override void UpdateDead() => PostUpdate();
}
