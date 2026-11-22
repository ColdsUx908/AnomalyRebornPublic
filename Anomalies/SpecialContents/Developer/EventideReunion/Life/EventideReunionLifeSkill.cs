namespace Anomalies.SpecialContents.Developer.EventideReunion;

public sealed partial class EventideReunionLifeSkill : AnomalyModProjectile
{
    public override bool IsLoadingEnabled(Mod mod) => AnomalySpecialContentToggler.EventideReunion;
    public override string Texture => EventideReunionAssets.WeaponPath;
    public override string LocalizationCategory => EventideReunion.LocalizationCategoryForEventideReunion;
    public override LocalizedText DisplayName => ModContent.GetModItem<EventideReunion>().DisplayName;
    // ai[0]：动作时钟；ai[1]：服务器已结算爆发；ai[2]：释放时朝向。
    internal int Age => (int)Projectile.ai[0];
    private int Facing => Projectile.ai[2] < 0f ? -1 : 1;
    private Vector2 Grip => Projectile.Owner.MountedCenter;
    private float Gravity => Projectile.Owner.gravDir;
    private float Rise => MathHelper.Clamp((Age - EventideReunionLifeRules.BurstTime) / (float)EventideReunionLifeRules.RiseDuration, 0f, 1f);
    private float Recovery => MathHelper.Clamp((Age - EventideReunionLifeRules.BurstTime - EventideReunionLifeRules.RiseDuration)
        / (float)EventideReunionLifeRules.RecoveryDuration, 0f, 1f);
    private readonly EventideReunionLifeOrbTrail _TipTrail = new();
    private float WeaponAngle
    {
        get
        {
            float plant = MathHelper.SmoothStep(-0.25f, 0.075f, Age / (float)EventideReunionLifeRules.PlantDuration);
            float angle = MathHelper.Lerp(plant, -MathHelper.PiOver2, MathHelper.SmoothStep(0f, 1f, Rise));
            return Facing > 0 ? Gravity * angle : MathHelper.Pi - Gravity * angle;
        }
    }
    private Vector2 WeaponTip => WeaponGrip + new Vector2(155f, -156f).RotatedBy(WeaponAngle + MathHelper.PiOver4);
    private float Push => MathHelper.SmoothStep(0f, 1f, (Age - EventideReunionLifeRules.BurstTime - EventideReunionLifeRules.RiseDuration) / 5f);
    private float FrontArmAngle => Facing * Gravity * MathHelper.Lerp(
        MathHelper.Lerp(-1.35f, -0.6f, MathHelper.Clamp(Age / (float)EventideReunionLifeRules.PlantDuration, 0f, 1f)),
        MathHelper.Lerp(MathHelper.Lerp(-1.85f, -3.05f, Push), -1.15f, Recovery * Recovery), Rise);

