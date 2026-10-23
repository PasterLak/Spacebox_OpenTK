using Engine;
using Engine.Networking;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Structures;
using Spacebox.Network.Messages;

namespace Spacebox.Network;

public static class SectorNetwork
{
    private const float PendingTimeout = 60f;

    private readonly struct PendingRequest
    {
        public readonly int Peer;
        public readonly Vector3i Sector;
        public readonly float Time;

        public PendingRequest(int peer, Vector3i sector, float time)
        {
            Peer = peer;
            Sector = sector;
            Time = time;
        }
    }

    private static readonly List<PendingRequest> _pending = new List<PendingRequest>();
    private static readonly List<NotGeneratedEntity> _customEntities = new List<NotGeneratedEntity>();

    public static void Register(NetworkManager network)
    {
        network.Messages.Register<SectorManifestRequestMessage>(OnManifestRequest);
        network.Messages.Register<SectorManifestMessage>(OnManifest);
        network.Messages.Register<EntityRemovedMessage>(OnEntityRemoved);
        World.SectorLoaded += OnSectorLoaded;
        Sector.EntityDestroyedPermanently += OnEntityDestroyed;
    }

    public static void Update()
    {
        if (_pending.Count == 0) return;

        var network = NetworkManager.Instance;
        var world = World.Instance;

        for (int i = _pending.Count - 1; i >= 0; i--)
        {
            var request = _pending[i];

            if (!network.Peers.Contains(request.Peer) || Time.Total - request.Time > PendingTimeout)
            {
                _pending.RemoveAt(i);
                continue;
            }

            if (world == null || !world.TryGetSector(request.Sector, out var sector)) continue;

            SendManifest(request.Peer, sector);
            _pending.RemoveAt(i);
        }
    }

    public static void Clear()
    {
        _pending.Clear();
    }

    private static void OnSectorLoaded(Sector sector)
    {
        if (NetworkManager.Instance.IsRemoteClient)
            NetworkManager.Instance.SendToServer(new SectorManifestRequestMessage { Sector = sector.PositionIndex });
    }

    private static void OnManifestRequest(int sender, SectorManifestRequestMessage message)
    {
        if (!NetworkManager.Instance.IsServer || sender == NetworkManager.Instance.LocalPeerId) return;

        var world = World.Instance;
        if (world != null && world.TryGetSector(message.Sector, out var sector)) SendManifest(sender, sector);
        else if (CountPending(sender) < MaxPendingPerPeer) _pending.Add(new PendingRequest(sender, message.Sector, Time.Total));
    }

    private const int MaxPendingPerPeer = 64;

    private static int CountPending(int peer)
    {
        int count = 0;
        for (int i = 0; i < _pending.Count; i++)
            if (_pending[i].Peer == peer) count++;
        return count;
    }

    public static void BroadcastManifest(Sector sector)
    {
        if (sector == null) return;
        foreach (int peer in NetworkManager.Instance.Peers) SendManifest(peer, sector);
    }

    private static void SendManifest(int peer, Sector sector)
    {
        _customEntities.Clear();
        sector.CollectCustomEntities(_customEntities);

        NetworkManager.Instance.Send(peer, new SectorManifestMessage
        {
            Sector = sector.PositionIndex,
            Suppressed = new List<long>(sector.SuppressedEntities),
            CustomEntities = new List<NotGeneratedEntity>(_customEntities)
        });

        _customEntities.Clear();
    }

    private static void OnManifest(int sender, SectorManifestMessage message)
    {
        if (NetworkManager.Instance.IsServer) return;

        var world = World.Instance;
        if (world == null || !world.TryGetSector(message.Sector, out var sector)) return;

        sector.ApplyRemoteManifest(message.Suppressed, message.CustomEntities);
    }

    private static void OnEntityDestroyed(Sector sector, long entityId)
    {
        var network = NetworkManager.Instance;
        if (!network.IsServer) return;

        network.SendToClients(new EntityRemovedMessage { Sector = sector.PositionIndex, EntityId = entityId });
    }

    private static void OnEntityRemoved(int sender, EntityRemovedMessage message)
    {
        if (NetworkManager.Instance.IsServer) return;

        var world = World.Instance;
        if (world == null || !world.TryGetSector(message.Sector, out var sector)) return;

        sector.RemoveEntityRemote(message.EntityId);
    }
}
