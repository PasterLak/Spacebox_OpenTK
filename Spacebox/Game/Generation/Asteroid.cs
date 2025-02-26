
using Engine;
using OpenTK.Mathematics;

namespace Spacebox.Game.Generation
{
    public class Asteroid : SpaceEntity
    {
        public Asteroid(int id, Vector3 positionWorld, Sector sector) : base(id, positionWorld, sector)
        {
        }

        public void OnGenerate()
        {

          AddChunk(new Chunk(new Vector3SByte(0, 0, 0), this));
            IsGenerated = true;
        }
    }
}
