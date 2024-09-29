using OpenTK.Mathematics;
using Spacebox.Entities;

namespace Spacebox.Common
{
    public class Collision : ICollidable
    {
        public bool IsStatic { get; private set; }
        public BoundingVolume BoundingVolume { get; private set; }
        public Transform Transform { get; private set; }

        private HashSet<ICollidable> _currentColliders = new HashSet<ICollidable>();

        public Collision(Transform transform, BoundingVolume boundingVolume, bool isStatic)
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

        public void OnCollisionEnter(ICollidable other)
        {
            if (_currentColliders.Contains(other))
                return;
            _currentColliders.Add(other);

            if (Transform.Name == "Player")
            {
                Console.WriteLine("Player enter !");
                //model.Material.Color = new Vector4(1, 0, 0, 1);
            }
            else
            {
                //Console.WriteLine("Player 2!");
            }
           
        }

        public void OnCollisionExit(ICollidable other)
        {
            if (_currentColliders.Contains(other))
            {
                if (Transform.Name == "Player")
                {
                    Console.WriteLine("Player exit!");
                    //model.Material.Color = new Vector4(1, 0, 0, 1);
                }
                else
                {
                    //Console.WriteLine("Player 2 exed!");
                }
                _currentColliders.Remove(other);
            }
        }

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
