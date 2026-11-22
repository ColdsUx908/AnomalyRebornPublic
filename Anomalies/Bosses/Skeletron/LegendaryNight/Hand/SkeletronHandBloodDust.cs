namespace Anomalies.Bosses.Skeletron.LegendaryNight;

/// <summary>
/// 生成和绘制每只手独立持有的血尘。
/// </summary>
internal static class SkeletronHandBloodDust
{
    /// <summary>
    /// 最大尘埃数量。
    /// </summary>
    private const int MaxDustCount = 96;
    internal static void Spawn(LocalDustState state, Vector2 position, int width, int height, float speedX, float speedY, int alpha, Color color, float scale)
    {
        if (Main.gamePaused || Main.gameMenu || state.LastSpawn == Main.GameUpdateCount)
            return;
        state.LastSpawn = Main.GameUpdateCount;
        if (!state.Initialized)
        {
            state.LastUpdate = Main.GameUpdateCount;
            state.Initialized = true;
        }

        // 复用原版血尘的随机位移、速度、动画帧和尺寸，但不做未投影世界坐标的视野裁剪。
        Dust dust = new()
        {
            active = true,
            type = DustID.Blood,
            position = position + new Vector2(Main.rand.Next(Math.Max(5, width) - 4) + 4f,
                Main.rand.Next(Math.Max(5, height) - 4) + 4f),
            velocity = new Vector2(Main.rand.Next(-20, 21) * 0.1f + speedX, Main.rand.Next(-20, 21) * 0.1f + speedY),
            frame = new Rectangle(10 * DustID.Blood, 10 * Main.rand.Next(3), 8, 8),
            scale = scale * (1f + Main.rand.Next(-20, 21) * 0.01f),
            alpha = alpha,
            color = color,
            noGravity = true
        };

        // 保留原版的血腥效果过滤行为。
        if (!ChildSafety.Disabled && ChildSafety.DangerousDust(dust.type))
        {
            if (Main.rand.Next(2) != 0)
                return;
            dust.type = DustID.Cloud;
            dust.scale = Main.rand.NextFloat() * 1.6f + 0.3f;
            dust.color = Color.Transparent;
            dust.frame = new Rectangle(10 * dust.type, 10 * Main.rand.Next(3), 8, 8);
        }
        if (state.Dust.Count >= MaxDustCount)
            state.Dust.RemoveAt(0);
        state.Dust.Add(dust);
    }

    internal static void UpdateAndDraw(LocalDustState state, SpriteBatch spriteBatch, Vector2 drawOffset, float opacity)
    {
        Update(state, Main.GameUpdateCount, Main.gamePaused);
        foreach (Dust dust in state.Dust)
        {
            Color light = Lighting.GetColor((int)((dust.position.X + 4f) / 16f), (int)((dust.position.Y + 4f) / 16f));
            Color color = dust.GetAlpha(light) * opacity;
            spriteBatch.Draw(TextureAssets.Dust.Value, dust.position + drawOffset, dust.frame, color,
                dust.GetVisualRotation(), new Vector2(4f), dust.GetVisualScale(), SpriteEffects.None, 0f);
            if (dust.color.PackedValue != 0)
            {
                Color tint = dust.GetColor(dust.GetAlpha(light)) * opacity;
                spriteBatch.Draw(TextureAssets.Dust.Value, dust.position + drawOffset, dust.frame, tint,
                    dust.GetVisualRotation(), new Vector2(4f), dust.GetVisualScale(), SpriteEffects.None, 0f);
            }
        }
    }

    internal static void Update(LocalDustState state, ulong updateCount, bool paused)
    {
        if (!state.Initialized)
        {
            state.LastUpdate = updateCount;
            state.Initialized = true;
            return;
        }
        ulong elapsed = updateCount >= state.LastUpdate ? updateCount - state.LastUpdate : 0;
        state.LastUpdate = updateCount;
        if (paused || elapsed == 0)
            return;
        // 若长时间没有绘制，直接淘汰已超过自然寿命的粒子，不补跑无限多帧。
        if (elapsed > 60)
        {
            state.Dust.Clear();
            return;
        }
        for (ulong frame = 0; frame < elapsed; frame++)
        {
            for (int i = state.Dust.Count - 1; i >= 0; i--)
            {
                Dust dust = state.Dust[i];
                // 对应原版无重力 Blood 的更新分支（Cloud 是血腥过滤后的替代效果）。
                dust.position += dust.velocity;
                if (dust.type == DustID.Blood)
                    dust.scale -= 0.04f;
                dust.velocity.X *= 0.99f;
                dust.rotation += dust.velocity.X * 0.5f;
                dust.scale -= 0.01f;
                dust.velocity *= 0.92f;
                dust.scale -= 0.04f;
                if (!dust.active || dust.scale < 0.1f)
                    state.Dust.RemoveAt(i);
            }
        }
    }
}