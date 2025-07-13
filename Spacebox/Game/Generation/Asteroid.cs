using OpenTK.Mathematics;
using Engine.Utils;

namespace Spacebox.Game.Generation
{
    public abstract class Asteroid : SpaceEntity
    {
        public const int ChunkSize = Chunk.Size;
        protected readonly int Seed;
        public Asteroid(ulong id, Vector3 positionWorld, Sector sector)
            : base(id, positionWorld, sector) {
            Seed = SeedHelper.ToIntSeed(id);
        }

        public virtual void OnGenerate() { }

        public virtual void FillChunkNoise(Chunk chunk) { }


    }

}
