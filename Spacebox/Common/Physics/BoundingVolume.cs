using OpenTK.Mathematics;

namespace Spacebox.Common.Physics
{
    public abstract class BoundingVolume
    {
        public Vector3 Center { get; set; }
        public abstract bool Intersects(BoundingVolume other);
        public abstract float GetLongestSide();
        public abstract BoundingVolume Clone();
    }
}
