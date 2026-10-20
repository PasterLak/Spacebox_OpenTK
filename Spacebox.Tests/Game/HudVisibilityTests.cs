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
}
