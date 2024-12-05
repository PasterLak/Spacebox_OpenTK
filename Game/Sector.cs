using OpenTK.Mathematics;
using Spacebox.Common;

namespace Spacebox.Game
{
    public class Sector
    {
        public const short SizeBlocks = 64; // 512
        public const short SizeBlocksHalf = SizeBlocks / 2;

        public Vector3 Position { get; private set; }
        public Vector3i Index { get; private set; }

        private readonly PointOctree<SpaceEntity> sectorOctree;
        public World World { get; private set; }

        public BoundingBox BoundingBox { get; private set; }

        private SimpleBlock simple;

        private static Shader sharedShader;
        private static Texture2D sharedTexture;

        private List<SpaceEntity> asteroids;

        public Sector(Vector3 position, Vector3i index, World world)
        {
            Position = position;
            Index = index;
            World = world;

            Vector3 sectorCenter = Position + new Vector3(SizeBlocksHalf);
            Vector3 sectorSize = new Vector3(SizeBlocks, SizeBlocks, SizeBlocks);

            BoundingBox = new BoundingBox(sectorCenter, sectorSize);

            sectorOctree = new PointOctree<SpaceEntity>(SizeBlocks, position, 1);

            asteroids = new List<SpaceEntity>();

            Initialize();
        }

        private void Initialize()
        {
            // Defer initialization that requires shared resources to the main thread
            World.EnqueueSectorInitialization(this);

            // Spawn asteroids
            SpawnAsteroids();
        }

        private void SpawnAsteroids()
        {
            int numAsteroids = 5; // For example, adjust as needed
            Random random = new Random();

            for (int i = 0; i < numAsteroids; i++)
            {
                Vector3 asteroidPosition;

                do
                {
                    float x = (float)(random.NextDouble() * (SizeBlocks - (SizeBlocks * 0.2f)) + Position.X - SizeBlocksHalf + (SizeBlocks * 0.1f));
                    float y = (float)(random.NextDouble() * (SizeBlocks - (SizeBlocks * 0.2f)) + Position.Y - SizeBlocksHalf + (SizeBlocks * 0.1f));
                    float z = (float)(random.NextDouble() * (SizeBlocks - (SizeBlocks * 0.2f)) + Position.Z - SizeBlocksHalf + (SizeBlocks * 0.1f));

                    asteroidPosition = new Vector3(x, y, z);

                } while (!IsPositionValid(asteroidPosition));

                SpaceEntity asteroid = new SpaceEntity(asteroidPosition);
                asteroids.Add(asteroid);
                sectorOctree.Add(asteroid, asteroidPosition);
            }
        }

        private bool IsPositionValid(Vector3 position)
        {
            // Ensure the asteroid is not too close to the sector boundaries
            float minDistance = SizeBlocks * 0.1f;
            BoundingBox innerBounds = new BoundingBox(
                BoundingBox.Center,
                BoundingBox.Size - new Vector3(minDistance * 2)
            );

            return innerBounds.Contains(position);
        }


        public void InitializeSharedResources()
        {
            // This method should be called from the main thread

            if (sharedShader == null)
            {
                sharedShader = ShaderManager.GetShader("Shaders/textured");
            }

            if (sharedTexture == null)
            {
                sharedTexture = TextureManager.GetTexture("Resources/Textures/selector.png", true);
            }

            simple = new SimpleBlock(sharedShader, sharedTexture, BoundingBox.Center);
            simple.Transform.Scale = new Vector3(SizeBlocks, SizeBlocks, SizeBlocks);
            //simple.Transform.Position = simple.Transform.Position;
        }

        public void Update()
        {
            // Update asteroids or other entities if needed

            VisualDebug.DrawBoundingBox(BoundingBox, new Color4(255, 255, 10, 100));
        }

        public void Render(Shader shader)
        {
            // Render the simple block
            //simple?.Render(Camera.Main);
            //VisualDebug.DrawBoundingBox(BoundingBox, new Color4(255, 255, 10, 100));
            // Render asteroids
            foreach (var asteroid in asteroids)
            {
                //asteroid.Render(shader);
            }
        }

        public void Dispose()
        {
            //simple?.Dispose();

            // Dispose asteroids if necessary
            foreach (var asteroid in asteroids)
            {
                //asteroid.Dispose();
            }
        }
    }
}
