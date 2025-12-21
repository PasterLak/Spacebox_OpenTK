using Engine;
using Engine.Multithreading;
using Engine.Physics;
using Engine.Utils;
using OpenTK.Mathematics;
using Spacebox.Game.GameMath;
using Spacebox.Game.Generation.Structures;
using Spacebox.Game.Generation.Tools;
using Spacebox.Game.Physics;
using Spacebox.Game.Player;
using Spacebox.Game.Resource;


namespace Spacebox.Game.Generation;


public class Sector : SpatialCell, IDisposable, ISpaceStructure
{
    // ------------------ CONST --------------------------------------------
    public const short SizeBlocks = 8192; // 256 512 2048 4096 8192
    public const short SizeBlocksHalf = SizeBlocks / 2;

    public readonly long Seed;

    // ------------------ Properties --------------------------------------------

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

    // ------------------ Private --------------------------------------------
    private readonly PointOctree<SpaceEntity> sectorOctree;
    private readonly PointOctree<NotGeneratedEntity> octreeNotGenerated;

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
                        var entity = NBTHelper.TagToSpaceEntity(tag, this);
                        if (Entities.Exists(e => e.EntityID == entity.EntityID)) return;

                        ApplyLoadedEntityFixes(entity, data, fileName);

                        AddEntity(entity, entity.PositionWorld);
                        octreeNotGenerated.Remove(data);
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.Error($"[Sector] Async load failed for {filePath}: {ex.Message}");
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
            long cleanId = rawId & 0x7FFFFFFFFFFFFFFF; // clear highest bit for generated entities, bit 64 = 0

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

        if (data.biome.AsteroidChances.Count == 0) return; // no asteroids in this biome

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

    public void PlacePlayerRandomInSector(Astronaut player, Random random)
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

        var pos = SpaceMath.Sector.GetRandomPointOnSphere(geometryBox.Center, random, radius);

        var near = IsPointInEntity(pos, out var nearest3);

        while (near == true)
        {
            pos = SpaceMath.Sector.GetRandomPointOnSphere(geometryBox.Center, random, radius);
            near = IsPointInEntity(pos, out var nearest4);
        }

        return pos;
    }

    public void SpawnPlayerNearRandomAsteroidData(Astronaut player, Random random) // todo: fix 
    {

        var asteroidsDataList = EntitiesGeneratedData.Values.ToList();

        var asteroidIndex = random.Next(0, asteroidsDataList.Count);

        player.SetPosition(GetRandomPositionNearAsteroid(random, asteroidsDataList[asteroidIndex]));
        player.SpawnPosition = player.Position;

    }

    public bool IsColliding(Vector3 positionWorld, BoundingVolume volume, out CollideInfo collideInfo)
    {
        if (TryGetNearestEntity(positionWorld, out var entity))
        {
            return entity.IsColliding(volume, out collideInfo);
        }
        collideInfo = new CollideInfo();
        return false;
    }

    public bool Raycast(Ray ray, out HitInfo hitInfo)
    {
        if (TryGetNearestEntity(ray.Origin, out var entity))
        {
            return entity.Raycast(ray, out hitInfo);
        }

        hitInfo = new HitInfo();
        return false;
    }

    public bool TryGetNearestEntity(Vector3 positionWorld, out SpaceEntity entity)
    {
        entity = null;
        if (Entities.Count == 0) return false;

        float nearestDistSq = float.MaxValue;

        for (byte i = 0; i < Entities.Count; i++)
        {
            Vector3 diff = positionWorld - Entities[i].CenterOfMass;
            float distSq = diff.LengthSquared;
            if (distSq < nearestDistSq)
            {
                nearestDistSq = distSq;
                entity = Entities[i];
            }
        }

        return true;
    }


    private void GenerateAsteroidFromData(NotGeneratedEntity data)
    {

        if (WorldPersistenceManager.TryGetCachedEntity(data.Id, out var cachedTag))
        {
            var entity = NBTHelper.TagToSpaceEntity(cachedTag, this);
            AddEntity(entity, entity.PositionWorld);
            octreeNotGenerated.Remove(data);
            return;
        }

        string sectorPath = WorldSaveLoad.GetSectorFolderPath(World.WorldData.WorldFolderPath, PositionIndex);

        string fileName = !string.IsNullOrEmpty(data.FileName) ? data.FileName : data.Id.ToString();
        string filePath = Path.Combine(sectorPath, fileName + ".entity");

        if (File.Exists(filePath))
        {
            if (IsPlayerInsideOrVeryClose(data))
            {
                var loadedEntity = WorldSaveLoad.LoadSpaceEntityFromFile(filePath, this);
                if (loadedEntity != null)
                {
                    ApplyLoadedEntityFixes(loadedEntity, data, fileName);
                    AddEntity(loadedEntity, loadedEntity.PositionWorld);
                    octreeNotGenerated.Remove(data);
                }
                return;
            }
            else
            {
                LoadEntityFromDiskAsync(filePath, data);
                return;
            }
        }

        if (data.Id >= 0)
        {
            var newEntity = new Asteroid(data, this);
            newEntity.Name = newEntity.EntityID.ToString();
            AddEntity(newEntity, newEntity.PositionWorld);
            octreeNotGenerated.Remove(data);
        }
    }

    private void ApplyLoadedEntityFixes(SpaceEntity loadedEntity, NotGeneratedEntity data, string realFileName)
    {

        if (loadedEntity is Asteroid asteroid)
        {
            asteroid.NotGeneratedEntity = data;
            asteroid.IsGenerated = true;
        }


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
            if (a.GeometryBoundingBox.Contains(point))
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
    }

    // demo
    public void MoveEntityToSector(SpaceEntity entity, Sector newSector)
    {
        //DeleteEntity(entity);
        newSector.AddEntity(entity, entity.PositionWorld);
    }

    public SpaceEntity CreateNewEntity(Vector3 positionWorld)
    {
        var id = GenerateDynamicEntityId();
        SpaceEntity entity = new SpaceEntity(id, positionWorld, this);

        AddEntity(entity, positionWorld);
        entity.IsGenerated = true;
        IsModified = true;

        return entity;
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
        for (int i = 0; i < Entities.Count; i++)
        {
            var entity = Entities[i];


            Entities[i].Update();

            VisualDebug.DrawSphere(Entities[i].CenterOfMass, Entities[i].GravityRadius, 8, Color4.Blue);

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

            if (!cam.Frustum.IsInFrustum(entity.GeometryBoundingBox))
                continue;

            float distSqr = Vector3.DistanceSquared(entity.GeometryBoundingBox.Center, cam.Position);
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

               // if (entity.StarsEffect.Enabled)
                 //   entity.StarsEffect.Enabled = false;
            }
            else
            {
               // if (!entity.StarsEffect.Enabled)
               //     entity.StarsEffect.Enabled = true;

                if (distSqr >= Settings.ENTITY_UNLOAD_DISTANCE * Settings.ENTITY_UNLOAD_DISTANCE)
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

    public void Dispose()
    {

        if (Entities != null)
        {
            // safe removal
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