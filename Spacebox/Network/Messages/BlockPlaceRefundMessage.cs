using Engine.Networking;

namespace Spacebox.Network.Messages;

public struct BlockPlaceRefundMessage : INetMessage
{
    public short BlockId;

    public void Serialize(NetWriter writer)
    {
        writer.Write(BlockId);
    }

    public void Deserialize(NetReader reader)
    {
        BlockId = reader.ReadShort();
    }
}
