using OpenTK.Mathematics;
using Spacebox.Common;
using Spacebox.Common.Physics;
using Spacebox.Game.Physics;
using Spacebox.Game.Player;

namespace Spacebox.Game.Generation
{
    public class Sector : IDisposable
    {
        public const short SizeBlocks = 256; // 512
        public const short SizeBlocksHalf = SizeBlocks / 2;

        public Vector3 PositionWorld { get; private set; }
        public Vector3i PositionIndex { get; private set; }

        private readonly PointOctree<SpaceEntity> sectorOctree;
        public World World { get; private set; }

        public BoundingBox BoundingBox { get; private set; }

        private SimpleBlock simple;

        private static Shader sharedShader;
        private static Texture2D sharedTexture;

        private List<SpaceEntity> asteroids;

        private List<Vector3> positions = new List<Vector3>();

        public static bool IsPlayerSpawned = false;

        public Sector(Vector3 positionWorld, Vector3i positionIndex, World world)
        {
            PositionWorld = positionWorld;
            PositionIndex = positionIndex;
            World = world;

            Vector3 sectorCenter = PositionWorld + new Vector3(SizeBlocksHalf);
            Vector3 sectorSize = new Vector3(SizeBlocks, SizeBlocks, SizeBlocks);

            BoundingBox = new BoundingBox(sectorCenter, sectorSize);

            sectorOctree = new PointOctree<SpaceEntity>(SizeBlocks, positionWorld, 1);

            asteroids = new List<SpaceEntity>();

            InitializeSharedResources();
            SpawnAsteroids();
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

        public void SpawnPlayerNearAsteroid(Astronaut player, Random random)
        {
            if (IsPlayerSpawned) return;
            if (asteroids.Count == 0) return;

            var asteroidID = random.Next(0, asteroids.Count);

            var radius = (asteroids[asteroidID].GeometryBoundingBox.Max.Length -
                          asteroids[asteroidID].GeometryBoundingBox.Min.Length) / 2 + 10;

            /*for (int i = 0; i < 100; i++)
            {
                var pos2 = GetRandomPointOnSphere(asteroids[asteroidID].GeometryBoundingBox.Center, random, radius);

                var near2 = IsPointInEntity(pos2, out var nearest);

                while (near2 == true)
                {
                    Debug.Error("Spawn collision!");

                    pos2 = GetRandomPointOnSphere(asteroids[asteroidID].GeometryBoundingBox.Center, random, radius);
                    near2 = IsPointInEntity(pos2, out var nearest2);
                }

                positions.Add(pos2);
            }*/

            var pos = GetRandomPointOnSphere(asteroids[asteroidID].GeometryBoundingBox.Center, random, radius);

            var near = IsPointInEntity(pos, out var nearest3);

            while (near == true)
            {
                pos = GetRandomPointOnSphere(asteroids[asteroidID].GeometryBoundingBox.Center, random, radius);
                near = IsPointInEntity(pos, out var nearest4);
            }

            player.Position = pos;

            //sectorOctree.GetNearby(pos, radius);

            IsPlayerSpawned = true;
        }

        private void Initialize()
        {
            //World.EnqueueSectorInitialization(this);

            SpawnAsteroids();
        }

        public bool IsColliding(Vector3 positionWorld, BoundingVolume volume)
        {
            if (TryGetNearestEntity(positionWorld, out var entity))
            {
                return entity.IsColliding(volume);
            }

            return false;
        }

        public bool Raycast(Ray ray, out VoxelPhysics.HitInfo hitInfo)
        {
            if (TryGetNearestEntity(ray.Origin, out var entity))
            {
                return entity.Raycast(ray, out hitInfo);
            }

            hitInfo = new VoxelPhysics.HitInfo();
            return false;
        }

        public bool TryGetNearestEntity(Vector3 positionWorld, out SpaceEntity entity)
        {
            entity = null;
            if (asteroids.Count == 0) return false;

            float nearestDistSq = float.MaxValue;

            for (byte i = 0; i < asteroids.Count; i++)
            {
                Vector3 diff = positionWorld - asteroids[i].GeometryBoundingBox.Center;
                float distSq = diff.LengthSquared;
                if (distSq < nearestDistSq)
                {
                    nearestDistSq = distSq;
                    entity = asteroids[i];
                }
            }

            return true;
        }


        private void SpawnAsteroids()
        {
            int numAsteroids = 3;
            Random random = World.Random == null ? new Random() : World.Random;

            for (int i = 0; i < numAsteroids; i++)
            {
                Vector3 asteroidPosition;

                do
                {
                    asteroidPosition = GetRandomPosition(PositionWorld, 0.1f, random);
                } while (!IsPositionValid(asteroidPosition));

                AddEntity(new SpaceEntity(asteroidPosition, this, true), asteroidPosition);
            }
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
            foreach (var a in asteroids)
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
            asteroids.Add(entity);
            sectorOctree.Add(entity, positionWorld);
        }

        public void RemoveEntity(SpaceEntity entity)
        {
            entity.Dispose();
            asteroids.Remove(entity);
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
            if (sharedShader == null)
            {
                sharedShader = ShaderManager.GetShader("Shaders/textured");
            }

            if (sharedTexture == null)
            {
                sharedTexture = TextureManager.GetTexture("Resources/Textures/selector.png", true);
            }

            simple = new SimpleBlock(sharedShader, sharedTexture, BoundingBox.Center);
            simple.Scale = new Vector3(SizeBlocks, SizeBlocks, SizeBlocks);
            simple.Position = simple.Position;
        }

        public void Update()
        {
            // VisualDebug.DrawBoundingBox(BoundingBox, new Color4(255, 255, 10, 100));

            for (int i = 0; i < asteroids.Count; i++)
            {
                asteroids[i].Update();
            }
        }

        int lastCount = 0;

        public void Render(Shader shader)
        {
            //sharedShader.SetVector4("color", new Vector4(0, 1, 0, 1));
            //simple?.Render(Camera.Main);
            VisualDebug.DrawBoundingBox(BoundingBox, new Color4(255, 255, 20, 100));

            var cam = Camera.Main;

            foreach (var p in positions)
            {
                VisualDebug.DrawPosition(p, Color4.Blue);
            }

            var asteroidsCount = 0;

            for (int i = 0; i < asteroids.Count; i++)
            {
                asteroids[i].Render(cam);
                continue;
                if (cam.Frustum.IsInFrustum(asteroids[i].GeometryBoundingBox))
                {
                    asteroids[i].Render(cam);
                    asteroidsCount++;
                }
            }

            if (lastCount != asteroidsCount)
            {
                lastCount = asteroidsCount;
                Debug.Success("Asteroids rendering: " + asteroidsCount);
            }
        }

        public void Dispose()
        {
            // DisposalManager.EnqueueForDispose(simple);
            simple?.Dispose();

            foreach (var asteroid in asteroids)
            {
                //DisposalManager.EnqueueForDispose(asteroid);
                asteroid?.Dispose();
            }

            asteroids = null;
        }

        public override string ToString()
        {
            return $"[Sector] Pos: {PositionWorld.ToString()} Index: {PositionIndex.ToString()}";
        }
    }
}