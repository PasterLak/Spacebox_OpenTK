using Engine.Networking;

namespace Spacebox.Network.Messages;

public struct BlockPlaceRefundMessage : INetMessage
{
    public short BlockId;
    public int Seq;

    public void Serialize(NetWriter writer)
    {
        writer.Write(BlockId);
        writer.Write(Seq);
    }

    public void Deserialize(NetReader reader)
    {
        BlockId = reader.ReadShort();
        Seq = reader.ReadInt();
    }
}
