using Engine.Networking;
using OpenTK.Mathematics;

namespace Spacebox.Network.Messages;

public struct EntityDataRequestMessage : INetMessage
{
    public Vector3i Sector;
    public long EntityId;
    public string FileName;

    public void Serialize(NetWriter writer)
    {
        writer.Write(Sector);
        writer.Write(EntityId);
        writer.Write(FileName);
    }

    public void Deserialize(NetReader reader)
    {
        Sector = reader.ReadVector3i();
        EntityId = reader.ReadLong();
        FileName = reader.ReadString();
    }
}

public struct EntityDataMessage : INetMessage
{
    public Vector3i Sector;
    public long EntityId;
    public byte[] Data;

    public void Serialize(NetWriter writer)
    {
        writer.Write(Sector);
        writer.Write(EntityId);
        writer.WriteBytes(Data ?? Array.Empty<byte>());
    }

    public void Deserialize(NetReader reader)
    {
        Sector = reader.ReadVector3i();
        EntityId = reader.ReadLong();
        Data = reader.ReadBytes();
    }
}
