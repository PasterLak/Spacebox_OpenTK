using Engine;
using Engine.Multithreading;
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
    private const double FrameBudgetMs = 3.0;

    private static readonly Queue<(int Peer, EntityDataRequestMessage Message)> _queue = new Queue<(int, EntityDataRequestMessage)>();
    private static readonly Dictionary<int, int> _queuedPerPeer = new Dictionary<int, int>();
    private static PendingAnswer _current;

    private sealed class PendingAnswer
    {
        public int Peer;
        public EntityDataRequestMessage Message;
        public IncrementalEntityTagWriter Writer;
    }

    public static void Update()
    {
        if (_queue.Count == 0 && _current == null) return;
        if (!NetworkManager.Instance.IsServer)
        {
            Clear();
            return;
        }

        long deadline = System.Diagnostics.Stopwatch.GetTimestamp() + (long)(FrameBudgetMs * System.Diagnostics.Stopwatch.Frequency / 1000.0);
        do
        {
            if (_current == null && !TryStartNext()) return;
            ContinueCurrent(deadline);
        }
        while (System.Diagnostics.Stopwatch.GetTimestamp() < deadline);
    }

    public static void Clear()
    {
        _queue.Clear();
        _queuedPerPeer.Clear();
        _current = null;
    }

    private static bool TryStartNext()
    {
        while (_queue.Count > 0)
        {
            var (peer, message) = _queue.Dequeue();
            if (_queuedPerPeer.TryGetValue(peer, out int count))
            {
                if (count <= 1) _queuedPerPeer.Remove(peer);
                else _queuedPerPeer[peer] = count - 1;
            }
            if (!IsConnected(peer)) continue;

            var world = World.Instance;
            if (world != null && world.TryFindEntity(message.EntityId, out var entity))
            {
                _current = new PendingAnswer
                {
                    Peer = peer,
                    Message = message,
                    Writer = new IncrementalEntityTagWriter(entity, SpaceMath.Sector.GetSectorPosition(message.Sector))
                };
                return true;
            }

            AnswerFromStorage(peer, message);
        }

        return false;
    }

    private static void ContinueCurrent(long deadline)
    {
        var answer = _current;
        if (!answer.Writer.IsValid)
        {
            _current = null;
            AnswerFromStorage(answer.Peer, answer.Message);
            return;
        }

        bool done;
        try
        {
            done = answer.Writer.Step(deadline);
        }
        catch (Exception ex)
        {
            Debug.Error($"[EntitySync] Failed to serialize entity {answer.Message.EntityId}: {ex.Message}");
            _current = null;
            SendCompressed(answer.Peer, answer.Message, null, true);
            return;
        }

        if (!done) return;

        _current = null;
        CompoundTag tag = null;
        try
        {
            tag = answer.Writer.Finish();
        }
        catch (Exception ex)
        {
            Debug.Error($"[EntitySync] Failed to serialize entity {answer.Message.EntityId}: {ex.Message}");
        }
        SendCompressed(answer.Peer, answer.Message, tag, true);
    }

    private static bool IsConnected(int peer)
    {
        var network = NetworkManager.Instance;
        return network.Peers.Contains(peer) || peer == network.LocalPeerId;
    }

    private static void OnRequest(int sender, EntityDataRequestMessage message)
    {
        if (!NetworkManager.Instance.IsServer) return;

        _queuedPerPeer.TryGetValue(sender, out int queued);
        if (queued >= MaxQueuedPerPeer) return;
        _queuedPerPeer[sender] = queued + 1;
        _queue.Enqueue((sender, message));
    }

    private static void AnswerFromStorage(int peer, EntityDataRequestMessage message)
    {
        if (WorldPersistenceManager.TryGetCachedEntity(message.EntityId, out var cached))
        {
            SendCompressed(peer, message, cached, false);
            return;
        }

        string path = StoredEntityPath(message);
        WorkerPoolManager.Enqueue(token =>
        {
            CompoundTag tag = null;
            try
            {
                if (path != null && File.Exists(path)) tag = WorldSaveLoad.LoadSpaceEntityTagFromFile(path);
            }
            catch (Exception ex)
            {
                Debug.Error($"[EntitySync] Failed to read entity {message.EntityId}: {ex.Message}");
            }
            MainThreadDispatcher.Instance.Enqueue(() => SendCompressed(peer, message, tag, false));
        }, WorkerPoolManager.Priority.High);
    }

    private static void SendCompressed(int peer, EntityDataRequestMessage message, CompoundTag tag, bool sendStates)
    {
        WorkerPoolManager.Enqueue(token =>
        {
            byte[] payload = Array.Empty<byte>();
            try
            {
                if (tag != null) payload = NbtSerializer.ToBytes(tag);
            }
            catch (Exception ex)
            {
                Debug.Error($"[EntitySync] Failed to compress entity {message.EntityId}: {ex.Message}");
            }

            MainThreadDispatcher.Instance.Enqueue(() => Send(peer, message, payload, sendStates));
        }, WorkerPoolManager.Priority.High);
    }

    private static void Send(int peer, EntityDataRequestMessage message, byte[] payload, bool sendStates)
    {
        var network = NetworkManager.Instance;
        if (!network.IsServer || !IsConnected(peer)) return;

        network.Send(peer, new EntityDataMessage { Sector = message.Sector, EntityId = message.EntityId, Data = payload });

        var world = World.Instance;
        if (sendStates && world != null && world.TryFindEntity(message.EntityId, out var entity)) BlockStateNetwork.SendEntityStatesTo(peer, entity);
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

    private static string StoredEntityPath(EntityDataRequestMessage message)
    {
        if (World.WorldData == null) return null;

        string fileName = string.IsNullOrEmpty(message.FileName) ? message.EntityId.ToString() : message.FileName;
        if (fileName != Path.GetFileName(fileName) || fileName.Contains("..")) return null;
        return EntityFiles.PathInWorld(World.WorldData.WorldFolderPath, fileName);
    }
}
