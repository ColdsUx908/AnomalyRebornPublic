namespace Anomalies.SpecialContents.Developer.EventideReunion;

public sealed partial class EventideReunionStarSkill
{
    private readonly List<(Vector2 Position, float Size, Color Tint)> _Sparks = new(30);

    private float SpearAngle => Direction.ToRotation();
    private Vector2 Core => Age < Launch ? Projectile.Owner.MountedCenter + TipOffset(SpearAngle) : Projectile.Center;
    private static readonly Color _Violet = new(130, 67, 215);
    private static readonly Color _Ice = new(155, 209, 255);

    private void PlayCues()
    {
        if (Age is 0 or 24 or 48)
            SoundEngine.PlaySound(SoundID.Item4 with { Pitch = -0.45f + Age / 64f, Volume = 0.5f }, Core);
        if (Age == Launch || Age == Arrival)
            SoundEngine.PlaySound((Age == Launch ? SoundID.Item71 : SoundID.Item29) with { Volume = 0.65f }, Core);
        if (Age == Burst)
            SoundEngine.PlaySound(SoundID.Item14 with { Pitch = -0.4f }, Core);
        if (Age - Burst is 24 or 36 or 48)
            SoundEngine.PlaySound(SoundID.Item4 with { Pitch = (Age - Burst) / 60f, Volume = 0.45f }, Projectile.Owner.Center);
        if (Age < Burst)
            Lighting.AddLight(Core, 0.23f, 0.12f, 0.46f);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Vector2 center = Core;
        Texture2D glow = BloomParticle.BloomCircleLarge;
        Texture2D ring = AnomalyTextures.Extra.BloomRing;
        void Draw(Texture2D texture, Vector2 at, Color color, float angle, float width, float height)
        {
            Main.EntitySpriteDraw(texture, at - Main.screenPosition, null, color, angle, texture.Size() * 0.5f,
                new Vector2(width / texture.Width, height / texture.Height), SpriteEffects.None);
        }
        _Sparks.Clear();

        if (Age < Recovery)
            Main.EntitySpriteDraw(EventideReunionAssets.Weapon, Projectile.Owner.MountedCenter - Main.screenPosition, null,
                Color.White, SpearAngle + MathHelper.PiOver4, _WeaponGrip, 1f, SpriteEffects.None);

        if (Age < Burst)
        {
            float collapse = MathHelper.Clamp((Age - Arrival - 48f) / 24f, 0f, 1f);
            float charge = Math.Min(Age / (float)Launch, 1f);
            float size = (16f + 48f * charge + 24f * MathHelper.SmoothStep(0f, 1f, (Age - Launch) / 8f)) * (1f - 0.88f * collapse);
            using (SpriteBatchScope emission = new(Main.spriteBatch, blendState: BlendState.Additive, samplerState: SamplerState.LinearClamp))
            {
                if (Age >= Launch && Age < Arrival)
                {
                    for (int i = 8; i > 0; i--)
                    {
                        Vector2 past = FlightPosition(Math.Max(0f, Age - Launch - i * 1.5f));
                        float strength = (1f - i / 9f) * MathHelper.Clamp(Vector2.Distance(past, center) / size, 0f, 1f);
                        Draw(glow, past, Color.Lerp(_Ice, _Violet, i / 8f) * (strength * 0.65f),
                            SpearAngle, size * 1.7f, size * (1f - i / 12f));
                    }
                }
                if (Age < Burst - 12)
                {
                    for (int i = 0; i < 28; i++)
                    {
                        float progress = (Age / 51f + i * 0.618034f) % 1f;
                        float angle = i * 2.4f + progress * 2.8f;
                        Vector2 orbit = new Vector2(MathF.Cos(angle), MathF.Sin(angle) * 0.6f).RotatedBy(-0.35f);
                        Draw(glow, center + orbit * (size + (1f - progress) * size * 2.5f), _Ice * MathF.Sin(MathHelper.Pi * progress),
                            angle, 10f, 4f);
                    }
                    Draw(ring, center, _Violet, -0.35f, size * 2.7f, size * 0.85f);
                }
                Draw(glow, center, _Violet, 0f, size * 2f, size * 2f);
            }
            EventideReunionEffects.DrawPhotosphere(center, size * 2.3f, Age / 60f, collapse, 1f, solar: false);
            _Sparks.Add((center, size * 0.46f, Color.White));
        }
        else
        {
            int elapsed = Age - Burst;
            if (!DrawShockwave(elapsed) && elapsed < WaveDuration)
            {
                float diameter = WaveRadius(elapsed) * 2f;
                using SpriteBatchScope emission = new(Main.spriteBatch, blendState: BlendState.Additive, samplerState: SamplerState.LinearClamp);
                Draw(ring, center, _Ice * (1f - elapsed / (float)WaveDuration), 0f, diameter, diameter);
            }
            if (elapsed < 48)
                _Sparks.Add((center, 180f * MathF.Exp(-elapsed / 10f), Color.White));
            for (int i = 0; i < 3 && elapsed < 48; i++)
            {
                Vector2 path = EventideReunionRecall.Position(center, Projectile.Owner.MountedCenter, elapsed, i, out float t);
                _Sparks.Add((path, 18f, Color.Lerp(_Ice, Color.White, t)));
            }
        }
        using SpriteBatchScope stars = new(Main.spriteBatch, blendState: BlendState.Additive, samplerState: SamplerState.LinearClamp);
        Texture2D flare = EventideReunionAssets.StarFlare;
        foreach ((Vector2 position, float size, Color tint) in _Sparks)
        {
            Color halo = Color.Lerp(_Violet, tint, 0.35f) * (0.38f * tint.A / 255f);
            Draw(glow, position, halo, 0f, size * 2.6f, size * 2.6f);
            Color ray = Color.Lerp(_Ice, tint, 0.6f) * (0.68f * tint.A / 255f);
            for (int axis = 0; axis < 2; axis++)
                Draw(flare, position, ray, axis * MathHelper.PiOver2, size * 1.15f, size * 3.8f);
        }
        return false;
    }

