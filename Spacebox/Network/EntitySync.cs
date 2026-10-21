using Engine;
using Engine.Networking;
using OpenTK.Mathematics;
using SharpNBT;
using Spacebox.Game.GameMath;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Structures;
using Spacebox.Game.Generation.Tools;
using Spacebox.Network.Messages;

namespace Spacebox.Network;

public static class EntitySync
{
    public static void Register(NetworkManager network)
    {
        network.Messages.Register<EntityDataRequestMessage>(OnRequest);
        network.Messages.Register<EntityDataMessage>(OnData);
    }

    public static void Request(Sector sector, NotGeneratedEntity data)
    {
        NetworkManager.Instance.SendToServer(new EntityDataRequestMessage
        {
            Sector = sector.PositionIndex,
            EntityId = data.Id,
            FileName = data.FileName
        });
    }

    private const int MaxQueuedPerPeer = 256;
    private const int MaxRequestsPerFrame = 16;

    private static readonly Queue<(int Peer, EntityDataRequestMessage Message)> _queue = new Queue<(int, EntityDataRequestMessage)>();
    private static readonly Dictionary<int, int> _queuedPerPeer = new Dictionary<int, int>();

    public static void Update()
    {
        if (_queue.Count == 0) return;
        if (!NetworkManager.Instance.IsServer)
        {
            Clear();
            return;
        }

        for (int i = 0; i < MaxRequestsPerFrame && _queue.Count > 0; i++)
        {
            var (peer, message) = _queue.Dequeue();
            if (_queuedPerPeer.TryGetValue(peer, out int count))
            {
                if (count <= 1) _queuedPerPeer.Remove(peer);
                else _queuedPerPeer[peer] = count - 1;
            }
            if (NetworkManager.Instance.Peers.Contains(peer) || peer == NetworkManager.Instance.LocalPeerId) Answer(peer, message);
        }
    }

    public static void Clear()
    {
        _queue.Clear();
        _queuedPerPeer.Clear();
    }

    private static void OnRequest(int sender, EntityDataRequestMessage message)
    {
        if (!NetworkManager.Instance.IsServer) return;

        _queuedPerPeer.TryGetValue(sender, out int queued);
        if (queued >= MaxQueuedPerPeer) return;
        _queuedPerPeer[sender] = queued + 1;
        _queue.Enqueue((sender, message));
    }

    private static void Answer(int sender, EntityDataRequestMessage message)
    {
        var network = NetworkManager.Instance;

        byte[] payload = Array.Empty<byte>();
        try
        {
            var tag = FindEntityTag(message);
            if (tag != null) payload = NbtSerializer.ToBytes(tag);
        }
        catch (Exception ex)
        {
            Debug.Error($"[EntitySync] Failed to serialize entity {message.EntityId}: {ex.Message}");
        }

        network.Send(sender, new EntityDataMessage { Sector = message.Sector, EntityId = message.EntityId, Data = payload });

        var world = World.Instance;
        if (world != null && world.TryFindEntity(message.EntityId, out var entity)) BlockStateNetwork.SendEntityStatesTo(sender, entity);
    }

    private static void OnData(int sender, EntityDataMessage message)
    {
        if (NetworkManager.Instance.IsServer) return;

        var world = World.Instance;
        if (world == null || !world.TryGetSector(message.Sector, out var sector)) return;

        CompoundTag tag = null;
        try
        {
            tag = NbtSerializer.FromBytes(message.Data);
        }
        catch (Exception ex)
        {
            Debug.Error($"[EntitySync] Failed to read entity {message.EntityId}: {ex.Message}");
        }

        sector.ApplyRemoteEntityData(message.EntityId, tag);
    }

    private static CompoundTag FindEntityTag(EntityDataRequestMessage message)
    {
        var world = World.Instance;
        if (world != null && world.TryFindEntity(message.EntityId, out var entity))
            return NBTHelper.SpaceEntityToTag(entity, SpaceMath.Sector.GetSectorPosition(message.Sector));

        if (WorldPersistenceManager.TryGetCachedEntity(message.EntityId, out var cached))
            return cached;

        if (World.WorldData == null) return null;

        string sectorPath = WorldSaveLoad.GetSectorFolderPath(World.WorldData.WorldFolderPath, message.Sector);
        string fileName = string.IsNullOrEmpty(message.FileName) ? message.EntityId.ToString() : message.FileName;
        string path = EntityFiles.PathOf(sectorPath, fileName);
        return File.Exists(path) ? WorldSaveLoad.LoadSpaceEntityTagFromFile(path) : null;
    }
}
