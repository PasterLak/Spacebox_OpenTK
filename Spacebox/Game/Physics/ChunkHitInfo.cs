using OpenTK.Mathematics;

using Spacebox.Game.Generation;


namespace Spacebox.Game.Physics
{
    public struct ChunkHitInfo
    {
        public Vector3 HitPosition;

        /// <summary> Distance from origin to hit point. </summary>
        public float Distance;

        public Chunk Chunk;


    }

}
