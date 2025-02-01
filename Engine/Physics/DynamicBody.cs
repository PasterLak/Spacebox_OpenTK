using OpenTK.Mathematics;

namespace Engine.Physics
{
    public class DynamicBody : Collision
    {
        public DynamicBody(BoundingVolume boundingVolume)
            : base(boundingVolume, false)
        {
        }

        public override void OnCollisionEnter(Collision other)
        {

        }

        public override void OnCollisionExit(Collision other)
        {

        }
    }
}
