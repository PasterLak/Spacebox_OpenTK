using Engine;
using Engine.Physics;
using OpenTK.Mathematics;
using Spacebox.Game.Physics;
using Spacebox.Game.Player;

namespace Spacebox.Game.Generation
{
    public class Sector : IDisposable
    {
        public const short SizeBlocks = 8192; // 256 512 2048 4096 8192
        public const short SizeBlocksHalf = SizeBlocks / 2;

        public Vector3 PositionWorld { get; private set; }
        public Vector3i PositionIndex { get; private set; }

        private readonly PointOctree<SpaceEntity> sectorOctree;
        public World World { get; private set; }

        public BoundingBox BoundingBox { get; private set; }



        public List<SpaceEntity> Entities { get; private set; }
        public Dictionary<string, SpaceEntity> EntitiesNames { get; private set; }

        public static bool IsPlayerSpawned = false;
        private bool isEdited = false;
        public int MaxEntityID = 3;
        public Sector(Vector3 positionWorld, Vector3i positionIndex, World world)
        {
            PositionWorld = positionWorld;
            PositionIndex = positionIndex;
            World = world;

            Vector3 sectorCenter = PositionWorld + new Vector3(SizeBlocksHalf);
            Vector3 sectorSize = new Vector3(SizeBlocks, SizeBlocks, SizeBlocks);

            BoundingBox = new BoundingBox(sectorCenter, sectorSize);

            sectorOctree = new PointOctree<SpaceEntity>(SizeBlocks, positionWorld, 1);

            Entities = new List<SpaceEntity>();
            EntitiesNames = new Dictionary<string, SpaceEntity>();
            InitializeSharedResources();


            int numAsteroids = 3;
            Random random =  World.Random;


            if (WorldSaveLoad.CanLoadSectorHere(PositionIndex, out var sectorFolderPath))
            {

                var entities = WorldSaveLoad.LoadSpaceEntities(this);

                foreach (var e in entities)
                {
                    AddEntity(e, e.PositionWorld);

                    e.GenerateMesh();
                }

                Debug.Success("Sector was loaded: " + PositionIndex);
            }

            for (int i = 0; i < numAsteroids; i++)
            {
                SpawnAsteroids(random, i);
            }


            //Initialize();

             
        }

        public void SpawnPlayerRandomInSector(Astronaut player, Random random)
        {
            if (IsPlayerSpawned) return;

            player.Position = GetRandomPositionWithCOllisionCheck(random, 0.2f);

            IsPlayerSpawned = true;
        }

        public Vector3 GetRandomPositionWithCOllisionCheck(Random random, float margin01)
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
                          geometryBox.Min.Length) / 2 + 10;

            var pos = GetRandomPointOnSphere(geometryBox.Center, random, radius);

            var near = IsPointInEntity(pos, out var nearest3);

            while (near == true)
            {
                pos = GetRandomPointOnSphere(geometryBox.Center, random, radius);
                near = IsPointInEntity(pos, out var nearest4);
            }


            return pos;
        }

        public void SpawnPlayerNearAsteroid(Astronaut player, Random random)
        {
            if (IsPlayerSpawned) return;
            if (Entities.Count == 0) return;

            var asteroidID = random.Next(0, Entities.Count);

            player.Position = GetRandomPositionNearAsteroid(random, Entities[asteroidID]);

            IsPlayerSpawned = true;
        }

        public bool IsColliding(Vector3 positionWorld, BoundingVolume volume, out CollideInfo collideInfo)
        {
            if (TryGetNearestEntity(positionWorld, out var entity))
            {
                return entity.IsColliding(volume, out  collideInfo);
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


        private void SpawnAsteroids(Random random, int id)
        {

            var name = "Asteroid" + id;
            Vector3 asteroidWorldPosition;
            asteroidWorldPosition = GetRandomPosition(PositionWorld, 0.1f, random);

            if (EntitiesNames.ContainsKey(name)) { return; }

            while (!IsPositionValid(asteroidWorldPosition))
            {
                asteroidWorldPosition = GetRandomPosition(PositionWorld, 0.1f, random);
            }

            var asteroid = new AsteroidLight(id, asteroidWorldPosition, this);
            asteroid.AddChunk(new Chunk(new Vector3SByte(0, 0, 0), asteroid));
            asteroid.Name = name;
            AddEntity(asteroid, asteroidWorldPosition);

        }

        public static Vector3 GetRandomPointOnSphere(Vector3 center, Random _random, float radius)
        {
            double theta = _random.NextDouble() * Math.PI * 2;
            double phi = Math.Acos(2 * _random.NextDouble() - 1);
            float x = center.X + (float)(radius * Math.Sin(phi) * Math.Cos(theta));
            float y = center.Y + (float)(radius * Math.Sin(phi) * Math.Sin(theta));
            float z = center.Z + (float)(radius * Math.Cos(phi));
            return new Vector3(x, y, z);
        }

        private bool IsPointInEntity(Vector3 point, out SpaceEntity entity)
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

        public SpaceEntity CreateEntity(Vector3 positionWorld)
        {
            SpaceEntity entity = new SpaceEntity(0, positionWorld, this);
            entity.Name = "NewEntity" + MaxEntityID++;

            while (EntitiesNames.ContainsKey(entity.Name))
            {
                entity.Name = "NewEntity" + MaxEntityID++;
            }
            AddEntity(entity, positionWorld);

            return entity;
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


        public void InitializeSharedResources()
        {
            /*if (sharedShader == null)
            {
                sharedShader = ShaderManager.GetShader("Shaders/textured");
            }

            if (sharedTexture == null)
            {
                sharedTexture = TextureManager.GetTexture("Resources/Textures/selector.png", true);
            }

            simple = new SimpleBlock(sharedShader, sharedTexture, BoundingBox.Center);
            simple.Scale = new Vector3(SizeBlocks, SizeBlocks, SizeBlocks);
            simple.Position = simple.Position;*/
        }

        public void Update()
        {
            // VisualDebug.DrawBoundingBox(BoundingBox, new Color4(255, 255, 10, 100));

            for (int i = 0; i < Entities.Count; i++)
            {
                Entities[i].Update();

                if(VisualDebug.ShowDebug)
                {
                    VisualDebug.DrawSphere(Entities[i].CenterOfMass, Entities[i].GravityRadius, 16, Color4.Blue);
                }
            }
        }


        public void Render(Shader shader)
        {

            VisualDebug.DrawBoundingBox(BoundingBox, new Color4(255, 255, 20, 100));

            var cam = Camera.Main;


            for (int i = 0; i < Entities.Count; i++)
            {

                var entity = Entities[i];
                if (cam.Frustum.IsInFrustum(entity.GeometryBoundingBox, cam))
                {
                    var disSqr = Vector3.DistanceSquared(entity.GeometryBoundingBox.Center, cam.Position);

                    if(disSqr < 500 * 500)
                    {
                        entity.Render(cam);

                        if (entity.StarsEffect.Enabled)
                            entity.StarsEffect.Enabled = false;

                       
                    }
                    else
                    {
                        if (!entity.StarsEffect.Enabled)
                            entity.StarsEffect.Enabled = true;

                        entity.RenderEffect(disSqr);
                    }
                    

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

        public override string ToString()
        {
            return $"[Sector] Pos: {PositionWorld.ToString()} Index: {PositionIndex.ToString()}";
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