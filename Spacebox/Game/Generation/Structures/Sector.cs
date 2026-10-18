using Engine;
using Engine.Multithreading;
using Engine.Physics;
using Engine.Utils;
using OpenTK.Mathematics;
using SharpNBT;
using Spacebox.Game.GameMath;
using Spacebox.Game.Generation.Structures;
using Spacebox.Game.Generation.Tools;
using Spacebox.Game.Physics;
using Spacebox.Game.Player;
using Spacebox.Game.Resource;


namespace Spacebox.Game.Generation;


public class Sector : SpatialCell, IDisposable, ISpaceStructure
{
    public const short SizeBlocks = 8192;
    public const short SizeBlocksHalf = SizeBlocks / 2;

    public readonly long Seed;


    public List<SpaceEntity> Entities { get; private set; }

    public HashSet<long> SuppressedEntities { get; private set; }
    public BiomesMap BiomesMap { get; private set; }
    private HashSet<long> PointsIds { get; set; }
    private Dictionary<long, NotGeneratedEntity> EntitiesGeneratedData { get; set; }
    public List<string> EntitiesDestroyed { get; private set; }

    private bool _isModified = false;
    public bool IsModified
    {
        get => _isModified;
        set
        {
            _isModified = value;
        }
    }

    private readonly PointOctree<SpaceEntity> sectorOctree;
    private readonly PointOctree<NotGeneratedEntity> octreeNotGenerated;
    private HashSet<long> loadingEntities = new HashSet<long>();
    private readonly List<NotGeneratedEntity> _streamingNearby = new List<NotGeneratedEntity>();
    private readonly List<NotGeneratedEntity> _observerNearby = new List<NotGeneratedEntity>();

    public Sector(Vector3 positionWorld, Vector3i positionIndex, int worldSeed)
    {
        PositionWorld = positionWorld;
        PositionIndex = positionIndex;
        Seed = SeedHelper.GetSectorId(worldSeed, positionIndex);


        Vector3 sectorCenter = PositionWorld + new Vector3(SizeBlocksHalf);
        Vector3 sectorSize = new Vector3(SizeBlocks, SizeBlocks, SizeBlocks);

        BoundingBox = new BoundingBox(sectorCenter, sectorSize);

        sectorOctree = new PointOctree<SpaceEntity>(SizeBlocks, positionWorld, 1);
        octreeNotGenerated = new PointOctree<NotGeneratedEntity>(SizeBlocks, positionWorld, 1);
        EntitiesGeneratedData = new Dictionary<long, NotGeneratedEntity>();
        SuppressedEntities = new HashSet<long>();
        PointsIds = new HashSet<long>();
        EntitiesDestroyed = new List<string>();

        Entities = new List<SpaceEntity>();

        PopulateSector();

        ScanAndRegisterCustomEntities();

        if (WorldSaveLoad.CanLoadSectorHere(PositionIndex, out var sectorFolderPath))
        {

            WorldSaveLoad.LoadSectorData(this);

            HandleSuppressedEntities();

        }

    }

    private void HandleSuppressedEntities()
    {
        if (SuppressedEntities.Count == 0) return;

        foreach (var suppressedIds in SuppressedEntities)
        {
            if (RemoveGeneratedData(suppressedIds))
            {

            }
            else
            {
                Debug.Error("[Sector] Suppressed entity not found in generated data! ID: " + suppressedIds);
            }
        }

    }


    private void LoadEntityFromDiskAsync(string filePath, NotGeneratedEntity data)
    {
        if (EntitiesGeneratedData.ContainsKey(data.Id) == false) return;

        WorkerPoolManager.Enqueue(token =>
        {
            try
            {
                var tag = WorldSaveLoad.LoadSpaceEntityTagFromFile(filePath);

                var fileName = Path.GetFileNameWithoutExtension(filePath);

                if (tag != null)
                {
                    MainThreadDispatcher.Instance.Enqueue(() =>
                    {
                        var entity = NBTHelper.TagToSpaceEntity(tag, this, data);
                        if (!Entities.Exists(e => e.EntityID == entity.EntityID))
                        {
                            ApplyLoadedEntityFixes(entity, data, fileName);
                            AddEntity(entity, entity.PositionWorld);
                            octreeNotGenerated.Remove(data);
                        }
                        loadingEntities.Remove(data.Id);
                    });
                }
                else
                {
                    MainThreadDispatcher.Instance.Enqueue(() => loadingEntities.Remove(data.Id));
                }
            }
            catch (Exception ex)
            {
                Debug.Error($"[Sector] Async load failed for {filePath}: {ex.Message}");
                MainThreadDispatcher.Instance.Enqueue(() => loadingEntities.Remove(data.Id));
            }
        }, WorkerPoolManager.Priority.High);
    }

