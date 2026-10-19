namespace Engine.Networking.Query;

public static class ServerQueryProtocol
{
    public const uint Magic = 0x31514253;
    public const int ChunkSize = 1024;
    public const int MaxInfoSize = 1100;
    public const int MaxBlobSize = 256 * 1024;
    public const int AllChunks = -1;

    public enum PacketType : byte
    {
        InfoRequest = 1,
        BlobRequest = 2,
        InfoResponse = 101,
        BlobChunk = 102
    }

    public static int GetChunkCount(int blobLength)
    {
        return blobLength <= 0 ? 0 : (blobLength + ChunkSize - 1) / ChunkSize;
    }

    public static ulong ComputeHash(ReadOnlySpan<byte> data)
    {
        ulong hash = 14695981039346656037UL;
        for (int i = 0; i < data.Length; i++)
        {
            hash ^= data[i];
            hash *= 1099511628211UL;
        }
        return hash;
    }

    public static void WriteHeader(NetWriter writer, PacketType type)
    {
        writer.Write(Magic);
        writer.Write((byte)type);
    }

    public static bool TryReadHeader(NetReader reader, out PacketType type)
    {
        type = 0;
        if (reader.Remaining < 5 || reader.ReadUInt() != Magic) return false;
        type = (PacketType)reader.ReadByte();
        return true;
    }
}
