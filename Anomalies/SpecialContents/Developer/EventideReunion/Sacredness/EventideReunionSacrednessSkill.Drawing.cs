namespace Anomalies.SpecialContents.Developer.EventideReunion;

public sealed partial class EventideReunionSacrednessSkill
{
    private static readonly Color _Gold = new(255, 218, 137);
    private static readonly Vector2 _Grip = new(144f, 156f);

    private float ArmAngle(Player player)
    {
        float up = -MathHelper.PiOver2 * player.gravDir;
        if (Age < DashStart)
            return up + 0.12f * player.direction * player.gravDir;
        if (IsDashing)
            return Aim.ToRotation();
        if (Age < BeamStart)
            return Aim.ToRotation() + MathHelper.WrapAngle(-MathHelper.PiOver2 - Aim.ToRotation())
                * MathHelper.SmoothStep(0f, 1f, (Age - ThrowStart) / 8f);
        return up + player.direction * player.gravDir * 0.65f;
    }

    private void PlayCues()
    {
        if (Age is 1 or DashStart or ThrowStart or BeamStart or RecallStart)
        {
            SoundStyle sound = Age switch
            {
                DashStart => SoundID.Item71,
                ThrowStart => SoundID.Item1,
                BeamStart => SoundID.Item122,
                _ => SoundID.Item4
            };
            SoundEngine.PlaySound(sound with { Volume = 0.7f }, Age >= BeamStart ? _Impact : Projectile.Owner.Center);
        }
        if (IsDashing || IsBeaming)
        {
            Vector2 center = IsDashing ? Projectile.Center : _Impact;
            Lighting.AddLight(center, 0.9f, 0.76f, 0.5f);
            for (int i = 0; i < 3; i++)
            {
                Vector2 offset = IsDashing ? -Aim * Main.rand.NextFloat(56f) : Vector2.UnitX * Main.rand.NextFloat(-BeamWidth * 0.5f, BeamWidth * 0.5f);
                Dust.NewDustPerfectAction(center + offset, DustID.RainbowMk2, dust =>
                {
                    dust.noGravity = true;
                    dust.color = Color.Lerp(_Gold, Color.White, Main.rand.NextFloat());
                    dust.velocity = (IsDashing ? -Aim : -Vector2.UnitY).RotatedByRandom(0.55f) * Main.rand.NextFloat(2f, 8f);
                    dust.scale = Main.rand.NextFloat(0.8f, 1.35f);
                });
            }
        }
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Player player = Projectile.Owner;
        Vector2 hand = player.MountedCenter;
        if (Age < DashStart)
            DrawHalo(hand, MathHelper.SmoothStep(0f, 1f, Age / 18f));
        if (Age < BeamStart)
        {
            Vector2 grip = hand;
            float angle = ArmAngle(player);
            if (Age >= ThrowStart)
            {
                float t = (Age - ThrowStart) / (float)(BeamStart - ThrowStart);
                if (t < 0.65f)
                    grip = _DashEnd - Vector2.UnitY * (560f * MathF.Pow(t / 0.65f, 2f));
                else
                {
                    float fall = (t - 0.65f) / 0.35f;
                    angle = MathHelper.PiOver2;
                    Vector2 tip = Vector2.Lerp(_DashEnd - Vector2.UnitY * 560f, _Impact, fall * fall);
                    grip = tip - Vector2.UnitY * SpearLength;
                    DrawCross(tip, 18f + 24f * fall, fall);
                }
                DrawSprite(AnomalyTextures.Extra.BloomRing, _Impact, _Gold with { A = 0 }, 0f,
                    new Vector2(BeamWidth * (1.35f - t * 0.35f), 24f), t);
            }
            Main.EntitySpriteDraw(EventideReunionAssets.Weapon, grip - Main.screenPosition, null, Color.White,
                angle + MathHelper.PiOver4, _Grip, 1f, SpriteEffects.None);
        }
        if (IsDashing || Age is >= ThrowStart and < ThrowStart + 8)
        {
            Vector2 head = IsDashing ? Projectile.Center : _DashEnd;
            float trail = Math.Min(380f, Vector2.Distance(_DashOrigin, head));
            float opacity = IsDashing ? 1f : 1f - (Age - ThrowStart) / 8f;
            DrawLight(head - Aim * (trail * 0.5f), Aim.ToRotation(),
                new Vector2(trail + 144f, 164f), opacity, trail);
            for (int i = 1; i <= 6; i++)
            {
                float fraction = i / 7f;
                float phase = Age * 0.35f + i * 1.9f;
                Vector2 normal = new(-Aim.Y, Aim.X);
                Vector2 point = head - Aim * (trail * fraction) + normal * (MathF.Sin(phase) * 47f * (1f - fraction));
                DrawCross(point, 7f + 8f * (1f + MathF.Cos(phase)), opacity * (1f - fraction));
            }
        }
        if (IsBeaming)
        {
            DrawLight(_Impact, 0f, new Vector2(BeamWidth * 2f, (BeamHalfHeight + BeamCapLength) * 2f), 1f);
            float flare = 58f + 26f * MathF.Exp(-(Age - BeamStart) / 8f) + Math.Max(0, Age - RecallStart + 10) * 5f;
            DrawCross(_Impact, flare, 1f);
        }
        if (Age >= RecallStart)
        {
            int elapsed = Age - RecallStart;
            for (int i = 0; i < 3; i++)
            {
                Vector2 point = EventideReunionRecall.Position(_Impact, hand, elapsed, i, out float progress);
                DrawCross(point, MathHelper.Lerp(29f, 12f, progress), 1f - 0.65f * progress);
            }
        }
        return false;
    }

