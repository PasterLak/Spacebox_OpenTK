using Spacebox.Game.GUI;

namespace Spacebox.Tests.Game;

public class HudVisibilityTests
{
    [Theory]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    public void PlayerHud_IsHiddenInCameraViewOrWithoutInterface(bool showInterface, bool usingCamera, bool expected)
    {
        Assert.Equal(expected, HudVisibility.IsPlayerHudVisible(showInterface, usingCamera));
    }

    [Theory]
    [InlineData(true, false, false, true)]
    [InlineData(false, false, true, true)]
    [InlineData(false, false, false, false)]
    [InlineData(true, true, true, false)]
    public void Hotbar_ShowsWithHiddenInterfaceWhileAnItemWindowIsOpen(bool showInterface, bool usingCamera, bool itemWindowOpen, bool expected)
    {
        Assert.Equal(expected, HudVisibility.IsHotbarVisible(showInterface, usingCamera, itemWindowOpen));
    }
}
