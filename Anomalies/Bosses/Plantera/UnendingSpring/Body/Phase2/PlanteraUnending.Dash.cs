using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Anomalies.Bosses.Plantera.UnendingSpring;

public sealed partial class PlanteraUnending
{
    private enum DashStage : byte { Idle, ApproachEdge, Aim, Charge, Recovery }
    internal const int AimTicks = 60;
    internal const float DashSpeed = 36f;
    internal const float HalfFanAngle = MathHelper.Pi / 20f;
    private const int ExtraStandoffTicks = 2 * 60;
    private const int ExitTicks = 30;
    private const int DashCount = 5;
    internal const int LoopTicks = 12;
    protected override int AI32Capacity => 25;
    protected override int AI64Capacity => 7;

    private DashStage CurrentDashStage
    {
        get => (DashStage)CurrentLocalPhase;
        set { NPC.ai[1] = (int)value; Timer = 0; NPC.netUpdate = true; }
    }
    private int DashAge => (int)Timer;
    private int DashNumber
    {
        get => AnomalyNPC.AnomalyAI32[0].I;
        set { AnomalyNPC.AnomalyAI32[0].I = value; AnomalyNPC.AIChanged32[0] = true; }
    }

    private int TravelTicks
    {
        get => AnomalyNPC.AnomalyAI32[1].I;
        set { AnomalyNPC.AnomalyAI32[1].I = value; AnomalyNPC.AIChanged32[1] = true; }
    }

    private float DashAngle
    {
        get => AnomalyNPC.AnomalyAI32[2].F;
        set { AnomalyNPC.AnomalyAI32[2].F = value; AnomalyNPC.AIChanged32[2] = true; }
    }

    private Vector2 DashOrigin
    {
        get => AnomalyNPC.AnomalyAI64[0].GetValue<Vector2>();
        set { AnomalyNPC.AnomalyAI64[0].SetValue(value); AnomalyNPC.AIChanged64[0] = true; }
    }

    private Vector2 DashEdge
    {
        get => AnomalyNPC.AnomalyAI64[1].GetValue<Vector2>();
        set { AnomalyNPC.AnomalyAI64[1].SetValue(value); AnomalyNPC.AIChanged64[1] = true; }
    }

