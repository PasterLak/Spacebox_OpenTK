using Engine.Networking;
using OpenTK.Mathematics;
using Spacebox.Game.Generation.Tools;
using Spacebox.Game.Player;
using Spacebox.Network.Messages;

namespace Spacebox.Tests.Network;

public class BulkBlockEditTests
{
    [Fact]
    public void Normalize_SortsCorners()
    {
        var min = new Vector3i(5, -2, 10);
        var max = new Vector3i(-3, 4, 1);

        BulkBlockEdit.Normalize(ref min, ref max);

        Assert.Equal(new Vector3i(-3, -2, 1), min);
        Assert.Equal(new Vector3i(5, 4, 10), max);
    }

    [Fact]
    public void CountCells_IsInclusive()
    {
        Assert.Equal(1, BulkBlockEdit.CountCells(new Vector3i(3, 3, 3), new Vector3i(3, 3, 3)));
        Assert.Equal(8, BulkBlockEdit.CountCells(new Vector3i(1, 1, 1), new Vector3i(0, 0, 0)));
        Assert.Equal(2 * 3 * 4, BulkBlockEdit.CountCells(new Vector3i(0, 0, 0), new Vector3i(1, 2, 3)));
    }

    [Fact]
    public void Validate_AcceptsEraseWithinLimit()
    {
        Assert.True(BulkBlockEdit.TryValidate(Vector3i.Zero, new Vector3i(127, 127, 127), BulkBlockEdit.EraseBlockId, _ => false, out var error));
        Assert.Null(error);
    }

    [Fact]
    public void Validate_RejectsTooLargeSelection()
    {
        Assert.False(BulkBlockEdit.TryValidate(Vector3i.Zero, new Vector3i(128, 127, 127), BulkBlockEdit.EraseBlockId, _ => true, out var error));
        Assert.NotNull(error);
    }

    [Fact]
    public void Validate_ChecksFillBlockExists()
    {
        Assert.True(BulkBlockEdit.TryValidate(Vector3i.Zero, Vector3i.One, 7, id => id == 7, out _));
        Assert.False(BulkBlockEdit.TryValidate(Vector3i.Zero, Vector3i.One, 8, id => id == 7, out _));
        Assert.False(BulkBlockEdit.TryValidate(Vector3i.Zero, Vector3i.One, -1, _ => true, out _));
    }

    [Fact]
    public void Message_RoundTrips()
    {
        var message = new BulkBlockEditMessage { EntityId = 123456789012, Min = new Vector3i(-5, 0, 7), Max = new Vector3i(3, 9, 40), BlockId = 12 };
        var writer = new NetWriter();
        message.Serialize(writer);

        var copy = new BulkBlockEditMessage();
        copy.Deserialize(new NetReader(writer.ToArray()));

        Assert.Equal(message.EntityId, copy.EntityId);
        Assert.Equal(message.Min, copy.Min);
        Assert.Equal(message.Max, copy.Max);
        Assert.Equal(message.BlockId, copy.BlockId);
    }

    [Fact]
    public void BlockPointer_CellUsesEntityIndices()
    {
        var pointer = new BlockPointer(1, null, new Vector3i(-1, 33, 64));

        Assert.Equal(new Vector3i(-1, 33, 64), pointer.Cell);
    }

    [Fact]
    public void Selection_WithoutPoints_IsEmpty()
    {
        CreativeTools.Reset();

        Assert.False(CreativeTools.TryGetSelection(out _, out _, out _));
    }
}