    private bool RemoveGeneratedData(long entityId)
    {
        if (EntitiesGeneratedData.TryGetValue(entityId, out NotGeneratedEntity? data))
        {
            octreeNotGenerated.Remove(data);
            EntitiesGeneratedData.Remove(entityId);
            return true;
        }


        return false;
    }

    private void PopulateSector()
    {
        BiomesMap = new BiomesMap(PositionIndex, World.BiomeGenerator);

        var points = SpaceMath.Sector.GenerateAsteroidPositions(Seed, PositionWorld, World.WorldGenerator.MinAsteroidsInSector,
            World.WorldGenerator.MaxAsteroidsInSector,
            World.WorldGenerator.RejectionSamples,
            World.WorldGenerator.MinDistanceBetweenAsteroids, true);

        GenerateDataForPoints(points);

    }

    private void ScanAndRegisterCustomEntities()
    {

        var savedEntities = WorldSaveLoad.ScanAllEntities(PositionIndex);

        foreach (var saved in savedEntities)
        {
            saved.positionWorld = LocalToWorldPosition(saved.positionInSector);

            if (EntitiesGeneratedData.TryGetValue(saved.Id, out var existing))
            {
                existing.FileName = saved.FileName;

                existing.positionWorld = saved.positionWorld;
                existing.positionInSector = saved.positionInSector;

                octreeNotGenerated.Remove(existing);
                octreeNotGenerated.Add(existing, existing.positionWorld);
            }
            else
            {

                EntitiesGeneratedData.Add(saved.Id, saved);
                octreeNotGenerated.Add(saved, saved.positionWorld);
            }
        }
    }

    private void GenerateDataForPoints(Vector3[] positions)
    {
        Random random = new Random(SeedHelper.ToIntSeed(Seed));

        foreach (var point in positions)
        {
            long rawId = GenerateUniqueIdForPoint(point, PointsIds);
            long cleanId = rawId & 0x7FFFFFFFFFFFFFFF;

            PointsIds.Add(cleanId);

            GenerateDataForPoint(point, cleanId, random);
        }
    }

    private void GenerateDataForPoint(Vector3 point, long id, Random random)
    {
        var data = new NotGeneratedEntity();
        data.Id = id;
        data.positionInSector = point;
        data.positionWorld = LocalToWorld(point);

        data.biome = BiomesMap.GetFromSectorLocalCoord(point);

        if (data.biome.AsteroidChances.Count == 0) return;

        var asteroidData = Biome.SelectAsteroidBySpawnChance(data.biome.AsteroidChances, random);

        data.radiusBlocks = random.Next(asteroidData.MinRadius, asteroidData.MaxRadius + 1);
        data.asteroid = asteroidData;
        data.rotation = Vector3.Zero;
        data.FileName = id.ToString();

        EntitiesGeneratedData.Add(data.Id, data);
        octreeNotGenerated.Add(data, point);
    }

    private long GenerateUniqueIdForPoint(Vector3 point, HashSet<long> usedIds)
    {
        var id = SeedHelper.GetAsteroidId(Seed, point);

        if (usedIds.Contains(id))
        {
            Debug.Error($"[Sector] You are lucky as fuck! Duplicate asteroid ID at {point}! Regenerating...");

            int collisionAttempt = 1;
            long newId;
            do
            {
                newId = SeedHelper.GetAsteroidId(Seed + (long)collisionAttempt, point);
                collisionAttempt++;

                if (collisionAttempt > 100)
                {
                    Debug.Error("[Sector] Failed to generate unique ID after 100 attempts! ");
                    break;
                }
            } while (usedIds.Contains(newId));

            id = newId;
        }

        return id;
    }

