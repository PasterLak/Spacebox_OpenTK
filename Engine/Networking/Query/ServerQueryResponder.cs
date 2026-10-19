using System.Net;
using static Engine.Networking.Query.ServerQueryProtocol;

namespace Engine.Networking.Query;

public sealed class ServerQueryResponder
{
    private readonly NetWriter _writer = new NetWriter(ChunkSize + 64);
    private readonly NetReader _reader = new NetReader();
    private byte[] _blob;
    private ulong _blobHash;

    public Func<byte[]> InfoProvider { get; set; }
    public Func<byte[]> BlobProvider { get; set; }
    public bool AnswerBroadcasts { get; set; } = true;

    public void Reset()
    {
        InfoProvider = null;
        BlobProvider = null;
        AnswerBroadcasts = true;
        _blob = null;
        _blobHash = 0;
    }

    public bool Handle(IPEndPoint sender, byte[] data, int offset, int length, bool isBroadcast, Action<IPEndPoint, byte[], int, int> send)
    {
        if (InfoProvider == null || send == null) return false;
        if (isBroadcast && !AnswerBroadcasts) return false;

        try
        {
            _reader.Set(data, offset, length);
            if (!TryReadHeader(_reader, out var type)) return false;

            switch (type)
            {
                case PacketType.InfoRequest:
                    SendInfo(sender, _reader.ReadInt(), send);
                    return true;
                case PacketType.BlobRequest:
                    SendBlob(sender, _reader.ReadULong(), _reader.ReadInt(), send);
                    return true;
                default:
                    return false;
            }
        }
        catch (Exception ex)
        {
            Debug.Warning($"[ServerQuery] Ignored malformed query from {sender}: {ex.Message}");
            return false;
        }
    }

    private void SendInfo(IPEndPoint sender, int requestId, Action<IPEndPoint, byte[], int, int> send)
    {
        var info = InfoProvider();
        if (info == null) return;
        if (info.Length > MaxInfoSize)
        {
            Debug.Warning($"[ServerQuery] Info payload is {info.Length} bytes, limit is {MaxInfoSize}");
            return;
        }

        RefreshBlob();

        _writer.Reset();
        WriteHeader(_writer, PacketType.InfoResponse);
        _writer.Write(requestId);
        _writer.Write(_blobHash);
        _writer.Write(_blob?.Length ?? 0);
        _writer.WriteBytes(info);
        send(sender, _writer.Buffer, 0, _writer.Length);
    }

    private void SendBlob(IPEndPoint sender, ulong hash, int chunkIndex, Action<IPEndPoint, byte[], int, int> send)
    {
        RefreshBlob();
        if (_blob == null || hash != _blobHash) return;

        int count = GetChunkCount(_blob.Length);
        if (chunkIndex == AllChunks)
        {
            for (int i = 0; i < count; i++)
                SendChunk(sender, i, count, send);
            return;
        }

        if (chunkIndex >= 0 && chunkIndex < count)
            SendChunk(sender, chunkIndex, count, send);
    }

    private void SendChunk(IPEndPoint sender, int index, int count, Action<IPEndPoint, byte[], int, int> send)
    {
        int start = index * ChunkSize;
        int size = Math.Min(ChunkSize, _blob.Length - start);

        _writer.Reset();
        WriteHeader(_writer, PacketType.BlobChunk);
        _writer.Write(_blobHash);
        _writer.Write(index);
        _writer.Write(count);
        _writer.Write(_blob.Length);
        _writer.WriteBytes(new ReadOnlySpan<byte>(_blob, start, size));
        send(sender, _writer.Buffer, 0, _writer.Length);
    }

    private void RefreshBlob()
    {
        var blob = BlobProvider?.Invoke();
        if (blob != null && blob.Length > MaxBlobSize) blob = null;
        if (ReferenceEquals(blob, _blob)) return;

        _blob = blob;
        _blobHash = blob == null ? 0 : ComputeHash(blob);
    }
}
