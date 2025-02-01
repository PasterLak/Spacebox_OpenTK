using OpenTK.Mathematics;

namespace Engine.Physics
{
    public class StaticBody : Collision
    {
        public StaticBody(BoundingVolume boundingVolume)
            : base(boundingVolume, true)
        {
        }
    }
}
