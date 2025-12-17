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
using static Spacebox.Game.GUI.CraftingCategory;

namespace Spacebox.Game.Generation;


public class Sector : SpatialCell, IDisposable, ISpaceStructure
{
    // ------------------ CONST --------------------------------------------
    public const short SizeBlocks = 8192; // 256 512 2048 4096 8192
    public const short SizeBlocksHalf = SizeBlocks / 2;

    public readonly ulong Seed;

    // ------------------ Properties --------------------------------------------

    public List<SpaceEntity> Entities { get; private set; }
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

        Entities = new List<SpaceEntity>();

        if (WorldSaveLoad.CanLoadSectorHere(PositionIndex, out var sectorFolderPath))
        {

            var entities = WorldSaveLoad.LoadSpaceEntities(this);

            foreach (var e in entities)
            {

                
                AddEntity(e, e.PositionWorld);

            }

        }

        PopulateSector();
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
        foreach (var point in positions)
        {
            var data = new NotGeneratedEntity();
            data.positionInSector = point;
            data.positionWorld = LocalToWorld(point);
            data.Id = SeedHelper.GetAsteroidId(Seed, point);


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


    public void PlacePlayerRandomInSector(Astronaut player, Random random)
    {

        if (TryGetNearestEntity(SpaceMath.Sector.GetCenter(this), out var entity))
        {
            player.SetPosition(GetRandomPositionNearAsteroid(random, entity));
        }
        else
            player.SetPosition(GetRandomPositionWithCollisionCheck(random, 0.2f));

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



    public void SpawnPlayerNearRandomAsteroid(Astronaut player, Random random)
    {

        if (Entities.Count == 0) return;

        PlacePlayerRandomInSector(player, random);
        player.SpawnPosition = player.Position;
        return;

        var asteroidID = random.Next(0, Entities.Count);

        player.Position = GetRandomPositionNearAsteroid(random, Entities[asteroidID]);


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
        entity.Name = "LA";


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

    public void RemoveEntity(SpaceEntity entity)
    {
        entity.Dispose();
        Entities.Remove(entity);

        sectorOctree.Remove(entity, entity.Position);
    }

    public SpaceEntity CreateEntity(Vector3 positionWorld)
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
        if(entity == null)
        {
            Debug.Error("[Sector] Trying to unload null entity!");
            return;
        }

        var asteroid = entity as Asteroid;


        if(entity.IsModified)
        {
            Debug.Success("[Sector] Saving modified asteroid " + entity.PositionWorld);
        }
        else
        {
            // just ignore
        }

        

        if (asteroid != null)
        {
            if(asteroid.IsGenerated)
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


        RemoveEntity(entity);

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

                if(distSqr >= Settings.ENTITY_UNLOAD_DISTANCE * Settings.ENTITY_UNLOAD_DISTANCE)
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