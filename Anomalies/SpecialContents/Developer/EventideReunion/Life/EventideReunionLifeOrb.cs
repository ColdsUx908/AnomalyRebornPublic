namespace Anomalies.SpecialContents.Developer.EventideReunion;

public sealed class EventideReunionLifeOrb : AnomalyModProjectile
{
    public override bool IsLoadingEnabled(Mod mod) => AnomalySpecialContentToggler.EventideReunion;
    public override string Texture => EventideReunionAssets.WeaponPath;
    public override string LocalizationCategory => EventideReunion.LocalizationCategoryForEventideReunion;
    public override LocalizedText DisplayName => ModContent.GetModItem<EventideReunion>().DisplayName;

    // ai[0] 为施法者，ai[1] 为大球的重力方向（小球为零），ai[2] 为飞行帧数。
    private readonly EventideReunionLifeOrbTrail _Trail = new();
    private bool Parent => Projectile.ai[1] != 0f;
    private const int SplitStart = 16;
    private const int SplitInterval = 3;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 16;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.netImportant = true;
        Projectile.timeLeft = 600;
    }

    public override bool? CanDamage() => false;

    public override void AI()
    {
        int caster = (int)Projectile.ai[0];
        if ((uint)caster >= Main.maxPlayers || !Main.player[caster].active || Main.player[caster].dead)
        {
            Projectile.Kill();
            return;
        }
        int age = (int)Projectile.ai[2]++;
        if (Parent)
        {
            Projectile.velocity *= 0.945f;
            int elapsed = age - SplitStart;
            if (elapsed >= 0 && elapsed % SplitInterval == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int piece = elapsed / SplitInterval;
                // 先交错释放三枚果实和三颗小球，再释放最后两颗小球。
                bool fruit = piece < EventideReunionLifeRules.FruitCount * 2 && piece % 2 == 0;
                Vector2 velocity = fruit
                    ? (MathHelper.PiOver2 + piece / 2 * MathHelper.TwoPi / EventideReunionLifeRules.FruitCount).ToRotationVector2() * 10f
                    : (piece * 2.4f).ToRotationVector2() * 8.5f;
                if (fruit)
                    Projectile.NewProjectileAction<EventideReunionLifeFruit>(Projectile.GetSource_FromThis(),
                        Projectile.Center, velocity, 0, 0f, Main.myPlayer, p => { p.ai[0] = caster; p.ai[2] = piece; });
                else
                    Projectile.NewProjectileAction<EventideReunionLifeOrb>(Projectile.GetSource_FromThis(),
                        Projectile.Center, velocity, 0, 0f, Main.myPlayer, p => p.ai[0] = caster);
                if (piece == EventideReunionLifeRules.OrbCount + EventideReunionLifeRules.FruitCount - 1)
                    Projectile.Kill();
            }
        }
        else
        {
            Player player = Main.player[caster];
            float spin = (Projectile.identity % 2 == 0 ? 1f : -1f) * 0.016f;
            if (age <= 8)
                Projectile.velocity = Projectile.velocity.RotatedBy(spin);
            else if (age <= 20)
                Projectile.velocity = Projectile.velocity.RotatedBy(spin) * ((20 - age) / (21f - age));
            else if (age > 23)
            {
                Vector2 delta = player.Center - Projectile.Center;
                float speed = Math.Min(24f, 4f + (age - 23) * 0.6f);
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, delta.SafeNormalize(Vector2.Zero) * speed, 0.18f);
                // 连续检测高速小球本帧的最近点，避免越过移动中的接收者。
                float along = MathHelper.Clamp(Vector2.Dot(delta, Projectile.velocity) / Math.Max(1f, Projectile.velocity.LengthSquared()), 0f, 1f);
                if ((delta - along * Projectile.velocity).LengthSquared() <= 18f * 18f && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectileAction<EventideReunionLifeHeal>(Projectile.GetSource_FromThis(),
                        player.Center, Vector2.Zero, 0, 0f, caster);
                    Projectile.Kill();
                }
            }
        }
        if (!Main.dedServ)
            _Trail.Record(Projectile.Center);
    }

    internal static void DrawHalo(Vector2 center, float diameter, float opacity)
    {
        if (EventideReunionLifeAssets.GlowDot?.Value is Texture2D glow)
            Main.spriteBatch.Draw(glow, center - Main.screenPosition, null, new Color(14, 226, 109, 0) * opacity,
                0f, glow.Size() * 0.5f, diameter / glow.Width, SpriteEffects.None, 0f);
    }

    internal static void DrawBall(Vector2 center, float radius)
    {
        if (radius <= 0f || AnomalyTextures.Extra.BasicCircleLarge is not Texture2D circle)
            return;
        DrawHalo(center, radius * 4.5f, 0.9f);
        Main.spriteBatch.Draw(circle, center - Main.screenPosition, null, new Color(16, 182, 96),
            0f, circle.Size() * 0.5f, radius * 2f / circle.Width, SpriteEffects.None, 0f);
        if (EventideReunionLifeAssets.GlowDot?.Value is Texture2D glow)
            Main.spriteBatch.Draw(glow, center - Main.screenPosition + new Vector2(-0.22f, -0.28f) * radius,
                null, new Color(128, 255, 181, 0), 0f, glow.Size() * 0.5f, radius * 2.2f / glow.Width, SpriteEffects.None, 0f);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        float shed = MathHelper.Clamp((Projectile.ai[2] - SplitStart) / (SplitInterval * 7f), 0f, 1f);
        _Trail.Draw(Projectile.Center, Parent ? 17f : 9f);
        DrawBall(Projectile.Center, Parent ? MathHelper.Lerp(25f, 6f, shed) : 6f);
        return false;
    }
}

internal sealed class EventideReunionLifeOrbTrail
{
    private readonly Vector2[] _Positions = new Vector2[12];
    private int _Count;

    internal void Record(Vector2 center)
    {
        Array.Copy(_Positions, 0, _Positions, 1, _Positions.Length - 1);
        _Positions[0] = center;
        _Count = Math.Min(_Count + 1, _Positions.Length);
    }

    internal void Draw(Vector2 center, float width)
    {
        if (EventideReunionLifeAssets.GlowDot?.Value is not Texture2D glow)
            return;
        for (int i = 0; i < _Count; i++)
        {
            Vector2 older = _Positions[i];
            Vector2 segment = center - older;
            float strength = 1f - i / (float)_Positions.Length;
            float thickness = width * strength;
            Main.spriteBatch.Draw(glow, (center + older) * 0.5f - Main.screenPosition, null,
                new Color(20, 235, 125, 0) * (strength * strength * 0.7f), segment.ToRotation(), glow.Size() * 0.5f,
                new Vector2(segment.Length() + thickness * 2f, thickness) / glow.Size(), SpriteEffects.None, 0f);
            center = older;
        }
    }
}