    public long GenerateDynamicEntityId()
    {
        while (true)
        {
            long id = SeedHelper.GenerateDynamicEntityId();

            if (!SuppressedEntities.Contains(id) && !Entities.Exists(e => e.EntityID == id))
            {
                return id;
            }
        }
    }

    public void PlacePlayerRandomInSector(LocalAstronaut player, Random random)
    {
        if (Entities.Count == 0) return;

        if (TryGetNearestEntity(SpaceMath.Sector.GetCenter(this), out var entity))
        {
            player.SetPosition(GetRandomPositionNearAsteroid(random, entity));
        }
        else
        {
            player.SetPosition(GetRandomPositionWithCollisionCheck(random, 0.2f));
        }

        player.SpawnPosition = player.Position;
    }

    public Vector3 GetRandomPositionWithCollisionCheck(Random random, float margin01)
    {
        var pos = SpaceMath.Sector.GetRandomPositionInside(this, margin01, random);
        var near = IsPointInEntity(pos, out var nearest3);

        while (near == true)
        {
            pos = SpaceMath.Sector.GetRandomPositionInside(this, margin01, random);
            near = IsPointInEntity(pos, out var nearest4);
        }

        return pos;
    }

    public Vector3 GetRandomPositionNearAsteroid(Random random, NotGeneratedEntity entity)
    {

        var radius = entity.radiusBlocks;

        var pos = SpaceMath.Sector.GetRandomPointOnSphere(entity.positionWorld, random, radius);

        var near = IsPointInEntity(pos, out var nearest3);

        while (near == true)
        {
            pos = SpaceMath.Sector.GetRandomPointOnSphere(entity.positionWorld, random, radius);
            near = IsPointInEntity(pos, out var nearest4);
        }

        return pos;
    }
    public Vector3 GetRandomPositionNearAsteroid(Random random, SpaceEntity entity)
    {
        var geometryBox = entity.GeometryBoundingBox;

        var radius = (geometryBox.Max.Length -
                      geometryBox.Min.Length) / 2 + 100;

        var center = entity.ToWorldSpace(geometryBox.Center);
        var pos = SpaceMath.Sector.GetRandomPointOnSphere(center, random, radius);

        var near = IsPointInEntity(pos, out var nearest3);

        while (near == true)
        {
            pos = SpaceMath.Sector.GetRandomPointOnSphere(center, random, radius);
            near = IsPointInEntity(pos, out var nearest4);
        }

        return pos;
    }

    public void SpawnPlayerNearRandomAsteroidData(LocalAstronaut player, Random random)
    {

        var asteroidsDataList = EntitiesGeneratedData.Values.ToList();

        var asteroidIndex = random.Next(0, asteroidsDataList.Count);

        player.SetPosition(GetRandomPositionNearAsteroid(random, asteroidsDataList[asteroidIndex]));
        player.SpawnPosition = player.Position;

    }

    private const float EntityQueryMargin = 1f;

    private readonly BoundingBox _rayProbeBox = new BoundingBox(Vector3.Zero, Vector3.Zero);

    public bool IsColliding(Vector3 positionWorld, BoundingVolume volume, out CollideInfo collideInfo)
    {
        float margin = volume.GetLongestSide() + EntityQueryMargin;
        float marginSquared = margin * margin;

        for (int i = 0; i < Entities.Count; i++)
        {
            var entity = Entities[i];
            if (DistanceSquaredToGeometry(entity, positionWorld) > marginSquared) continue;
            if (entity.IsColliding(volume, out collideInfo)) return true;
        }

        collideInfo = new CollideInfo();
        return false;
    }

    public bool Raycast(Ray ray, out HitInfo hitInfo)
    {
        return Raycast(ray, out hitInfo, null);
    }

    public bool Raycast(Ray ray, out HitInfo hitInfo, SpaceEntity? ignored)
    {
        hitInfo = new HitInfo();
        bool found = false;
        float closestSquared = float.MaxValue;

        for (int i = 0; i < Entities.Count; i++)
        {
            var entity = Entities[i];
            if (entity == ignored || !RayMayHit(entity, ray)) continue;
            if (!entity.Raycast(ray, out var hit)) continue;

            float distanceSquared = (hit.hitPosition - ray.Origin).LengthSquared;
            if (distanceSquared >= closestSquared) continue;

            closestSquared = distanceSquared;
            hitInfo = hit;
            found = true;
        }

        return found;
    }

