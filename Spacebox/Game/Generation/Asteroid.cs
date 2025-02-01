using Engine;

using OpenTK.Mathematics;

namespace Spacebox.Game.Generation
{
    public class Asteroid : SpaceEntity
    {
        public Asteroid(int id, Vector3 positionWorld, Sector sector) : base(id, positionWorld, sector)
        {
        }
    }
}
