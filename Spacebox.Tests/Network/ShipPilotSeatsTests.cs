using OpenTK.Mathematics;
using Spacebox.Network;

namespace Spacebox.Tests.Network;

public class ShipPilotSeatsTests
{
    private static readonly PilotSeat Console = new PilotSeat(7, new Vector3i(1, 2, 3));
    private static readonly PilotSeat OtherConsole = new PilotSeat(7, new Vector3i(4, 2, 3));

    [Fact]
    public void TryTake_GivesSeatToFirstPeerOnly()
    {
        var seats = new ShipPilotSeats();

        Assert.True(seats.TryTake(2, Console));
        Assert.False(seats.TryTake(3, Console));
        Assert.True(seats.IsPilotOf(2, Console));
        Assert.False(seats.IsPilotOf(3, Console));
    }

    [Fact]
    public void TryTake_SameSeatAgain_KeepsPilot()
    {
        var seats = new ShipPilotSeats();
        seats.TryTake(2, Console);

        Assert.True(seats.TryTake(2, Console));
        Assert.Equal(1, seats.Count);
    }

    [Fact]
    public void TryTake_OtherSeat_MovesPilot()
    {
        var seats = new ShipPilotSeats();
        seats.TryTake(2, Console);

        Assert.True(seats.TryTake(2, OtherConsole));
        Assert.False(seats.IsTaken(Console));
        Assert.True(seats.IsPilotOf(2, OtherConsole));
    }

    [Fact]
    public void Release_AfterPilotDied_FreesSeatAndRejectsHisCommands()
    {
        var seats = new ShipPilotSeats();
        seats.TryTake(2, Console);

        Assert.True(seats.TryRelease(2, out var released));

        Assert.Equal(Console, released);
        Assert.False(seats.IsPilotOf(2, Console));
        Assert.False(seats.IsTaken(Console));
        Assert.True(seats.TryTake(3, Console));
    }

    [Fact]
    public void Release_UnknownPeer_DoesNothing()
    {
        var seats = new ShipPilotSeats();
        seats.TryTake(2, Console);

        Assert.False(seats.TryRelease(5, out _));
        Assert.True(seats.IsPilotOf(2, Console));
    }

    [Fact]
    public void Clear_RemovesAllPilots()
    {
        var seats = new ShipPilotSeats();
        seats.TryTake(2, Console);
        seats.TryTake(3, OtherConsole);

        seats.Clear();

        Assert.Equal(0, seats.Count);
        Assert.False(seats.IsTaken(Console));
        Assert.False(seats.IsTaken(OtherConsole));
    }

    [Fact]
    public void TryReleaseSeat_FreesSeatAndReturnsPilot()
    {
        var seats = new ShipPilotSeats();
        seats.TryTake(2, Console);
        seats.TryTake(3, OtherConsole);

        Assert.True(seats.TryReleaseSeat(Console, out int peer));
        Assert.Equal(2, peer);
        Assert.False(seats.IsTaken(Console));
        Assert.True(seats.IsPilotOf(3, OtherConsole));
        Assert.False(seats.TryReleaseSeat(Console, out _));
    }
}
