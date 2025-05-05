
using Spacebox.Game.Generation;
using Engine;
using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Game.Physics
{
    public struct CollideInfo
    {
        public Vector3Byte blockPositionIndex;
        public Chunk chunk;
        public Vector3SByte normal;
        public Block block;
    }
}
