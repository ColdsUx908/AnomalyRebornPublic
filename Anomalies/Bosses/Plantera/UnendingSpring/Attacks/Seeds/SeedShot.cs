using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Anomalies.Bosses.Plantera.UnendingSpring;

public abstract class SeedShot : ModProjectile
{
    private const int StraightTicks = 30;
    private const float CrossScale = 1.25f;
    private const int TurnTicks = 24;
    private const float TurnDegreesPerTick = 2.5f;
    // Nonnegative motion IDs select a boss volley. Negative IDs select a thorn seed.
    internal const int CrossLeft = -1;
    internal const int CrossRight = -2;
    private const float CrossSpeed = 7.03828125f;
    internal const float CrossLaunchSpeed = 0.6f * CrossSpeed;
    // Former endpoint was 1502.64 px from the burst after 30 straight, 24 turning and 186 straight steps.
    private const float CrossRange = 2629.62f;
    private static readonly float[] _RingDegreesPerTick = { 2f, -3f, 4f, -5f };
    private Vector2 _BurstCenter;
    private ulong _SpawnTick;
    private bool IsCrossSeed => Projectile.ai[1] < 0f;
    protected abstract int SpriteId { get; }
    public override string Texture => $"Terraria/Images/Projectile_{SpriteId}";

    public override void SetStaticDefaults()
    {
        Main.projFrames[Type] = Main.projFrames[SpriteId];
        ProjectileID.Sets.TrailingMode[Type] = 0;
        ProjectileID.Sets.TrailCacheLength[Type] = 13;
    }

    public override void OnSpawn(IEntitySource source)
    {
        _SpawnTick = Main.GameUpdateCount;
        if (IsCrossSeed) _BurstCenter = Projectile.Center;
        ApplyCrossSize();
    }

    public override void SendExtraAI(BinaryWriter writer)
    {
        if (IsCrossSeed)
        {
            writer.Write(_BurstCenter.X);
            writer.Write(_BurstCenter.Y);
        }
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        if (IsCrossSeed) _BurstCenter = new Vector2(reader.ReadSingle(), reader.ReadSingle());
    }

    private void ApplyCrossSize()
    {
        if (IsCrossSeed && Projectile.scale != CrossScale)
        {
            Projectile.scale = CrossScale;
            Projectile.Resize(13, 13); // The engine uses whole-pixel hitboxes (10 × 1.25 = 12.5).
        }
    }

    // Start every member of a ring or pair together regardless of its projectile update slot.
    public override bool ShouldUpdatePosition() => _SpawnTick != Main.GameUpdateCount;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 10;
        Projectile.hostile = true;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = 240;
    }

    public override void AI()
    {
        NPC boss = Main.npc[(int)Projectile.ai[0]];
        if (!boss.active || boss.type != NPCID.Plantera)
        {
            Projectile.Kill();
            return;
        }
        ApplyCrossSize(); // Also applies to network-created projectiles on clients.
        if (ShouldUpdatePosition())
        {
            int age = (int)Projectile.ai[2];
            int turnStart = IsCrossSeed ? StraightTicks : 0;
            if (age < turnStart + TurnTicks)
            {
                if (age >= turnStart)
                {
                    if (IsCrossSeed && age == turnStart)
                        Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * CrossSpeed;
                    float rate = IsCrossSeed
                        ? (Projectile.ai[1] == CrossLeft ? -TurnDegreesPerTick : TurnDegreesPerTick)
                        : _RingDegreesPerTick[(int)Projectile.ai[1]];
                    Projectile.velocity = Projectile.velocity.RotatedBy(MathHelper.ToRadians(rate));
                }
                Projectile.ai[2] = age + 1;
            }
            if (IsCrossSeed)
            {
                Vector2 next = Projectile.Center + Projectile.velocity;
                Projectile.timeLeft = Vector2.DistanceSquared(next, _BurstCenter) >= CrossRange * CrossRange ? 1 : 2;
            }
        }
        Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        if (++Projectile.frameCounter >= 6)
        {
            Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
            Projectile.frameCounter = 0;
        }
    }

    internal static void Fire(IEntitySource source, Vector2 origin, Vector2 velocity, int damage, int boss, int motion)
    {
        int type = Main.rand.NextBool() ? ModContent.ProjectileType<RedSeed>() : ModContent.ProjectileType<GreenSeed>();
        Projectile.NewProjectileAction(source, origin, velocity, type, damage, 0f, Main.myPlayer, p =>
        {
            p.ai[0] = boss;
            p.ai[1] = motion;
            if (p.ModProjectile is SeedShot shot && shot.IsCrossSeed)
            {
                shot._BurstCenter = p.Center;
                shot.ApplyCrossSize();
            }
        });
    }

    public override void OnKill(int timeLeft) => ClearParticles.Burst(Projectile.Center, SeedSprites.EdgeColor(this is RedSeed));

    public override bool PreDraw(ref Color lightColor)
    {
        bool pink = this is RedSeed;
        float opacity = 1f;
        if (IsCrossSeed)
        {
            float remaining = CrossRange - Vector2.Distance(Projectile.Center, _BurstCenter);
            opacity = MathHelper.Clamp(remaining / (20f * CrossSpeed), 0f, 1f);
            SeedSprites.DrawTrail(Projectile, SeedSprites.EdgeColor(pink), opacity);
        }
        SeedSprites.Draw(Projectile, pink, lightColor, opacity);
        return false;
    }
}

public sealed class RedSeed : SeedShot
{
    protected override int SpriteId => ProjectileID.SeedPlantera;
}

public sealed class GreenSeed : SeedShot
{
    protected override int SpriteId => ProjectileID.PoisonSeedPlantera;
}