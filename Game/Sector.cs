using OpenTK.Mathematics;
using Spacebox.Common;

namespace Spacebox.Game
{
    public class Sector
    {
        public const short SizeBlocks = 512;
        public const short SizeBlocksHalf = SizeBlocks / 2;

        public Vector3 Position { get; private set; }
        public Vector3i Index { get; private set; }
        public List<Chunk> Chunks { get; private set; }

        public World World { get; private set; }

        public BoundingBox BoundingBox { get; private set; }

       
        public Sector(Vector3 position, Vector3i index, World world)
        {
            Position = position;
            Index = index;
            Chunks = new List<Chunk>();

            InitializeChunks();
            World = world;

            BoundingBox = new BoundingBox(position + new Vector3(SizeBlocksHalf, SizeBlocksHalf, SizeBlocksHalf), new Vector3(SizeBlocks, SizeBlocks, SizeBlocks));
        }

        private void InitializeChunks()
        {
            // Position chunks relative to the sector's position
            Vector3 chunkPosition = Position; // Adjust if multiple chunks per sector
            ChunkSaveLoadManager.WorldChunks world = ChunkSaveLoadManager.LoadWorld(World.Instance.WorldData.Info.Name);
            Debug.Log($"chunks count: {world.Chunks.Count}");
            Chunk loadedChunk = null;

            if(world.Chunks.ContainsKey(new Vector3(0, 0, 0)))
            {
                loadedChunk = world.Chunks[new Vector3(0, 0, 0)];
            }
            if (loadedChunk != null)
            {
                Console.WriteLine("Loaded Chunk!");
                Chunks.Add(loadedChunk);
            }
            else
            {
                Console.WriteLine("New Chunk!");
                Chunk newChunk = new Chunk(chunkPosition);
                Chunks.Add(newChunk);
            }
        }

        public void Update()
        {
            // Update contents of the sector if necessary
            foreach (var chunk in Chunks)
            {
                chunk.Test(World.Player); // Assuming you have a reference to the player
            }

            VisualDebug.DrawBoundingBox(BoundingBox, Color4.Yellow);
        }

        public void Render(Shader shader)
        {
            foreach (var chunk in Chunks)
            {
                chunk.Draw(shader);
            }
        }

        public void Shift(Vector3 shiftAmount)
        {
            Position -= shiftAmount;

            // Shift all chunks within the sector
            foreach (var chunk in Chunks)
            {
                chunk.Shift(shiftAmount);
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
