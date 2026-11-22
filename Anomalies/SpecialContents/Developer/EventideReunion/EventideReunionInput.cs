using Terraria.GameInput;

namespace Anomalies.SpecialContents.Developer.EventideReunion;

internal static class EventideReunionInput
{
    internal static bool StopPressed(Player player) => PlayerInput.Triggers.JustPressed.MouseLeft
        && !Main.gameMenu && !Main.playerInventory && !Main.blockMouse && !player.mouseInterface;
}
