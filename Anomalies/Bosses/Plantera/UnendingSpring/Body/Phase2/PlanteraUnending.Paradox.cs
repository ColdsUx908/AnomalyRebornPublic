using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace Anomalies.Bosses.Plantera.UnendingSpring;

public sealed partial class PlanteraUnending
{

    private int ParadoxEnd => SilenceTicks + ParadoxDuration;
    internal const float ParadoxInset = 2.5f * UnendingSpringPaths.PetalTipRadius;
    private const float PassageRatio = 0.70f;
    internal const int StandingPlayerHeight = 42;
    // Half of the original central passage, measured from the innermost seed
    // center, includes half a seed and half a standing player before it is usable.
    private const float PassageClearance = (ParadoxSeed.SeedSize + StandingPlayerHeight) / 2f;
    private const float HookOffset = PassageClearance + PassageRatio * (144f - PassageClearance);
    private const float TipDepth = -64f;
    private const int ParadoxVolleyTicks = 4;
    private const int BlindSpotDelay = 24;
    private const float HookLeadTicks = 16f, MaxHookShift = 100f;
    private const float ForwardLaunch = -2.8f * ParadoxScale;

    // Both firing hooks use the same outward deflection in opposite directions.



    // Match the original 448-pixel playfield height to the arena's diameter.
    internal const float ParadoxScale = 2f * PlanteraArena.Radius / 448f;

    // +1 maps the source pattern's downward axis to world right, -1 to world left.

    private int ParadoxDuration
    {
        get => AnomalyNPC.AnomalyAI32[16].I;
        set { AnomalyNPC.AnomalyAI32[16].I = value; AnomalyNPC.AIChanged32[16] = true; }
    }
    private int BlindTicks
    {
        get => AnomalyNPC.AnomalyAI32[17].I;
        set { AnomalyNPC.AnomalyAI32[17].I = value; AnomalyNPC.AIChanged32[17] = true; }
    }
    private int BlindPlayer
    {
        get => AnomalyNPC.AnomalyAI32[18].I;
        set { AnomalyNPC.AnomalyAI32[18].I = value; AnomalyNPC.AIChanged32[18] = true; }
    }
    private float Steer
    {
        get => AnomalyNPC.AnomalyAI32[19].F;
        set { AnomalyNPC.AnomalyAI32[19].F = value; AnomalyNPC.AIChanged32[19] = true; }
    }
    private float SteerGoal
    {
        get => AnomalyNPC.AnomalyAI32[20].F;
        set { AnomalyNPC.AnomalyAI32[20].F = value; AnomalyNPC.AIChanged32[20] = true; }
    }
    private float LastSyncedGoal
    {
        get => AnomalyNPC.AnomalyAI32[21].F;
        set { AnomalyNPC.AnomalyAI32[21].F = value; AnomalyNPC.AIChanged32[21] = true; }
    }
    private float ThornGridPhase
    {
        get => AnomalyNPC.AnomalyAI32[22].F;
        set { AnomalyNPC.AnomalyAI32[22].F = value; AnomalyNPC.AIChanged32[22] = true; }
    }
    private int SectorPatternBits
    {
        get => AnomalyNPC.AnomalyAI32[23].I;
        set { AnomalyNPC.AnomalyAI32[23].I = value; AnomalyNPC.AIChanged32[23] = true; }
    }
    private int FallSide
    {
        get => AnomalyNPC.AnomalyAI32[24].I;
        set { AnomalyNPC.AnomalyAI32[24].I = value; AnomalyNPC.AIChanged32[24] = true; }
    }
    private Vector2 ParadoxCenter
    {
        get => AnomalyNPC.AnomalyAI64[2].GetValue<Vector2>();
        set { AnomalyNPC.AnomalyAI64[2].SetValue(value); AnomalyNPC.AIChanged64[2] = true; }
    }
    private Vector2 ParadoxStart
    {
        get => AnomalyNPC.AnomalyAI64[3].GetValue<Vector2>();
        set { AnomalyNPC.AnomalyAI64[3].SetValue(value); AnomalyNPC.AIChanged64[3] = true; }
    }

