using Engine;
using Engine.Components;
using Engine.Generation;
using Engine.Physics;
using Engine.Utils;
using OpenTK.Mathematics;
using Spacebox.Game.Generation.Structures;
using Spacebox.Game.Generation.Tools;
using Spacebox.Game.Physics;
using Spacebox.Game.Player;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Generation
{
    public class NotGeneratedEntity
    {
        public ulong Id;
        public EntityType entityType;
        public Vector3 positionInSector;
        public Vector3 positionWorld;
        public Vector3 rotation;
    }
    public enum EntityType : byte
    {
        MadeByPlayer,
        AsteroidLight,
        AsteroidMedium,
        AsteroidHeavy
    }
    public class Sector : SpatialCell, IDisposable, ISpaceStructure
    {
        // ------------------ CONST --------------------------------------------
        public const short SizeBlocks = 8192; // 256 512 2048 4096 8192
        public const short SizeBlocksHalf = SizeBlocks / 2;

        public readonly ulong Seed;

        // ------------------ Properties --------------------------------------------

        //public static bool IsPlayerSpawned = false;
        public World World { get; private set; }

        public List<SpaceEntity> Entities { get; private set; }
        private Dictionary<ulong, NotGeneratedEntity> EntitiesGeneratedData { get; set; }

        public Dictionary<string, SpaceEntity> EntitiesNames { get; private set; }

        // ------------------ Private --------------------------------------------
        private readonly PointOctree<SpaceEntity> sectorOctree;
        private readonly PointOctree<NotGeneratedEntity> octreeNotGenerated;

        private bool isEdited = false;

        public Sector(Vector3 positionWorld, Vector3i positionIndex, World world)
        {
            PositionWorld = positionWorld;
            PositionIndex = positionIndex;
            Seed = SeedHelper.GetSectorId(World.Seed, positionIndex);
            World = world;

            Vector3 sectorCenter = PositionWorld + new Vector3(SizeBlocksHalf);
            Vector3 sectorSize = new Vector3(SizeBlocks, SizeBlocks, SizeBlocks);

            BoundingBox = new BoundingBox(sectorCenter, sectorSize);

            sectorOctree = new PointOctree<SpaceEntity>(SizeBlocks, positionWorld, 1);
            octreeNotGenerated = new PointOctree<NotGeneratedEntity>(SizeBlocks, positionWorld, 1);
            EntitiesGeneratedData = new Dictionary<ulong, NotGeneratedEntity>();

            Entities = new List<SpaceEntity>();
            EntitiesNames = new Dictionary<string, SpaceEntity>();


            /*if (WorldSaveLoad.CanLoadSectorHere(PositionIndex, out var sectorFolderPath))
            {

                var entities = WorldSaveLoad.LoadSpaceEntities(this);

                foreach (var e in entities)
                {
                    AddEntity(e, e.PositionWorld);

                    e.GenerateMesh();
                }

            }*/


            /*for (int i = 0; i < numAsteroids; i++)
            {
                SpawnPoints(random, i);
            }*/
            PopulateSector();
        }

        private void PopulateSector()
        {
            var points = GenerateAsteroidPositions(32, 64);
            DebugPositions(new Vector3[] {new Vector3(3323,1888,4183)});
            GenerateDataForPoints(new Vector3[] { new Vector3(3323, 1888, 4183) });
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

                var type = random.Next(0,3);

                switch(type)
                {
                    case 0: data.entityType = EntityType.AsteroidLight;
                        break;
                    case 1:
                        data.entityType = EntityType.AsteroidMedium;
                        break;
                    default:
                        data.entityType = EntityType.AsteroidHeavy;
                        break;
                }
                data.rotation = Vector3.Zero;

                EntitiesGeneratedData.Add(data.Id, data);
                octreeNotGenerated.Add(data, point);
            }
        }

        

        private void DebugPositions(Vector3[] positions)
        {
            foreach (var point in positions)
            {
                Debug.Log(point);
                var cube = World.Owner.AddChild(new CubeRenderer(LocalToWorld(point)));
                cube.AttachComponent(new AABBCollider());
                cube.Color = Color4.Green;
                cube.SetScale(8);
            }
        }

        private Vector3[] GenerateAsteroidPositions(int minCount, int maxCount)
        {
            var seed = SeedHelper.ToIntSeed(Seed);
            Random random = new Random(seed);
            var settings = new GeneratorSettings();
            settings.RejectionSamples = 5;
            settings.Seed = seed;
            settings.Count = random.Next(minCount, maxCount);

            return SectorPointProvider.CreatePoints(new FarthestPointGenerator(), settings).ToArray();
        }

        public void PlacePlayerRandomInSector(Astronaut player, Random random)
        {

            if (TryGetNearestEntity(GetCenter(), out var entity))
            {
                player.Position = GetRandomPositionNearAsteroid(random, entity);
            }
            else
                player.Position = GetRandomPositionWithCollisionCheck(random, 0.2f);

        }

        public Vector3 GetRandomPositionWithCollisionCheck(Random random, float margin01)
        {
            var pos = GetRandomPosition(PositionWorld, margin01, random);
            var near = IsPointInEntity(pos, out var nearest3);

            while (near == true)
            {
                pos = GetRandomPosition(PositionWorld, margin01, random);
                near = IsPointInEntity(pos, out var nearest4);
            }

            return pos;
        }

        public Vector3 GetRandomPositionNearAsteroid(Random random, SpaceEntity entity)
        {
            var geometryBox = entity.GeometryBoundingBox;

            var radius = (geometryBox.Max.Length -
                          geometryBox.Min.Length) / 2 + 100;

            var pos = GetRandomPointOnSphere(geometryBox.Center, random, radius);

            var near = IsPointInEntity(pos, out var nearest3);

            while (near == true)
            {
                pos = GetRandomPointOnSphere(geometryBox.Center, random, radius);
                near = IsPointInEntity(pos, out var nearest4);
            }

            return pos;
        }

        public Vector3 GetCenter()
        {
            return PositionWorld + new Vector3(SizeBlocksHalf, SizeBlocksHalf, SizeBlocksHalf);
        }


        public void SpawnPlayerNearAsteroid(Astronaut player, Random random)
        {

            if (Entities.Count == 0) return;

            PlacePlayerRandomInSector(player, random);
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



        private void GenerateAsteroidFromPoint(NotGeneratedEntity data)
        {

            Asteroid entity;


            switch (data.entityType)
            {
               /* case EntityType.AsteroidHeavy:
                    entity = new AsteroidHeavy(data.Id, data.positionWorld, this);
                    entity.Name = "HA";
                    break;
                case EntityType.AsteroidMedium:
                    entity = new AsteroidMedium(data.Id, data.positionWorld, this);
                    entity.Name = "MA";
                    break;*/
                case EntityType.AsteroidHeavy:
                default:
                    entity = new AsteroidHeavy(data.Id, data.positionWorld, this);
                    entity.Name = "LA";
                    break;
            }


            Entities.Add(entity);
            sectorOctree.Add(entity, data.positionWorld);
            octreeNotGenerated.Remove(data);

        }

        private static Vector3 GetRandomPointOnSphere(Vector3 center, Random _random, float radius)
        {
            double theta = _random.NextDouble() * Math.PI * 2;
            double phi = Math.Acos(2 * _random.NextDouble() - 1);
            float x = center.X + (float)(radius * Math.Sin(phi) * Math.Cos(theta));
            float y = center.Y + (float)(radius * Math.Sin(phi) * Math.Sin(theta));
            float z = center.Z + (float)(radius * Math.Cos(phi));
            return new Vector3(x, y, z);
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
            EntitiesNames.Add(entity.Name, entity);
            sectorOctree.Add(entity, positionWorld);
        }

        public void RemoveEntity(SpaceEntity entity)
        {
            entity.Dispose();
            Entities.Remove(entity);
            EntitiesNames.Remove(entity.Name);
            sectorOctree.Remove(entity, entity.Position);
        }


        public static Vector3 GetRandomPosition(Vector3 positionWorld, float margin01, Random _random)
        {
            float margin = SizeBlocks * margin01;

            float minX = positionWorld.X + margin;
            float maxX = positionWorld.X + SizeBlocks - margin;

            float minY = positionWorld.Y + margin;
            float maxY = positionWorld.Y + SizeBlocks - margin;

            float minZ = positionWorld.Z + margin;
            float maxZ = positionWorld.Z + SizeBlocks - margin;

            float x = (float)(_random.NextDouble() * (maxX - minX) + minX);
            float y = (float)(_random.NextDouble() * (maxY - minY) + minY);
            float z = (float)(_random.NextDouble() * (maxZ - minZ) + minZ);

            return new Vector3(x, y, z);
        }

        private bool IsPositionValid(Vector3 position)
        {
            float minDistance = SizeBlocks * 0.1f;
            BoundingBox innerBounds = new BoundingBox(
                BoundingBox.Center,
                BoundingBox.Size - new Vector3(minDistance * 2)
            );

            return innerBounds.Contains(position);
        }

        public SpaceEntity CreateEntity(Vector3 positionWorld)
        {
            SpaceEntity entity = new SpaceEntity(0, positionWorld, this);
            // entity.Name = "NewEntity" + MaxEntityID++;

            while (EntitiesNames.ContainsKey(entity.Name))
            {
                // entity.Name = "NewEntity" + MaxEntityID++;
            }
            AddEntity(entity, positionWorld);

            return entity;
        }

        public void Update()
        {
            var camera = Camera.Main;
            if (camera == null) return;
            List<NotGeneratedEntity> nearby = new List<NotGeneratedEntity>();
            if (octreeNotGenerated.GetNearbyNonAlloc(camera.PositionWorld, 1000, nearby))
            {
                foreach (var entity in nearby)
                {

                    GenerateAsteroidFromPoint(entity);
                }
            }
            for (int i = 0; i < Entities.Count; i++)
            {
                Entities[i].Update();

                if (VisualDebug.Enabled)
                {
                    VisualDebug.DrawSphere(Entities[i].CenterOfMass, Entities[i].GravityRadius, 16, Color4.Blue);
                }
            }
        }

        public void Render(BlockMaterial shader)
        {

            VisualDebug.DrawBoundingBox(BoundingBox, new Color4(255, 255, 20, 100));

            var cam = Camera.Main;


            for (int i = 0; i < Entities.Count; i++)
            {

                var entity = Entities[i];

                if (cam.Frustum.IsInFrustum(entity.GeometryBoundingBox))
                {

                    var disSqr = Vector3.DistanceSquared(entity.GeometryBoundingBox.Center, cam.Position);

                    if (!entity.IsGenerated)
                    {
                        var e = entity as Asteroid;

                        if (e != null)
                        {
                            e.OnGenerate();
                        }

                        continue;
                    }

                    if (disSqr < 1000 * 1000)
                    {
                        if (!entity.Tag.Enabled)
                        {
                            // entity.Tag.Enabled = true;
                        }
                        entity.Render(cam, shader);

                        if (entity.StarsEffect.Enabled)
                            entity.StarsEffect.Enabled = false;


                    }
                    else
                    {
                        if (!entity.StarsEffect.Enabled)
                            entity.StarsEffect.Enabled = true;

                        if (entity.Tag.Enabled)
                        {
                            // entity.Tag.Enabled = false;
                        }

                        entity.RenderEffect(disSqr);
                    }


                }
                else
                {

                }
            }

        }

        public void Dispose()
        {

            foreach (var asteroid in Entities)
            {
                asteroid?.Dispose();
            }

            Entities = null;
            EntitiesNames = null;
        }

        public Vector3 LocalToWorldPosition(Vector3 local)
        {
            return PositionWorld + local;
        }

        public string ToFolderName()
        {
            return IndexToFolderName(PositionIndex);
        }

        public static string IndexToFolderName(Vector3i index)
        {
            int x = index.X;
            int y = index.Y;
            int z = index.Z;

            string xStr = x >= 0 ? "+" + x : "-" + x;
            string yStr = y >= 0 ? "+" + y : "-" + y;
            string zStr = z >= 0 ? "+" + z : "-" + z;

            return "Sector" + xStr + yStr + zStr;
        }
    }
}