using OpenTK.Mathematics;

namespace Spacebox.Common
{
    public class DynamicBody : Collision
    {
        public DynamicBody(Transform transform, BoundingVolume boundingVolume)
            : base(transform, boundingVolume, false)
        {
        }

        public override void OnCollisionEnter(Collision other)
        {
            // Optional override in derived classes
        }

        public override void OnCollisionExit(Collision other)
        {
            // Optional override in derived classes
        }
    }
}
