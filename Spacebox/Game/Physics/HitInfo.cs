using OpenTK.Mathematics;

using Spacebox.Game.Generation;
using Engine;

namespace Spacebox.Game.Physics
{
    public struct HitInfo
    {
        public Vector3 position;
        public Vector3Byte blockPositionIndex;
        public Vector3i blockPositionEntity;
        public Vector3SByte normal;
        public Chunk chunk;
        public Block block;


    }
}
