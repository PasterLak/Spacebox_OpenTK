using Engine.Networking;

namespace Spacebox.Network.Messages;

public struct JoinRequestMessage : INetMessage
{
    public string Name;
    public string SkinColor;
    public string GameVersion;
    public string ModId;

    public void Serialize(NetWriter writer)
    {
        writer.Write(Name);
        writer.Write(SkinColor);
        writer.Write(GameVersion);
        writer.Write(ModId);
    }

    public void Deserialize(NetReader reader)
    {
        Name = reader.ReadString();
        SkinColor = reader.ReadString();
        GameVersion = reader.ReadString();
        ModId = reader.ReadString();
    }
}

public struct JoinAcceptedMessage : INetMessage
{
    public string ServerName;
    public string WorldName;
    public string Seed;
    public string ModId;
    public byte GameMode;
    public int Day;
    public int Ticks;
    public string PlayerJson;

    public void Serialize(NetWriter writer)
    {
        writer.Write(ServerName);
        writer.Write(WorldName);
        writer.Write(Seed);
        writer.Write(ModId);
        writer.Write(GameMode);
        writer.Write(Day);
        writer.Write(Ticks);
        writer.Write(PlayerJson);
    }

    public void Deserialize(NetReader reader)
    {
        ServerName = reader.ReadString();
        WorldName = reader.ReadString();
        Seed = reader.ReadString();
        ModId = reader.ReadString();
        GameMode = reader.ReadByte();
        Day = reader.ReadInt();
        Ticks = reader.ReadInt();
        PlayerJson = reader.ReadString();
    }
}

public struct JoinRejectedMessage : INetMessage
{
    public string Reason;

    public void Serialize(NetWriter writer) => writer.Write(Reason);
    public void Deserialize(NetReader reader) => Reason = reader.ReadString();
}

public struct ClientReadyMessage : INetMessage
{
    public void Serialize(NetWriter writer)
    {
    }

    public void Deserialize(NetReader reader)
    {
    }
}

public struct PlayerDataMessage : INetMessage
{
    public string Json;

    public void Serialize(NetWriter writer) => writer.Write(Json);
    public void Deserialize(NetReader reader) => Json = reader.ReadString();
}
