using OpenTK.Mathematics;

namespace Spacebox.Common
{
    public abstract class Collision
    {
        public bool IsStatic { get; protected set; }
        public BoundingVolume BoundingVolume { get; protected set; }
        public Transform Transform { get; protected set; }

        private HashSet<Collision> _currentColliders = new HashSet<Collision>();

        protected Collision(Transform transform, BoundingVolume boundingVolume, bool isStatic)
        {
            Transform = transform;
            BoundingVolume = boundingVolume;
            IsStatic = isStatic;
        }

        public void UpdateBounding()
        {
            if (BoundingVolume is BoundingBox box)
            {
                box.Center = Transform.Position;
            }
            else if (BoundingVolume is BoundingSphere sphere)
            {
                sphere.Center = Transform.Position;
            }
        }

        public virtual void OnCollisionEnter(Collision other) { }

        public virtual void OnCollisionExit(Collision other) { }

        public void DrawDebug()
        {
            if (BoundingVolume is BoundingBox box)
            {
                Debug.DrawBoundingBox(box, Color4.Yellow);
            }
            else if (BoundingVolume is BoundingSphere sphere)
            {
                Debug.DrawBoundingSphere(sphere, Color4.Yellow);
            }
        }
    }
}
