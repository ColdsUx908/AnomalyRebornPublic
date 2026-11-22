
namespace Anomalies.GameContents.AnomalyItems.HoneyedWildbloom;

public sealed class FriendlyBeePet : AnomalyModProjectile
{
    private const int TurnConfirmationTicks = 12;
    private int _FollowDirection;
    private int _OppositeDirectionTicks;

    public override LocalizedText DisplayName => Language.GetText("Mods.Anomalies.Bosses.QueenBee.FriendlyBee.DisplayName");
    public override string Texture => AnomalySharedData.ModPath + "GameContents/AnomalyItems/HoneyedWildbloom/FriendlyBeePet";

    public override void SetStaticDefaults()
    {
        Main.projFrames[Type] = 2;
        Main.projPet[Type] = true;

        ProjectileID.Sets.CharacterPreviewAnimations[Type] = ProjectileID.Sets.SimpleLoop(0, Main.projFrames[Type], 5)
            .WithOffset(-10f, -26f).WhenNotSelected(0, 0);
    }

    public override void SetDefaults()
    {
        Projectile.width = 32;
        Projectile.height = 32;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.tileCollide = false;
        Projectile.timeLeft = 1200;
        Projectile.netImportant = true;
    }

    public override void AI()
    {
        Projectile.damage = 0;

        if (++Projectile.frameCounter >= 5)
        {
            Projectile.frameCounter = 0;
            if (++Projectile.frame >= 2)
                Projectile.frame = 0;
        }

        Player player = Owner;
        if (Projectile.owner == Main.myPlayer && (!player.Alive || !player.HasBuff<HoneyedWildbloomBuff>()))
        {
            Projectile.Kill();
            return;
        }

        // 沙暴瓶会连续翻转玩家朝向，只有持续的转向才切换跟随侧。
        if (_FollowDirection == 0)
            _FollowDirection = player.direction;

        if (player.direction == _FollowDirection)
            _OppositeDirectionTicks = 0;
        else if (++_OppositeDirectionTicks >= TurnConfirmationTicks)
        {
            _FollowDirection = player.direction;
            _OppositeDirectionTicks = 0;
        }

        Projectile.FloatingPetAI(false, 0.03f, lightPet: false, ownerDirection: _FollowDirection);
        Projectile.timeLeft = 300;
    }

    public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) => behindNPCs.Add(index);
}

