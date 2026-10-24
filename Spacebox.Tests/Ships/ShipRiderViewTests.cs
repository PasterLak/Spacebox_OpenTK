using Spacebox.Game.Ships;

namespace Spacebox.Tests.Ships;

public class ShipRiderViewTests
{
    [Fact]
    public void SeatedPilotWithoutCamera_KeepsTheirView()
    {
        Assert.False(ShipRider.TurnsView(true, false));
    }

    [Fact]
    public void WalkingRidersAndCameraPilots_TurnWithTheShip()
    {
        Assert.True(ShipRider.TurnsView(false, false));
        Assert.True(ShipRider.TurnsView(true, true));
    }
}
