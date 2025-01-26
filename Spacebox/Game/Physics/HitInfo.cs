using OpenTK.Mathematics;
using Spacebox.Common;
using Spacebox.Game.Generation;

namespace Spacebox.Game.Physics
{
    public struct HitInfo
    {
        public Vector3 position;
        public Vector3Byte blockPositionIndex;
        public Vector3SByte normal;
        public Chunk chunk;
        public Block block;


    }
}
