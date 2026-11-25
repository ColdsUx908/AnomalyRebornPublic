using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace Anomalies.Bosses.Plantera.UnendingSpring;

public sealed class BurstThorn : ModProjectile
{
    public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.ThornBall}";

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 24;
        Projectile.hostile = true;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = 60;
    }

    public override void AI()
    {
        int bossIndex = (int)Projectile.ai[0];
        NPC boss = Main.npc[bossIndex];
        if (!boss.active || boss.type != NPCID.Plantera)
        {
            Projectile.Kill();
            return;
        }

        Projectile.velocity *= 0.985f;
        Projectile.rotation += 0.12f;
        if (Projectile.timeLeft == 1 && Main.netMode != NetmodeID.MultiplayerClient)
        {
            const int Ways = 18;
            float phase = Main.rand.NextFloat(MathHelper.TwoPi);
            IEntitySource source = Projectile.GetSource_FromAI();
            for (int i = 0; i < Ways; i++)
            {
                float angle = phase + i * MathHelper.TwoPi / Ways;
                Vector2 velocity = angle.ToRotationVector2() * SeedShot.CrossLaunchSpeed;
                SeedShot.Fire(source, Projectile.Center, velocity, Projectile.damage, bossIndex, SeedShot.CrossLeft);
                SeedShot.Fire(source, Projectile.Center, velocity, Projectile.damage, bossIndex, SeedShot.CrossRight);
            }
        }
    }
    private bool _Pink;

    public override void OnSpawn(IEntitySource source)
    {
        if (TONetworkEnvironment.NotClient)
            _Pink = Main.rand.NextBool();
    }

    public override void SendExtraAI(BinaryWriter writer) => writer.Write(_Pink);

    public override void ReceiveExtraAI(BinaryReader reader) => _Pink = reader.ReadBoolean();

    public override void OnKill(int timeLeft) => ClearParticles.Burst(Projectile.Center, SeedSprites.EdgeColor(_Pink), 12);

    public override bool PreDraw(ref Color lightColor)
    {
        SeedSprites.Draw(Projectile, _Pink, lightColor);
        return false;
    }
}