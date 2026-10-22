using Spacebox.Game.Player;

namespace Spacebox.Tests.Game;

public class SavedDeadPlayerTests
{
    [Theory]
    [InlineData(0, true)]
    [InlineData(-5, true)]
    [InlineData(1, false)]
    [InlineData(100, false)]
    public void WasSavedDead_OnlyForNoHealth(int health, bool expected)
    {
        Assert.Equal(expected, PlayerSaveLoadManager.WasSavedDead(health));
    }
}
