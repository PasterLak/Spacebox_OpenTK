namespace Engine.Networking;

public struct AssignPeerIdMessage : INetMessage
{
    public int PeerId;

    public void Serialize(NetWriter writer) => writer.Write(PeerId);
    public void Deserialize(NetReader reader) => PeerId = reader.ReadInt();
}

public struct SpawnObjectMessage : INetMessage
{
    public uint NetId;
    public int OwnerPeer;
    public string PrefabKey;
    public byte[] SpawnData;
    public byte[] State;

    public void Serialize(NetWriter writer)
    {
        writer.Write(NetId);
        writer.Write(OwnerPeer);
        writer.Write(PrefabKey);
        writer.WriteBytes(SpawnData ?? Array.Empty<byte>());
        writer.WriteBytes(State ?? Array.Empty<byte>());
    }

    public void Deserialize(NetReader reader)
    {
        NetId = reader.ReadUInt();
        OwnerPeer = reader.ReadInt();
        PrefabKey = reader.ReadString();
        SpawnData = reader.ReadBytes();
        State = reader.ReadBytes();
    }
}

public struct DespawnObjectMessage : INetMessage
{
    public uint NetId;

    public void Serialize(NetWriter writer) => writer.Write(NetId);
    public void Deserialize(NetReader reader) => NetId = reader.ReadUInt();
}

public struct OwnershipMessage : INetMessage
{
    public uint NetId;
    public int OwnerPeer;

    public void Serialize(NetWriter writer)
    {
        writer.Write(NetId);
        writer.Write(OwnerPeer);
    }

    public void Deserialize(NetReader reader)
    {
        NetId = reader.ReadUInt();
        OwnerPeer = reader.ReadInt();
    }
}

public struct RpcMessage : INetMessage
{
    public uint NetId;
    public byte Behaviour;
    public uint MethodId;
    public byte[] Payload;

    public void Serialize(NetWriter writer)
    {
        writer.Write(NetId);
        writer.Write(Behaviour);
        writer.Write(MethodId);
        writer.WriteBytes(Payload ?? Array.Empty<byte>());
    }

    public void Deserialize(NetReader reader)
    {
        NetId = reader.ReadUInt();
        Behaviour = reader.ReadByte();
        MethodId = reader.ReadUInt();
        Payload = reader.ReadBytes();
    }
}

public struct VariablesMessage : INetMessage
{
    public uint NetId;
    public byte[] Payload;

    public void Serialize(NetWriter writer)
    {
        writer.Write(NetId);
        writer.WriteBytes(Payload ?? Array.Empty<byte>());
    }

    public void Deserialize(NetReader reader)
    {
        NetId = reader.ReadUInt();
        Payload = reader.ReadBytes();
    }
}
