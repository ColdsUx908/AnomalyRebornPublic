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
                    DrawLane(NPC, CoilingCenter, PortalExitDirection(i), OutwardChargeDistance, PortalChargeDuration - Timer1, true);
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
                    DrawLane(head, origin, direction, length, remaining, locked);
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
                    DrawLane(head, worm.LaunchOrigin, worm.LaunchDirection, WaveSpeed * WaveTravelDuration, remaining, locked);
                }
                break;
        }

        void DrawLane(NPC attacker, Vector2 start, Vector2 direction, float length, int remaining, bool locked)
        {
            if (remaining <= 0 || direction.LengthSquared() < 0.5f)
                return;
            float width = ChargeWidth(attacker, direction);
            float angle = direction.ToRotation();
            float appear = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp((Timer1 + 1f) / 10f, 0f, 1f));
            float fade = MathHelper.Clamp(remaining / 5f, 0f, 1f);
            float nextBeat = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp((60f - remaining) / 20f, 0f, 1f));
            float opacity = (locked ? MathHelper.Lerp(0.54f, 1f, nextBeat) : 0.58f * appear) * fade;
            if (EaterofWorldsWarningSystem.ChargeChannel is Texture2D texture)
            {
                // LinearWrap tiles the grain in world pixels. Both rails remain parallel through the target.
                Rectangle source = new((-Timer1 * 2) % texture.Width, 0, Math.Max(1, (int)MathF.Ceiling(length)), texture.Height);
                spriteBatch.Draw(texture, start - screenPos, source, Color.White * opacity, angle,
                    new Vector2(0f, texture.Height * 0.5f), new Vector2(length / source.Width, width / EaterofWorldsWarningSystem.ChannelRailSpacing),
                    SpriteEffects.None, 0f);
                return;
            }
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Vector2 normal = new(-direction.Y, direction.X);
            Vector2 origin = new(0f, pixel.Height * 0.5f);
            spriteBatch.Draw(pixel, start - screenPos, null, new Color(147, 91, 178) * (0.18f * opacity), angle,
                origin, new Vector2(length / pixel.Width, width / pixel.Height), SpriteEffects.None, 0f);
            for (int side = -1; side <= 1; side += 2)
                spriteBatch.Draw(pixel, start - screenPos + normal * (side * width * 0.5f), null,
                    new Color(221, 164, 237) * (opacity * 0.8f), angle, origin,
                    new Vector2(length / pixel.Width, 2f / pixel.Height), SpriteEffects.None, 0f);
        }
    }
}