    public bool TryGetNearestEntity(Vector3 positionWorld, out SpaceEntity entity)
    {
        entity = null;
        if (Entities.Count == 0) return false;

        float nearestGeometry = float.MaxValue;
        float nearestCenter = float.MaxValue;

        for (int i = 0; i < Entities.Count; i++)
        {
            var candidate = Entities[i];
            float geometry = DistanceSquaredToGeometry(candidate, positionWorld);
            float center = (positionWorld - candidate.CenterOfMassWorld).LengthSquared;

            if (geometry < nearestGeometry || (geometry == nearestGeometry && center < nearestCenter))
            {
                nearestGeometry = geometry;
                nearestCenter = center;
                entity = candidate;
            }
        }

        return true;
    }

    private static float DistanceSquaredToGeometry(SpaceEntity entity, Vector3 positionWorld)
    {
        if (entity.Chunks.Count == 0) return float.MaxValue;
        return SpaceMath.World.DistanceToBox(entity.ToEntitySpace(positionWorld), entity.GeometryBoundingBox);
    }

    private bool RayMayHit(SpaceEntity entity, Ray ray)
    {
        if (entity.Chunks.Count == 0) return false;

        var bounds = entity.ToWorldBounds(entity.GeometryBoundingBox);
        _rayProbeBox.Center = bounds.Center;
        _rayProbeBox.Size = bounds.Size + new Vector3(EntityQueryMargin * 2f);
        return _rayProbeBox.Contains(ray.Origin) || ray.Intersects(_rayProbeBox, out _);
    }


    private void GenerateAsteroidFromData(NotGeneratedEntity data)
    {
        if (loadingEntities.Contains(data.Id)) return;

        if (Spacebox.Network.GameSession.IsRemoteClient)
        {
            loadingEntities.Add(data.Id);
            Spacebox.Network.EntitySync.Request(this, data);
            return;
        }

        if (WorldPersistenceManager.TryGetCachedEntity(data.Id, out var cachedTag))
        {
            var entity = NBTHelper.TagToSpaceEntity(cachedTag, this, data);
            AddEntity(entity, entity.PositionWorld);
            octreeNotGenerated.Remove(data);
            return;
        }

        string sectorPath = WorldSaveLoad.GetSectorFolderPath(World.WorldData.WorldFolderPath, PositionIndex);

        string fileName = !string.IsNullOrEmpty(data.FileName) ? data.FileName : data.Id.ToString();
        string filePath = EntityFiles.PathOf(sectorPath, fileName);

        if (File.Exists(filePath))
        {
            loadingEntities.Add(data.Id);
            LoadEntityFromDiskAsync(filePath, data);
            return;
        }

        if (data.Id >= 0)
        {
            loadingEntities.Add(data.Id);
            GenerateFreshAsteroid(data);
        }
    }

    public void ApplyRemoteEntityData(long entityId, CompoundTag tag)
    {
        if (!loadingEntities.Contains(entityId)) return;

        if (!EntitiesGeneratedData.TryGetValue(entityId, out var data))
        {
            loadingEntities.Remove(entityId);
            return;
        }

        if (tag == null)
        {
            if (data.Id >= 0) GenerateFreshAsteroid(data);
            else loadingEntities.Remove(entityId);
            return;
        }

        var entity = NBTHelper.TagToSpaceEntity(tag, this, data);
        if (entity != null && !Entities.Exists(e => e.EntityID == entity.EntityID))
        {
            ApplyLoadedEntityFixes(entity, data, entity.Name);
            AddEntity(entity, entity.PositionWorld);
            octreeNotGenerated.Remove(data);
        }

        loadingEntities.Remove(entityId);
    }

    private void GenerateFreshAsteroid(NotGeneratedEntity data)
    {
        WorkerPoolManager.Enqueue(token =>
        {
            try
            {
                var newEntity = new Asteroid(data, this, true);
                newEntity.Name = newEntity.EntityID.ToString();
                newEntity.OnGenerate();

                MainThreadDispatcher.Instance.Enqueue(() =>
                {
                    AddEntity(newEntity, newEntity.PositionWorld);
                    octreeNotGenerated.Remove(data);
                    loadingEntities.Remove(data.Id);
                });
            }
            catch (Exception ex)
            {
                Debug.Error($"Failed to generate asteroid {data.Id}: {ex}");
                MainThreadDispatcher.Instance.Enqueue(() => loadingEntities.Remove(data.Id));
            }
        }, WorkerPoolManager.Priority.Medium);
    }

