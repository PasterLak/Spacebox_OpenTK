using Engine;
using Engine.Networking;
using OpenTK.Mathematics;
using SharpNBT;
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

    private static void OnRequest(int sender, EntityDataRequestMessage message)
    {
        var network = NetworkManager.Instance;
        if (!network.IsServer) return;

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
            return NBTHelper.SpaceEntityToTag(entity);

        if (WorldPersistenceManager.TryGetCachedEntity(message.EntityId, out var cached))
            return cached;

        if (World.WorldData == null) return null;

        string sectorPath = WorldSaveLoad.GetSectorFolderPath(World.WorldData.WorldFolderPath, message.Sector);
        string fileName = string.IsNullOrEmpty(message.FileName) ? message.EntityId.ToString() : message.FileName;
        if (!fileName.EndsWith(".entity")) fileName += ".entity";

        string path = Path.Combine(sectorPath, fileName);
        return File.Exists(path) ? WorldSaveLoad.LoadSpaceEntityTagFromFile(path) : null;
    }
}
