namespace Engine.Networking;

public struct TransferRequestMessage : INetMessage
{
    public int RequestId;
    public string Name;

    public void Serialize(NetWriter writer)
    {
        writer.Write(RequestId);
        writer.Write(Name);
    }

    public void Deserialize(NetReader reader)
    {
        RequestId = reader.ReadInt();
        Name = reader.ReadString();
    }
}

public struct TransferChunkMessage : INetMessage
{
    public int RequestId;
    public int Total;
    public int Offset;
    public byte[] Data;
    public int DataOffset;
    public int DataLength;

    public void Serialize(NetWriter writer)
    {
        writer.Write(RequestId);
        writer.Write(Total);
        writer.Write(Offset);
        writer.WriteBytes(new ReadOnlySpan<byte>(Data, DataOffset, DataLength));
    }

    public void Deserialize(NetReader reader)
    {
        RequestId = reader.ReadInt();
        Total = reader.ReadInt();
        Offset = reader.ReadInt();
        Data = reader.ReadBytes();
        DataOffset = 0;
        DataLength = Data.Length;
    }
}

public struct TransferFailedMessage : INetMessage
{
    public int RequestId;
    public string Reason;

    public void Serialize(NetWriter writer)
    {
        writer.Write(RequestId);
        writer.Write(Reason);
    }

    public void Deserialize(NetReader reader)
    {
        RequestId = reader.ReadInt();
        Reason = reader.ReadString();
    }
}
