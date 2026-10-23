using Engine;
using Engine.Networking;
using OpenTK.Mathematics;
using Spacebox.Game.Player.Respawn;

namespace Spacebox.Network.Players;

public struct ReconstructionStatusRequestMessage : INetMessage
{
    public void Serialize(NetWriter writer)
    {
    }

    public void Deserialize(NetReader reader)
    {
    }
}

public struct ReconstructionStatusMessage : INetMessage
{
    public string Blocker;

    public void Serialize(NetWriter writer)
    {
        writer.Write(Blocker ?? "");
    }

    public void Deserialize(NetReader reader)
    {
        Blocker = reader.ReadString();
    }
}

public struct ReconstructionRequestMessage : INetMessage
{
    public int RequestId;
    public Vector3 Position;

    public void Serialize(NetWriter writer)
    {
        writer.Write(RequestId);
        writer.Write(Position);
    }

    public void Deserialize(NetReader reader)
    {
        RequestId = reader.ReadInt();
        Position = reader.ReadVector3();
    }
}

public struct ReconstructionResultMessage : INetMessage
{
    public int RequestId;
    public bool Granted;
    public long EntityId;
    public Vector3i Cell;
    public Vector3 SpawnPosition;
    public Quaternion Rotation;
    public short BlockId;
    public Vector3 Center;

    public static ReconstructionResultMessage From(int requestId, ReconstructionGrant? grant)
    {
        if (grant is not { } value) return new ReconstructionResultMessage { RequestId = requestId, Rotation = Quaternion.Identity };

        return new ReconstructionResultMessage
        {
            RequestId = requestId,
            Granted = true,
            EntityId = value.Key.EntityId,
            Cell = value.Key.Cell,
            SpawnPosition = value.SpawnPosition,
            Rotation = value.Rotation,
            BlockId = value.BlockId,
            Center = value.Center
        };
    }

    public ReconstructionGrant? ToGrant()
    {
        if (!Granted) return null;
        return new ReconstructionGrant(new ReconstructorKey(EntityId, Cell), SpawnPosition, Rotation, BlockId, Center);
    }

    public void Serialize(NetWriter writer)
    {
        writer.Write(RequestId);
        writer.Write(Granted);
        if (!Granted) return;

        writer.Write(EntityId);
        writer.Write(Cell);
        writer.Write(SpawnPosition);
        writer.Write(Rotation);
        writer.Write(BlockId);
        writer.Write(Center);
    }

    public void Deserialize(NetReader reader)
    {
        RequestId = reader.ReadInt();
        Granted = reader.ReadBool();
        Rotation = Quaternion.Identity;
        if (!Granted) return;

        EntityId = reader.ReadLong();
        Cell = reader.ReadVector3i();
        SpawnPosition = reader.ReadVector3();
        Rotation = reader.ReadQuaternion();
        BlockId = reader.ReadShort();
        Center = reader.ReadVector3();
    }
}

public static class ReconstructionNetwork
{
    public const string Checking = "Checking DNA Reconstructors...";
    private const float StatusInterval = 1f;
    private const float RequestTimeout = 180f;

    private sealed class PendingRequest
    {
        public Action<ReconstructionGrant?> Done;
        public float SentAt;
    }

    private static readonly Dictionary<int, PendingRequest> _pending = new Dictionary<int, PendingRequest>();
    private static readonly List<int> _expired = new List<int>();
    private static readonly Dictionary<int, object> _busyPeers = new Dictionary<int, object>();
    private static int _nextRequest = 1;
    private static string _blocker;
    private static float _lastStatusRequest = float.MinValue;

    public static IReconstructionChannel Channel { get; } = new RemoteChannel();

    public static void Register(NetworkManager network)
    {
        network.Messages.Register<ReconstructionStatusRequestMessage>(OnStatusRequest);
        network.Messages.Register<ReconstructionStatusMessage>(OnStatus);
        network.Messages.Register<ReconstructionRequestMessage>(OnRequest);
        network.Messages.Register<ReconstructionResultMessage>(OnResult);
    }