    private void DrawLight(Vector2 center, float angle, Vector2 size, float opacity, float trail = -1f)
    {
        bool dash = trail >= 0f;
        if (!AnomalyClientConfig.Instance.EnableShaders || EventideReunionEffects.EventideSacredLight is null)
        {
            Texture2D bloom = BloomParticle.BloomCircleLargeEmission;
            if (dash)
            {
                DrawSprite(bloom, center, _Gold with { A = 0 }, angle, size, opacity);
                DrawSprite(bloom, center, Color.White with { A = 0 }, angle, size * new Vector2(1f, 0.65f), opacity);
            }
            else
            {
                DrawBeamBloom(bloom, center, size.X, _Gold with { A = 0 });
                DrawBeamBloom(bloom, center, size.X * 0.65f, Color.White with { A = 0 });
            }
            return;
        }
        using SpriteBatchScope scope = new(Main.spriteBatch, SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp);
        Effect effect = EventideReunionEffects.EventideSacredLight.Asset.Value;
        effect.Parameters["uExtent"].SetValue(size);
        effect.Parameters["uTime"].SetValue(Age / 60f);
        effect.Parameters["uTrail"].SetValue(Math.Max(0f, trail));
        effect.CurrentTechnique.Passes[dash ? "Dash" : "Pass0"].Apply();
        DrawSprite(TOTextures.FillerTexture, center, Color.White, angle, size, opacity);
    }

    private static void DrawBeamBloom(Texture2D bloom, Vector2 center, float width, Color color)
    {
        // 中心扫描线沿整条光柱保持相同亮度，两端接纹理各自的半圆。
        float x = center.X - Main.screenPosition.X - width * 0.5f;
        float top = center.Y - Main.screenPosition.Y - BeamHalfHeight;
        int middle = bloom.Height / 2;
        DrawSlice(0, middle, top - BeamCapLength, BeamCapLength);
        DrawSlice(middle, 1, top, BeamHalfHeight * 2f);
        DrawSlice(middle, bloom.Height - middle, top + BeamHalfHeight * 2f, BeamCapLength);

        void DrawSlice(int row, int rows, float y, float height) =>
            Main.spriteBatch.Draw(bloom, new Vector2(x, y), new Rectangle(0, row, bloom.Width, rows), color,
                0f, Vector2.Zero, new Vector2(width / bloom.Width, height / rows), SpriteEffects.None, 0f);
    }

    private void DrawHalo(Vector2 center, float expansion)
    {
        Texture2D ring = AnomalyTextures.Extra.BloomRing;
        for (int layer = 0; layer < 2; layer++)
        {
            float rotation = (layer == 0 ? -0.52f : 0.66f) + 0.12f * MathF.Sin(Age * 0.07f);
            float radius = (layer == 0 ? 75f : 58f) * expansion;
            Vector2 axes = new(radius, radius * 0.34f);
            DrawSprite(ring, center, _Gold with { A = 0 }, rotation, axes * 2f, expansion);
            for (int i = 0; i < 8; i++)
            {
                float phase = i * MathHelper.PiOver4 + Age * (layer == 0 ? 0.065f : -0.055f);
                Vector2 point = center + (phase.ToRotationVector2() * axes).RotatedBy(rotation);
                float depth = (MathF.Sin(phase) + 1f) * 0.5f;
                DrawCross(point, (5f + depth * 6f) * expansion, 0.4f + depth * 0.6f);
            }
        }
    }

    private static void DrawCross(Vector2 position, float size, float opacity)
    {
        DrawSprite(BloomParticle.BloomCircleLargeEmission, position, _Gold with { A = 0 }, 0f, new Vector2(size * 2.7f), opacity * 0.65f);
        for (int i = 0; i < 2; i++)
            DrawSprite(EventideReunionAssets.StarFlare, position, Color.White with { A = 0 }, i * MathHelper.PiOver2,
                new Vector2(size * 0.65f, size * 2.4f), opacity);
    }

    private static void DrawSprite(Texture2D texture, Vector2 center, Color color, float angle, Vector2 size, float opacity) =>
        Main.EntitySpriteDraw(texture, center - Main.screenPosition, null, color * opacity, angle,
            texture.Size() * 0.5f, size / texture.Size(), SpriteEffects.None);
}
