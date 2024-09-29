using OpenTK.Mathematics;

namespace Spacebox.Common
{
    public class Trigger : StaticBody
    {
        public Trigger(Transform transform, BoundingVolume boundingVolume)
            : base(transform, boundingVolume)
        {
        }

        public override void OnCollisionEnter(Collision other)
        {
            // Implement trigger-specific logic
        }

        public override void OnCollisionExit(Collision other)
        {
            // Implement trigger-specific logic
        }
    }
}
