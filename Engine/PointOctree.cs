
using OpenTK.Mathematics;
using Engine.Physics;


namespace Engine
{
    public class PointOctree<T>
    {
        
        public int Count { get; private set; }

       
        PointOctreeNode<T> rootNode;

        // Size that the octree was on creation
        readonly float initialSize;

        // Minimum side length that a node can be - essentially an alternative to having a max depth
        readonly float minSize;

        /// <summary>
        /// Constructor for the point octree.
        /// </summary>
        /// <param name="initialWorldSize">Size of the sides of the initial node. The octree will never shrink smaller than this.</param>
        /// <param name="initialWorldPos">Position of the centre of the initial node.</param>
        /// <param name="minNodeSize">Nodes will stop splitting if the new nodes would be smaller than this.</param>
        public PointOctree(float initialWorldSize, Vector3 initialWorldPos, float minNodeSize)
        {
            if (minNodeSize > initialWorldSize)
            {
                Console.WriteLine("Minimum node size must be at least as big as the initial world size. Was: " + minNodeSize + " Adjusted to: " + initialWorldSize);
                minNodeSize = initialWorldSize;
            }
            Count = 0;
            initialSize = initialWorldSize;
            minSize = minNodeSize;
            rootNode = new PointOctreeNode<T>(initialSize, minSize, initialWorldPos);
        }

        /// <summary>
        /// Add an object.
        /// </summary>
        /// <param name="obj">Object to add.</param>
        /// <param name="objPos">Position of the object.</param>
        public void Add(T obj, Vector3 objPos)
        {
            int count = 0; // Safety check against infinite/excessive growth
            while (!rootNode.Add(obj, objPos))
            {
                Grow(objPos - rootNode.Center);
                if (++count > 20)
                {
                    Console.WriteLine("Aborted Add operation as it seemed to be going on forever (" + (count - 1) + ") attempts at growing the octree.");
                    return;
                }
            }
            Count++;
        }

        /// <summary>
        /// Remove an object. Makes the assumption that the object only exists once in the tree.
        /// </summary>
        /// <param name="obj">Object to remove.</param>
        /// <returns>True if the object was removed successfully.</returns>
        public bool Remove(T obj)
        {
            bool removed = rootNode.Remove(obj);

            // See if we can shrink the octree down now that we've removed the item
            if (removed)
            {
                Count--;
                Shrink();
            }

            return removed;
        }

        /// <summary>
        /// Removes the specified object at the given position. Makes the assumption that the object only exists once in the tree.
        /// </summary>
        /// <param name="obj">Object to remove.</param>
        /// <param name="objPos">Position of the object.</param>
        /// <returns>True if the object was removed successfully.</returns>
        public bool Remove(T obj, Vector3 objPos)
        {
            bool removed = rootNode.Remove(obj, objPos);

            if (removed)
            {
                Count--;
                Shrink();
            }

            return removed;
        }

        /// <summary>
        /// Returns objects that are within <paramref name="maxDistance"/> of the specified ray.
        /// If none, returns false. Uses supplied list for results.
        /// </summary>
        public bool GetNearbyNonAlloc(Ray ray, float maxDistance, List<T> nearBy)
        {
            nearBy.Clear();
            rootNode.GetNearby(ref ray, maxDistance, nearBy);
            return nearBy.Count > 0;
        }

        /// <summary>
        /// Returns objects that are within <paramref name="maxDistance"/> of the specified ray.
        /// If none, returns an empty array (not null).
        /// </summary>
        public T[] GetNearby(Ray ray, float maxDistance)
        {
            List<T> collidingWith = new List<T>();
            rootNode.GetNearby(ref ray, maxDistance, collidingWith);
            return collidingWith.ToArray();
        }

        /// <summary>
        /// Returns objects that are within <paramref name="maxDistance"/> of the specified position.
        /// If none, returns an empty array (not null).
        /// </summary>
        public T[] GetNearby(Vector3 position, float maxDistance)
        {
            List<T> collidingWith = new List<T>();
            rootNode.GetNearby(ref position, maxDistance, collidingWith);
            return collidingWith.ToArray();
        }

        /// <summary>
        /// Returns objects that are within <paramref name="maxDistance"/> of the specified position.
        /// If none, returns false. Uses supplied list for results.
        /// </summary>
        public bool GetNearbyNonAlloc(Vector3 position, float maxDistance, List<T> nearBy)
        {
            nearBy.Clear();
            rootNode.GetNearby(ref position, maxDistance, nearBy);
            return nearBy.Count > 0;
        }

        /// <summary>
        /// Return all objects in the tree.
        /// If none, returns an empty array (not null).
        /// </summary>
        public ICollection<T> GetAll()
        {
            List<T> objects = new List<T>(Count);
            rootNode.GetAll(objects);
            return objects;
        }
        
        public bool ContainsAny(Vector3 point)
        {
            return rootNode.ContainsAny(point);
        }

        


        // #### PRIVATE METHODS ####

        /// <summary>
        /// Grow the octree to fit in all objects.
        /// </summary>
        private void Grow(Vector3 direction)
        {
            int xDirection = direction.X >= 0 ? 1 : -1;
            int yDirection = direction.Y >= 0 ? 1 : -1;
            int zDirection = direction.Z >= 0 ? 1 : -1;
            PointOctreeNode<T> oldRoot = rootNode;
            float half = rootNode.SideLength / 2;
            float newLength = rootNode.SideLength * 2;
            Vector3 newCenter = rootNode.Center + new Vector3(xDirection * half, yDirection * half, zDirection * half);

            rootNode = new PointOctreeNode<T>(newLength, minSize, newCenter);

            if (oldRoot.HasAnyObjects())
            {
                int rootPos = rootNode.BestFitChild(oldRoot.Center);
                PointOctreeNode<T>[] children = new PointOctreeNode<T>[8];
                for (int i = 0; i < 8; i++)
                {
                    if (i == rootPos)
                    {
                        children[i] = oldRoot;
                    }
                    else
                    {
                        int xi = (i % 2 == 0) ? -1 : 1;
                        int yi = (i > 3) ? -1 : 1;
                        int zi = (i < 2 || (i > 3 && i < 6)) ? -1 : 1;
                        Vector3 childCenter = newCenter + new Vector3(xi * half, yi * half, zi * half);
                        children[i] = new PointOctreeNode<T>(oldRoot.SideLength, minSize, childCenter);
                    }
                }

                rootNode.SetChildren(children);
            }
        }

        /// <summary>
        /// Shrink the octree if possible, else leave it the same.
        /// </summary>
        private void Shrink()
        {
            rootNode = rootNode.ShrinkIfPossible(initialSize);
        }
    }
}
