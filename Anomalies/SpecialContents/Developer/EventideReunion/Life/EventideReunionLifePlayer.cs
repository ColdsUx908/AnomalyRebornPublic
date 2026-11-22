namespace Anomalies.SpecialContents.Developer.EventideReunion;

public sealed class EventideReunionLifePlayer : AnomalyPlayerBehavior
{
    public override bool IsLoadingEnabled(Mod mod) => AnomalySpecialContentToggler.EventideReunion;

    public override void PostUpdateEquips()
    {
        int stacks = EventideReunionLifeBlessing.Stacks(Player);
        AnomalyPlayer.EventideReunion_LifeBlessingBonus = EventideReunionLifeRules.BonusLife(Player.statLifeMax2, stacks);
        Player.statLifeMax2 += AnomalyPlayer.EventideReunion_LifeBlessingBonus;
    }

    public override void PostUpdate()
    {
        if (AnomalyPlayer.EventideReunion_LifeCooldown > 0)
            AnomalyPlayer.EventideReunion_LifeCooldown--;
        bool blessed = EventideReunionLifeBlessing.Stacks(Player) > 0;
        if (Player.whoAmI == Main.myPlayer && AnomalyPlayer.EventideReunion_LifeBlessingWasActive
            && !blessed && Player.statLife > Player.statLifeMax2)
        {
            Player.statLife = Player.statLifeMax2;
            if (Main.netMode == NetmodeID.MultiplayerClient)
                NetMessage.SendData(MessageID.PlayerLifeMana, number: Player.whoAmI);
        }
        AnomalyPlayer.EventideReunion_LifeBlessingWasActive = blessed;
    }

    public override void UpdateDead()
    {
        EventideReunionLifeBlessing.Clear(Player);
        AnomalyPlayer.EventideReunion_LifeBlessingBonus = 0;
        AnomalyPlayer.EventideReunion_LifeBlessingWasActive = false;
        if (AnomalyPlayer.EventideReunion_LifeCooldown > 0)
            AnomalyPlayer.EventideReunion_LifeCooldown--;
    }

    public override void SetControls()
    {
        if (Player.ownedProjectileCounts[ModContent.ProjectileType<EventideReunionLifeSkill>()] > 0)
            Player.controlUseItem = Player.controlUseTile = false;
    }
}

public class EventideReunionLifeBlessing : ModBuff
{
    public override bool IsLoadingEnabled(Mod mod) => AnomalySpecialContentToggler.EventideReunion;
    public override string Texture => EventideReunionLifeAssets.AssetPath + nameof(EventideReunionLifeBlessing);
    public override string LocalizationCategory => EventideReunion.LocalizationCategoryForEventideReunion;

    // 用原版 Buff 类型同步层数，每次吸收只保留当前一档。
    private static int BuffType(int stacks) => stacks switch
    {
        1 => ModContent.BuffType<EventideReunionLifeBlessing>(),
        2 => ModContent.BuffType<EventideReunionLifeBlessing2>(),
        _ => ModContent.BuffType<EventideReunionLifeBlessing3>()
    };

    internal static int Stacks(Player player)
    {
        for (int i = 0; i < Player.MaxBuffs && player.buffType[i] != 0; i++)
            if (ModContent.GetModBuff(player.buffType[i]) is EventideReunionLifeBlessing blessing)
                return blessing.StackCount;
        return 0;
    }

    protected virtual int StackCount => 1;
    private const string TextKey = "Mods.Anomalies.SpecialContents.Developer.EventideReunion.EventideReunionLifeBlessing.";
    public override LocalizedText DisplayName => Language.GetText(TextKey + "DisplayName");
    public override LocalizedText Description => Language.GetText(TextKey + "Description");

    internal static void Clear(Player player)
    {
        int stacks = Stacks(player);
        if (stacks > 0)
            player.ClearBuff(BuffType(stacks));
    }

    internal static int BaseLife(Player player)
    {
        int divisor = EventideReunionLifeRules.FruitBonusDivisor + Stacks(player);
        return (int)(((long)player.statLifeMax2 * EventideReunionLifeRules.FruitBonusDivisor + divisor - 1) / divisor);
    }

    internal static void Absorb(Player player)
    {
        int previousMaximum = player.statLifeMax2;
        int baseLife = BaseLife(player);
        int previous = Stacks(player);
        int stacks = Math.Min(EventideReunionLifeRules.MaxBlessingStacks, previous + 1);
        if (previous > 0 && previous != stacks)
            player.ClearBuff(BuffType(previous));
        player.AddBuff(BuffType(stacks), EventideReunionLifeRules.BlessingDuration);
        player.Anomaly.EventideReunion_LifeBlessingBonus = EventideReunionLifeRules.BonusLife(baseLife, Stacks(player));
        player.statLifeMax2 = baseLife + player.Anomaly.EventideReunion_LifeBlessingBonus;
        int granted = EventideReunionLifeRules.FruitLifeGain(previousMaximum, player.statLifeMax2, player.statLife);
        if (granted > 0)
        {
            player.statLife += granted;
            player.HealEffect(granted, broadcast: false);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                NetMessage.SendData(MessageID.PlayerLifeMana, number: player.whoAmI);
        }
    }

    public override void SetStaticDefaults()
    {
        Main.buffNoSave[Type] = true;
    }
}

public sealed class EventideReunionLifeBlessing2 : EventideReunionLifeBlessing
{
    protected override int StackCount => 2;
}

public sealed class EventideReunionLifeBlessing3 : EventideReunionLifeBlessing
{
    protected override int StackCount => 3;
}

// 服务器按接收者生成一次性治疗凭证，接收者客户端沿用原版 SpiritHeal 同步生命。
public class EventideReunionLifeHeal : AnomalyModProjectile
{
    public override bool IsLoadingEnabled(Mod mod) => AnomalySpecialContentToggler.EventideReunion;
    public override string Texture => EventideReunionRuneAssets.AssetPath + "Life";
    public override string LocalizationCategory => EventideReunion.LocalizationCategoryForEventideReunion;
    public override LocalizedText DisplayName => ModContent.GetModItem<EventideReunion>().DisplayName;
    private bool _Consumed;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 2;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = 120;
        Projectile.hide = true;
    }

    public override bool? CanDamage() => false;
    public override bool PreDraw(ref Color lightColor) => false;

    public override void AI()
    {
        if (!Projectile.IsOnOwnerClient || _Consumed)
            return;
        _Consumed = true;
        Player player = Projectile.Owner;
        if (player.active && !player.dead)
        {
            ApplyReward(player);
        }
        Projectile.Kill();
    }

    protected virtual void ApplyReward(Player player)
    {
        // ai[0]：0=追踪光球，1=队友爆发。
        int healing = (int)Projectile.ai[0] switch
        {
            1 => EventideReunionLifeRules.AllyHealing((int)Projectile.ai[1]),
            _ => EventideReunionLifeRules.OrbHealing
        };
        healing = EventideReunionLifeRules.EffectiveHealing(healing, player.statLife, player.statLifeMax2, player.moonLeech);
        if (healing > 0)
        {
            player.statLife += healing;
            player.HealEffect(healing, broadcast: false);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                NetMessage.SendData(MessageID.SpiritHeal, number: player.whoAmI, number2: healing);
        }
    }
}

// 独立类型使接收端在初始弹幕同步时就能识别果实奖励。
public sealed class EventideReunionLifeFruitPickup : EventideReunionLifeHeal
{
    protected override void ApplyReward(Player player) => EventideReunionLifeBlessing.Absorb(player);
}
