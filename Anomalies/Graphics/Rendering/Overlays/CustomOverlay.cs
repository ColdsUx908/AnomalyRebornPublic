using Terraria.Graphics.Effects;
using Transoceanic.Runtime.Reflection;

namespace Anomalies.Visuals.Overlays;

public abstract class CustomOverlay : RegisteredContent<CustomOverlay>, IOverlaySource, IOverlayDrawable
{
    public virtual OverlayPriority Priority => OverlayPriority.Default;
    public virtual void SubmitOverlays(OverlayQueue overlays) => overlays.Submit(this, Priority);
    void IOverlayDrawable.DrawOverlay(SpriteBatch spriteBatch) => Draw(spriteBatch, RenderLayers.All, true);
    public virtual void Update() { }
    public abstract void Draw(SpriteBatch spriteBatch, RenderLayers layer, bool beginSpriteBatch);
}

public abstract class PostTilesOverlay : RegisteredContent<PostTilesOverlay>
{
    public virtual void Update() { }
    public abstract void Draw(SpriteBatch spriteBatch);
}

public sealed class CustomOverlayManager : ModSystem, IContentLoader
{
    private static List<CustomOverlay> _Overlays = [];
    private static List<PostTilesOverlay> _PostTilesOverlays = [];
    private static readonly OverlayQueue _Queue = new();

    public override void PreUpdateEntities()
    {
        foreach (CustomOverlay overlay in _Overlays)
            overlay.Update();
        foreach (PostTilesOverlay overlay in _PostTilesOverlays)
            overlay.Update();
    }

    private static void On_OverlayManager_Draw(On_OverlayManager.orig_Draw orig, OverlayManager self, SpriteBatch spriteBatch, RenderLayers layer, bool beginSpriteBatch)
    {
        orig(self, spriteBatch, layer, beginSpriteBatch);
        if (Main.dedServ || Main.gameMenu)
            return;
        if (layer != RenderLayers.All)
        {
            foreach (CustomOverlay overlay in _Overlays)
                overlay.Draw(spriteBatch, layer, beginSpriteBatch);
            return;
        }

        // All 通常没有活动批次；兼容由其他调用方提供已开始批次的情况。
        using SpriteBatchScope scope = new(spriteBatch, beginWhenDisposed: !beginSpriteBatch);
        if (!beginSpriteBatch)
            spriteBatch.End();
        _Queue.BeginCollection();
        try
        {
            foreach (CustomOverlay overlay in _Overlays)
                overlay.SubmitOverlays(_Queue);
            foreach (NPC npc in NPC.ActiveNPCs)
                if (npc.ModNPC is IOverlaySource source)
                    source.SubmitOverlays(_Queue);
            foreach (Projectile projectile in Projectile.ActiveProjectiles)
                if (projectile.ModProjectile is IOverlaySource source)
                    source.SubmitOverlays(_Queue);
            foreach (Item item in Item.ActiveItems)
                if (item.ModItem is IOverlaySource source)
                    source.SubmitOverlays(_Queue);
            foreach (Player player in Player.ActivePlayers)
                foreach (ModPlayer modPlayer in player.ModPlayers)
                    if (modPlayer is IOverlaySource source)
                        source.SubmitOverlays(_Queue);
            _Queue.Draw(spriteBatch);
        }
        finally
        {
            _Queue.Clear();
        }
    }

    public override void PostDrawTiles()
    {
        foreach (PostTilesOverlay overlay in _PostTilesOverlays)
            overlay.Draw(Main.spriteBatch);
    }

    void IContentLoader.PostSetupContent()
    {
        _Overlays.AddRange(TOReflectionUtils.GetAllSupportedMods().SelectMany(mod => mod.GetContent<CustomOverlay>()));
        _PostTilesOverlays.AddRange(TOReflectionUtils.GetAllSupportedMods().SelectMany(mod => mod.GetContent<PostTilesOverlay>()));
        On_OverlayManager.Draw += On_OverlayManager_Draw;
    }

    void IContentLoader.OnModUnload()
    {
        _Queue.Clear();
        _Overlays.Clear();
        _PostTilesOverlays.Clear();
    }

    public override void OnWorldUnload() => _Queue.Clear();
}
