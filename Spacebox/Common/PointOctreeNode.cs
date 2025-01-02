using System;
using System.Collections.Generic;
using OpenTK.Mathematics;
using Spacebox.Common.Physics;


namespace Spacebox.Common
{
    public class PointOctreeNode<T>
    {
        // Centre of this node
        public Vector3 Center { get; private set; }

        // Length of the sides of this node
        public float SideLength { get; private set; }

        // Minimum size for a node in this octree
        private float minSize;

        // Bounding box that represents this node
        private BoundingBox bounds;

        // Objects in this node
        private readonly List<OctreeObject> objects = new List<OctreeObject>();

        // Child nodes, if any
        private PointOctreeNode<T>[] children = null;

        public bool HasChildren => children != null;

        // Bounds of potential children to this node
        private BoundingBox[] childBounds;

        // If there are already NUM_OBJECTS_ALLOWED in a node, we split it into children
        private const int NUM_OBJECTS_ALLOWED = 8;

        // For reverting the bounds size after temporary changes
        private Vector3 actualBoundsSize;

        // An object in the octree
        private class OctreeObject
        {
            public T Obj;
            public Vector3 Pos;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public PointOctreeNode(float baseLengthVal, float minSizeVal, Vector3 centerVal)
        {
            SetValues(baseLengthVal, minSizeVal, centerVal);
        }

        // #### PUBLIC METHODS ####

        /// <summary>
        /// Add an object.
        /// </summary>
        public bool Add(T obj, Vector3 objPos)
        {
            if (!Encapsulates(bounds, objPos))
            {
                return false;
            }
            SubAdd(obj, objPos);
            return true;
        }

        /// <summary>
        /// Remove an object. Makes the assumption that the object only exists once in the tree.
        /// </summary>
        public bool Remove(T obj)
        {
            bool removed = false;

            for (int i = 0; i < objects.Count; i++)
            {
                if (objects[i].Obj.Equals(obj))
                {
                    objects.RemoveAt(i);
                    removed = true;
                    break;
                }
            }

            if (!removed && children != null)
            {
                for (int i = 0; i < 8; i++)
                {
                    removed = children[i].Remove(obj);
                    if (removed) break;
                }
            }

            if (removed && children != null)
            {
                // Check if we should merge nodes now that we've removed an item
                if (ShouldMerge())
                {
                    Merge();
                }
            }

            return removed;
        }

        /// <summary>
        /// Removes the specified object at the given position. Makes the assumption that the object only exists once in the tree.
        /// </summary>
        public bool Remove(T obj, Vector3 objPos)
        {
            if (!Encapsulates(bounds, objPos))
            {
                return false;
            }
            return SubRemove(obj, objPos);
        }

        /// <summary>
        /// Return objects that are within maxDistance of the specified ray.
        /// </summary>
        public void GetNearby(ref Ray ray, float maxDistance, List<T> result)
        {
            // Does the ray hit this node at all?
            // Note: Expanding the bounds is not exactly the same as a real distance check, but it's fast.
            bounds.Size += new Vector3(maxDistance * 2);
            float distance;
            if (!ray.Intersects(bounds, out distance))
            {
                bounds.Size = actualBoundsSize;
                return;
            }
            bounds.Size = actualBoundsSize;

            // Check against any objects in this node
            for (int i = 0; i < objects.Count; i++)
            {
                if (SqrDistanceToRay(ray, objects[i].Pos) <= (maxDistance * maxDistance))
                {
                    result.Add(objects[i].Obj);
                }
            }

            // Check children
            if (children != null)
            {
                for (int i = 0; i < 8; i++)
                {
                    children[i].GetNearby(ref ray, maxDistance, result);
                }
            }
        }

        /// <summary>
        /// Return objects that are within <paramref name="maxDistance"/> of the specified position.
        /// </summary>
        public void GetNearby(ref Vector3 position, float maxDistance, List<T> result)
        {
            float sqrMaxDistance = maxDistance * maxDistance;

            // Does the node intersect with the sphere of center = position and radius = maxDistance?
            Vector3 closestPoint = Vector3.Clamp(position, bounds.Min, bounds.Max);
            if ((closestPoint - position).LengthSquared > sqrMaxDistance)
            {
                return;
            }

            // Check against any objects in this node
            for (int i = 0; i < objects.Count; i++)
            {
                if ((position - objects[i].Pos).LengthSquared <= sqrMaxDistance)
                {
                    result.Add(objects[i].Obj);
                }
            }

            // Check children
            if (children != null)
            {
                for (int i = 0; i < 8; i++)
                {
                    children[i].GetNearby(ref position, maxDistance, result);
                }
            }
        }

