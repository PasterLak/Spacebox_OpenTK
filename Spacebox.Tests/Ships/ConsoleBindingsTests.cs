using OpenTK.Mathematics;
using Spacebox.Game.Ships;

namespace Spacebox.Tests.Ships;

public class ConsoleBindingsTests
{
    private static readonly Vector3i Cell = new Vector3i(1, 2, 3);

    [Fact]
    public void Cycle_GoesThroughGroupsAndBackToUnbound()
    {
        var bindings = new ConsoleBindings(3, false);

        Assert.Equal(0, bindings.Cycle(Cell));
        Assert.Equal(1, bindings.Cycle(Cell));
        Assert.Equal(2, bindings.Cycle(Cell));
        Assert.Equal(-1, bindings.Cycle(Cell));
        Assert.Equal(-1, bindings.GetGroup(Cell));
    }

    [Fact]
    public void Cycle_SkipsGroupsWithoutKey()
    {
        var bindings = new ConsoleBindings(4, false);
        bindings.SetKey(1, ShipKeys.None);
        bindings.SetKey(2, ShipKeys.None);

        Assert.Equal(0, bindings.Cycle(Cell));
        Assert.Equal(3, bindings.Cycle(Cell));
        Assert.Equal(-1, bindings.Cycle(Cell));
    }

    [Fact]
    public void Cycle_Backward_WrapsToLastGroup()
    {
        var bindings = new ConsoleBindings(3, false);

        Assert.Equal(2, bindings.Cycle(Cell, -1));
        Assert.Equal(1, bindings.Cycle(Cell, -1));
        Assert.Equal(0, bindings.Cycle(Cell, -1));
        Assert.Equal(-1, bindings.Cycle(Cell, -1));
    }

    [Fact]
    public void Cycle_NoKeys_StaysUnbound()
    {
        var bindings = new ConsoleBindings(2, false);
        bindings.SetKey(0, ShipKeys.None);
        bindings.SetKey(1, ShipKeys.None);

        Assert.Equal(-1, bindings.Cycle(Cell));
    }

    [Fact]
    public void DefaultKeys_WeaponsUseLeftMouse()
    {
        Assert.Equal(ShipKeys.MouseLeft, new ConsoleBindings(1, true).GetKey(0));
        Assert.NotEqual(ShipKeys.MouseLeft, new ConsoleBindings(1, false).GetKey(0));
    }

    [Fact]
    public void ReadButtons_SetsBitsOfHeldGroups()
    {
        var bindings = new ConsoleBindings(4, false);
        bindings.SetKey(2, 77);

        ushort buttons = bindings.ReadButtons(key => key == 77);

        Assert.Equal((ushort)(1 << 2), buttons);
        Assert.True(bindings.IsGroupPressed(2, buttons));
        Assert.False(bindings.IsGroupPressed(1, buttons));
    }

    [Fact]
    public void Unbind_ClearsCamera()
    {
        var bindings = new ConsoleBindings(2, false);
        bindings.ToggleCamera(Cell);

        Assert.True(bindings.Unbind(Cell));
        Assert.Null(bindings.Camera);
    }

    [Fact]
    public void Changes_IncrementVersion()
    {
        var bindings = new ConsoleBindings(2, false);
        int version = bindings.Version;

        bindings.Bind(Cell, 1);
        bindings.Bind(Cell, 1);

        Assert.Equal(version + 1, bindings.Version);
    }

    [Fact]
    public void Codec_RoundTrip()
    {
        var bindings = new ConsoleBindings(5, false);
        bindings.Bind(Cell, 3);
        bindings.Bind(new Vector3i(-4, 0, 9), 0);
        bindings.SetKey(4, ShipKeys.MouseRight);
        bindings.SetCamera(new Vector3i(7, 7, 7));

        var copy = ConsoleBindingsCodec.FromBytes(ConsoleBindingsCodec.ToBytes(bindings));

        Assert.Equal(5, copy.GroupCount);
        Assert.False(copy.Weapons);
        Assert.Equal(3, copy.GetGroup(Cell));
        Assert.Equal(0, copy.GetGroup(new Vector3i(-4, 0, 9)));
        Assert.Equal(ShipKeys.MouseRight, copy.GetKey(4));
        Assert.Equal(new Vector3i(7, 7, 7), copy.Camera);
    }

    [Fact]
    public void Codec_EmptyData_ReturnsNull()
    {
        Assert.Null(ConsoleBindingsCodec.FromBytes(Array.Empty<byte>()));
    }
}
