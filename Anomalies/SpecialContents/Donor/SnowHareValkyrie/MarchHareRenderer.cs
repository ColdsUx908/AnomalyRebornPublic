namespace Anomalies.SpecialContents.Donor.SnowHareValkyrie;

public sealed class MarchHareRenderer : ModSystem
{
    private readonly record struct Mist(Vector2 Center, ulong Birth, int Lifetime, bool Red, bool Ears);

    private readonly List<MarchHare> _Visible = [];
    private readonly List<Mist> _Mist = [];
    private int _RecycledMist;
    private BlendState _LightBlend;

    public override bool IsLoadingEnabled(Mod mod) => AnomalySpecialContentToggler.SnowHareValkyrie;

    public override void Load()
    {
        if (!Main.dedServ)
        {
            _LightBlend = MarchHare.CreateLightBlend();
            On_Main.DrawProjectiles += DrawProjectiles;
        }
    }

    public override void Unload() => _LightBlend?.Dispose();

    public override void OnWorldUnload()
    {
        _Visible.Clear();
        _Mist.Clear();
        _RecycledMist = 0;
    }

    internal void Submit(MarchHare hare) => _Visible.Add(hare);

    internal void EmitMist(Projectile projectile, bool red, bool ears)
    {
        // 原作池中新雾光存活 31 帧，回收后的槽位存活 41 帧。每对耳朵共享一团雾光。
        bool recycled = _RecycledMist > 0;
        if (recycled)
            _RecycledMist--;
        ulong birth = (ulong)Main.GameUpdateCount * 2 + (projectile.numUpdates >= projectile.MaxUpdates / 2 ? 0u : 1u);
        _Mist.Add(new(projectile.Center, birth, recycled ? 41 : 31, red, ears));
    }

    public override void PostUpdateProjectiles()
    {
        ulong now = (ulong)Main.GameUpdateCount * 2 + 1;
        for (int i = _Mist.Count - 1; i >= 0; i--)
        {
            if (now - _Mist[i].Birth < (ulong)_Mist[i].Lifetime)
                continue;
            _Mist.RemoveAt(i);
            _RecycledMist++;
        }
    }

    private void DrawProjectiles(On_Main.orig_DrawProjectiles orig, Main self)
    {
        _Visible.Clear();
        orig(self);
        if (Main.gameMenu || (_Visible.Count == 0 && _Mist.Count == 0))
            return;

        int type = ModContent.ProjectileType<MarchHare>();
        Texture2D atlas = TextureAssets.Projectile[type].Value;
        SpriteBatch batch = Main.spriteBatch;
        batch.Begin(SpriteSortMode.Deferred, _LightBlend, SamplerState.LinearClamp, DepthStencilState.None,
            Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        ulong now = (ulong)Main.GameUpdateCount * 2 + 1;
        foreach (Mist mist in _Mist)
        {
            int age = (int)(now - mist.Birth);
            if (age < mist.Lifetime)
                MarchHare.DrawMist(batch, atlas, mist.Center - Main.screenPosition, age, mist.Red, mist.Ears);
        }
        foreach (MarchHare hare in _Visible)
            hare.Draw(batch, atlas);
        batch.End();
    }
}