    private void ApplyLoadedEntityFixes(SpaceEntity loadedEntity, NotGeneratedEntity data, string realFileName)
    {

        if (loadedEntity is Asteroid asteroid)
        {
            asteroid.NotGeneratedEntity = data;
            asteroid.IsGenerated = true;
        }


        realFileName = EntityFiles.NameOf(realFileName);

        if (loadedEntity.Name != realFileName)
        {
            Debug.Warning($"[Sector] Fixed name mismatch for {loadedEntity.EntityID}. Old: '{loadedEntity.Name}', New: '{realFileName}'");
            loadedEntity.Name = realFileName;

            loadedEntity.IsModified = true;
        }
    }


    public bool IsPointInEntity(Vector3 point, out SpaceEntity entity)
    {
        entity = null;
        foreach (var a in Entities)
        {
            if (a.GeometryBoundingBox.Contains(a.ToEntitySpace(point)))
            {
                entity = a;
                return true;
            }
        }

        return false;
    }

    private void AddEntity(SpaceEntity entity, Vector3 positionWorld)
    {
        Entities.Add(entity);

        sectorOctree.Add(entity, positionWorld);
    }

    public static event Action<Sector, long> EntityDestroyedPermanently;

    internal void OnEntityMoved(SpaceEntity entity, Vector3 oldPosition)
    {
        sectorOctree.Remove(entity, oldPosition);
        sectorOctree.Add(entity, entity.PositionWorld);
    }

    public void DestroyEntity(SpaceEntity entity, bool isDestroyedPermentantly)
    {
        var id = entity.EntityID;
        var name = entity.Name;
        entity.Dispose();
        Entities.Remove(entity);
        sectorOctree.Remove(entity, entity.PositionWorld);

        if (!isDestroyedPermentantly) return;

        if (entity.IsProcedural() && !SuppressedEntities.Contains(id))
        {
            SuppressedEntities.Add(id);
        }

        EntitiesDestroyed.Add(name);
        IsModified = true;
        EntityDestroyedPermanently?.Invoke(this, id);
    }

    public void CollectCustomEntities(List<NotGeneratedEntity> output)
    {
        for (int i = 0; i < Entities.Count; i++)
        {
            var entity = Entities[i];
            if (!entity.IsDynamic()) continue;

            output.Add(new NotGeneratedEntity
            {
                Id = entity.EntityID,
                FileName = entity.Name,
                positionInSector = WorldToLocalPosition(entity.PositionWorld)
            });
        }

        foreach (var data in EntitiesGeneratedData.Values)
        {
            if (data.Id < 0 && !Entities.Exists(e => e.EntityID == data.Id)) output.Add(data);
        }
    }

    public void ApplyRemoteManifest(IReadOnlyList<long> suppressed, IReadOnlyList<NotGeneratedEntity> customEntities)
    {
        for (int i = 0; i < suppressed.Count; i++)
            RemoveEntityRemote(suppressed[i]);

        for (int i = 0; i < customEntities.Count; i++)
        {
            var saved = customEntities[i];
            if (SuppressedEntities.Contains(saved.Id) || EntitiesGeneratedData.ContainsKey(saved.Id) || Entities.Exists(e => e.EntityID == saved.Id)) continue;

            saved.positionWorld = LocalToWorldPosition(saved.positionInSector);
            EntitiesGeneratedData.Add(saved.Id, saved);
            octreeNotGenerated.Add(saved, saved.positionWorld);
        }
    }

    public void RemoveEntityRemote(long entityId)
    {
        if (entityId >= 0) SuppressedEntities.Add(entityId);
        RemoveGeneratedData(entityId);
        loadingEntities.Remove(entityId);

        var entity = Entities.Find(e => e.EntityID == entityId);
        if (entity != null) DestroyEntity(entity, false);
    }

    public void MoveEntityToSector(SpaceEntity entity, Sector newSector)
    {
        newSector.AddEntity(entity, entity.PositionWorld);
    }

    public SpaceEntity CreateNewEntity(Vector3 positionWorld)
    {
        return CreateNewEntity(positionWorld, GenerateDynamicEntityId());
    }