        /// <summary>
        /// Return all objects in the tree.
        /// </summary>
        public void GetAll(List<T> result)
        {
            foreach (var obj in objects)
            {
                result.Add(obj.Obj);
            }

            if (children != null)
            {
                for (int i = 0; i < 8; i++)
                {
                    children[i].GetAll(result);
                }
            }
        }
        
        public bool ContainsAny(Vector3 point)
        {
            if (!Encapsulates(bounds, point)) return false;
            for (int i = 0; i < objects.Count; i++)
            {
                if (objects[i].Pos == point) return true;
            }
            if (children != null)
            {
                return children[BestFitChild(point)].ContainsAny(point);
            }
            return false;
        }

        /// <summary>
        /// Set the 8 children of this octree.
        /// </summary>
        public void SetChildren(PointOctreeNode<T>[] childOctrees)
        {
            if (childOctrees.Length != 8)
            {
                Console.WriteLine("Child octree array must be length 8. Was length: " + childOctrees.Length);
                return;
            }

            children = childOctrees;
        }

        /// <summary>
        /// We can shrink the octree if:
        /// - This node is >= double minLength in length
        /// - All objects in the root node are within one octant
        /// - This node doesn't have children, or does but 7/8 children are empty
        /// We can also shrink it if there are no objects left at all!
        /// </summary>
        public PointOctreeNode<T> ShrinkIfPossible(float minLength)
        {
            if (SideLength < (2 * minLength))
            {
                return this;
            }
            if (objects.Count == 0 && !HasChildren)
            {
                return this;
            }

            // Check objects in root
            int bestFit = -1;
            for (int i = 0; i < objects.Count; i++)
            {
                OctreeObject curObj = objects[i];
                int newBestFit = BestFitChild(curObj.Pos);
                if (i == 0 || newBestFit == bestFit)
                {
                    bestFit = newBestFit;
                }
                else
                {
                    return this; // Can't reduce - objects fit in different octants
                }
            }

            // Check objects in children if there are any
            if (children != null)
            {
                bool childHadContent = false;
                for (int i = 0; i < children.Length; i++)
                {
                    if (children[i].HasAnyObjects())
                    {
                        if (childHadContent)
                        {
                            return this; // Can't shrink - another child had content already
                        }
                        if (bestFit >= 0 && bestFit != i)
                        {
                            return this; // Can't reduce - objects in root are in a different octant to objects in child
                        }
                        childHadContent = true;
                        bestFit = i;
                    }
                }
            }

            // Can reduce
            if (children == null)
            {
                SetValues(SideLength / 2, minSize, childBounds[bestFit].Center);
                return this;
            }

            // We have children. Use the appropriate child as the new root node
            return children[bestFit];
        }

        /// <summary>
        /// Find which child node this object would be most likely to fit in.
        /// </summary>
        public int BestFitChild(Vector3 objPos)
        {
            return (objPos.X <= Center.X ? 0 : 1) + (objPos.Y >= Center.Y ? 0 : 4) + (objPos.Z <= Center.Z ? 0 : 2);
        }

        /// <summary>
        /// Checks if this node or anything below it has something in it.
        /// </summary>
        public bool HasAnyObjects()
        {
            if (objects.Count > 0) return true;

            if (children != null)
            {
                for (int i = 0; i < 8; i++)
                {
                    if (children[i].HasAnyObjects()) return true;
                }
            }

            return false;
        }

        // #### PRIVATE METHODS ####

        /// <summary>
        /// Set values for this node. 
        /// </summary>
        private void SetValues(float baseLengthVal, float minSizeVal, Vector3 centerVal)
        {
            SideLength = baseLengthVal;
            minSize = minSizeVal;
            Center = centerVal;

            // Create the bounding box.
            actualBoundsSize = new Vector3(SideLength);
            bounds = new BoundingBox(Center, actualBoundsSize);

            float quarter = SideLength / 4f;
            float childActualLength = SideLength / 2;
            Vector3 childActualSize = new Vector3(childActualLength);
            childBounds = new BoundingBox[8];
            childBounds[0] = new BoundingBox(Center + new Vector3(-quarter, quarter, -quarter), childActualSize);
            childBounds[1] = new BoundingBox(Center + new Vector3(quarter, quarter, -quarter), childActualSize);
            childBounds[2] = new BoundingBox(Center + new Vector3(-quarter, quarter, quarter), childActualSize);
            childBounds[3] = new BoundingBox(Center + new Vector3(quarter, quarter, quarter), childActualSize);
            childBounds[4] = new BoundingBox(Center + new Vector3(-quarter, -quarter, -quarter), childActualSize);
            childBounds[5] = new BoundingBox(Center + new Vector3(quarter, -quarter, -quarter), childActualSize);
            childBounds[6] = new BoundingBox(Center + new Vector3(-quarter, -quarter, quarter), childActualSize);
            childBounds[7] = new BoundingBox(Center + new Vector3(quarter, -quarter, quarter), childActualSize);
        }

