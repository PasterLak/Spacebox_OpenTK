using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Structures;
using Spacebox.Game.Protection;
using Spacebox.Network.Players;

namespace Spacebox.Network.Dashboard.Map;

public sealed class WorldMapSource
{
    private readonly List<NotGeneratedEntity> _unloaded = new List<NotGeneratedEntity>();

    public List<MapPlayerRecord> CollectPlayers()
    {
        var players = new List<MapPlayerRecord>();
        foreach (var player in PlayerRegistry.All)
        {
            if (!PlayerLocator.TryGetPose(player.Peer, out var position, out _)) continue;
            players.Add(new MapPlayerRecord(player.Name, position, AstronautNetwork.Find(player.Peer)?.IsAlive ?? false));
        }
        return players;
    }

    public Vector3 DefaultCenter()
    {
        if (Spacebox.Game.Player.Respawn.WorldSpawn.TryGet(out var spawn)) return spawn;
        return World.Instance?.Player?.Position ?? Vector3.Zero;
    }

    public List<MapEntityRecord> CollectEntities()
    {
        var entities = new List<MapEntityRecord>();
        var world = World.Instance;
        if (world == null) return entities;

        foreach (var sector in world.LoadedSectors)
        {
            foreach (var entity in sector.Entities)
                entities.Add(FromLoaded(sector, entity));

            _unloaded.Clear();
            sector.CollectUnloadedData(_unloaded);
            foreach (var data in _unloaded)
                entities.Add(FromUnloaded(sector, data));
        }
        return entities;
    }

    public Dictionary<long, MapClaimRecord> CollectClaims()
    {
        var claims = new Dictionary<long, MapClaimRecord>();
        foreach (var pair in ClaimRegistry.Index.Entries)
        {
            if (!MapSnapshotBuilder.TryParseClaimEntity(pair.Key, out long entityId)) continue;
            if (claims.TryGetValue(entityId, out var existing) && existing.IsActive) continue;
            claims[entityId] = new MapClaimRecord(pair.Value.Owner, pair.Value.Active);
        }
        return claims;
    }

    private static MapEntityRecord FromLoaded(Sector sector, SpaceEntity entity)
    {
        var bounds = entity.GeometryBoundingBox;
        bool hasGeometry = bounds != null && bounds.Size.LengthSquared > 0f;
        Vector3 position = hasGeometry ? bounds.Center : entity.PositionWorld;
        float radius = hasGeometry ? MathF.Max(1f, bounds.Size.Length * 0.5f) : 8f;

        var persistence = entity.IsModified ? MapPersistence.Modified
            : sector.IsSavedHere(entity.EntityID) ? MapPersistence.Saved
            : MapPersistence.Regenerated;
        return new MapEntityRecord(entity.EntityID, position, radius, entity.IsDynamic(), true, persistence);
    }

    private static MapEntityRecord FromUnloaded(Sector sector, NotGeneratedEntity data)
    {
        var persistence = sector.IsSavedHere(data.Id) ? MapPersistence.Saved : MapPersistence.Regenerated;
        return new MapEntityRecord(data.Id, data.positionWorld, Math.Max(1, data.radiusBlocks), data.Id < 0, false, persistence);
    }
}
