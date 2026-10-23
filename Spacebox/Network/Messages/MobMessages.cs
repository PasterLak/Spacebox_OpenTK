using Engine.Networking;
using OpenTK.Mathematics;

namespace Spacebox.Network.Messages;

public struct MobSpawnRequestMessage : INetMessage
{
    public short ItemId;
    public Vector3 Position;

    public void Serialize(NetWriter writer)
    {
        writer.Write(ItemId);
        writer.Write(Position);
    }

    public void Deserialize(NetReader reader)
    {
        ItemId = reader.ReadShort();
        Position = reader.ReadVector3();
    }
}

public struct MobSpawnRefundMessage : INetMessage
{
    public short ItemId;
    public int Seq;

    public void Serialize(NetWriter writer)
    {
        writer.Write(ItemId);
        writer.Write(Seq);
    }

    public void Deserialize(NetReader reader)
    {
        ItemId = reader.ReadShort();
        Seq = reader.ReadInt();
    }
}