    public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        DrawHookRays();
        if (CurrentBehavior == Behavior.DashFan && CurrentDashStage == DashStage.Aim)
            DashVisuals.DrawTelegraph(DashOrigin, DashAngle, DashAge);
    }

    private void BeginEdgeApproach(NPC boss)
    {

        DashNumber = 0;
        CurrentDashStage = DashStage.ApproachEdge;

        DashOrigin = boss.Center;
        Vector2 center = (FindOwned<PlanteraArena>()?.Center ?? boss.Center);
        Vector2 fallback = boss.HasValidTarget && boss.HasPlayerTarget
            ? (center - Main.player[boss.target].Center).SafeNormalize(-Vector2.UnitY) : -Vector2.UnitY;
        DashEdge = center + (DashOrigin - center).SafeNormalize(fallback) * PlanteraArena.Radius;
        boss.netUpdate = true;
    }

    private void PlaceApproachingBoss(NPC boss)
    {
        float progress = Math.Clamp(DashAge / (SilenceTicks - 1f), 0f, 1f);
        boss.Center = Vector2.SmoothStep(DashOrigin, DashEdge, progress);
        boss.velocity = Vector2.Zero;
    }

    private void BeginAim(NPC boss)
    {
        DashNumber++;
        DashOrigin = boss.Center;
        Vector2 center = (FindOwned<PlanteraArena>()?.Center ?? boss.Center);
        Vector2 aim = boss.HasValidTarget && boss.HasPlayerTarget ? Main.player[boss.target].Center : center;
        // A target temporarily pulled back from outside the arena must still give
        // the next charge a chord through the circle.
        Vector2 offset = aim - center;
        if (offset.LengthSquared() >= PlanteraArena.Radius * PlanteraArena.Radius)
            aim = center + offset.SafeNormalize(Vector2.UnitY) * (PlanteraArena.Radius - 1f);
        DashAngle = (aim - DashOrigin).SafeNormalize((center - DashOrigin).SafeNormalize(Vector2.UnitY)).ToRotation();

        CurrentDashStage = DashStage.Aim;
        boss.netUpdate = true;
    }

    private void BeginCharge(NPC boss)
    {
        Vector2 offset = DashOrigin - (FindOwned<PlanteraArena>()?.Center ?? boss.Center);
        float projection = Vector2.Dot(offset, DashAngle.ToRotationVector2());
        float discriminant = projection * projection + PlanteraArena.Radius * PlanteraArena.Radius - offset.LengthSquared();
        float farExit = -projection + MathF.Sqrt(Math.Max(0f, discriminant));
        TravelTicks = (int)MathF.Floor(Math.Max(0f, farExit) / DashSpeed) + 1 + ExitTicks;

        CurrentDashStage = DashStage.Charge;
        Projectile.NewProjectileAction(boss.GetSource_FromAI(), DashOrigin, Vector2.Zero,
            ModContent.ProjectileType<DashFan>(), Math.Max(1, boss.defDamage / 2), 0f, Main.myPlayer,
            p => { p.ai[0] = boss.whoAmI; p.ai[1] = DashAngle; p.ai[2] = Main.rand.Next(1 << 23); });
        boss.netUpdate = true;
    }

    private void PlaceChargingBoss(NPC boss)
    {
        int steps = Math.Clamp(DashAge + 1, 0, TravelTicks);
        boss.Center = DashOrigin + DashAngle.ToRotationVector2() * (steps * DashSpeed);
        boss.velocity = Vector2.Zero;
        boss.rotation = DashAngle + MathHelper.PiOver2;
    }

    private bool UpdateDashRound(NPC boss, bool authority)
    {
        if (CurrentDashStage == DashStage.Idle && authority) BeginEdgeApproach(boss);
        if (CurrentDashStage == DashStage.ApproachEdge)
        {
            PlaceApproachingBoss(boss);
            if (!authority || DashAge < SilenceTicks) return true;
            BeginAim(boss);
        }
        if (CurrentDashStage == DashStage.Aim)
        {
            boss.rotation = DashAngle + MathHelper.PiOver2;
            if (!authority || DashAge < AimTicks) return true;
            BeginCharge(boss);
        }
        if (CurrentDashStage == DashStage.Charge)
        {
            if (DashAge < TravelTicks)
            {
                int step = DashAge;
                // Distribute whole loops over the actual path, including the very last pixel.
                int loops = Math.Max(1, TravelTicks / LoopTicks);
                int loop = step * loops / TravelTicks;
                int first = (loop * TravelTicks + loops - 1) / loops;
                if (authority && step == first)
                {
                    int end = ((loop + 1) * TravelTicks + loops - 1) / loops;
                    Vector2 start = DashOrigin + DashAngle.ToRotationVector2() * (step * DashSpeed);
                    Projectile.NewProjectileAction(boss.GetSource_FromAI(), start, Vector2.Zero, ModContent.ProjectileType<DashLoop>(),
                        Math.Max(1, boss.defDamage / 2), 0f, Main.myPlayer, p => { p.ai[0] = boss.whoAmI; p.ai[1] = DashAngle; p.ai[2] = (loop == 0 ? -1 : 1) * (end - first); });
                }
                PlaceChargingBoss(boss);
                return true;
            }
            if (!authority) return true;
            if (DashNumber < DashCount) BeginAim(boss);
            else
            {
                CurrentDashStage = DashStage.Recovery;

                boss.netUpdate = true;
            }
            return true;
        }
        if (!authority || DashAge < 180) return true;
        CurrentDashStage = DashStage.Idle;

        // The next round starts its clock two seconds below zero.
        EnterBehavior(Phase.Phase2, Behavior.Standoff);
        Timer = -ExtraStandoffTicks;
        foreach (Projectile p in Main.ActiveProjectiles)
            if ((int)p.ai[0] == boss.whoAmI && p.ModProjectile is DashLoop or DashFan or
                SeedShot or SeekingRingSeed or ButterflyFlowers or AimedNeedle or BurstThorn)
                p.Kill();
        boss.netUpdate = true;
        return false;
    }
}