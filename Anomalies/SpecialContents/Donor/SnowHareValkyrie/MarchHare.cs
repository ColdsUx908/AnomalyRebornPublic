using Terraria.Utilities;

namespace Anomalies.SpecialContents.Donor.SnowHareValkyrie;

public sealed partial class MarchHare : AnomalyModProjectile
{
    private const int Substeps = 4;
    private Vector2 _StepStart;
    private const int Lifetime = 14395 * Substeps; // 约 14400 个原作逻辑帧
    private bool IsRed => Projectile.ai[0] == 1f;
    private int Age => SourceFrame((Lifetime - Projectile.timeLeft) / Substeps);

    public override string LocalizationCategory => SnowHareValkyrie.LocalizationCategoryForSnowHareValkyrie;
    public override bool IsLoadingEnabled(Mod mod) => AnomalySpecialContentToggler.SnowHareValkyrie;

    // Unity TimeManager 的固定步长为 0.00833 秒；Terraria 每 tick 有两个半刻，每个半刻再细分四次碰撞更新。
    internal static int SourceFrame(int halfTicks) => (int)((long)halfTicks * 2500 / 2499);

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = (int)MathF.Ceiling(2f * BodyRadius);
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.friendly = true;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.penetrate = 1;
        Projectile.extraUpdates = 2 * Substeps - 1;
        Projectile.timeLeft = Lifetime;
    }

    public override void AI()
    {
        _StepStart = Projectile.Center;
        int elapsed = Lifetime - Projectile.timeLeft;
        int halfTick = elapsed / Substeps;
        int substep = elapsed % Substeps;
        Vector2 displacement = Vector2.Zero;
        for (int frame = SourceFrame(halfTick); frame < SourceFrame(halfTick + 1); frame++)
        {
            int age = frame + 1;
            if (substep == 0 && !Main.dedServ && (age == BodyStart(IsRed) || age == EarStart))
                ModContent.GetInstance<MarchHareRenderer>().EmitMist(Projectile, IsRed, age == EarStart);
            int period = IsRed ? 40 : 90;
            int cycle = frame / period;
            int phase = frame % period;
            int start = IsRed ? 5 : 15;
            int stop = IsRed ? 30 : 60;
            FastRandom random = new FastRandom((int)Projectile.ai[1]).WithModifier((ulong)cycle);
            float angle = TurnAngle(ref random);
            float peak = IsRed ? 6f + (2f * random.NextFloat() - 1f) : 3f + random.NextFloat() - 0.5f;
            if (phase < start)
            {
                FastRandom previous = new FastRandom((int)Projectile.ai[1]).WithModifier((ulong)(cycle - 1));
                float previousAngle = cycle == 0 ? 270f : TurnAngle(ref previous);
                angle = MathHelper.Lerp(previousAngle, angle, (phase + 1f) / start);
            }
            float speed = peak * (Ramp(start, IsRed ? 10 : 15) - Ramp(stop, IsRed ? 10 : 30));
            // 原作 Dcos/Dsin 朝下为 270 度，以 1/25 度查表。世界坐标的 Y 轴方向相反。
            Projectile.rotation = MathHelper.ToRadians(270f - angle);
            float heading = MathHelper.ToRadians(-MathF.Floor(angle * 25f) / 25f);
            displacement += heading.ToRotationVector2() * (speed * MovementScale);

            float Ramp(int first, int duration) => Math.Clamp((phase - first + 1f) / duration, 0f, 1f);
        }
        Projectile.velocity = displacement;
        float fade = Math.Min((Projectile.ai[2] - Projectile.Center.Y) / 320f, Projectile.timeLeft / (36f * Substeps));
        Projectile.Opacity = MathHelper.SmoothStep(0f, 1f, Math.Clamp(fade, 0f, 1f));
        if (Projectile.Center.Y > Projectile.ai[2])
        {
            Projectile.timeLeft = 0;
            Projectile.Kill();
        }

        float TurnAngle(ref FastRandom random) => 270f + (2f * random.NextFloat() - 1f) * (IsRed ? 5f : 12f);
    }

    public override bool? CanDamage() => Age < BodyStart(IsRed) ? false : null;

    private ProjectileDamageBounds DamageBounds => ProjectileDamageBounds.Swept(_StepStart, Projectile.Center, BodyRadius);

    public override bool? CanHitNPC(NPC target) => DamageBounds.CanHitNPC(Type, target);

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        Vector2 end = Projectile.Center;
        if (DamageBounds.Excludes(targetHitbox))
            return false;
        if (new Circle(end, BodyRadius).Collides(targetHitbox))
            return true;
        Vector2 travel = end - _StepStart;
        if (travel.LengthSquared() <= 0.000001f || Vector2.DistanceSquared(travel, Projectile.velocity) > 0.001f)
            return false;
        // 平移圆扫过的区域为两端圆与中间矩形的并集。
        return new Circle(_StepStart, BodyRadius).Collides(targetHitbox)
            || RotatedRectangle.FromLineSegment(_StepStart, end, 2f * BodyRadius).Collides(targetHitbox);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        ModContent.GetInstance<MarchHareRenderer>().Submit(this);
        return false;
    }

    internal void Draw(SpriteBatch batch, Texture2D atlas) =>
        DrawHare(batch, atlas, Projectile.Center - Main.screenPosition, Projectile.rotation, Age, IsRed, Projectile.Opacity);

    public override void OnKill(int timeLeft)
    {
        if (Main.dedServ)
            return;
        Color color = IsRed ? new Color(255, 118, 126) : Color.White;
        for (int i = 0; i < 9; i++)
        {
            ParticleHandler.SpawnParticle(new OrbParticle(Projectile.Center, Main.rand.NextPolarVector2(2.3f, 4.8f),
                Main.rand.Next(18, 25), Main.rand.NextFloat(0.3f, 0.5f), color, lifeEndRatio: 0.3f));
        }
    }
}
