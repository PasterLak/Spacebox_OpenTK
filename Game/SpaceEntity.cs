using OpenTK.Mathematics;
using Spacebox.Common;

namespace Spacebox.Game
{
    internal class SpaceEntity : Node3D
    {
        public const byte SizeChunks = 4; // will be 16
        public const byte SizeChunksHalf = SizeChunks / 2;
        public const short SizeBlocks = SizeChunks * Chunk.Size;
        public const short SizeBlocksHalf = SizeChunks * Chunk.Size / 2;


        public BoundingBox BoundingBox { get; private set; }
        public Sector Sector { get; private set; }

        public SpaceEntity(Vector3 position)
        {
            BoundingBox = new BoundingBox(position + new Vector3(SizeBlocksHalf, SizeBlocksHalf, SizeBlocksHalf), new Vector3(SizeBlocks, SizeBlocks, SizeBlocks));
        }

    }
}
