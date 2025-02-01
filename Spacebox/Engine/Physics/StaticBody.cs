using OpenTK.Mathematics;

namespace Spacebox.Engine.Physics
{
    public class StaticBody : Collision
    {
        public StaticBody(BoundingVolume boundingVolume)
            : base(boundingVolume, true)
        {
        }
    }
}