        /// <summary>
        /// Private counterpart to the public Add method.
        /// </summary>
        private void SubAdd(T obj, Vector3 objPos)
        {
            if (!HasChildren)
            {
                if (objects.Count < NUM_OBJECTS_ALLOWED || (SideLength / 2f) < minSize)
                {
                    OctreeObject newObj = new OctreeObject { Obj = obj, Pos = objPos };
                    objects.Add(newObj);
                    return;
                }

                Split();

                if (children == null)
                {
                    Console.WriteLine("Child creation failed for an unknown reason. Early exit.");
                    return;
                }

                for (int i = objects.Count - 1; i >= 0; i--)
                {
                    OctreeObject existingObj = objects[i];
                    int bestFitChild = BestFitChild(existingObj.Pos);
                    children[bestFitChild].SubAdd(existingObj.Obj, existingObj.Pos);
                    objects.RemoveAt(i);
                }
            }

            int bestFit = BestFitChild(objPos);
            children[bestFit].SubAdd(obj, objPos);
        }

        /// <summary>
        /// Private counterpart to the public Remove method.
        /// </summary>
        private bool SubRemove(T obj, Vector3 objPos)
        {
            bool removed = false;

            for (int i = 0; i < objects.Count; i++)
            {
                if (objects[i].Obj.Equals(obj))
                {
                    objects.RemoveAt(i);
                    removed = true;
                    break;
                }
            }

            if (!removed && children != null)
            {
                int bestFitChild = BestFitChild(objPos);
                removed = children[bestFitChild].SubRemove(obj, objPos);
            }

            if (removed && children != null)
            {
                if (ShouldMerge())
                {
                    Merge();
                }
            }

            return removed;
        }

        /// <summary>
        /// Splits the octree into eight children.
        /// </summary>
        private void Split()
        {
            float quarter = SideLength / 4f;
            float newLength = SideLength / 2;
            children = new PointOctreeNode<T>[8];
            children[0] = new PointOctreeNode<T>(newLength, minSize, Center + new Vector3(-quarter, quarter, -quarter));
            children[1] = new PointOctreeNode<T>(newLength, minSize, Center + new Vector3(quarter, quarter, -quarter));
            children[2] = new PointOctreeNode<T>(newLength, minSize, Center + new Vector3(-quarter, quarter, quarter));
            children[3] = new PointOctreeNode<T>(newLength, minSize, Center + new Vector3(quarter, quarter, quarter));
            children[4] = new PointOctreeNode<T>(newLength, minSize, Center + new Vector3(-quarter, -quarter, -quarter));
            children[5] = new PointOctreeNode<T>(newLength, minSize, Center + new Vector3(quarter, -quarter, -quarter));
            children[6] = new PointOctreeNode<T>(newLength, minSize, Center + new Vector3(-quarter, -quarter, quarter));
            children[7] = new PointOctreeNode<T>(newLength, minSize, Center + new Vector3(quarter, -quarter, quarter));
        }

        /// <summary>
        /// Merge all children into this node - the opposite of Split.
        /// </summary>
        private void Merge()
        {
            for (int i = 0; i < 8; i++)
            {
                PointOctreeNode<T> curChild = children[i];
                int numObjects = curChild.objects.Count;
                for (int j = numObjects - 1; j >= 0; j--)
                {
                    OctreeObject curObj = curChild.objects[j];
                    objects.Add(curObj);
                }
            }
            children = null;
        }

        /// <summary>
        /// Checks if outerBounds encapsulates the given point.
        /// </summary>
        private static bool Encapsulates(BoundingBox outerBounds, Vector3 point)
        {
            return outerBounds.Min.X <= point.X && outerBounds.Max.X >= point.X &&
                   outerBounds.Min.Y <= point.Y && outerBounds.Max.Y >= point.Y &&
                   outerBounds.Min.Z <= point.Z && outerBounds.Max.Z >= point.Z;
        }

        /// <summary>
        /// Checks if there are few enough objects in this node and its children that the children should all be merged into this.
        /// </summary>
        private bool ShouldMerge()
        {
            int totalObjects = objects.Count;
            if (children != null)
            {
                foreach (PointOctreeNode<T> child in children)
                {
                    if (child.children != null)
                    {
                        return false;
                    }
                    totalObjects += child.objects.Count;
                }
            }
            return totalObjects <= NUM_OBJECTS_ALLOWED;
        }

        /// <summary>
        /// Returns the closest distance to the given ray from a point.
        /// </summary>
        public static float SqrDistanceToRay(Ray ray, Vector3 point)
        {
            Vector3 cross = Vector3.Cross(ray.Direction, point - ray.Origin);
            return cross.LengthSquared / ray.Direction.LengthSquared;
        }
    }
}
