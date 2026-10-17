using Engine.Networking;
using OpenTK.Mathematics;

namespace Spacebox.Network.Messages;

public struct BlockEditMessage : INetMessage
{
    public long EntityId;
    public Vector3i Cell;
    public short BlockId;
    public byte Direction;
    public byte Rotation;
    public bool IsRemoval;

    public void Serialize(NetWriter writer)
    {
        writer.Write(EntityId);
        writer.Write(Cell);
        writer.Write(BlockId);
        writer.Write(Direction);
        writer.Write(Rotation);
        writer.Write(IsRemoval);
    }

    public void Deserialize(NetReader reader)
    {
        EntityId = reader.ReadLong();
        Cell = reader.ReadVector3i();
        BlockId = reader.ReadShort();
        Direction = reader.ReadByte();
        Rotation = reader.ReadByte();
        IsRemoval = reader.ReadBool();
    }
}
