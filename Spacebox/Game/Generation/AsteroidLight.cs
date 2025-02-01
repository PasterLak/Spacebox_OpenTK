using OpenTK.Mathematics;
using Engine;
namespace Spacebox.Game.Generation
{
    public class AsteroidLight : Asteroid
    {
        public AsteroidLight(int id, Vector3 positionWorld, Sector sector) : base(id, positionWorld, sector)
        {
        }
    }
}
