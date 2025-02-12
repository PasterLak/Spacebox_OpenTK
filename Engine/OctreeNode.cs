using OpenTK.Mathematics;
using Engine.Physics;


namespace Engine
{
    public class OctreeNode<T>
    {
        public Vector3 Center { get; private set; }
        public float BaseLength { get; private set; }

        private float looseness;
        private float minSize;
        private float adjLength;
        private BoundingBox bounds;

        private readonly List<OctreeObject> objects = new List<OctreeObject>();
        private OctreeNode<T>[] children = null;

        private bool HasChildren => children != null;

        private BoundingBox[] childBounds;

        private const byte NUM_OBJECTS_ALLOWED = 8;

        private struct OctreeObject
        {
            public T Obj;
            public BoundingBox Bounds;
        }

        public OctreeNode(float baseLengthVal, float minSizeVal, float loosenessVal, Vector3 centerVal)
        {
            SetValues(baseLengthVal, minSizeVal, loosenessVal, centerVal);
        }

        public bool Add(T obj, BoundingBox objBounds)
        {
            if (!Encapsulates(bounds, objBounds))
            {
                return false;
            }
            SubAdd(obj, objBounds);
            return true;
        }

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
                if (ShouldMerge())
                {
                    Merge();
                }
            }

            return removed;
        }

        public bool Remove(T obj, BoundingBox objBounds)
        {
            if (!Encapsulates(bounds, objBounds))
            {
                return false;
            }
            return SubRemove(obj, objBounds);
        }

        public bool IsColliding(ref BoundingBox checkBounds)
        {
            if (!bounds.Intersects(checkBounds))
            {
                return false;
            }

            for (int i = 0; i < objects.Count; i++)
            {
                if (objects[i].Bounds.Intersects(checkBounds))
                {
                    return true;
                }
            }

            if (children != null)
            {
                for (int i = 0; i < 8; i++)
                {
                    if (children[i].IsColliding(ref checkBounds))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public bool IsColliding(ref Vector3 worldPoint)
        {
            if (!bounds.Contains(worldPoint))
            {
                return false;
            }

            for (int i = 0; i < objects.Count; i++)
            {
                if (objects[i].Bounds.Contains(worldPoint))
                {
                    return true;
                }
            }

            if (children != null)
            {
                for (int i = 0; i < 8; i++)
                {
                    if (children[i].IsColliding(ref worldPoint))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public bool IsColliding(ref Ray checkRay, float maxDistance = float.PositiveInfinity)
        {
            float distance;
            if (!checkRay.Intersects(bounds, out distance) || distance > maxDistance)
            {
                return false;
            }

            for (int i = 0; i < objects.Count; i++)
            {
                if (checkRay.Intersects(objects[i].Bounds, out distance) && distance <= maxDistance)
                {
                    return true;
                }
            }

            if (children != null)
            {
                for (int i = 0; i < 8; i++)
                {
                    if (children[i].IsColliding(ref checkRay, maxDistance))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public void FindDataInBox(ref BoundingBox checkBounds, HashSet<T> result)
        {
            if (!bounds.Intersects(checkBounds))
            {
                return;
            }

            for (int i = 0; i < objects.Count; i++)
            {
                if (objects[i].Bounds.Intersects(checkBounds))
                {
                    result.Add(objects[i].Obj);
                }
            }

            if (children != null)
            {
                for (int i = 0; i < 8; i++)
                {
                    children[i].FindDataInBox(ref checkBounds, result);
                }
            }
        }

        public void FindDataInRadius(Vector3 center, float radius, HashSet<T> result)
        {
            BoundingSphere searchBounds = new BoundingSphere(center, radius );

            if (!bounds.Intersects(searchBounds))
            {
                return;
            }

            float sqrRadius = radius * radius;

            for (int i = 0; i < objects.Count; i++)
            {
                if ((objects[i].Bounds.Center - center).LengthSquared <= sqrRadius)
                {
                    result.Add(objects[i].Obj);
                }
            }

            if (children != null)
            {
                for (int i = 0; i < 8; i++)
                {
                    children[i].FindDataInRadius(center, radius, result);
                }
            }
        }

        public bool TryFindDataAtPosition(Vector3 worldPosition, out T data)
        {
            data = default(T);

            if (!bounds.Contains(worldPosition))
            {
                return false;
            }

            for (int i = 0; i < objects.Count; i++)
            {
                if (objects[i].Bounds.Contains(worldPosition))
                {
                    data = objects[i].Obj;
                    return true;
                }
            }

            if (children != null)
            {
                for (int i = 0; i < 8; i++)
                {
                    if (children[i].TryFindDataAtPosition(worldPosition, out data))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public void GetColliding(ref BoundingBox checkBounds, List<T> result)
        {
            if (!bounds.Intersects(checkBounds))
            {
                return;
            }

            for (int i = 0; i < objects.Count; i++)
            {
                if (objects[i].Bounds.Intersects(checkBounds))
                {
                    result.Add(objects[i].Obj);
                }
            }

            if (children != null)
            {
                for (int i = 0; i < 8; i++)
                {
                    children[i].GetColliding(ref checkBounds, result);
                }
            }
        }

        public void GetColliding(ref Ray checkRay, List<T> result, float maxDistance = float.PositiveInfinity)
        {
            float distance;
            if (!checkRay.Intersects(bounds, out distance) || distance > maxDistance)
            {
                return;
            }

            for (int i = 0; i < objects.Count; i++)
            {
                if (checkRay.Intersects(objects[i].Bounds, out distance) && distance <= maxDistance)
                {
                    result.Add(objects[i].Obj);
                }
            }

            if (children != null)
            {
                for (int i = 0; i < 8; i++)
                {
                    children[i].GetColliding(ref checkRay, result, maxDistance);
                }
            }
        }

        public void SetChildren(OctreeNode<T>[] childOctrees)
        {
            if (childOctrees.Length != 8)
            {
                Console.WriteLine("Child octree array must be length 8.");
                return;
            }

            children = childOctrees;
        }

        public BoundingBox GetBounds()
        {
            return bounds;
        }

        public OctreeNode<T> ShrinkIfPossible(float minLength)
        {
            if (BaseLength < (2 * minLength))
            {
                return this;
            }
            if (objects.Count == 0 && !HasChildren)
            {
                return this;
            }

            int bestFit = -1;
            for (int i = 0; i < objects.Count; i++)
            {
                OctreeObject curObj = objects[i];
                int newBestFit = BestFitChild(curObj.Bounds.Center);
                if (i == 0 || newBestFit == bestFit)
                {
                    if (Encapsulates(childBounds[newBestFit], curObj.Bounds))
                    {
                        bestFit = newBestFit;
                    }
                    else
                    {
                        return this;
                    }
                }
                else
                {
                    return this;
                }
            }

            if (children != null)
            {
                bool childHadContent = false;
                for (int i = 0; i < children.Length; i++)
                {
                    if (children[i].HasAnyObjects())
                    {
                        if (childHadContent)
                        {
                            return this;
                        }
                        if (bestFit >= 0 && bestFit != i)
                        {
                            return this;
                        }
                        childHadContent = true;
                        bestFit = i;
                    }
                }
            }

            if (children == null)
            {
                SetValues(BaseLength / 2, minSize, looseness, childBounds[bestFit].Center);
                return this;
            }

            return children[bestFit];
        }

        public int BestFitChild(Vector3 objBoundsCenter)
        {
            return (objBoundsCenter.X <= Center.X ? 0 : 1) +
                   (objBoundsCenter.Y >= Center.Y ? 0 : 4) +
                   (objBoundsCenter.Z <= Center.Z ? 0 : 2);
        }

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

        private void SetValues(float baseLengthVal, float minSizeVal, float loosenessVal, Vector3 centerVal)
        {
            BaseLength = baseLengthVal;
            minSize = minSizeVal;
            looseness = loosenessVal;
            Center = centerVal;
            adjLength = looseness * baseLengthVal;

            Vector3 size = new Vector3(adjLength, adjLength, adjLength);
            bounds = new BoundingBox(Center, size);

            float quarter = BaseLength / 4f;
            float childActualLength = (BaseLength / 2f) * looseness;
            Vector3 childActualSize = new Vector3(childActualLength, childActualLength, childActualLength);
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

        private void SubAdd(T obj, BoundingBox objBounds)
        {
            if (!HasChildren)
            {
                if (objects.Count < NUM_OBJECTS_ALLOWED || (BaseLength / 2f) < minSize)
                {
                    OctreeObject newObj = new OctreeObject { Obj = obj, Bounds = objBounds };
                    objects.Add(newObj);
                    return;
                }

                Split();

                if (children == null)
                {
                    Console.WriteLine("Failed to create child nodes for an unknown reason.");
                    return;
                }

                for (int i = objects.Count - 1; i >= 0; i--)
                {
                    OctreeObject existingObj = objects[i];
                    int bestFitChild = BestFitChild(existingObj.Bounds.Center);
                    if (Encapsulates(children[bestFitChild].bounds, existingObj.Bounds))
                    {
                        children[bestFitChild].SubAdd(existingObj.Obj, existingObj.Bounds);
                        objects.RemoveAt(i);
                    }
                }
            }

            int bestFit = BestFitChild(objBounds.Center);
            if (Encapsulates(children[bestFit].bounds, objBounds))
            {
                children[bestFit].SubAdd(obj, objBounds);
            }
            else
            {
                OctreeObject newObj = new OctreeObject { Obj = obj, Bounds = objBounds };
                objects.Add(newObj);
            }
        }

        private bool SubRemove(T obj, BoundingBox objBounds)
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
                int bestFitChild = BestFitChild(objBounds.Center);
                removed = children[bestFitChild].SubRemove(obj, objBounds);
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

        private void Split()
        {
            float quarter = BaseLength / 4f;
            float newLength = BaseLength / 2f;
            children = new OctreeNode<T>[8];
            children[0] = new OctreeNode<T>(newLength, minSize, looseness, Center + new Vector3(-quarter, quarter, -quarter));
            children[1] = new OctreeNode<T>(newLength, minSize, looseness, Center + new Vector3(quarter, quarter, -quarter));
            children[2] = new OctreeNode<T>(newLength, minSize, looseness, Center + new Vector3(-quarter, quarter, quarter));
            children[3] = new OctreeNode<T>(newLength, minSize, looseness, Center + new Vector3(quarter, quarter, quarter));
            children[4] = new OctreeNode<T>(newLength, minSize, looseness, Center + new Vector3(-quarter, -quarter, -quarter));
            children[5] = new OctreeNode<T>(newLength, minSize, looseness, Center + new Vector3(quarter, -quarter, -quarter));
            children[6] = new OctreeNode<T>(newLength, minSize, looseness, Center + new Vector3(-quarter, -quarter, quarter));
            children[7] = new OctreeNode<T>(newLength, minSize, looseness, Center + new Vector3(quarter, -quarter, quarter));
        }

        private void Merge()
        {
            for (int i = 0; i < 8; i++)
            {
                OctreeNode<T> curChild = children[i];
                for (int j = curChild.objects.Count - 1; j >= 0; j--)
                {
                    OctreeObject curObj = curChild.objects[j];
                    objects.Add(curObj);
                }
            }
            children = null;
        }

        private static bool Encapsulates(BoundingBox outerBounds, BoundingBox innerBounds)
        {
            return outerBounds.Min.X <= innerBounds.Min.X && outerBounds.Max.X >= innerBounds.Max.X &&
                   outerBounds.Min.Y <= innerBounds.Min.Y && outerBounds.Max.Y >= innerBounds.Max.Y &&
                   outerBounds.Min.Z <= innerBounds.Min.Z && outerBounds.Max.Z >= innerBounds.Max.Z;
        }

        private bool ShouldMerge()
        {
            int totalObjects = objects.Count;
            if (children != null)
            {
                foreach (OctreeNode<T> child in children)
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

        public void DrawDebug()
        {

            if (HasChildren)
            {
                foreach (var child in children)
                {
                    child.DrawDebug();
                }
            }
            else
            {
                VisualDebug.DrawBoundingBox(bounds, new Color4(255, 192, 200, 50));

                if(objects.Count > 0)
                {
                    foreach (var obj in objects)
                    {
                        //VisualDebug.DrawBoundingBox(obj.Bounds, new Color4(20, 192, 70, 80));
                    }
                }
            }

            
        }

    }

    
}
