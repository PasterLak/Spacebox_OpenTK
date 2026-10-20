using Engine.Networking;
using OpenTK.Mathematics;

namespace Spacebox.Network.Messages;

public struct BlockDamageMessage : INetMessage
{
    public long EntityId;
    public Vector3i Cell;
    public short BlockId;
    public byte Damage;

    public void Serialize(NetWriter writer)
    {
        writer.Write(EntityId);
        writer.Write(Cell);
        writer.Write(BlockId);
        writer.Write(Damage);
    }

    public void Deserialize(NetReader reader)
    {
        EntityId = reader.ReadLong();
        Cell = reader.ReadVector3i();
        BlockId = reader.ReadShort();
        Damage = reader.ReadByte();
    }
}
