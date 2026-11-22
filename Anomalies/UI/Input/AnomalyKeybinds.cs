namespace Anomalies.Common;

public sealed class AnomalyKeybinds : ModSystem
{
#if TWEAK
    internal static ModKeybind SinglePressDash { get; private set; }

    public override void Load()
    {
        if (!Main.dedServ)
            SinglePressDash = KeybindLoader.RegisterKeybind(Mod, nameof(SinglePressDash), "C");
    }

    public override void Unload() => SinglePressDash = null;
#endif
}
