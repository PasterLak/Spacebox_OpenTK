using OpenTK.Mathematics;
using Spacebox.Common;

namespace Spacebox.Game
{
    public class Sector0
    {
        public const short SizeBlocks = 64; // 512
        public const short SizeBlocksHalf = SizeBlocks / 2;

        public Vector3 Position { get; private set; }
        public Vector3i Index { get; private set; }
        public List<Chunk> Chunks { get; private set; }
        private readonly PointOctree<SpaceEntity> sectorOctree;
        public World World { get; private set; }

        public BoundingBox BoundingBox { get; private set; }


        public Sector0(Vector3 position, Vector3i index, World world)
        {
            Position = position;
            Index = index;
            Chunks = new List<Chunk>();

            InitializeChunks();
            World = world;

            BoundingBox = new BoundingBox(position + new Vector3(SizeBlocksHalf, SizeBlocksHalf, SizeBlocksHalf), new Vector3(SizeBlocks, SizeBlocks, SizeBlocks));
            sectorOctree = new PointOctree<SpaceEntity>(SizeBlocks,
                   position, 1);
        }

        private void InitializeChunks()
        {
            // Position chunks relative to the sector's position
            Vector3 chunkPosition = Position; // Adjust if multiple chunks per sector
            ChunkSaveLoadManager.WorldChunks world = ChunkSaveLoadManager.LoadWorld(World.Instance.WorldData.Info.Name);

            Chunk loadedChunk = null;

            if (world.Chunks.ContainsKey(new Vector3(0, 0, 0)))
            {
                loadedChunk = world.Chunks[new Vector3(0, 0, 0)];
            }
            if (loadedChunk != null)
            {

                Chunks.Add(loadedChunk);

            }
            else
            {

                Chunk newChunk = new Chunk(chunkPosition);
                Chunks.Add(newChunk);
            }
        }

        public void Update()
        {

            foreach (var chunk in Chunks)
            {
                chunk.Test(World.Player);
            }

            //VisualDebug.DrawBoundingBox(BoundingBox, Color4.Yellow);
        }

        public void Render(Shader shader)
        {
            foreach (var chunk in Chunks)
            {
                chunk.Draw(shader);
            }
        }

        public void Dispose()
        {
            foreach (var chunk in Chunks)
            {
                chunk.Dispose();
            }
        }
    }
}
