using Spacebox.Game.Ships;

namespace Spacebox.Game.GUI;

public static class HudVisibility
{
    public static bool ShowPlayerHud => IsPlayerHudVisible(Settings.ShowInterface, ShipPilot.IsUsingCamera);

    public const string ItemWindow = "inventory";

    public static bool ShowHotbar => IsHotbarVisible(Settings.ShowInterface, ShipPilot.IsUsingCamera, Engine.UIManager.IsOpen(ItemWindow));

    public static bool IsPlayerHudVisible(bool showInterface, bool usingCamera)
    {
        return showInterface && !usingCamera;
    }

    public static bool IsHotbarVisible(bool showInterface, bool usingCamera, bool itemWindowOpen)
    {
        return !usingCamera && (showInterface || itemWindowOpen);
    }
}
