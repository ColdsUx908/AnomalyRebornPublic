namespace Anomalies.SpecialContents.Developer.EventideReunion;

public sealed class EventideReunionLifeField : AnomalyModProjectile
{
    public override bool IsLoadingEnabled(Mod mod) => AnomalySpecialContentToggler.EventideReunion;
    public override string Texture => EventideReunionRuneAssets.AssetPath + "Life";
    public override string LocalizationCategory => EventideReunion.LocalizationCategoryForEventideReunion;
    public override LocalizedText DisplayName => ModContent.GetModItem<EventideReunion>().DisplayName;
    private int Age => EventideReunionLifeRules.FieldDuration - Projectile.timeLeft;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 2;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = EventideReunionLifeRules.FieldDuration;
        Projectile.netImportant = true;
    }

    public override bool? CanDamage() => false;

    public override void AI()
    {
        if (Main.dedServ)
            return;
        float gravity = Projectile.ai[0] < 0f ? -1f : 1f;
        if (Age < 45 && Age % 2 == 0)
            for (int i = 0; i < 10; i++)
            {
                Vector2 direction = Main.rand.NextVector2Unit();
                ParticleHandler.SpawnParticle(new EventideReunionLifePetal(Projectile.Center - new Vector2(0f, gravity * 32f),
                    new Vector2(direction.X * Main.rand.NextFloat(8f, 20f), -gravity * Main.rand.NextFloat(4f, 12f)), gravity));
            }
    }

    public override bool PreDraw(ref Color lightColor) => false;
}

public sealed class EventideReunionLifePetal : Particle
{
    public override bool AutoLoadTexture => false;
    private readonly float _Gravity;
    private readonly float _Size;

    private bool _Settled;

    public EventideReunionLifePetal(Vector2 position, Vector2 velocity, float gravity)
    {
        Center = position;
        Velocity = velocity;
        Lifetime = EventideReunionLifeRules.FieldDuration;
        Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
        Asset = Main.rand.Next(3) switch
        {
            0 => EventideReunionLifeAssets.LifePetal1,
            1 => EventideReunionLifeAssets.LifePetal2,
            _ => EventideReunionLifeAssets.LifePetal3
        };
        _Gravity = gravity;
        _Size = Main.rand.NextFloat(0.8f, 1.35f);
    }

    public override void Update()
    {
        Vector2 below = Vector2.UnitY * _Gravity * 2f;
        if (_Settled && Collision.IsWorldPointSolid(Center + below))
            return;
        _Settled = false;
        Velocity.X *= 0.986f;
        Velocity.Y = MathHelper.Clamp(Velocity.Y + 0.24f * _Gravity, -14f, 14f);
        if (Velocity.Y * _Gravity > 0f)
        {
            int steps = Math.Max(1, (int)MathF.Ceiling(Velocity.Length() / 2f));
            for (int i = 1; i <= steps; i++)
            {
                Vector2 next = Center + Velocity * (i / (float)steps);
                if (!Collision.IsWorldPointSolid(next) && Collision.IsWorldPointSolid(next + below))
                {
                    Center = next;
                    Velocity = Vector2.Zero;
                    _Settled = true;
                    return;
                }
            }
        }
        Rotation += 0.035f;
    }

    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 drawOffset = default)
    {
        Texture2D texture = Texture;
        float fade = MathHelper.Clamp(LifetimeCompletion * 20f, 0f, 1f) * MathHelper.Clamp((1f - LifetimeCompletion) * 5f, 0f, 1f);
        spriteBatch.Draw(texture, Center + drawOffset - Main.screenPosition, null, Color.White * fade,
            Rotation, texture.Size() * 0.5f, new Vector2(Math.Max(_Settled ? 0.7f : 0.25f, Math.Abs(MathF.Cos(Rotation))), 1f) * _Size,
            SpriteEffects.None, 0f);
        return false;
    }
}
