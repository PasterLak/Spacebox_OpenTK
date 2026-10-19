using Engine.Networking;
using OpenTK.Mathematics;

namespace Spacebox.Network.Messages;

public struct BulkBlockEditMessage : INetMessage
{
    public long EntityId;
    public Vector3i Min;
    public Vector3i Max;
    public short BlockId;

    public void Serialize(NetWriter writer)
    {
        writer.Write(EntityId);
        writer.Write(Min);
        writer.Write(Max);
        writer.Write(BlockId);
    }

    public void Deserialize(NetReader reader)
    {
        EntityId = reader.ReadLong();
        Min = reader.ReadVector3i();
        Max = reader.ReadVector3i();
        BlockId = reader.ReadShort();
    }
}