    public SpaceEntity CreateNewEntity(Vector3 positionWorld, long id)
    {
        SpaceEntity entity = new SpaceEntity(id, positionWorld, this);

        AddEntity(entity, positionWorld);
        entity.IsGenerated = true;
        IsModified = true;

        return entity;
    }

    public void CollectStreamingWork(Vector3 cameraPosition, ref StreamingWork work)
    {
        work.Entities += loadingEntities.Count;

        _streamingNearby.Clear();
        if (GetNotGeneratedEntitiesNearby(cameraPosition, _streamingNearby))
            work.Entities += _streamingNearby.Count;

        for (int i = 0; i < Entities.Count; i++)
            Entities[i].CollectStreamingWork(cameraPosition, ref work);
    }

    public bool GetNotGeneratedEntitiesNearby(Vector3 pos, List<NotGeneratedEntity> nearby)
    {

        return
           octreeNotGenerated.GetNearbyNonAlloc(pos, Settings.ENTITY_SEARCH_RADIUS, nearby);
    }

    public bool GetGeneratedEntitiesNearby(Vector3 pos, List<SpaceEntity> nearby)
    {

        return
           sectorOctree.GetNearbyNonAlloc(pos, Settings.ENTITY_SEARCH_RADIUS, nearby);
    }

    private void UnloadEntity(SpaceEntity entity)
    {
        if (entity == null) return;

        if (entity.IsModified)
        {
            WorldPersistenceManager.CacheUnloadedEntity(entity);
        }

        if (entity is Asteroid asteroid)
        {
            var data = asteroid.NotGeneratedEntity;

            if (data != null)
            {
                data.positionWorld = entity.PositionWorld;
                data.positionInSector = WorldToLocalPosition(entity.PositionWorld);

                octreeNotGenerated.Add(data, data.positionWorld);

                if (!EntitiesGeneratedData.ContainsKey(data.Id))
                {
                    EntitiesGeneratedData[data.Id] = data;
                }
            }
        }
        else
        {
            Debug.Success("[Sector] Unloaded spaceship " + entity.PositionWorld);
        }

        DestroyEntity(entity, false);
    }

    public override void Update()
    {
        base.Update();

        var camera = Camera.Main;
        if (camera == null) return;
        List<NotGeneratedEntity> nearby = new List<NotGeneratedEntity>();

        if (GetNotGeneratedEntitiesNearby(camera.PositionWorld, nearby))
        {
            foreach (NotGeneratedEntity entity in nearby)
            {

                GenerateAsteroidFromData(entity);
            }
        }

        var observers = WorldObservers.Remote;
        for (int i = 0; i < observers.Count; i++)
        {
            _observerNearby.Clear();
            if (!octreeNotGenerated.GetNearbyNonAlloc(observers[i], WorldObservers.RemoteEntityRadius, _observerNearby)) continue;

            for (int j = 0; j < _observerNearby.Count; j++)
                GenerateAsteroidFromData(_observerNearby[j]);
        }
        for (int i = Entities.Count - 1; i >= 0; i--)
        {
            var entity = Entities[i];

            entity.Update();
        }
    }

    public void Render(BlockMaterial shader)
    {
        VisualDebug.DrawBoundingBox(BoundingBox, new Color4(255, 255, 20, 100));

        var cam = Camera.Main;

        const float visibleRadiusSqr = Settings.ENTITY_VISIBLE_RADIUS * Settings.ENTITY_VISIBLE_RADIUS;

        for (int i = 0; i < Entities.Count; i++)
        {
            var entity = Entities[i];

            var worldBounds = entity.ToWorldBounds(entity.GeometryBoundingBox);
            if (!cam.Frustum.IsInFrustum(worldBounds))
                continue;

            float distSqr = Vector3.DistanceSquared(worldBounds.Center, cam.Position);
            bool isWithinRenderDistance = distSqr < visibleRadiusSqr;

            if (isWithinRenderDistance)
            {

                if (!entity.IsGenerated)
                {
                    if (entity is Asteroid asteroid)
                    {
                        asteroid.OnGenerate();
                    }
                    continue;
                }

                entity.Render(cam, shader);

            }
            else
            {

                if (distSqr >= Settings.ENTITY_UNLOAD_DISTANCE * Settings.ENTITY_UNLOAD_DISTANCE &&
                    !WorldObservers.IsNearRemote(worldBounds.Center, Settings.ENTITY_UNLOAD_DISTANCE))
                {
                    UnloadEntity(entity);
                }

                entity.RenderEffect(distSqr);
            }
        }
    }

