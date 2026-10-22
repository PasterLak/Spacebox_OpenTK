using Engine.Networking;
using Spacebox.Network.Messages;

namespace Spacebox.Tests.Network;

public sealed class PlayerDataMessageTests
{
    [Fact]
    public void DataAndAck_CarryTheSequence()
    {
        var writer = new NetWriter();
        new PlayerDataMessage { Json = "{}", AppliedSeq = 4, Sequence = 9 }.Serialize(writer);
        var data = new PlayerDataMessage();
        data.Deserialize(new NetReader(writer.ToArray()));

        var ackWriter = new NetWriter();
        new PlayerDataAckMessage { Sequence = 9 }.Serialize(ackWriter);
        var ack = new PlayerDataAckMessage();
        ack.Deserialize(new NetReader(ackWriter.ToArray()));

        Assert.Equal("{}", data.Json);
        Assert.Equal(4, data.AppliedSeq);
        Assert.Equal(9, data.Sequence);
        Assert.Equal(9, ack.Sequence);
    }

    [Theory]
    [InlineData("Red", "Red")]
    [InlineData("../../evil", "Yellow")]
    [InlineData("", "Yellow")]
    [InlineData("VeryLongColorNameXX", "Yellow")]
    public void SkinColor_OnlyAllowsShortLetterNames(string color, string expected)
    {
        Assert.Equal(expected, Spacebox.Network.NetworkSession.SafeSkinColor(color));
    }
}
