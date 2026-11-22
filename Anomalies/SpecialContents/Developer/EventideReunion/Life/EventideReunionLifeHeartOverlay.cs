using Terraria.GameContent.UI.ResourceSets;

namespace Anomalies.SpecialContents.Developer.EventideReunion;

[Autoload(Side = ModSide.Client)]
public sealed class EventideReunionLifeHeartOverlay : ModResourceOverlay
{
    private const string Fancy = "Images/UI/PlayerResourceSets/FancyClassic/";
    private const string Bars = "Images/UI/PlayerResourceSets/HorizontalBars/";
    private Asset<Texture2D> _FancyHeart;
    private Asset<Texture2D> _FancyFruit;
    private Asset<Texture2D> _Bar;
    private Asset<Texture2D> _BarHoney;
    private int _Bonus;
    private int _BaseLife;
    private readonly List<(ResourceOverlayDrawContext Context, float Coverage, bool Bar)> _Units = [];

    public override bool IsLoadingEnabled(Mod mod) => AnomalySpecialContentToggler.EventideReunion;

    public override void Load()
    {
        _FancyHeart = Main.Assets.Request<Texture2D>(Fancy + "Heart_Fill");
        _FancyFruit = Main.Assets.Request<Texture2D>(Fancy + "Heart_Fill_B");
        _Bar = Main.Assets.Request<Texture2D>(Bars + "HP_Fill");
        _BarHoney = Main.Assets.Request<Texture2D>(Bars + "HP_Fill_Honey");
    }

    public override bool PreDrawResourceDisplay(PlayerStatsSnapshot snapshot, IPlayerResourcesDisplaySet displaySet,
        bool drawingLife, ref Color textColor, out bool drawText)
    {
        drawText = true;
        _Units.Clear();
        _Bonus = drawingLife && !Main.LocalPlayer.dead ? Main.LocalPlayer.Anomaly.EventideReunion_LifeBlessingBonus : 0;
        _BaseLife = snapshot.LifeMax - _Bonus;
        return true;
    }

    public override void PostDrawResource(ResourceOverlayDrawContext context)
    {
        if (_Bonus <= 0 || context.resourceNumber < 0 || context.resourceNumber >= context.snapshot.AmountOfLifeHearts)
            return;
        Asset<Texture2D> texture = context.texture;
        bool bar = texture == _Bar || texture == _BarHoney;
        if (!bar && texture != TextureAssets.Heart && texture != TextureAssets.Heart2 && texture != _FancyHeart && texture != _FancyFruit)
            return;

        float coverage = EventideReunionLifeRules.HeartCoverage(_BaseLife, _Bonus, context.resourceNumber);
        if (coverage > 0f)
            _Units.Add((context, coverage, bar));
    }

    public override void PostDrawResourceDisplay(PlayerStatsSnapshot snapshot, IPlayerResourcesDisplaySet displaySet,
        bool drawingLife, Color textColor, bool drawText)
    {
        if (!drawingLife || _Units.Count == 0 || EventideReunionEffects.EventideLifeHeart?.Asset.Value is not Effect effect)
            return;

        // 在资源层全部提交后着色，复用当前样式的填充裁切、跳动缩放和 UI 变换。
        SpriteBatch batch = _Units[0].Context.SpriteBatch;
        using (batch.Scope(SpriteSortMode.Immediate, customEffect: effect))
        {
            effect.Parameters["uTime"].SetValue((float)Main.GlobalTimeWrappedHourly);
            foreach ((ResourceOverlayDrawContext context, float coverage, bool bar) in _Units)
            {
                effect.Parameters["uCoverage"].SetValue(coverage);
                effect.Parameters["uBar"].SetValue(bar ? 1f : 0f);
                effect.Parameters["uTexelWidth"].SetValue(1f / context.texture.Value.Width);
                effect.CurrentTechnique.Passes["Pass0"].Apply();
                context.Draw();
            }
        }
    }
}