    public static void Update()
    {
        if (_pending.Count == 0) return;

        _expired.Clear();
        foreach (var pair in _pending)
        {
            if (Time.Total - pair.Value.SentAt >= RequestTimeout) _expired.Add(pair.Key);
        }

        for (int i = 0; i < _expired.Count; i++)
        {
            if (!_pending.Remove(_expired[i], out var request)) continue;

            Debug.Warning("[Reconstruction] The server did not answer the reconstruction request");
            request.Done(null);
        }
    }

    public static void Reset()
    {
        _pending.Clear();
        _busyPeers.Clear();
        _blocker = null;
        _lastStatusRequest = float.MinValue;
    }

    private static string GetBlocker()
    {
        if (Time.Total - _lastStatusRequest >= StatusInterval)
        {
            _lastStatusRequest = (float)Time.Total;
            NetworkManager.Instance.SendToServer(new ReconstructionStatusRequestMessage());
        }

        return _blocker ?? Checking;
    }

    private static void Request(Vector3 position, Action<ReconstructionGrant?> done)
    {
        int requestId = _nextRequest++;
        _pending[requestId] = new PendingRequest { Done = done, SentAt = (float)Time.Total };
        NetworkManager.Instance.SendToServer(new ReconstructionRequestMessage { RequestId = requestId, Position = position });
    }

    private static void OnStatusRequest(int sender, ReconstructionStatusRequestMessage message)
    {
        var network = NetworkManager.Instance;
        if (!network.IsServer) return;

        network.Send(sender, new ReconstructionStatusMessage { Blocker = DnaReconstructorRegistry.GetBlocker() });
    }

    private static void OnStatus(int sender, ReconstructionStatusMessage message)
    {
        if (NetworkManager.Instance.IsServer) return;
        _blocker = string.IsNullOrEmpty(message.Blocker) ? null : message.Blocker;
    }

    private const float MaxReportedOffset = 32f;

    public static bool AcceptsReportedPosition(Vector3 tracked, Vector3 reported)
    {
        return NetValidation.IsFinite(reported) && Vector3.DistanceSquared(tracked, reported) <= MaxReportedOffset * MaxReportedOffset;
    }

    private static bool TryGetDeathPosition(AstronautNetwork player, Vector3 reported, out Vector3 position)
    {
        position = reported;
        if (player.Owner is not Spacebox.Game.Player.Astronaut astronaut) return NetValidation.IsFinite(reported);

        position = astronaut.Position;
        return AcceptsReportedPosition(position, reported);
    }

    private static void OnRequest(int sender, ReconstructionRequestMessage message)
    {
        var network = NetworkManager.Instance;
        if (!network.IsServer) return;

        int requestId = message.RequestId;
        var player = AstronautNetwork.Find(sender);
        var token = new object();
        if (player == null || player.IsAlive || !TryGetDeathPosition(player, message.Position, out var position) || !_busyPeers.TryAdd(sender, token))
        {
            network.Send(sender, ReconstructionResultMessage.From(requestId, null));
            return;
        }

        DnaReconstructionService.Request(position, sender, grant =>
        {
            if (!IsCurrent(sender, token)) return;
            _busyPeers.Remove(sender);
            NetworkManager.Instance.Send(sender, ReconstructionResultMessage.From(requestId, grant));
        }, () => IsCurrent(sender, token));
    }

    private static bool IsCurrent(int peer, object token) => _busyPeers.TryGetValue(peer, out var current) && current == token;

    public static void ForgetPeer(int peer)
    {
        _busyPeers.Remove(peer);
    }

    private static void OnResult(int sender, ReconstructionResultMessage message)
    {
        if (NetworkManager.Instance.IsServer) return;
        if (_pending.Remove(message.RequestId, out var request)) request.Done(message.ToGrant());
    }

    private sealed class RemoteChannel : IReconstructionChannel
    {
        public string GetBlocker(Vector3 position) => ReconstructionNetwork.GetBlocker();

        public void Request(Vector3 position, Action<ReconstructionGrant?> done) => ReconstructionNetwork.Request(position, done);
    }
}
