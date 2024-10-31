using System;
using System.Collections.Generic;
using OpenTK.Mathematics;
using Spacebox.Common;

namespace Spacebox.Common
{
    public class Octree<T>
    {
        public int Count { get; private set; }

        private OctreeNode<T> rootNode;

        private readonly float looseness;
        private readonly float initialSize;
        private readonly float minSize;

        public Octree(float initialWorldSize, Vector3 initialWorldPos, float minNodeSize, float loosenessVal)
        {
            if (minNodeSize > initialWorldSize)
            {
                Console.WriteLine("Minimum node size must be at least as big as the initial world size. Adjusted to: " + initialWorldSize);
                minSize = initialWorldSize;
            }
            else
            {
                minSize = minNodeSize;
            }

            Count = 0;
            initialSize = initialWorldSize;
            looseness = MathHelper.Clamp(loosenessVal, 1.0f, 2.0f);
            rootNode = new OctreeNode<T>(initialSize, minSize, looseness, initialWorldPos);
        }

        public void Add(T obj, BoundingBox objBounds)
        {
            int count = 0;
            while (!rootNode.Add(obj, objBounds))
            {
                Grow(objBounds.Center - rootNode.Center);
                if (++count > 20)
                {
                    Console.WriteLine("Aborted Add operation after too many attempts at growing the octree.");
                    return;
                }
            }
            Count++;
        }

        public bool Remove(T obj)
        {
            bool removed = rootNode.Remove(obj);
            if (removed)
            {
                Count--;
                Shrink();
            }
            return removed;
        }

        public bool Remove(T obj, BoundingBox objBounds)
        {
            bool removed = rootNode.Remove(obj, objBounds);
            if (removed)
            {
                Count--;
                Shrink();
            }
            return removed;
        }

        public void FindDataInBox(ref BoundingBox checkBounds, HashSet<T> result)
        {
            rootNode.FindDataInBox(ref checkBounds, result);
        }

        public void FindDataInRadius(Vector3 center, float radius, HashSet<T> result)
        {
            rootNode.FindDataInRadius(center, radius, result);
        }

        public bool TryFindDataAtPosition(Vector3 worldPosition, out T data)
        {
            return rootNode.TryFindDataAtPosition(worldPosition, out data);
        }

        public bool IsColliding(BoundingBox checkBounds)
        {
            return rootNode.IsColliding(ref checkBounds);
        }

        public bool IsColliding(Vector3 worldPoint)
        {
            return rootNode.IsColliding(ref worldPoint);
        }

        public bool IsColliding(Ray checkRay, float maxDistance = float.PositiveInfinity)
        {
            return rootNode.IsColliding(ref checkRay, maxDistance);
        }

        public void GetColliding(List<T> collidingWith, BoundingBox checkBounds)
        {
            rootNode.GetColliding(ref checkBounds, collidingWith);
        }

        public void GetColliding(List<T> collidingWith, Ray checkRay, float maxDistance = float.PositiveInfinity)
        {
            rootNode.GetColliding(ref checkRay, collidingWith, maxDistance);
        }

        public BoundingBox GetMaxBounds()
        {
            return rootNode.GetBounds();
        }

        private void Grow(Vector3 direction)
        {
            int xDirection = direction.X >= 0 ? 1 : -1;
            int yDirection = direction.Y >= 0 ? 1 : -1;
            int zDirection = direction.Z >= 0 ? 1 : -1;
            OctreeNode<T> oldRoot = rootNode;
            float half = rootNode.BaseLength / 2;
            float newLength = rootNode.BaseLength * 2;
            Vector3 newCenter = rootNode.Center + new Vector3(xDirection * half, yDirection * half, zDirection * half);

            rootNode = new OctreeNode<T>(newLength, minSize, looseness, newCenter);

            if (oldRoot.HasAnyObjects())
            {
                int rootPos = rootNode.BestFitChild(oldRoot.Center);
                OctreeNode<T>[] children = new OctreeNode<T>[8];
                for (int i = 0; i < 8; i++)
                {
                    if (i == rootPos)
                    {
                        children[i] = oldRoot;
                    }
                    else
                    {
                        int xi = i % 2;
                        int yi = (i / 2) % 2;
                        int zi = (i / 4) % 2;
                        Vector3 childCenter = newCenter;
                        childCenter.X += (xi == 0 ? -half : half);
                        childCenter.Y += (yi == 0 ? -half : half);
                        childCenter.Z += (zi == 0 ? -half : half);
                        children[i] = new OctreeNode<T>(oldRoot.BaseLength, minSize, looseness, childCenter);
                    }
                }
                rootNode.SetChildren(children);
            }
        }

        public void Shift(Vector3 shiftAmount)
        {
            rootNode.Shift(shiftAmount);
        }


        private void Shrink()
        {
            rootNode = rootNode.ShrinkIfPossible(initialSize);
        }
    }
}
