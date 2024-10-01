using OpenTK.Mathematics;

namespace Spacebox.Common
{
    public class StaticBody : Collision
    {
        public StaticBody(BoundingVolume boundingVolume)
            : base( boundingVolume, true)
        {
        }
    }
}
