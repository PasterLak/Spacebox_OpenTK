using Engine.Networking;
using OpenTK.Mathematics;

namespace Spacebox.Network.Messages;

public struct BlockRepairMessage : INetMessage
{
    public long EntityId;
    public Vector3i Cell;
    public short BlockId;
    public byte Amount;

    public void Serialize(NetWriter writer)
    {
        writer.Write(EntityId);
        writer.Write(Cell);
        writer.Write(BlockId);
        writer.Write(Amount);
    }

    public void Deserialize(NetReader reader)
    {
        EntityId = reader.ReadLong();
        Cell = reader.ReadVector3i();
        BlockId = reader.ReadShort();
        Amount = reader.ReadByte();
    }
}
