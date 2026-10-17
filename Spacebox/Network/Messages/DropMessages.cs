using Engine.Networking;
using OpenTK.Mathematics;

namespace Spacebox.Network.Messages;

public struct DropSpawnMessage : INetMessage
{
    public uint NetId;
    public short ItemId;
    public int Quantity;
    public Vector3 Position;
    public Vector3 Velocity;
    public float PickupDelay;
    public float Deceleration;
    public long SourceEntityId;
    public bool FromBlock;
    public byte ColorR;
    public byte ColorG;
    public byte ColorB;

    public void Serialize(NetWriter writer)
    {
        writer.Write(NetId);
        writer.Write(ItemId);
        writer.Write(Quantity);
        writer.Write(Position);
        writer.Write(Velocity);
        writer.Write(PickupDelay);
        writer.Write(Deceleration);
        writer.Write(SourceEntityId);
        writer.Write(FromBlock);
        writer.Write(ColorR);
        writer.Write(ColorG);
        writer.Write(ColorB);
    }

    public void Deserialize(NetReader reader)
    {
        NetId = reader.ReadUInt();
        ItemId = reader.ReadShort();
        Quantity = reader.ReadInt();
        Position = reader.ReadVector3();
        Velocity = reader.ReadVector3();
        PickupDelay = reader.ReadFloat();
        Deceleration = reader.ReadFloat();
        SourceEntityId = reader.ReadLong();
        FromBlock = reader.ReadBool();
        ColorR = reader.ReadByte();
        ColorG = reader.ReadByte();
        ColorB = reader.ReadByte();
    }
}

public struct DropSpawnRequestMessage : INetMessage
{
    public short ItemId;
    public int Quantity;
    public Vector3 Position;
    public Vector3 Velocity;
    public float PickupDelay;
    public float Deceleration;
    public long SourceEntityId;

    public void Serialize(NetWriter writer)
    {
        writer.Write(ItemId);
        writer.Write(Quantity);
        writer.Write(Position);
        writer.Write(Velocity);
        writer.Write(PickupDelay);
        writer.Write(Deceleration);
        writer.Write(SourceEntityId);
    }

    public void Deserialize(NetReader reader)
    {
        ItemId = reader.ReadShort();
        Quantity = reader.ReadInt();
        Position = reader.ReadVector3();
        Velocity = reader.ReadVector3();
        PickupDelay = reader.ReadFloat();
        Deceleration = reader.ReadFloat();
        SourceEntityId = reader.ReadLong();
    }
}

public struct DropPickupRequestMessage : INetMessage
{
    public uint NetId;

    public void Serialize(NetWriter writer) => writer.Write(NetId);
    public void Deserialize(NetReader reader) => NetId = reader.ReadUInt();
}

public struct DropPickupGrantedMessage : INetMessage
{
    public short ItemId;
    public int Quantity;
    public Vector3 Position;

    public void Serialize(NetWriter writer)
    {
        writer.Write(ItemId);
        writer.Write(Quantity);
        writer.Write(Position);
    }

    public void Deserialize(NetReader reader)
    {
        ItemId = reader.ReadShort();
        Quantity = reader.ReadInt();
        Position = reader.ReadVector3();
    }
}

public struct DropRemovedMessage : INetMessage
{
    public uint NetId;

    public void Serialize(NetWriter writer) => writer.Write(NetId);
    public void Deserialize(NetReader reader) => NetId = reader.ReadUInt();
}

public struct DropUpdateMessage : INetMessage
{
    public uint NetId;
    public int Quantity;
    public Vector3 Position;

    public void Serialize(NetWriter writer)
    {
        writer.Write(NetId);
        writer.Write(Quantity);
        writer.Write(Position);
    }

    public void Deserialize(NetReader reader)
    {
        NetId = reader.ReadUInt();
        Quantity = reader.ReadInt();
        Position = reader.ReadVector3();
    }
}