    private bool DrawShockwave(int elapsed)
    {
        if (!AnomalyClientConfig.Instance.EnableShaders || EventideReunionEffects.EventideStarShockwave is not { } shockwave
            || EventideReunionEffects.EventideStarNebula is not { } nebula)
            return false;

        float radius = WaveRadius(elapsed);


        const float Padding = 1.65f;
        float lifetime = End - Burst;
        float progress = Math.Min(elapsed / 71f, 1f);
        float expansion = 1f - MathF.Pow(1f - progress, 3f);
        float baseRadius = expansion > 0f ? radius / expansion : 0f;
        float remnant = Math.Clamp((elapsed - 31f) / (lifetime - 32f), 0f, 1f);
        float rimOpacity = 1f - MathHelper.SmoothStep(0f, 1f, elapsed / (float)WaveDuration);
        float drift = Math.Clamp((elapsed - 52f) / (lifetime - 53f), 0f, 1f);
        Vector4 shell = new(remnant, 1f, MathHelper.SmoothStep(0f, 1f, (remnant - 0.12f) / 0.87f),
            1f - MathHelper.SmoothStep(0f, 1f, (remnant - 0.15f) / 0.85f));
        Vector3 interior = new(drift, MathHelper.SmoothStep(0f, 1.04f, drift * drift),
            1f - MathHelper.SmoothStep(0f, 1f, (drift - 0.25f) / 0.75f));
        using SpriteBatchScope scope = new(Main.spriteBatch, SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp);
        Texture2D pixel = TOTextures.FillerTexture;
        float scale = baseRadius * 2f * Padding / pixel.Width;
        Vector2 position = Projectile.Center - Main.screenPosition;
        nebula.Data.SetCustomParameter("uGlobalTime", elapsed / 60f)
            .SetCustomParameter("uExpansionRatio", 1f)
            .SetCustomParameter("uOuterGeometry", new Vector2(1f, Padding))
            .SetCustomParameter("uRemnantState", shell)
            .SetCustomParameter("uInteriorState", interior)
            .SetCustomParameter("uYellowPalette", 0f)
            .SetCustomParameter("uNoiseTexture", AnomalyTextures.Noise.Perlin2)
            .Apply();
        nebula.Asset.Value.CurrentTechnique.Passes["Pass0"].Apply();
        Main.spriteBatch.DrawFromCenter(pixel, position, null, Color.White, scale: Math.Max(32f, radius) * 2f * Padding / pixel.Width);
        if (radius > 0f && rimOpacity > 0f)
        {
            shockwave.Data.SetCustomParameter("uGlobalTime", elapsed / 60f)
                .SetCustomParameter("uLifetimeRatio", progress)
                .SetCustomParameter("uExpansionRatio", expansion)
                .SetCustomParameter("uOuterGeometry", new Vector2(expansion, Padding))
                .SetCustomParameter("uNoiseTexture", AnomalyTextures.Noise.Perlin2)
                .SetCustomParameter("uTextureSize", new Vector2(baseRadius * 2f))
                .Apply();
            shockwave.Asset.Value.CurrentTechnique.Passes["Pass0"].Apply();
            Main.spriteBatch.DrawFromCenter(pixel, position, null, Color.White * rimOpacity, scale: scale);
        }
        return true;
    }
}
