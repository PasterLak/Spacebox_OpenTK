using OpenTK.Mathematics;
using Spacebox.Common;
using Spacebox.Common.Physics;
using Spacebox.Game.Physics;

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
                Debug.Log("Nearest entity found ");
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
            Random random = new Random();

            for (int i = 0; i < numAsteroids; i++)
            {
                Vector3 asteroidPosition;

                do
                {
                    asteroidPosition = GeneratePosition(PositionWorld, 0.2f, random);
                } 
                while (!IsPositionValid(asteroidPosition));

                SpaceEntity asteroid = new SpaceEntity(asteroidPosition, this, i != 0);
                asteroids.Add(asteroid);
                sectorOctree.Add(asteroid, asteroidPosition);
            }
        }

        public Vector3 GeneratePosition(Vector3 chunkPosition, float margin01,  Random _random)
        {
            
            float margin = SizeBlocks * margin01; 
            
            float minX = chunkPosition.X + margin;
            float maxX = chunkPosition.X + SizeBlocks - margin;

            float minY = chunkPosition.Y + margin;
            float maxY = chunkPosition.Y + SizeBlocks - margin;

            float minZ = chunkPosition.Z + margin;
            float maxZ = chunkPosition.Z + SizeBlocks - margin;
            
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
            sharedShader.SetVector4("color", new Vector4(0, 1, 0, 1));
            //simple?.Render(Camera.Main);
            VisualDebug.DrawBoundingBox(BoundingBox, new Color4(255, 255, 20, 100));

            var cam = Camera.Main;
           
            
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
            if( lastCount != asteroidsCount)
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