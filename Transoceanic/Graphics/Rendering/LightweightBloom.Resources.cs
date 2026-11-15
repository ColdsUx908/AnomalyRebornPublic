namespace Transoceanic.Graphics.Rendering;

internal sealed class LightweightBloomResources : ILoadable
{
    public void Load(Mod mod) { }

    public void Unload() => LightweightBloom.ReleaseResources();
}
