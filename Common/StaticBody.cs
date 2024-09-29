using OpenTK.Mathematics;

namespace Spacebox.Common
{
    public class StaticBody : Collision
    {
        public StaticBody(Transform transform, BoundingVolume boundingVolume)
            : base(transform, boundingVolume, true)
        {
        }
    }
}
