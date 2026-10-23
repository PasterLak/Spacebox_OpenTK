using Engine.Networking;
using OpenTK.Mathematics;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Player.Building;
using Spacebox.Network.Messages;

namespace Spacebox.Tests.Network;

public class PasteBlocksMessageTests
{
    [Fact]
    public void Message_RoundTrips()
    {
        var message = new PasteBlocksMessage
        {
            EntityId = 42,
            Min = new Vector3i(-5, 3, 7),
            Size = new Vector3i(4, 2, 3),
            Cells = new List<ClipboardCell>
            {
                new ClipboardCell(new Vector3i(0, 0, 0), 5, Direction.Up, Rotation.None),
                new ClipboardCell(new Vector3i(3, 1, 2), 12, Direction.Left, Rotation.Half)
            }
        };

        var writer = new NetWriter(64);
        message.Serialize(writer);
        var read = new PasteBlocksMessage();
        read.Deserialize(new NetReader(writer.Span.ToArray()));

        Assert.Equal(message.EntityId, read.EntityId);
        Assert.Equal(message.Min, read.Min);
        Assert.Equal(message.Size, read.Size);
        Assert.Equal(message.Cells, read.Cells);
    }
}
