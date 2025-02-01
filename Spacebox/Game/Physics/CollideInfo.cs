using OpenTK.Mathematics;
using Spacebox.Game.Generation;

namespace Spacebox.Game.Physics
{
    public struct CollideInfo
    {
        public Vector3Byte blockPositionIndex;
        public Chunk chunk;
        public Engine.Vector3SByte normal;
        public Block block;
    }
}
