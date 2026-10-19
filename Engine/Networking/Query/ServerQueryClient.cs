using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using static Engine.Networking.Query.ServerQueryProtocol;

namespace Engine.Networking.Query;

public sealed class ServerQueryClient : IDisposable
{
    private const int MaxBlobAttempts = 3;

    private sealed class PendingRequest
    {
        public string Key;
        public double SentAt;
        public bool Broadcast;
    }

    private sealed class BlobAssembly
    {
        public IPEndPoint EndPoint;
        public byte[] Data;
        public bool[] Received;
        public int Remaining;
        public double LastRequest;
        public int Attempts;
    }

    private readonly IQuerySocket _socket;
    private readonly Func<double> _clock;
    private readonly NetWriter _writer = new NetWriter(64);
    private readonly NetReader _reader = new NetReader();
    private readonly Dictionary<int, PendingRequest> _pending = new Dictionary<int, PendingRequest>();
    private readonly Dictionary<ulong, BlobAssembly> _blobs = new Dictionary<ulong, BlobAssembly>();
    private readonly ConcurrentQueue<(string key, IPEndPoint endPoint)> _resolved = new ConcurrentQueue<(string, IPEndPoint)>();
    private readonly List<int> _expired = new List<int>();
    private readonly List<ulong> _expiredBlobs = new List<ulong>();
    private bool _started;
    private int _nextRequestId = 1;

    public float Timeout { get; set; } = 3f;

    public event Action<ServerQueryResult> InfoReceived;
    public event Action<string> InfoTimedOut;
    public event Action<ulong, byte[]> BlobReceived;

    public ServerQueryClient(IQuerySocket socket, Func<double> clock = null)
    {
        _socket = socket ?? throw new ArgumentNullException(nameof(socket));
        _clock = clock ?? CreateStopwatchClock();
        _socket.Received += OnReceived;
    }

    public void RequestInfo(string key, string host, int port)
    {
        if (string.IsNullOrWhiteSpace(host) || port <= 0 || port > ushort.MaxValue)
        {
            InfoTimedOut?.Invoke(key);
            return;
        }

        if (IPAddress.TryParse(host.Trim(), out var address))
        {
            SendInfoRequest(key, new IPEndPoint(address, port));
            return;
        }

        ResolveAsync(key, host.Trim(), port);
    }

    public void Discover(int port, IReadOnlyList<IPAddress> directedBroadcasts = null)
    {
        if (!EnsureStarted()) return;

        int requestId = Register(null, true);
        WriteInfoRequest(requestId);
        _socket.Broadcast(port, _writer.Buffer, 0, _writer.Length);

        if (directedBroadcasts == null) return;

        for (int i = 0; i < directedBroadcasts.Count; i++)
            _socket.Send(new IPEndPoint(directedBroadcasts[i], port), _writer.Buffer, 0, _writer.Length);
    }

    public void RequestBlob(IPEndPoint endPoint, ulong hash, int length)
    {
        if (endPoint == null || length <= 0 || length > MaxBlobSize || _blobs.ContainsKey(hash)) return;
        if (!EnsureStarted()) return;

        int count = GetChunkCount(length);
        _blobs[hash] = new BlobAssembly
        {
            EndPoint = endPoint,
            Data = new byte[length],
            Received = new bool[count],
            Remaining = count,
            LastRequest = _clock()
        };
        SendBlobRequest(endPoint, hash, AllChunks);
    }

    public void Update()
    {
        while (_resolved.TryDequeue(out var item))
        {
            if (item.endPoint == null) InfoTimedOut?.Invoke(item.key);
            else SendInfoRequest(item.key, item.endPoint);
        }

        if (_started) _socket.Poll();

        ExpireRequests();
        RetryBlobs();
    }

    public void Dispose()
    {
        _socket.Received -= OnReceived;
        _socket.Dispose();
        _pending.Clear();
        _blobs.Clear();
    }

    private bool EnsureStarted()
    {
        if (!_started) _started = _socket.Start();
        return _started;
    }

    private int Register(string key, bool broadcast)
    {
        int requestId = _nextRequestId++;
        _pending[requestId] = new PendingRequest { Key = key, SentAt = _clock(), Broadcast = broadcast };
        return requestId;
    }

    private void SendInfoRequest(string key, IPEndPoint endPoint)
    {
        if (!EnsureStarted())
        {
            InfoTimedOut?.Invoke(key);
            return;
        }

        int requestId = Register(key, false);
        WriteInfoRequest(requestId);
        _socket.Send(endPoint, _writer.Buffer, 0, _writer.Length);
    }

    private void WriteInfoRequest(int requestId)
    {
        _writer.Reset();
        WriteHeader(_writer, PacketType.InfoRequest);
        _writer.Write(requestId);
    }