    private bool IsPlayerInsideOrVeryClose(NotGeneratedEntity data)
    {
        var cam = Camera.Main;
        if (cam == null) return false;

        float distSq = Vector3.DistanceSquared(cam.Position, data.positionWorld);
        float radius = data.radiusBlocks * 1.5f;
        return distSq < radius * radius;
    }

    public void PreloadAreaBlocking(Vector3 position, float radius)
    {
        Debug.Log($"[Sector] Preloading area at {position} with radius {radius}...");

        var nearby = new List<NotGeneratedEntity>();
        if (!octreeNotGenerated.GetNearbyNonAlloc(position, radius, nearby))
        {
            return;
        }

        string sectorPath = WorldSaveLoad.GetSectorFolderPath(World.WorldData.WorldFolderPath, PositionIndex);
        int totalToLoad = nearby.Count;
        int loadedCount = 0;

        foreach (var data in nearby)
        {
            WorkerPoolManager.Enqueue(token =>
            {
                try
                {
                    SpaceEntity entity = null;

                    if (WorldPersistenceManager.TryGetCachedEntity(data.Id, out var cachedTag))
                    {
                        entity = NBTHelper.TagToSpaceEntity(cachedTag, this, data);
                    }
                    else
                    {
                        string fileName = EntityFiles.NameOf(!string.IsNullOrEmpty(data.FileName) ? data.FileName : data.Id.ToString());
                        string filePath = EntityFiles.PathOf(sectorPath, fileName);

                        if (File.Exists(filePath))
                        {
                            var loaded = WorldSaveLoad.LoadSpaceEntityFromFile(filePath, this, data);
                            if (loaded != null)
                            {
                                ApplyLoadedEntityFixes(loaded, data, fileName);
                                entity = loaded;
                            }
                        }
                    }

                    if (entity == null && data.Id >= 0)
                    {
                        var newEntity = new Asteroid(data, this, true);
                        newEntity.Name = newEntity.EntityID.ToString();
                        newEntity.OnGenerate();
                        entity = newEntity;
                    }

                    MainThreadDispatcher.Instance.Enqueue(() =>
                    {
                        if (entity != null)
                        {
                            AddEntity(entity, entity.PositionWorld);
                            octreeNotGenerated.Remove(data);

                            if (entity.Chunks.Count > 0)
                            {
                                foreach (var chunk in entity.Chunks)
                                {
                                    chunk.QueueMeshUpdate(UpdateReason.Generation);
                                }
                            }
                        }
                        loadedCount++;
                    });
                }
                catch (Exception ex)
                {
                    Debug.Error($"[Sector] Preload failed for entity {data.Id}: {ex.Message}");
                    MainThreadDispatcher.Instance.Enqueue(() => { loadedCount++; });
                }
            }, WorkerPoolManager.Priority.High);
        }

        while (loadedCount < totalToLoad)
        {
            MainThreadDispatcher.Instance.ExecutePending();
            System.Threading.Thread.Sleep(5);
        }

        Debug.Success($"[Sector] Preload complete. Loaded {loadedCount} entities.");
    }

    public void Dispose()
    {

        if (Entities != null)
        {
            for (int i = Entities.Count - 1; i >= 0; i--)
            {
                UnloadEntity(Entities[i]);
            }
        }


        Entities = null;
        EntitiesGeneratedData.Clear();
        PointsIds.Clear();
        SuppressedEntities.Clear();
        EntitiesDestroyed.Clear();
        loadingEntities.Clear();
    }

    public Vector3 LocalToWorldPosition(Vector3 local)
    {
        return PositionWorld + local;
    }

    public Vector3 WorldToLocalPosition(Vector3 worldPos)
    {
        return worldPos - PositionWorld;
    }

    public string ToFolderName()
    {
        return IndexToFolderName(PositionIndex);
    }

    public static string IndexToFolderName(Vector3i index)
    {
        return SpaceMath.Sector.IndexToFolderName(index);
    }
}