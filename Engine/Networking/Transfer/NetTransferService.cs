namespace Engine.Networking;

public sealed class NetTransferService
{
    public const int ChunkSize = 32 * 1024;
    public const int MaxTransferSize = 512 * 1024 * 1024;
    private const int ChunksPerUpdate = 16;

    private sealed class OutgoingTransfer
    {
        public int Peer;
        public int RequestId;
        public byte[] Data;
        public int Sent;
    }

    private sealed class IncomingTransfer
    {
        public byte[] Buffer;
        public int Received;
        public Action<byte[]> Completed;
        public Action<string> Failed;
        public Action<float> Progress;
    }

    private readonly NetworkManager _network;
    private readonly Dictionary<string, Func<byte[]>> _providers = new Dictionary<string, Func<byte[]>>();
    private readonly List<OutgoingTransfer> _outgoing = new List<OutgoingTransfer>();
    private readonly Dictionary<int, IncomingTransfer> _incoming = new Dictionary<int, IncomingTransfer>();
    private int _nextRequestId = 1;

    internal NetTransferService(NetworkManager network)
    {
        _network = network;
        network.Messages.Register<TransferRequestMessage>(OnRequest);
        network.Messages.Register<TransferChunkMessage>(OnChunk);
        network.Messages.Register<TransferFailedMessage>(OnFailed);
        network.PeerDisconnected += (peer, _) => _outgoing.RemoveAll(transfer => transfer.Peer == peer);
        network.Stopped += OnStopped;
    }

    public bool IsReceiving => _incoming.Count > 0;

    public void Serve(string name, Func<byte[]> provider)
    {
        _providers[name] = provider;
    }

    public void StopServing(string name)
    {
        _providers.Remove(name);
    }

    public void Request(string name, Action<byte[]> completed, Action<string> failed, Action<float> progress = null)
    {
        int requestId = _nextRequestId++;
        _incoming[requestId] = new IncomingTransfer { Completed = completed, Failed = failed, Progress = progress };
        _network.SendToServer(new TransferRequestMessage { RequestId = requestId, Name = name });
    }

    internal void Update()
    {
        for (int i = _outgoing.Count - 1; i >= 0; i--)
        {
            var transfer = _outgoing[i];

            for (int chunk = 0; chunk < ChunksPerUpdate && transfer.Sent < transfer.Data.Length; chunk++)
                SendChunk(transfer);

            if (transfer.Sent >= transfer.Data.Length) _outgoing.RemoveAt(i);
        }
    }

    private void SendChunk(OutgoingTransfer transfer)
    {
        int length = Math.Min(ChunkSize, transfer.Data.Length - transfer.Sent);
        _network.Send(transfer.Peer, new TransferChunkMessage
        {
            RequestId = transfer.RequestId,
            Total = transfer.Data.Length,
            Offset = transfer.Sent,
            Data = transfer.Data,
            DataOffset = transfer.Sent,
            DataLength = length
        });
        transfer.Sent += length;
    }

    private void OnRequest(int sender, TransferRequestMessage message)
    {
        if (!_network.IsServer) return;

        byte[] data = null;
        string error = null;

        if (HasActiveTransfer(sender))
        {
            error = "Another transfer is still running";
        }
        else if (string.IsNullOrEmpty(message.Name) || !_providers.TryGetValue(message.Name, out var provider))
        {
            error = $"Unknown transfer '{message.Name}'";
        }
        else
        {
            try
            {
                data = provider() ?? Array.Empty<byte>();
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }
        }

        if (error != null)
        {
            Debug.Error($"[Network] Transfer '{message.Name}' for peer {sender} failed: {error}");
            _network.Send(sender, new TransferFailedMessage { RequestId = message.RequestId, Reason = error });
            return;
        }

        var transfer = new OutgoingTransfer { Peer = sender, RequestId = message.RequestId, Data = data };
        if (data.Length == 0) SendChunk(transfer);
        else _outgoing.Add(transfer);
    }

    private bool HasActiveTransfer(int peer)
    {
        for (int i = 0; i < _outgoing.Count; i++)
            if (_outgoing[i].Peer == peer) return true;
        return false;
    }

    private void OnChunk(int sender, TransferChunkMessage message)
    {
        if (sender != NetworkManager.ServerPeerId && !_network.IsServer) return;
        if (!_incoming.TryGetValue(message.RequestId, out var transfer)) return;

        if (!IsValidChunk(message.Total, message.Offset, message.DataLength))
        {
            Fail(message.RequestId, "Invalid transfer data");
            return;
        }

        transfer.Buffer ??= new byte[message.Total];
        if (transfer.Buffer.Length != message.Total)
        {
            Fail(message.RequestId, "Transfer size changed");
            return;
        }

        System.Buffer.BlockCopy(message.Data, message.DataOffset, transfer.Buffer, message.Offset, message.DataLength);
        transfer.Received += message.DataLength;
        transfer.Progress?.Invoke(message.Total == 0 ? 1f : (float)transfer.Received / message.Total);

        if (transfer.Received < message.Total) return;

        _incoming.Remove(message.RequestId);
        transfer.Completed?.Invoke(transfer.Buffer);
    }

    public static bool IsValidChunk(int total, int offset, int length)
    {
        return total >= 0 && total <= MaxTransferSize && offset >= 0 && length >= 0 && (long)offset + length <= total;
    }

    private void OnFailed(int sender, TransferFailedMessage message)
    {
        Fail(message.RequestId, message.Reason);
    }

    private void Fail(int requestId, string reason)
    {
        if (!_incoming.Remove(requestId, out var transfer)) return;
        transfer.Failed?.Invoke(reason);
    }

    private void OnStopped()
    {
        _outgoing.Clear();
        if (_incoming.Count == 0) return;

        var pending = new List<IncomingTransfer>(_incoming.Values);
        _incoming.Clear();

        foreach (var transfer in pending)
            transfer.Failed?.Invoke("Disconnected");
    }
}
