using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Anomalies.Bosses.Plantera.UnendingSpring;

// All thorns in a row share their velocity, color and lifetime.
public sealed class RainThorn : ModProjectile
{
    private const int HitWidth = 26;
    private const float Speed = 6f;
    private static readonly float _FallSpeed = Speed * MathF.Sin(MathHelper.Pi / 3f);
    private static readonly float _SidewaysTravel = 2f * PlanteraArena.Radius * (Speed / 2f) / _FallSpeed;
    private bool _Positioned;
    private bool Pink => Projectile.velocity.X > 0f;
    private Color Tint => SeedSprites.EdgeColor(Pink);
    private float Spacing => Projectile.ai[1];
    private int Count => Spacing > 0f ?
        1 + (int)MathF.Ceiling((2f * PlanteraArena.Radius + _SidewaysTravel) / Spacing) : 0;
    private float FirstX => Projectile.Center.X - (Count - 1) * Spacing / 2f;
    public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.ThornBall}";
    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 4200;
    public override bool ShouldUpdatePosition() => false;

    private static Vector2 LaunchOffset(bool pink) =>
        new((pink ? -0.5f : 0.5f) * _SidewaysTravel, -PlanteraArena.Radius);

    internal static void FireRow(NPC boss, int row)
    {
        Vector2 center = PlanteraUnending.GetInstance(boss).ArenaCenter;
        int playerWidth = boss.HasPlayerTarget ? Main.player[boss.target].width : Player.defaultWidth;
        float spacing = 3f * (HitWidth + playerWidth * 2.5f);
        bool pink = row % 2 == 0;
        Vector2 velocity = new(pink ? Speed / 2f : -Speed / 2f, _FallSpeed);
        Vector2 start = center + LaunchOffset(pink);
        Projectile.NewProjectileAction(boss.GetSource_FromAI(), start, velocity, ModContent.ProjectileType<RainThorn>(),
            Math.Max(1, boss.defDamage / 2), 0f, Main.myPlayer, p => { p.ai[0] = boss.whoAmI; p.ai[1] = spacing; p.ai[2] = row; });
    }

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = HitWidth;
        Projectile.hostile = true;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.netImportant = true;
        Projectile.timeLeft = 902;
    }

    public override void AI()
    {
        NPC boss = Main.npc[(int)Projectile.ai[0]];
        if (!boss.active || boss.type != NPCID.Plantera || !PlanteraUnending.GetInstance(boss).LightningActive)
        {
            Projectile.Kill();
            return;
        }
        // The arena can still pan at the start of the attack. Every row must use
        // the same top tangent and its scheduled age, including on a joining client.
        int age = PlanteraUnending.GetInstance(boss).LightningTick -
            PlanteraUnending.RainSpawnTick((int)Projectile.ai[2]) + 1;
        _Positioned = age > 0;
        if (!_Positioned) return;
        Vector2 arena = PlanteraUnending.GetInstance(boss).ArenaCenter;
        Vector2 launch = arena + LaunchOffset(Pink);
        Projectile.Center = launch + age * Projectile.velocity;
        if (age * Projectile.velocity.Y > 2f * PlanteraArena.Radius)
        {
            Projectile.Kill();
            return;
        }
        Projectile.rotation = age * (Pink ? 0.1f : -0.1f);
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle target)
    {
        if (!_Positioned || Count <= 0) return false;
        int nearest = Math.Clamp((int)MathF.Round((target.Center.X - FirstX) / Spacing), 0, Count - 1);
        float x = FirstX + nearest * Spacing;
        float y = Projectile.Center.Y;
        return x + HitWidth / 2f > target.Left && x - HitWidth / 2f < target.Right &&
               y + HitWidth / 2f > target.Top && y - HitWidth / 2f < target.Bottom;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        if (!_Positioned) return false;
        Vector2 p = new Vector2(FirstX, Projectile.Center.Y) - Main.screenPosition;
        for (int i = 0; i < Count; i++, p.X += Spacing)
            if (p.X >= -48 && p.X <= Main.screenWidth + 48)
                SeedSprites.DrawSpellShot(ProjectileID.ThornBall, Tint, 902 - Projectile.timeLeft,
                    p, Projectile.rotation, 38f, 1f);
        return false;
    }

    public override void OnKill(int timeLeft)
    {
        if (Main.netMode == NetmodeID.Server) return;
        Vector2 near = Main.Camera.ScaledPosition - new Vector2(48f);
        Vector2 far = Main.Camera.ScaledPosition + Main.Camera.ScaledSize + new Vector2(48f);
        Vector2 p = new(FirstX, Projectile.Center.Y);
        if (p.Y < near.Y || p.Y > far.Y) return;
        for (int i = 0; i < Count; i++, p.X += Spacing)
        {
            if (p.X < near.X || p.X > far.X) continue;
            for (int j = 0; j < 8; j++)
            {
                Vector2 velocity = (MathHelper.TwoPi * j / 8f).ToRotationVector2() * Main.rand.NextFloat(1.5f, 4.5f);
                Dust.NewDustPerfectAction(p, DustID.RainbowMk2, dust => { dust.velocity = velocity; dust.color = Tint; dust.scale = 1.3f; dust.noGravity = true; });
            }
        }
    }
}