    private bool UpdateParadox(NPC boss, int time)
    {
        if (time == 0)
        {
            ParadoxDuration = ParadoxSector.LastStart + ParadoxSector.RoundTicks;
            ParadoxCenter = ArenaCenter;
            ParadoxStart = boss.Center;
            // Both stops share a height. A right-side target means moving to the left stop and firing right.
            if (!boss.HasPlayerTarget || !boss.HasValidTarget) boss.TargetClosest(false);
            FallSide = boss.HasPlayerTarget && boss.HasValidTarget
                ? Math.Sign(Main.player[boss.target].Center.X - ParadoxCenter.X) : 0;
            if (FallSide == 0) FallSide = Main.rand.NextBool() ? 1 : -1;
            Steer = SteerGoal = LastSyncedGoal = 0f;
            BlindTicks = 0;
            EnsureHooks();
            for (int i = 0; i < 3; i++)
            {
                AnomalyNPC.AnomalyAI64[4 + i].SetValue(HookSlot(i) >= 0 ? Main.npc[HookSlot(i)].Center : boss.Center);
                AnomalyNPC.AIChanged64[4 + i] = true;
            }
            boss.netUpdate = true;
        }
        PlaceParadoxBoss(boss, time);
        int tick = time - SilenceTicks;
        int sectorTick = tick - ParadoxSector.FirstTick;
        if (sectorTick >= 0 && tick <= ParadoxSector.LastStart && sectorTick % ParadoxSector.RoundTicks == 0)
        {
            if (sectorTick == 0) SectorPatternBits = Main.rand.Next(4);
            else SectorPatternBits ^= 3;
            int seed = (Main.rand.Next(1 << 20) << 2) | SectorPatternBits;
            int slot = -1;
            Projectile.NewProjectileAction(boss.GetSource_FromAI(), Rim, Vector2.Zero, ModContent.ProjectileType<ParadoxSector>(),
                Math.Max(1, boss.defDamage / 2), 0f, Main.myPlayer, p => { p.ai[0] = boss.whoAmI; p.ai[1] = FallSide * (seed + 1); slot = p.whoAmI; });
            if (tick == ParadoxSector.LastStart && (uint)slot < Main.maxProjectiles &&
                Main.projectile[slot].ModProjectile is ParadoxSector lastRound)
            {
                // NPCs update before projectiles: allow the last seed's exit particles to run.
                ParadoxDuration = tick + lastRound.CompletionAge + 1;
                boss.netUpdate = true;
            }
        }
        if (time < ParadoxEnd && GuidingTreeVolley.IsDue(tick))
        {
            if (!(HookSlot(0) >= 0)) EnsureHooks();
            if ((HookSlot(0) >= 0))
            {
                NPC rearHook = Main.npc[HookSlot(0)];
                PlaceParadoxHook(boss, rearHook, time);
                float gridPhase = ThornGridPhase;
                GuidingTreeVolley.Fire(boss, rearHook.Center, tick, FallSide, ref gridPhase);
                ThornGridPhase = gridPhase;
            }
        }
        if (time == ParadoxEnd)
        {
            foreach (Projectile p in Main.ActiveProjectiles)
                if ((int)p.ai[0] == boss.whoAmI && p.ModProjectile is GuidingTreeVolley or ParadoxSector or
                    AimedNeedle or PetalWheel or DashLoop or DashFan or DashPinkSeed or RainThorn or HookLightning or
                    SeedShot or SeekingRingSeed or ButterflyFlowers or BurstThorn or TransitionSpell)
                    p.Kill();
        }
        if (tick >= 0 && time < ParadoxEnd) UpdateParadoxSteer(boss, tick);
        if (tick >= 0 && time < ParadoxEnd && tick % ParadoxVolleyTicks == 0)
        {
            // The original emitter first samples the running phase after its 60-frame charge.
            float modulation = 0.25f * MathF.Sin(MathHelper.ToRadians(((tick + 60) % 72) * 5f));
            // Both seeds of one hook receive the same inward shift. The inner
            // seed moves outward at 70% of its old rate, and strand spacing is preserved.
            for (int i = 1; i <= 2; i++)
            {
                if (!(HookSlot(i) >= 0)) continue;
                int side = i == 1 ? -1 : 1;
                Vector2 origin = boss.Center + ToWorld(SideTip(side));
                for (int strand = -1; strand <= 1; strand += 2)
                {
                    Vector2 velocity = new(side * (StrandSpeed(modulation, strand) + Steer), ForwardLaunch);
                    Projectile.NewProjectileAction(boss.GetSource_FromAI(), origin, ToWorld(velocity),
                        ModContent.ProjectileType<ParadoxSeed>(), Math.Max(1, boss.defDamage / 2), 0f, Main.myPlayer,
                        p => { p.ai[0] = FallSide; p.ai[1] = ParadoxCenter.X + FallSide * PlanteraArena.Radius; p.ai[2] = side == strand ? 1 : -1; });
                }
            }
        }
        if (time < ParadoxEnd + 120) return true;
        EnterBehavior(Phase.Phase2, Behavior.Standoff);
        boss.netUpdate = true;
        return false;
    }
}
