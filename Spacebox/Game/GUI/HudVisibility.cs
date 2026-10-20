using Spacebox.Game.Ships;

namespace Spacebox.Game.GUI;

public static class HudVisibility
{
    public static bool ShowPlayerHud => IsPlayerHudVisible(Settings.ShowInterface, ShipPilot.IsUsingCamera);

    public static bool IsPlayerHudVisible(bool showInterface, bool usingCamera)
    {
        return showInterface && !usingCamera;
    }
}
