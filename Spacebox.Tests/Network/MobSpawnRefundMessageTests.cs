using Engine.Networking;
using Spacebox.Network.Messages;

namespace Spacebox.Tests.Network;

public class MobSpawnRefundMessageTests
{
    [Fact]
    public void RoundTrip_KeepsItemAndSequence()
    {
        var writer = new NetWriter();
        new MobSpawnRefundMessage { ItemId = 42, Seq = 7 }.Serialize(writer);

        var read = new MobSpawnRefundMessage();
        read.Deserialize(new NetReader(writer.ToArray()));

        Assert.Equal(42, read.ItemId);
        Assert.Equal(7, read.Seq);
    }
}
