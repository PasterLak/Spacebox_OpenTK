using OpenTK.Mathematics;

namespace Spacebox.Common.Physics
{
    public class StaticBody : Collision
    {
        public StaticBody(BoundingVolume boundingVolume)
            : base(boundingVolume, true)
        {
        }
    }
}
