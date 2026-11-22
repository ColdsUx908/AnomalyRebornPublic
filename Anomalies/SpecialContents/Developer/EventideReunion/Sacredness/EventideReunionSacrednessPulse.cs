using Terraria.UI;

namespace Anomalies.SpecialContents.Developer.EventideReunion;

public sealed class EventideReunionSacrednessPulse : ModSystem
{
    private const float Radius = 2000f;
    private const int FadeTime = 18;
    private static int _FlashTime;

    public override bool IsLoadingEnabled(Mod mod) => AnomalySpecialContentToggler.EventideReunion;

    internal static void Release(Player caster)
    {
        foreach (Player target in Main.ActivePlayers)
        {
            if (target.dead || Vector2.DistanceSquared(caster.Center, target.Center) > Radius * Radius)
                continue;
            if (Main.netMode == NetmodeID.SinglePlayer)
                ApplyToLocalPlayer();
            else
            {
                // 玩家客户端持有完整 Buff 列表；仅向本次脉冲的接收者发出峰值事件。
                ModPacket packet = AnomalySynchronization.GetCAPacket();
                packet.Write(AnomalySynchronization.ID.EventideSacredPulse);
                packet.Send(target.whoAmI);
            }
        }
    }

    internal static void ApplyToLocalPlayer()
    {
        Player player = Main.LocalPlayer;
        if (!player.active || player.dead)
            return;
        _FlashTime = FadeTime;
        bool changed = false;
        for (int i = player.buffType.Length - 1; i >= 0; i--)
        {
            if (player.buffType[i] <= 0 || !Main.debuff[player.buffType[i]])
                continue;
            player.DelBuff(i);
            changed = true;
        }
        if (changed && Main.netMode == NetmodeID.MultiplayerClient)
            NetMessage.SendData(MessageID.PlayerBuffs, number: player.whoAmI);
    }

    public override void OnWorldUnload() => _FlashTime = 0;

    public override void PostUpdateEverything()
    {
        if (_FlashTime > 0)
            _FlashTime--;
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers) =>
        layers.Insert(0, new LegacyGameInterfaceLayer("Anomalies: Sacred Judgment", DrawFlash, InterfaceScaleType.None));

    private static bool DrawFlash()
    {
        if (_FlashTime <= 0 || Main.gameMenu)
            return true;
        Viewport viewport = Main.instance.GraphicsDevice.Viewport;
        float opacity = MathHelper.SmoothStep(0f, 1f, _FlashTime / (FadeTime - 1f));
        Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, viewport.Bounds, new Rectangle(0, 0, 1, 1), Color.White * opacity);
        return true;
    }
}
