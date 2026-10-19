using Engine;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Resource;
using Spacebox.Game.Ships;

namespace Spacebox.Tests.Ships;

public class ThrusterNozzleTests
{
    private static ThrusterBlockData Thruster(int nozzle, byte sx = 1, byte sy = 1, byte sz = 1)
    {
        var data = new BlockData("Thruster", "thruster")
        {
            Size = new Vector3Byte(sx, sy, sz),
            BaseFrontDirection = Direction.Up
        };
        return new ThrusterBlockData(data, new ThrusterJSON { NozzleLength = nozzle });
    }

    private static List<Vector3i> Area(ThrusterBlockData data, Direction direction, Rotation rotation = Rotation.None)
    {
        var cells = new List<Vector3i>();
        data.GetReservedArea(direction, rotation, cells);
        return cells;
    }

    [Fact]
    public void SingleCell_LengthOne_ReservesCellInFrontOfNozzle()
    {
        Assert.Equal(new[] { new Vector3i(0, 1, 0) }, Area(Thruster(1), Direction.Up));
    }

    [Fact]
    public void SingleCell_LengthTwo_ReservesLine()
    {
        var cells = Area(Thruster(2), Direction.Up);

        Assert.Equal(2, cells.Count);
        Assert.Contains(new Vector3i(0, 1, 0), cells);
        Assert.Contains(new Vector3i(0, 2, 0), cells);
    }

    [Theory]
    [InlineData(Direction.Down, 0, -1, 0)]
    [InlineData(Direction.Right, 1, 0, 0)]
    [InlineData(Direction.Left, -1, 0, 0)]
    [InlineData(Direction.Forward, 0, 0, 1)]
    [InlineData(Direction.Back, 0, 0, -1)]
    public void Rotated_FollowsNozzleDirection(Direction direction, int x, int y, int z)
    {
        var cells = Area(Thruster(2), direction);

        Assert.Contains(new Vector3i(x, y, z), cells);
        Assert.Contains(new Vector3i(x * 2, y * 2, z * 2), cells);
    }

    [Fact]
    public void TallThruster_StartsAboveItsTopCell()
    {
        var cells = Area(Thruster(2, 1, 2, 1), Direction.Up);

        Assert.Equal(new[] { new Vector3i(0, 2, 0), new Vector3i(0, 3, 0) }, cells.OrderBy(c => c.Y));
    }

    [Fact]
    public void WideThruster_ReservesWholeNozzleFace()
    {
        var cells = Area(Thruster(2, 2, 1, 2), Direction.Up);

        Assert.Equal(8, cells.Count);
        Assert.All(cells, c => Assert.InRange(c.Y, 1, 2));
        Assert.Equal(4, cells.Count(c => c.Y == 1));
    }

    [Fact]
    public void ZeroLength_ReservesNothing()
    {
        Assert.Empty(Area(Thruster(0), Direction.Up));
    }

    [Theory]
    [InlineData(-3, 0)]
    [InlineData(2, 2)]
    [InlineData(99, ThrusterBlockData.MaxNozzleLength)]
    public void NozzleLength_IsClamped(int json, int expected)
    {
        Assert.Equal(expected, Thruster(json).NozzleLength);
        Assert.True(Thruster(json).AreaMustBeInsideEntity == false);
    }

    [Fact]
    public void Door_ReservedAreaIsItsSlideArea()
    {
        var baseData = new BlockData("Door", "door") { Size = new Vector3Byte(2, 2, 1) };
        var door = new DoorBlockData(baseData, new DoorJSON());
        var expected = new List<Vector3i>();
        var actual = new List<Vector3i>();

        Spacebox.Game.Generation.Doors.DoorSystem.GetSlideArea(door, Direction.Up, Rotation.None, expected);
        door.GetReservedArea(Direction.Up, Rotation.None, actual);

        Assert.Equal(expected, actual);
        Assert.True(door.AreaMustBeInsideEntity);
    }

    [Fact]
    public void Reservations_TrackOwnersAndOverlaps()
    {
        var reservations = new CellReservations();
        var a = new object();
        var b = new object();
        var shared = new Vector3i(0, 1, 0);

        reservations.Set(a, new[] { shared, new Vector3i(0, 2, 0) });
        reservations.Set(b, new[] { shared });

        Assert.True(reservations.IsReserved(shared));
        reservations.Remove(a);
        Assert.True(reservations.IsReserved(shared));
        Assert.False(reservations.IsReserved(new Vector3i(0, 2, 0)));
        reservations.Remove(b);
        Assert.False(reservations.IsReserved(shared));
    }

    [Fact]
    public void Reservations_SetReplacesPreviousCellsOfOwner()
    {
        var reservations = new CellReservations();
        var owner = new object();

        reservations.Set(owner, new[] { new Vector3i(1, 0, 0) });
        reservations.Set(owner, new[] { new Vector3i(2, 0, 0) });

        Assert.False(reservations.IsReserved(new Vector3i(1, 0, 0)));
        Assert.True(reservations.IsReserved(new Vector3i(2, 0, 0)));
    }
}
