namespace Anomalies.SpecialContents.Developer.EventideReunion;

// 服务器确认接触并向拾取者发放一次临时上限奖励。
public sealed class EventideReunionLifeFruit : AnomalyModProjectile
{
    public override bool IsLoadingEnabled(Mod mod) => AnomalySpecialContentToggler.EventideReunion;
    public override string Texture => "Terraria/Images/Item_" + ItemID.LifeFruit;
    public override string LocalizationCategory => EventideReunion.LocalizationCategoryForEventideReunion;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 20;
        Projectile.timeLeft = EventideReunionLifeRules.FruitDuration;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.netImportant = true;
    }

    public override bool? CanDamage() => false;

    public override bool PreDraw(ref Color lightColor)
    {
        EventideReunionLifeOrb.DrawHalo(Projectile.Center, 58f, Math.Min(1f, Projectile.timeLeft / 45f));
        return true;
    }

    public override void AI()
    {
        int age = EventideReunionLifeRules.FruitDuration - Projectile.timeLeft;
        Projectile.velocity *= 0.95f;
        if (age >= 55)
            Projectile.velocity = Vector2.UnitY * (0.32f * MathF.Cos(age * 0.06f + Projectile.ai[2]));
        Projectile.rotation = MathF.Sin((EventideReunionLifeRules.FruitDuration - Projectile.timeLeft) * 0.06f) * 0.12f;
        if (!Main.dedServ)
            Lighting.AddLight(Projectile.Center, new Vector3(0.15f, 0.35f, 0.08f));
        if (Main.netMode == NetmodeID.MultiplayerClient || Projectile.ai[1] != 0f)
            return;
        int casterIndex = (int)Projectile.ai[0];
        if ((uint)casterIndex >= Main.maxPlayers || !Main.player[casterIndex].active)
        {
            Projectile.Kill();
            return;
        }
        Player caster = Main.player[casterIndex];
        Rectangle pickupArea = Projectile.Hitbox;
        pickupArea.Inflate(8, 8);
        foreach (Player player in Main.ActivePlayers)
        {
            if (player.dead
                || !EventideReunionLifeRules.Ally(casterIndex, player.whoAmI, caster.team, player.team, caster.hostile, player.hostile)
                || !player.Hitbox.Intersects(pickupArea))
                continue;
            Projectile.NewProjectileAction<EventideReunionLifeFruitPickup>(Projectile.GetSource_FromThis(), player.Center,
                Vector2.Zero, 0, 0f, player.whoAmI, receipt =>
                {
                    if (Main.netMode == NetmodeID.Server)
                        NetMessage.SendData(MessageID.SyncProjectile, number: receipt.whoAmI);
                    Projectile.ai[1] = 1f;
                });
            if (Projectile.ai[1] != 0f)
                Projectile.Kill();
            break;
        }
    }

    public override Color? GetAlpha(Color lightColor) => Color.White * Math.Min(1f, Projectile.timeLeft / 45f);
}
