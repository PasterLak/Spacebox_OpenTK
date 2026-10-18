using Engine.Networking;
using OpenTK.Mathematics;
using Spacebox.Game.Generation.Structures;

namespace Spacebox.Network.Messages;

public struct SectorManifestRequestMessage : INetMessage
{
    public Vector3i Sector;

    public void Serialize(NetWriter writer) => writer.Write(Sector);
    public void Deserialize(NetReader reader) => Sector = reader.ReadVector3i();
}

public struct SectorManifestMessage : INetMessage
{
    public Vector3i Sector;
    public List<long> Suppressed;
    public List<NotGeneratedEntity> CustomEntities;

    public void Serialize(NetWriter writer)
    {
        writer.Write(Sector);
        writer.Write(Suppressed.Count);
        for (int i = 0; i < Suppressed.Count; i++) writer.Write(Suppressed[i]);

        writer.Write(CustomEntities.Count);
        for (int i = 0; i < CustomEntities.Count; i++)
        {
            var entity = CustomEntities[i];
            writer.Write(entity.Id);
            writer.Write(entity.FileName ?? string.Empty);
            writer.Write(entity.positionInSector);
        }
    }

    public void Deserialize(NetReader reader)
    {
        Sector = reader.ReadVector3i();

        int suppressedCount = reader.ReadInt();
        Suppressed = new List<long>(suppressedCount);
        for (int i = 0; i < suppressedCount; i++) Suppressed.Add(reader.ReadLong());

        int entityCount = reader.ReadInt();
        CustomEntities = new List<NotGeneratedEntity>(entityCount);
        for (int i = 0; i < entityCount; i++)
        {
            CustomEntities.Add(new NotGeneratedEntity
            {
                Id = reader.ReadLong(),
                FileName = reader.ReadString(),
                positionInSector = reader.ReadVector3()
            });
        }
    }
}

public struct EntityRemovedMessage : INetMessage
{
    public Vector3i Sector;
    public long EntityId;

    public void Serialize(NetWriter writer)
    {
        writer.Write(Sector);
        writer.Write(EntityId);
    }

    public void Deserialize(NetReader reader)
    {
        Sector = reader.ReadVector3i();
        EntityId = reader.ReadLong();
    }
}

public struct EntityCreateRequestMessage : INetMessage
{
    public Vector3 PositionWorld;
    public short BlockId;
    public byte Direction;
    public byte Rotation;

    public void Serialize(NetWriter writer)
    {
        writer.Write(PositionWorld);
        writer.Write(BlockId);
        writer.Write(Direction);
        writer.Write(Rotation);
    }

    public void Deserialize(NetReader reader)
    {
        PositionWorld = reader.ReadVector3();
        BlockId = reader.ReadShort();
        Direction = reader.ReadByte();
        Rotation = reader.ReadByte();
    }
}

public struct EntityCreatedMessage : INetMessage
{
    public Vector3i Sector;
    public long EntityId;
    public Vector3 PositionWorld;
    public short BlockId;
    public byte Direction;
    public byte Rotation;

    public void Serialize(NetWriter writer)
    {
        writer.Write(Sector);
        writer.Write(EntityId);
        writer.Write(PositionWorld);
        writer.Write(BlockId);
        writer.Write(Direction);
        writer.Write(Rotation);
    }

    public void Deserialize(NetReader reader)
    {
        Sector = reader.ReadVector3i();
        EntityId = reader.ReadLong();
        PositionWorld = reader.ReadVector3();
        BlockId = reader.ReadShort();
        Direction = reader.ReadByte();
        Rotation = reader.ReadByte();
    }
}
