namespace Anomalies.Bosses.EaterofWorlds;

public sealed partial class EaterofWorldsHead
{
    internal float CoilMaskStrength
    {
        get
        {
            // Keep the original birth cloud duration independent of the teleport interval.
            const int BirthCloudDuration = 270;
            if (CurrentBehavior == Behavior.Phase1_Hunt && CycleIndex == 0)
                return MathHelper.SmoothStep(0f, 1f, Math.Clamp(Timer1 / 18f, 0f, 1f))
                    * MathHelper.SmoothStep(0f, 1f, Math.Clamp((BirthCloudDuration - Timer1) / 30f, 0f, 1f));
            return 0f;
        }
    }

    private void SpawnCoilDust()
    {
        if (Main.dedServ || CoilMaskStrength <= 0f)
            return;
        for (int i = 0; i < 5; i++)
        {
            if (Main.rand.NextFloat() > CoilMaskStrength)
                continue;
            Vector2 radial = Main.rand.NextVector2Unit() * Main.rand.NextFloat(100f, 190f);
            Dust.NewDustPerfectAction(CoilingCenter + radial, Main.rand.NextBool(5) ? DustID.CorruptGibs : DustID.Smoke, dust =>
            {
                dust.velocity = radial.RotatedBy(MathHelper.PiOver2) * 0.012f - radial * 0.006f;
                dust.noGravity = true;
                dust.scale = 2.2f * CoilMaskStrength;
                dust.alpha = 80 + (int)(150f * (1f - CoilMaskStrength));
                dust.color = GetCoilSmokeColor();
            });
        }
    }

    internal void DrawCoilMask(SpriteBatch spriteBatch, Vector2 screenPos)
    {
        float opacity = CoilMaskStrength;
        Texture2D cloud = EaterofWorldsWarningSystem.CoilCloud;
        if (opacity <= 0f || cloud is null)
            return;
        Color smoke = GetCoilSmokeColor();
        float time = Main.GlobalTimeWrappedHourly;
        // Two irregular, feathered smoke layers replace the concentric disc and repeated petal pattern.
        spriteBatch.Draw(cloud, CoilingCenter - screenPos, null, smoke * (opacity * 0.97f),
            time * 0.12f, cloud.Size() * 0.5f, 460f / cloud.Width, SpriteEffects.None, 0f);
        spriteBatch.Draw(cloud, CoilingCenter - screenPos, null,
            Color.Lerp(smoke, new Color(81, 66, 92), 0.22f) * (opacity * 0.97f),
            -time * 0.09f + 1.7f, cloud.Size() * 0.5f, 430f / cloud.Width, SpriteEffects.FlipHorizontally, 0f);
    }

    private Color GetCoilSmokeColor()
    {
        Color ambient = Lighting.GetColor((int)(CoilingCenter.X / 16f), (int)(CoilingCenter.Y / 16f));
        return Color.Lerp(new Color(76, 71, 82), ambient, 0.38f);
    }

    internal void DrawAttackWarnings(SpriteBatch spriteBatch, Vector2 screenPos)
    {
        if (!IsFirstSegment || CurrentPhase == Phase.Initialize)
            return;
        Color color = new(128, 78, 159);
        switch (CurrentBehavior)
        {
            case Behavior.Phase1_Split:
            case Behavior.Phase1_Combine:
                bool combine = CurrentBehavior == Behavior.Phase1_Combine;
                if (CurrentLocalPhase == 1)
                {
                    for (int i = 0; i < (combine ? 1 : 3); i++)
                        Warning(CoilingCenter, CoilLaunchDirection(i, combine), Timer1, PortalChargeDuration,
                            warningColor: color);
                }
                break;
        }

        // Same layered beam as SlimeMount; keep the shared renderer independent of Boss modules.
        // Ease both ends of the warning window and grow the beam without a one-frame length jump.
        void Warning(Vector2 start, Vector2 direction, float elapsed, float duration, float maxLength = 520f, Color? warningColor = null)
        {
            if (direction.LengthSquared() < 0.001f)
                return;
            float growth = Smooth(elapsed / 16f);
            Color tint = warningColor ?? color;
            AnomalyDrawUtils.DrawLayeredWarning(spriteBatch, start, direction.ToRotation(),
                maxLength * growth, MathHelper.Lerp(20f, 60f, growth), tint,
                Color.Lerp(tint, new Color(198, 163, 215), 0.65f), Envelope(elapsed, duration) * 0.85f,
                startFadeLength: 120f, coreWidth: 5f);
        }

        static float Smooth(float value) => MathHelper.SmoothStep(0f, 1f, Math.Clamp(value, 0f, 1f));

        static float Envelope(float elapsed, float duration) => Smooth(elapsed / 10f) * Smooth((duration - elapsed) / 8f);

    }
}
