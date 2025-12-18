using Engine;
using Engine.Physics;
using Engine.Utils;
using OpenTK.Mathematics;
using Spacebox.Game.GameMath;
using Spacebox.Game.Generation.Structures;
using Spacebox.Game.Generation.Tools;
using Spacebox.Game.Physics;
using Spacebox.Game.Player;
using Spacebox.Game.Resource;
using System.Drawing;

namespace Spacebox.Game.Generation;


public class Sector : SpatialCell, IDisposable, ISpaceStructure
{
    // ------------------ CONST --------------------------------------------
    public const short SizeBlocks = 8192; // 256 512 2048 4096 8192
    public const short SizeBlocksHalf = SizeBlocks / 2;

    public readonly ulong Seed;

    // ------------------ Properties --------------------------------------------

    public List<SpaceEntity> Entities { get; private set; }

    public HashSet<ulong> SuppressedEntities { get; private set; }
    public BiomesMap BiomesMap { get; private set; }
    private Dictionary<ulong, NotGeneratedEntity> EntitiesGeneratedData { get; set; }

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
        EntitiesGeneratedData = new Dictionary<ulong, NotGeneratedEntity>();
        SuppressedEntities = new HashSet<ulong>();

        Entities = new List<SpaceEntity>();

        PopulateSector();

        if (WorldSaveLoad.CanLoadSectorHere(PositionIndex, out var sectorFolderPath))
        {

            WorldSaveLoad.LoadSectorData(this);

            HandleSuppressedEntities();

            LoadEnities();

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

    private void LoadEnities()
    {
        var entities = WorldSaveLoad.LoadSpaceEntities(this);

        foreach (var e in entities)
        {

            if (Entities.Contains(e))
            {
                Debug.Error("[Sector] Entity already exists in sector upon loading! ID: " + e.EntityID);
                continue;
            }

            if (SuppressedEntities.Contains(e.EntityID))
            {
                Debug.Error("[Sector] Loaded entity is marked as suppressed, skipping addition to sector. ID: " + e.EntityID);
                continue;
            }

            // remove from not generated
            RemoveGeneratedData(e.EntityID);


            AddEntity(e, e.PositionWorld);

        }
    }

    private bool RemoveGeneratedData(ulong entityId)
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

    private void GenerateDataForPoints(Vector3[] positions)
    {
        Random random = new Random(SeedHelper.ToIntSeed(Seed));
        HashSet<ulong> usedIds = new HashSet<ulong>();

        foreach (var point in positions)
        {
            var id = GenerateUniqueIdForPoint(point, usedIds);

            usedIds.Add(id);

            var data = new NotGeneratedEntity();
            data.Id = id;
            data.positionInSector = point;
            data.positionWorld = LocalToWorld(point);

            data.biome = BiomesMap.GetFromSectorLocalCoord(point);

            if (data.biome.AsteroidChances.Count == 0) continue;

            var asteroidData = Biome.SelectAsteroidBySpawnChance(data.biome.AsteroidChances, random);

            data.radiusBlocks = random.Next(asteroidData.MinRadius, asteroidData.MaxRadius + 1);
            data.asteroid = asteroidData;
            data.rotation = Vector3.Zero;

            EntitiesGeneratedData.Add(data.Id, data);
            octreeNotGenerated.Add(data, point);
        }
    }

    private ulong GenerateUniqueIdForPoint(Vector3 point, HashSet<ulong> usedIds)
    {
        var id = SeedHelper.GetAsteroidId(Seed, point);

        if (usedIds.Contains(id))
        {
            Debug.Error($"[Sector] You are lucky as fuck! Duplicate asteroid ID at {point}! Regenerating...");

            int collisionAttempt = 1;
            ulong newId;
            do
            {
                newId = SeedHelper.GetAsteroidId(Seed + (ulong)collisionAttempt, point);
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

        Asteroid entity = new Asteroid(data, this);
        entity.Name = entity.EntityID.ToString();


        Entities.Add(entity);
        sectorOctree.Add(entity, data.positionWorld);
        octreeNotGenerated.Remove(data);

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
        entity.Dispose();
        Entities.Remove(entity);
        sectorOctree.Remove(entity, entity.PositionWorld);

        if (!isDestroyedPermentantly) return;

        if (!SuppressedEntities.Contains(entity.EntityID))
        {
            SuppressedEntities.Add(entity.EntityID);
            // delete the file on disk too
        }
        else
        {
            Debug.Error("[Sector] Entity already marked for removal! Id: " + entity.EntityID);
        }
    }

    // demo
    public void MoveEntityToSector(SpaceEntity entity, Sector newSector)
    {
        //DeleteEntity(entity);
        newSector.AddEntity(entity, entity.PositionWorld);
    }

    public SpaceEntity CreateNewEntity(Vector3 positionWorld)
    {
        SpaceEntity entity = new SpaceEntity(0, positionWorld, this);

        AddEntity(entity, positionWorld);

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
        if (entity == null)
        {
            Debug.Error("[Sector] Trying to unload null entity!");
            return;
        }

        var asteroid = entity as Asteroid;


        if (entity.IsModified)
        {
            Debug.Success("[Sector] Saving modified asteroid " + entity.PositionWorld);
        }
        else
        {
            // just ignore
        }

        if (asteroid != null)
        {
            if (asteroid.IsGenerated)
            {
                var data = asteroid.NotGeneratedEntity;

                octreeNotGenerated.Add(data, data.positionWorld);
                Debug.Success("[Sector] Unloaded asteroid " + data.positionWorld);
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

                if (entity.StarsEffect.Enabled)
                    entity.StarsEffect.Enabled = false;
            }
            else
            {
                if (!entity.StarsEffect.Enabled)
                    entity.StarsEffect.Enabled = true;

                if (distSqr >= Settings.ENTITY_UNLOAD_DISTANCE * Settings.ENTITY_UNLOAD_DISTANCE)
                {
                    UnloadEntity(entity);
                }

                entity.RenderEffect(distSqr);
            }
        }
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
        SuppressedEntities.Clear();
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