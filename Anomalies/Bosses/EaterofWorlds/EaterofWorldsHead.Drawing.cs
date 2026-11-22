namespace Anomalies.Bosses.EaterofWorlds;

public sealed partial class EaterofWorldsHead
{
    internal float CoilMaskStrength
    {
        get
        {
            const int BirthCloudDuration = HuntDuration + UppercutDuration;
            int birthAge = CurrentLocalPhase == 0 ? Timer1 : HuntDuration + Timer1;
            if (CurrentPhase == Phase.Phase1 && CurrentBehavior == Behavior.Phase1_Hunt && CycleIndex == 0)
                return MathHelper.SmoothStep(0f, 1f, Math.Clamp(birthAge / 18f, 0f, 1f))
                    * MathHelper.SmoothStep(0f, 1f, Math.Clamp((BirthCloudDuration - birthAge) / 30f, 0f, 1f));
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
        if (!IsFirstSegment)
            return;
        switch (CurrentBehavior)
        {
            case Behavior.Phase1_Split when CurrentLocalPhase == 1:
                for (int i = 0; i < RegularHeadCount; i++)
                    DrawLane(CoilingCenter, PortalExitDirection(i), OutwardChargeDistance, PortalExitDistance,
                        Timer1, PortalChargeDuration - Timer1, true, false);
                break;
            case Behavior.Phase1_Charge:
                foreach (NPC head in GetEncounterHeads())
                {
                    EaterofWorldsHead worm = GetInstance(head);
                    int rank = RegularRank(worm.SegmentIndex);
                    if (rank >= 2)
                        continue;
                    bool locked = CurrentLocalPhase == 1;
                    Vector2 origin = locked ? worm.LaunchOrigin : head.Center;
                    Vector2 focus = locked ? AttackCenter : Target.Center;
                    Vector2 direction = locked ? worm.LaunchDirection : (focus - origin).SafeNormalize(Vector2.UnitY);
                    int remaining = locked ? LockDuration + rank * ChargeDuration - Timer1 : AimDuration;
                    float length = locked ? LockedChargeSpeed(origin) * ChargeDuration : ChargeSpeed * ChargeDuration;
                    DrawLane(origin, direction, length, Vector2.Distance(origin, focus),
                        Timer1, remaining, locked, false);
                }
                break;
            case Behavior.ShortWormFormation when CurrentLocalPhase is 0 or 1:
                foreach (NPC head in GetEncounterHeads())
                {
                    if (head == NPC)
                        continue;
                    EaterofWorldsHead worm = GetInstance(head);
                    bool locked = CurrentLocalPhase == 1;
                    int remaining = locked ? -WaveAge(ShortIndex(worm.SegmentIndex)) : FormationDuration;
                    DrawLane(worm.LaunchOrigin, worm.LaunchDirection, WaveSpeed * WaveTravelDuration, WaveApproachDistance,
                        Timer1, remaining, locked, true);
                }
                break;
        }

        // Thin violet filaments cross at the aimed point. Only the luminous spine is the actual route.
        void DrawLane(Vector2 start, Vector2 direction, float length, float focusDistance,
            int elapsed, int remaining, bool locked, bool shortWorm)
        {
            if (remaining <= 0 || direction.LengthSquared() < 0.5f)
                return;
            Vector2 normal = new(-direction.Y, direction.X);
            float focus = Math.Clamp(focusDistance, 64f, length - 64f);
            float appear = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp((elapsed + 1f) / 10f, 0f, 1f));
            float fade = MathHelper.Clamp(remaining / 5f, 0f, 1f);
            float nextBeat = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp((60f - remaining) / 20f, 0f, 1f));
            float opacity = (locked ? MathHelper.Lerp(0.46f, 1f, nextBeat) : 0.48f * appear) * fade;
            float tighten = locked ? MathHelper.Clamp(elapsed / 16f, 0f, 1f) : 0f;
            float spread = (shortWorm ? 8f : 24f) * MathHelper.Lerp(1.4f, 0.65f, tighten);
            Vector2 end = start + direction * length;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Texture2D glow = AnomalyTextures.Extra.LaserTelegraphBeam ?? pixel;
            Color violet = new(155, 65, 225);
            Color lilac = new(230, 187, 255);
            float haloWidth = shortWorm ? 11f : 17f;
            for (int layer = 0; layer < 6; layer++)
            {
                float inner = layer / 5f;
                Stroke(start, end, MathHelper.Lerp(haloWidth, 3f, inner), glow,
                    violet * (opacity * MathHelper.Lerp(0.025f, 0.14f, inner * inner)));
            }
            int pairs = shortWorm ? 1 : 2;
            for (int i = -pairs; i <= pairs; i++)
            {
                float slope = i * Math.Min(spread / focus, 0.055f);
                Vector2 a = start + normal * (slope * focus);
                Vector2 b = end - normal * (slope * (length - focus));
                Stroke(a, b, i == 0 ? 1.8f : 1.15f, pixel,
                    (i == 0 ? lilac : new Color(190, 120, 249)) * (opacity * (i == 0 ? 0.96f : 0.66f)));
            }
            Vector2 intersection = start + direction * focus;
            Stroke(intersection - direction * 16f, intersection + direction * 16f, 2.8f, glow, lilac * (opacity * 0.5f));
            float pulse = (elapsed % 30) / 30f;
            Vector2 glint = start + direction * (focus * pulse);
            Stroke(glint, glint + direction * 23f, 3f, glow, lilac * (opacity * (1f - pulse)));

            void Stroke(Vector2 a, Vector2 b, float width, Texture2D texture, Color color)
            {
                Vector2 edge = b - a;
                spriteBatch.Draw(texture, a - screenPos, null, color, edge.ToRotation(),
                    new Vector2(0f, texture.Height * 0.5f), new Vector2(edge.Length() / texture.Width, width / texture.Height),
                    SpriteEffects.None, 0f);
            }
        }
    }
}
