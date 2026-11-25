namespace Anomalies.Bosses.EaterofWorlds;

// Draw independently of NPC screen culling: a head outside the viewport can still telegraph a charge into it.
public sealed class EaterofWorldsWarningSystem : ModSystem
{
    internal static Texture2D CoilCloud { get; private set; }
    internal static Texture2D ChargeChannel { get; private set; }
    internal const float ChannelRailSpacing = 48f;
    private static bool _TexturesEnabled;

    public override void Load()
    {
        if (!Main.dedServ)
        {
            _TexturesEnabled = true;
            On_Main.DrawNPCs += DrawMasks;
            Main.QueueMainThreadAction(CreateTextures);
        }
    }

    public override void Unload()
    {
        _TexturesEnabled = false;
        Texture2D cloud = CoilCloud;
        Texture2D channel = ChargeChannel;
        CoilCloud = null;
        ChargeChannel = null;
        if (cloud is not null)
            Main.QueueMainThreadAction(cloud.Dispose);
        if (channel is not null)
            Main.QueueMainThreadAction(channel.Dispose);
    }

    private static void CreateTextures()
    {
        if (!_TexturesEnabled)
            return;
        ChargeChannel = CreateChargeChannel(Main.graphics.GraphicsDevice);
        const int Size = 256;
        Color[] pixels = new Color[Size * Size];
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                float u = (x + 0.5f - Size * 0.5f) / (Size * 0.5f);
                float v = (y + 0.5f - Size * 0.5f) / (Size * 0.5f);
                float noise = MathF.Sin(u * 13f + MathF.Sin(v * 9f)) * MathF.Sin(v * 11f + MathF.Cos(u * 7f));
                float radius = MathF.Sqrt(u * u + v * v) + noise * 0.085f;
                float alpha = 1f - MathHelper.SmoothStep(0f, 1f, Math.Clamp((radius - 0.58f) / 0.4f, 0f, 1f));
                pixels[y * Size + x] = Color.White * alpha;
            }
        }
        CoilCloud = new Texture2D(Main.graphics.GraphicsDevice, Size, Size);
        CoilCloud.SetData(pixels);
    }

    // One repeating RGBA tile for the entire lane, including both soft shoulders and its translucent interior.
    internal static Texture2D CreateChargeChannel(GraphicsDevice device)
    {
        const int Width = 256;
        const int Height = 64;
        Color[] pixels = new Color[Width * Height];
        Random grain = new(13457);
        for (int x = 0; x < Width; x++)
        {
            float phase = MathHelper.TwoPi * x / Width;
            for (int y = 0; y < Height; y++)
            {
                float cross = y + 0.5f - Height * 0.5f;
                float sidePhase = phase + (cross > 0f ? 1.8f : 0f);
                float grainValue = grain.NextSingle();
                float mottling = 0.5f + 0.5f * MathF.Sin(sidePhase * 3f + MathF.Sin(phase * 7f + cross * 0.24f));
                float d = Math.Abs(cross) - ChannelRailSpacing * 0.5f - 0.36f * MathF.Sin(sidePhase * 8f);
                float rim = MathF.Exp(-0.44f * d * d);
                float halo = MathF.Exp(-0.061f * d * d);
                float inside = Math.Clamp(0.5f - d * 0.65f, 0f, 1f);
                float alpha = inside * (0.13f + 0.09f * mottling + 0.05f * grainValue)
                    + halo * (0.15f + 0.11f * mottling) + rim * (0.32f + 0.37f * grainValue);
                Color color = Color.Lerp(new Color(141, 88, 174), new Color(225, 175, 238),
                    Math.Clamp(halo * 0.32f + rim * 0.55f + 0.12f * grainValue, 0f, 1f));
                pixels[y * Width + x] = color * Math.Clamp(alpha, 0f, 1f);
            }
        }
        Texture2D tile = new(device, Width, Height);
        tile.SetData(pixels);
        return tile;
    }

    private static void DrawMasks(On_Main.orig_DrawNPCs orig, Main self, bool behindTiles)
    {
        orig(self, behindTiles);
        if (behindTiles || !AnomalySharedData.Anomaly)
            return;
        // DrawNPCs is called inside an active world-space batch; cover the NPCs after both NPC passes.
        foreach (NPC npc in Main.ActiveNPCs)
        {
            if (npc.type != NPCID.EaterofWorldsHead || !npc.Anomaly.ShouldRunAnomalyAI)
                continue;
            EaterofWorldsHead behavior = EaterofWorldsHead.GetInstance(npc);
            if (behavior.IsFirstSegment)
                behavior.DrawCoilMask(Main.spriteBatch, Main.screenPosition);
        }
    }

    public override void PostDrawTiles()
    {
        if (Main.dedServ || !AnomalySharedData.Anomaly)
            return;
        bool begun = false;
        try
        {
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (npc.type != NPCID.EaterofWorldsHead || !npc.Anomaly.ShouldRunAnomalyAI)
                    continue;
                EaterofWorldsHead behavior = EaterofWorldsHead.GetInstance(npc);
                if (!behavior.IsFirstSegment)
                    continue;
                if (!begun)
                {
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearWrap,
                        DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
                    begun = true;
                }
                behavior.DrawAttackWarnings(Main.spriteBatch, Main.screenPosition);
            }
        }
        finally
        {
            if (begun)
                Main.spriteBatch.End();
        }
    }
}