    private Vector2 WeaponGrip
    {
        get
        {
            Player player = Projectile.Owner;
            Player.CompositeArmData arm = player.compositeFrontArm;
            Vector2 hand = player.GetFrontHandPosition(arm.stretch, arm.rotation * Gravity);
            if (Gravity < 0f)
                hand.Y = 2f * player.Center.Y - hand.Y;
            return hand + new Vector2(0f, player.gfxOffY);
        }
    }

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 2;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.netImportant = true;
    }

    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => false;

    public override void AI()
    {
        Player player = Projectile.Owner;
        if (!player.active || player.dead || player.noItems || player.CCed
            || player.HeldItem.type != ModContent.ItemType<EventideReunion>())
        {
            Projectile.Kill();
            return;
        }
        Projectile.Center = player.Center;
        Projectile.timeLeft = 2;
        player.heldProj = Projectile.whoAmI;
        player.itemTime = player.itemAnimation = 2;
        player.ChangeDir(Facing);
        bool braced = Rise > 0.8f && Push < 0.5f;
        player.SetCompositeArmFront(true, braced ? Player.CompositeArmStretchAmount.Quarter : Player.CompositeArmStretchAmount.Full, FrontArmAngle);
        player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.ThreeQuarters, FrontArmAngle - Facing * 0.25f);
        if (Age == EventideReunionLifeRules.LaunchTime && Main.netMode != NetmodeID.MultiplayerClient)
            Projectile.NewProjectileAction<EventideReunionLifeOrb>(Projectile.GetSource_FromThis(), WeaponTip,
                new Vector2(0f, -Gravity * 13f), 0, 0f, Main.myPlayer,
                p => { p.ai[0] = player.whoAmI; p.ai[1] = Gravity; });
        if (!Main.dedServ)
        {
            if (Age is 0 or EventideReunionLifeRules.BurstTime)
                SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.5f, Pitch = Age == 0 ? -0.3f : 0.3f }, Grip);
            if (Age % 2 == 0)
            {
                Vector2 offset = Main.rand.NextVector2Circular(75f, 45f);
                ParticleHandler.SpawnParticle(new EventideReunionLifePetal(Grip + offset,
                    Recovery > 0f ? -offset * 0.08f : new Vector2(Main.rand.NextFloat(-1f, 1f), -Gravity * 2f), Gravity));
            }
            if (Age == EventideReunionLifeRules.LaunchTime)
                SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.7f, Pitch = -0.25f }, WeaponTip);
            Vector2 tip = WeaponTip;
            if (Age < EventideReunionLifeRules.BurstTime && Age % 2 == 0)
            {
                Vector2 offset = Main.rand.NextVector2Unit() * Main.rand.NextFloat(32f, 60f);
                ParticleHandler.SpawnParticle(new GenericSparkle(tip + offset, -offset / 12f, Color.Aquamarine,
                    Color.SeaGreen, 0.19f, 12));
            }
            if (Age >= EventideReunionLifeRules.BurstTime && Age <= EventideReunionLifeRules.LaunchTime)
                _TipTrail.Record(tip);
            Lighting.AddLight(Grip, new Vector3(0.12f, 0.4f, 0.12f));
        }
        if (Age >= EventideReunionLifeRules.BurstTime && Projectile.ai[1] == 0f && Main.netMode != NetmodeID.MultiplayerClient)
        {
            Projectile.ai[1] = 1f;
            Projectile.netUpdate = true;
            Bloom();
        }
        if (++Projectile.ai[0] >= EventideReunionLifeRules.CastDuration)
            Projectile.Kill();

        void Bloom()
        {
            int maximumLife = EventideReunionLifeBlessing.BaseLife(player);
            foreach (Player recipient in Main.ActivePlayers)
            {
                if (recipient == player || recipient.dead || !EventideReunionLifeRules.Ally(player.whoAmI, recipient.whoAmI,
                    player.team, recipient.team, player.hostile, recipient.hostile)
                    || Vector2.DistanceSquared(player.Center, recipient.Center) > EventideReunionLifeRules.Radius * EventideReunionLifeRules.Radius)
                    continue;
                Projectile.NewProjectileAction<EventideReunionLifeHeal>(Projectile.GetSource_FromThis(), recipient.Center,
                    Vector2.Zero, 0, 0f, recipient.whoAmI, p =>
                    {
                        p.ai[0] = 1f;
                        p.ai[1] = maximumLife;
                    });
            }
            foreach (NPC npc in Main.ActiveNPCs)
            {
                Vector2 nearest = Vector2.Clamp(player.Center, npc.position, npc.position + npc.Size);
                if (npc.friendly || npc.dontTakeDamage || npc.life <= 0 || npc.lifeMax <= 5
                    || Vector2.DistanceSquared(player.Center, nearest) > EventideReunionLifeRules.Radius * EventideReunionLifeRules.Radius)
                    continue;
                NPC root = npc.realLife >= 0 && npc.realLife < Main.maxNPCs ? Main.npc[npc.realLife] : npc;
                if (!root.active || root.life <= 0)
                    continue;
                root.Anomaly.EventideReunion_LifeMarkTime = EventideReunionLifeRules.MarkDuration;
                root.netUpdate = true;
            }
            Projectile.NewProjectileAction<EventideReunionLifeField>(Projectile.GetSource_FromThis(),
                player.Center + new Vector2(0f, player.height * 0.5f * Gravity), Vector2.Zero, 0, 0f, Main.myPlayer,
                p => { p.ai[0] = Gravity; p.ai[1] = player.whoAmI; });
        }
    }
}
