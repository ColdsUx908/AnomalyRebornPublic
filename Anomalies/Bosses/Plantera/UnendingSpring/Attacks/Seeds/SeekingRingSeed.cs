using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Anomalies.Bosses.Plantera.UnendingSpring;

public sealed class SeekingRingSeed : ModProjectile
{
    internal const int FormationTicks = 60;
    private const float Speed = 25f;
    private const float TurnPerTick = MathHelper.Pi / 360f; // Half a degree per tick.
    private bool _Pink;
    private byte _FormationAge;
    private uint _StateTick;
    private bool Flying => Projectile.ai[2] > 0;
    public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.SeedPlantera}";

    public override void SetStaticDefaults()
    {
        Main.projFrames[Type] = Main.projFrames[ProjectileID.SeedPlantera];
        ProjectileID.Sets.TrailingMode[Type] = 0;
        ProjectileID.Sets.TrailCacheLength[Type] = 13;
    }

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 10;
        Projectile.hostile = true;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.netImportant = true;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 240;
    }

    public override void OnSpawn(IEntitySource source)
    {
        _StateTick = Main.GameUpdateCount;
        if (Main.netMode != NetmodeID.MultiplayerClient) _Pink = Main.rand.NextBool();
    }

    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(_Pink);
        writer.Write(_FormationAge);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        _Pink = reader.ReadBoolean();
        _FormationAge = reader.ReadByte();
        _StateTick = Main.GameUpdateCount;
    }
    public override bool ShouldUpdatePosition() => Flying;
    public override bool? CanDamage() => Flying ? null : false;

    public override void AI()
    {
        NPC boss = Main.npc[(int)Projectile.ai[0]];
        if (!boss.active || boss.type != NPCID.Plantera)
        {
            Projectile.Kill();
            return;
        }
        if (!Flying)
        {
            Vector2 radial = Projectile.ai[1].ToRotationVector2();
            bool advance = _StateTick != Main.GameUpdateCount;
            _StateTick = Main.GameUpdateCount;
            if (advance && _FormationAge < FormationTicks) _FormationAge++;
            float radius = UnendingSpringPaths.PetalTipRadius * _FormationAge / FormationTicks;
            Projectile.Center = boss.Center + radial * radius;
            Projectile.rotation = Projectile.ai[1] + MathHelper.PiOver2;
            Projectile.timeLeft = 240;
            // Server controls launch order and sends the exact launch position and aim.
            if (Main.netMode == NetmodeID.MultiplayerClient || !advance || ++Projectile.ai[2] < 0)
                return;
            if (!boss.HasPlayerTarget || !boss.HasValidTarget)
            {
                Projectile.Kill();
                return;
            }
            Projectile.velocity = (Main.player[boss.target].Center - Projectile.Center).SafeNormalize(radial) * Speed;
            Projectile.ai[2] = 1;
            Projectile.netUpdate = true;
        }
        else if (boss.HasPlayerTarget && boss.HasValidTarget)
        {
            Vector2 toPlayer = Main.player[boss.target].Center - Projectile.Center;
            if (toPlayer.LengthSquared() > 1f)
            {
                float heading = Projectile.velocity.ToRotation().AngleTowards(toPlayer.ToRotation(), TurnPerTick);
                Projectile.velocity = heading.ToRotationVector2() * Speed;
            }
            if (Main.netMode == NetmodeID.Server && Projectile.timeLeft % 30 == 0) Projectile.netUpdate = true;
        }
        Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        if (++Projectile.frameCounter >= 6)
        {
            Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
            Projectile.frameCounter = 0;
        }
    }

    public override void OnKill(int timeLeft) => ClearParticles.Burst(Projectile.Center, SeedSprites.EdgeColor(_Pink));

    public override bool PreDraw(ref Color lightColor)
    {
        SeedSprites.DrawTrail(Projectile, SeedSprites.EdgeColor(_Pink));
        SeedSprites.Draw(Projectile, _Pink, lightColor);
        return false;
    }
}