    private void SendBlobRequest(IPEndPoint endPoint, ulong hash, int chunkIndex)
    {
        _writer.Reset();
        WriteHeader(_writer, PacketType.BlobRequest);
        _writer.Write(hash);
        _writer.Write(chunkIndex);
        _socket.Send(endPoint, _writer.Buffer, 0, _writer.Length);
    }

    private void ResolveAsync(string key, string host, int port)
    {
        Task.Run(async () =>
        {
            IPEndPoint endPoint = null;
            try
            {
                var addresses = await Dns.GetHostAddressesAsync(host).ConfigureAwait(false);
                var address = Array.Find(addresses, a => a.AddressFamily == AddressFamily.InterNetwork) ?? (addresses.Length > 0 ? addresses[0] : null);
                if (address != null) endPoint = new IPEndPoint(address, port);
            }
            catch (Exception ex)
            {
                Debug.Warning($"[ServerQuery] Could not resolve {host}: {ex.Message}");
            }
            _resolved.Enqueue((key, endPoint));
        });
    }

    private void OnReceived(IPEndPoint sender, byte[] data, int offset, int length)
    {
        try
        {
            _reader.Set(data, offset, length);
            if (!TryReadHeader(_reader, out var type)) return;

            if (type == PacketType.InfoResponse) ReadInfo(sender);
            else if (type == PacketType.BlobChunk) ReadChunk();
        }
        catch (Exception ex)
        {
            Debug.Warning($"[ServerQuery] Ignored malformed response from {sender}: {ex.Message}");
        }
    }

    private void ReadInfo(IPEndPoint sender)
    {
        int requestId = _reader.ReadInt();
        ulong blobHash = _reader.ReadULong();
        int blobLength = _reader.ReadInt();
        byte[] info = _reader.ReadBytes();

        if (!_pending.TryGetValue(requestId, out var request)) return;
        if (!request.Broadcast) _pending.Remove(requestId);

        InfoReceived?.Invoke(new ServerQueryResult
        {
            Key = request.Key,
            EndPoint = sender,
            Info = info,
            RoundTripMs = (int)Math.Round((_clock() - request.SentAt) * 1000.0),
            BlobHash = blobHash,
            BlobLength = blobLength,
            FromBroadcast = request.Broadcast
        });
    }

    private void ReadChunk()
    {
        ulong hash = _reader.ReadULong();
        int index = _reader.ReadInt();
        int count = _reader.ReadInt();
        int total = _reader.ReadInt();
        byte[] chunk = _reader.ReadBytes();

        if (!_blobs.TryGetValue(hash, out var blob)) return;
        if (total != blob.Data.Length || count != blob.Received.Length || index < 0 || index >= count) return;
        if (blob.Received[index]) return;

        int start = index * ChunkSize;
        int expected = Math.Min(ChunkSize, total - start);
        if (chunk.Length != expected) return;

        Buffer.BlockCopy(chunk, 0, blob.Data, start, expected);
        blob.Received[index] = true;
        blob.Remaining--;

        if (blob.Remaining > 0) return;

        _blobs.Remove(hash);
        if (ComputeHash(blob.Data) == hash) BlobReceived?.Invoke(hash, blob.Data);
    }

    private void ExpireRequests()
    {
        if (_pending.Count == 0) return;

        double now = _clock();
        _expired.Clear();
        foreach (var pair in _pending)
        {
            if (now - pair.Value.SentAt >= Timeout) _expired.Add(pair.Key);
        }

        foreach (int requestId in _expired)
        {
            var request = _pending[requestId];
            _pending.Remove(requestId);
            if (!request.Broadcast) InfoTimedOut?.Invoke(request.Key);
        }
    }

    private void RetryBlobs()
    {
        if (_blobs.Count == 0) return;

        double now = _clock();
        _expiredBlobs.Clear();
        foreach (var pair in _blobs)
        {
            var blob = pair.Value;
            if (now - blob.LastRequest < Timeout) continue;

            if (++blob.Attempts >= MaxBlobAttempts)
            {
                _expiredBlobs.Add(pair.Key);
                continue;
            }

            blob.LastRequest = now;
            for (int i = 0; i < blob.Received.Length; i++)
            {
                if (!blob.Received[i]) SendBlobRequest(blob.EndPoint, pair.Key, i);
            }
        }

        foreach (ulong hash in _expiredBlobs)
            _blobs.Remove(hash);
    }

    private static Func<double> CreateStopwatchClock()
    {
        var stopwatch = Stopwatch.StartNew();
        return () => stopwatch.Elapsed.TotalSeconds;
    }
}
