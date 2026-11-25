namespace Anomalies.Bosses.Plantera.UnendingSpring;

public sealed class UnendingSpringHook : AnomalyNPCBehavior<UnendingSpringHook>
{
    public override int ApplyingType => NPCID.PlanterasHook;
    // ai[0..1] 为锚点图格，ai[2..3] 为主人索引和爪序号（均加一）。
    public override bool ShouldProcess => NPC.ai[2] >= 1f && NPC.ai[2] <= Main.maxNPCs && NPC.ai[3] is >= 1f and <= 3f;

    public override bool CanHitNPC(NPC target) => false;
    public override bool CanHitPlayer(Player target, ref int cooldownSlot) => false;
    public override bool AllowCalamityLogic(CalamityLogicType_NPCBehavior type) => type != CalamityLogicType_NPCBehavior.VanillaOverrideAI;

    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        NPC boss = Main.npc[(int)NPC.ai[2] - 1];
        if (!boss.active || boss.type != NPCID.Plantera)
            return false;
        if (!PlanteraUnending.GetInstance(boss).DrawParadoxVine(boss, NPC))
        {
            Texture2D vine = TextureAssets.Chain26.Value;
            Vector2 offset = boss.Center - NPC.Center;
            float length = offset.Length();
            Vector2 direction = offset.SafeNormalize(Vector2.Zero);
            for (float along = 0f; along < length; along += vine.Height)
            {
                int height = Math.Min(vine.Height, (int)MathF.Ceiling(length - along));
                spriteBatch.Draw(vine, NPC.Center + direction * along - screenPos,
                    new Rectangle(0, 0, vine.Width, height), Color.White, direction.ToRotation() - MathHelper.PiOver2,
                    new Vector2(vine.Width * 0.5f, 0f), 1f, SpriteEffects.None, 0f);
            }
        }
        return true;
    }

    public override bool PreAI()
    {
        int owner = (int)NPC.ai[2] - 1;
        NPC boss = Main.npc[owner];
        if (!boss.active || boss.type != NPCID.Plantera || !PlanteraUnending.GetInstance(boss).ShouldProcess)
        {
            if (TONetworkEnvironment.NotClient)
            {
                NPC.active = false;
                NPC.netUpdate = true;
            }
            return false;
        }
        PlanteraUnending behavior = PlanteraUnending.GetInstance(boss);
        NPC.aiStyle = -1;
        NPC.timeLeft = boss.timeLeft;
        if (behavior.PositionHook(NPC, (int)NPC.ai[3] - 1))
        {
            NPC.localAI[1] = 1f;
            return false;
        }
        if (NPC.localAI[1] == 1f)
        {
            NPC.localAI[1] = 0f;
            NPC.ai[0] = NPC.ai[1] = NPC.localAI[0] = 0f;
            NPC.netUpdate = TONetworkEnvironment.NotClient;
        }
        if (!boss.HasPlayerTarget)
            return false;
        bool half = boss.life < boss.lifeMax / 2;
        bool quarter = boss.life < boss.lifeMax / 4;
        bool retreat = !boss.HasValidTarget;
        if (TONetworkEnvironment.NotClient)
        {
            NPC.localAI[0] -= 5 * (1 + (half ? 2 : 0) + (quarter ? 2 : 0)) + (retreat ? 10 : 0);
            bool initial = NPC.ai[0] == 0f;
            if (initial || NPC.localAI[0] <= 0f)
            {
                bool wait = false;
                if (!initial && !retreat)
                {
                    foreach (NPC other in Main.ActiveNPCs)
                    {
                        if (other.whoAmI != NPC.whoAmI && behavior.OwnsHook(other.whoAmI) && other.velocity != Vector2.Zero)
                        {
                            wait = true;
                            break;
                        }
                    }
                }
                if (wait)
                    NPC.localAI[0] = Main.rand.Next(60, 300);
                else
                {
                    Vector2 near = Main.player[boss.target].Center;
                    if (initial)
                        near = (near + boss.Center) / 2f;
                    if (retreat)
                        near = boss.position + Vector2.UnitY * 400f;
                    Point tile = near.ToTileCoordinates();
                    NPC.ai[0] = Math.Clamp(tile.X + Main.rand.Next(-20, 21), 1, Main.maxTilesX - 2);
                    NPC.ai[1] = Math.Clamp(tile.Y + Main.rand.Next(-20, 21), 1, Main.maxTilesY - 2);
                    NPC.localAI[0] = Main.rand.Next(300, 600);
                    NPC.netUpdate = true;
                }
            }
        }
        if (NPC.ai[0] <= 0f || NPC.ai[1] <= 0f)
            return false;
        Vector2 offset = new Vector2(NPC.ai[0], NPC.ai[1]) * 16f - new Vector2(8f) - NPC.Center;
        float speed = (quarter ? 10f : half ? 8f : 6f) + (Main.expertMode ? half ? 2f : 1f : 0f);
        if (retreat)
            speed *= 4f;
        NPC.velocity = offset.LengthSquared() <= MathF.Pow(Math.Max(speed * 2.7f, speed + 12f), 2)
            ? offset : offset.SafeNormalize(Vector2.Zero) * speed * 2.7f;
        NPC.rotation = (boss.Center - NPC.Center).ToRotation() - MathHelper.PiOver2;
        return false;
    }
}