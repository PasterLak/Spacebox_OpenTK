using OpenTK.Mathematics;
using Engine;

namespace Spacebox.Game.Generation
{
    public abstract class Asteroid : SpaceEntity
    {
        public const int ChunkSize = Chunk.Size;

        public Asteroid(int id, Vector3 positionWorld, Sector sector)
            : base(id, positionWorld, sector) { }

        public virtual void OnGenerate() { }

        public virtual void FillChunkNoise(Chunk chunk) { }
    }

}
