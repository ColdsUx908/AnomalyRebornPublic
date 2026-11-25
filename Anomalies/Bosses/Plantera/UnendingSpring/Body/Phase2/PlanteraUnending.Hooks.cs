namespace Anomalies.Bosses.Plantera.UnendingSpring;

public sealed partial class PlanteraUnending
{
    internal const float RayLength = PlanteraArena.Radius * 2f;
    private const int HookDuration = 900;
    private const int WarmupTicks = 90;
    internal int LightningTick => (int)Timer - SilenceTicks;
    internal bool LightningActive => CurrentBehavior == Behavior.HookLightning && LightningTick is >= 0 and < HookDuration;
    internal Vector2 ArenaCenter => FindOwned<PlanteraArena>()?.Center ?? NPC.Center;
    internal static int RainSpawnTick(int row) => (row * 3969 + 99) / 100;

    internal bool OwnsHook(int slot) => (uint)slot < Main.maxNPCs && Main.npc[slot] is { active: true, type: NPCID.PlanterasHook } hook
        && hook.ai[2] == NPC.whoAmI + 1 && hook.ai[3] is >= 1f and <= 3f;

    private int HookSlot(int index)
    {
        int slot = AnomalyNPC.AnomalyAI32[3 + index].I - 1;
        return OwnsHook(slot) && Main.npc[slot].ai[3] == index + 1 ? slot : -1;
    }

    private void EnsureHooks()
    {
        if (!TONetworkEnvironment.NotClient)
            return;
        for (int i = 0; i < 3; i++)
        {
            if (HookSlot(i) >= 0)
                continue;
            NPC.NewNPCAction(SourceAI, NPC.Center, NPCID.PlanterasHook, NPC.whoAmI, hook =>
            {
                hook.Center = NPC.Center;
                hook.ai[2] = NPC.whoAmI + 1;
                hook.ai[3] = i + 1;
                AnomalyNPC.AnomalyAI32[3 + i].I = hook.whoAmI + 1;
                AnomalyNPC.AIChanged32[3 + i] = true;
                NPC.netUpdate = true;
            });
        }
    }

    internal bool PositionHook(NPC hook, int index)
    {
        if (CurrentBehavior == Behavior.ParadoxSector)
            return PlaceParadoxHook(NPC, hook, (int)Timer);
        if (CurrentBehavior != Behavior.HookLightning || LightningTick >= HookDuration)
            return false;
        Vector2 vertex = NPC.Center + (index * MathHelper.TwoPi / 3f - MathHelper.PiOver2).ToRotationVector2() * (RayLength / 3f);
        hook.Center += (vertex - hook.Center) / Math.Max(1, -LightningTick);
        hook.rotation = (NPC.Center - hook.Center).ToRotation() - MathHelper.PiOver2;
        hook.velocity = Vector2.Zero;
        return true;
    }

    private float RayAngle(int hook, int ray, int ghost = 0) => hook * MathHelper.TwoPi / 9f + ray * MathHelper.TwoPi / 3f
        + CurrentLocalPhase * (LightningTick * (0.3584f / 60f) - ghost * 0.04f);

    private void UpdateHookFormation()
    {
        if (Timer == 0)
        {
            NPC.ai[1] = Main.rand.NextBool() ? 1 : -1;
            AnomalyNPC.AnomalyAI32[6].I = 0;
            for (int i = 0; i < 9; i++)
            {
                AnomalyNPC.AnomalyAI32[7 + i].I = 0;
                AnomalyNPC.AIChanged32[7 + i] = true;
            }
            NPC.netUpdate = true;
        }
        if (!LightningActive)
            return;
        int tick = LightningTick;
        if (tick * 100 % 3969 < 100)
            RainThorn.FireRow(NPC, tick * 100 / 3969);
        if (tick < WarmupTicks)
            return;

        int previous = AnomalyNPC.AnomalyAI32[6].I;
        int touched = 0;
        Player player = Main.player[NPC.target];
        for (int i = 0; i < 3; i++)
        {
            int slot = HookSlot(i);
            if (slot < 0)
                continue;
            Vector2 origin = Main.npc[slot].Center;
            for (int ray = 0; ray < 3; ray++)
            {
                float angle = RayAngle(i, ray);
                Vector2 end = origin + angle.ToRotationVector2() * RayLength;
                if (!RotatedRectangle.FromLineSegment(origin, end, 3f).Collides(player.Hitbox))
                    continue;
                int index = 3 * i + ray;
                int bit = 1 << index;
                touched |= bit;
                if ((previous & bit) != 0)
                    continue;
                AnomalyNPC.AnomalyAI32[7 + index].I = tick;
                AnomalyNPC.AIChanged32[7 + index] = true;
                Projectile.NewProjectileAction(SourceAI, origin, Vector2.Zero, ModContent.ProjectileType<HookLightning>(),
                    AttackDamage, 0f, Main.myPlayer, p => { p.ai[0] = NPC.whoAmI; p.ai[1] = angle; p.ai[2] = slot; });
            }
        }
        AnomalyNPC.AnomalyAI32[6].I = touched;
        if ((touched & ~previous) != 0)
        {
            NPC.netUpdate = true;
            Projectile.NewProjectileAction(SourceAI, player.Center, Vector2.Zero, ModContent.ProjectileType<HookWarningRing>(),
                0, 0f, Main.myPlayer, p => p.ai[0] = player.whoAmI);
        }
    }

    private void DrawHookRays()
    {
        if (!LightningActive)
            return;
        float opacity = MathHelper.Lerp(0.2f, 1f, Math.Min(1f, LightningTick / (float)WarmupTicks));
        for (int i = 0; i < 3; i++)
        {
            int slot = HookSlot(i);
            if (slot < 0)
                continue;
            Vector2 origin = Main.npc[slot].Center - Main.screenPosition;
            for (int ray = 0; ray < 3; ray++)
            {
                int flash = AnomalyNPC.AnomalyAI32[7 + 3 * i + ray].I;
                int age = LightningTick - flash;
                Color main = flash >= WarmupTicks && age >= 0 && age < HookLightning.Delay
                    ? (age / 3 % 2 == 0 ? Color.White : new Color(100, 244, 145) * 0.25f)
                    : new Color(165, 255, 186) * 0.88f;
                for (int ghost = 7; ghost >= 0; ghost--)
                {
                    Color color = ghost == 0 ? main : new Color(73, 223, 125) * (0.32f * (8 - ghost) / 8f);
                    DashVisuals.Line(origin, origin + RayAngle(i, ray, ghost).ToRotationVector2() * RayLength,
                        color * opacity, ghost == 0 ? 2.5f : 1.5f);
                }
            }
        }
    }
}