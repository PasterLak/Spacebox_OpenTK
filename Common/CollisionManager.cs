//public List<ICollidable> Collidables = new List<ICollidable>();


using OpenTK.Mathematics;

namespace Spacebox.Common
{
    public class CollisionManager
    {
        private readonly float _cellSize;
        private readonly Dictionary<(int, int, int), List<Collision>> _spatialHash;
        private HashSet<(Collision, Collision)> _activeCollisions;
        public List<Collision> Collidables = new List<Collision>();
        public CollisionManager(float cellSize = 10.0f)
        {
            _cellSize = cellSize;
            _spatialHash = new Dictionary<(int, int, int), List<Collision>>();
            _activeCollisions = new HashSet<(Collision, Collision)>(new CollisionPairComparer());
        }

        public void Add(Collision obj)
        {
            if (obj is Trigger)
            {

            }
            else
            {
                obj.SetCollisionDebugColor(Color4.Yellow);
            }
            

            if (!Collidables.Contains(obj))
            {
                Collidables.Add(obj);
                obj.CollisionManager = this;
            }

            foreach (var cell in GetOccupiedCells(obj.BoundingVolume))
            {
                if (!_spatialHash.TryGetValue(cell, out var list))
                {
                    list = new List<Collision>();
                    _spatialHash[cell] = list;
                }
                list.Add(obj);
               
            }
        }

        public void Remove(Collision obj)
        {
            obj.SetCollisionDebugColor(Color4.Gray);

            foreach (var cell in GetOccupiedCells(obj.BoundingVolume))
            {
                if (_spatialHash.TryGetValue(cell, out var list))
                {
                    list.Remove(obj);
                    if (list.Count == 0)
                    {
                        _spatialHash.Remove(cell);
                    }
                }
            }

            var toRemove = new List<(Collision, Collision)>();
            foreach (var pair in _activeCollisions)
            {
                if (pair.Item1 == obj || pair.Item2 == obj)
                {
                    pair.Item1.OnCollisionExit(pair.Item2);
                    pair.Item2.OnCollisionExit(pair.Item1);
                    toRemove.Add(pair);
                }
            }
            foreach (var pair in toRemove)
            {
                _activeCollisions.Remove(pair);
            }

            Collidables.Remove(obj);
        }

        public void Update(Collision obj, BoundingVolume oldVolume)
        {
            var oldCells = GetOccupiedCells(oldVolume);
            var newCells = GetOccupiedCells(obj.BoundingVolume);

            var cellsToRemove = new HashSet<(int, int, int)>(oldCells);
            cellsToRemove.ExceptWith(newCells);

            var cellsToAdd = new HashSet<(int, int, int)>(newCells);
            cellsToAdd.ExceptWith(oldCells);

            foreach (var cell in cellsToRemove)
            {
                if (_spatialHash.TryGetValue(cell, out var list))
                {
                    list.Remove(obj);
                    if (list.Count == 0)
                    {
                        _spatialHash.Remove(cell);
                    }
                }
            }

            foreach (var cell in cellsToAdd)
            {
                if (!_spatialHash.TryGetValue(cell, out var list))
                {
                    list = new List<Collision>();
                    _spatialHash[cell] = list;
                }
                list.Add(obj);
            }
        }

        public void CheckCollisions()
        {
            var currentCollisions = new HashSet<(Collision, Collision)>(new CollisionPairComparer());

            foreach (var cell in _spatialHash.Values)
            {
                for (int i = 0; i < cell.Count; i++)
                {
                    for (int j = i + 1; j < cell.Count; j++)
                    {
                        var a = cell[i];
                        var b = cell[j];
                        if (a == b)
                            continue;

                        var pair = (a, b);
                        if (a.GetHashCode() > b.GetHashCode())
                        {
                            pair = (b, a);
                        }

                        if (a.BoundingVolume.Intersects(b.BoundingVolume))
                        {
                            currentCollisions.Add(pair);
                            if (!_activeCollisions.Contains(pair))
                            {
                                a.OnCollisionEnter(b);
                                b.OnCollisionEnter(a);
                                _activeCollisions.Add(pair);
                            }
                        }
                    }
                }
            }

            var collisionsToRemove = new List<(Collision, Collision)>();
            foreach (var pair in _activeCollisions)
            {
                if (!currentCollisions.Contains(pair))
                {
                    pair.Item1.OnCollisionExit(pair.Item2);
                    pair.Item2.OnCollisionExit(pair.Item1);
                    collisionsToRemove.Add(pair);
                }
            }

            foreach (var pair in collisionsToRemove)
            {
                _activeCollisions.Remove(pair);
            }

            foreach (var pair in currentCollisions)
            {
                _activeCollisions.Add(pair);
            }
        }

        private IEnumerable<(int, int, int)> GetOccupiedCells(BoundingVolume volume)
        {
            if (volume is BoundingBox box)
            {
                Vector3 min = box.Min;
                Vector3 max = box.Max;

                int xMin = (int)Math.Floor(min.X / _cellSize);
                int yMin = (int)Math.Floor(min.Y / _cellSize);
                int zMin = (int)Math.Floor(min.Z / _cellSize);

                int xMax = (int)Math.Floor(max.X / _cellSize);
                int yMax = (int)Math.Floor(max.Y / _cellSize);
                int zMax = (int)Math.Floor(max.Z / _cellSize);

                for (int x = xMin; x <= xMax; x++)
                {
                    for (int y = yMin; y <= yMax; y++)
                    {
                        for (int z = zMin; z <= zMax; z++)
                        {
                            yield return (x, y, z);
                        }
                    }
                }
            }
            else if (volume is BoundingSphere sphere)
            {
                Vector3 min = sphere.Center - new Vector3(sphere.Radius);
                Vector3 max = sphere.Center + new Vector3(sphere.Radius);

                int xMin = (int)Math.Floor(min.X / _cellSize);
                int yMin = (int)Math.Floor(min.Y / _cellSize);
                int zMin = (int)Math.Floor(min.Z / _cellSize);

                int xMax = (int)Math.Floor(max.X / _cellSize);
                int yMax = (int)Math.Floor(max.Y / _cellSize);
                int zMax = (int)Math.Floor(max.Z / _cellSize);

                for (int x = xMin; x <= xMax; x++)
                {
                    for (int y = yMin; y <= yMax; y++)
                    {
                        for (int z = zMin; z <= zMax; z++)
                        {
                            yield return (x, y, z);
                        }
                    }
                }
            }
            else
            {
                throw new NotSupportedException("Unsupported BoundingVolume type.");
            }
        }

        public bool IsColliding(BoundingVolume volume, Collision exclude = null)
        {
            foreach (var cell in GetOccupiedCells(volume))
            {
                if (_spatialHash.TryGetValue(cell, out var list))
                {
                    foreach (var obj in list)
                    {
                        if (obj.IsTrigger) continue;
                        if (obj == exclude)
                            continue;

                        if (volume.Intersects(obj.BoundingVolume))
                            return true;
                    }
                }
            }
            return false;
        }
        public BoundingVolume GetBoundingVolume(Collision obj)
        {
            return obj.BoundingVolume.Clone();
        }


        /// <summary>
        /// Проверяет пересечение луча с объектами сцены с учётом слоёв.
        /// </summary>
        /// <param name="ray">Луч, который нужно проверить.</param>
        /// <param name="hitPosition">Позиция точки пересечения, если есть пересечение.</param>
        /// <param name="hitObject">Объект, с которым произошло пересечение.</param>
        /// <param name="layerMask">Маска слоёв для фильтрации объектов. По умолчанию все слои.</param>
        /// <returns>True, если произошло пересечение; иначе False.</returns>
        public bool Raycast(Ray ray, out Vector3 hitPosition, out Collision hitObject, CollisionLayer layerMask = CollisionLayer.All)
        {
            hitPosition = Vector3.Zero;
            hitObject = null;
            float closestDistance = float.MaxValue;

            // Определяем ячейки, через которые проходит луч
            foreach (var cell in GetRayOccupiedCells(ray))
            {
                if (_spatialHash.TryGetValue(cell, out var list))
                {
                    foreach (var obj in list)
                    {
                        if (obj.IsTrigger) continue; // Пропускаем триггеры, если необходимо

                        // Проверяем, входит ли объект в маску слоёв
                        if (!layerMask.HasFlag(obj.Layer))
                            continue;

                        // Проверяем пересечение луча с ограничивающим объемом объекта
                        bool intersects = false;
                        float distance = 0f;

                        if (obj.BoundingVolume is BoundingSphere sphere)
                        {
                            intersects = ray.Intersects(sphere, out distance);
                        }
                        else if (obj.BoundingVolume is BoundingBox box)
                        {
                            intersects = ray.Intersects(box, out distance);
                        }

                        if (intersects)
                        {
                            if (distance < closestDistance && distance >= 0 && distance <= ray.Length)
                            {
                                closestDistance = distance;
                                hitPosition = ray.Origin + ray.Direction * distance;
                                hitObject = obj;
                            }
                        }
                    }
                }

                // Если нашли очень близкое пересечение, можно прервать раннее
                if (closestDistance < _cellSize)
                    break;

                // Прекращаем, если достигли максимальной длины луча
                if (closestDistance >= ray.Length)
                    break;
            }

            return hitObject != null;
        }


        private bool IntersectRayWithBoundingVolume(Ray ray, BoundingVolume volume, out float distance)
        {
            distance = 0f;
            if (volume is BoundingBox box)
            {
                return IntersectRayWithBoundingBox(ray, box, out distance);
            }
            else if (volume is BoundingSphere sphere)
            {
                return ray.Intersects(sphere, out distance);
            }
            else
            {
                throw new NotSupportedException("Unsupported BoundingVolume type.");
            }
        }

        private bool IntersectRayWithBoundingBox(Ray ray, BoundingBox box, out float distance)
        {
            distance = 0f;
            float tMin = (box.Min.X - ray.Origin.X) / ray.Direction.X;
            float tMax = (box.Max.X - ray.Origin.X) / ray.Direction.X;

            if (tMin > tMax)
            {
                float temp = tMin;
                tMin = tMax;
                tMax = temp;
            }

            float tyMin = (box.Min.Y - ray.Origin.Y) / ray.Direction.Y;
            float tyMax = (box.Max.Y - ray.Origin.Y) / ray.Direction.Y;

            if (tyMin > tyMax)
            {
                float temp = tyMin;
                tyMin = tyMax;
                tyMax = temp;
            }

            if ((tMin > tyMax) || (tyMin > tMax))
                return false;

            if (tyMin > tMin)
                tMin = tyMin;

            if (tyMax < tMax)
                tMax = tyMax;

            float tzMin = (box.Min.Z - ray.Origin.Z) / ray.Direction.Z;
            float tzMax = (box.Max.Z - ray.Origin.Z) / ray.Direction.Z;

            if (tzMin > tzMax)
            {
                float temp = tzMin;
                tzMin = tzMax;
                tzMax = temp;
            }

            if ((tMin > tzMax) || (tzMin > tMax))
                return false;

            if (tzMin > tMin)
                tMin = tzMin;

            if (tzMax < tMax)
                tMax = tzMax;

            distance = tMin;

            if (distance < 0)
            {
                distance = tMax;
                if (distance < 0)
                    return false;
            }

            return true;
        }


        private IEnumerable<(int, int, int)> GetRayOccupiedCells(Ray ray)
        {
            Vector3 origin = ray.Origin;
            Vector3 direction = ray.Direction;

            // Преобразуем координаты в индексы ячеек
            int x = (int)Math.Floor(origin.X / _cellSize);
            int y = (int)Math.Floor(origin.Y / _cellSize);
            int z = (int)Math.Floor(origin.Z / _cellSize);

            int stepX = direction.X > 0 ? 1 : (direction.X < 0 ? -1 : 0);
            int stepY = direction.Y > 0 ? 1 : (direction.Y < 0 ? -1 : 0);
            int stepZ = direction.Z > 0 ? 1 : (direction.Z < 0 ? -1 : 0);

            float tMaxX = stepX != 0 ? ((_cellSize * (x + (stepX > 0 ? 1 : 0))) - origin.X) / direction.X : float.MaxValue;
            float tMaxY = stepY != 0 ? ((_cellSize * (y + (stepY > 0 ? 1 : 0))) - origin.Y) / direction.Y : float.MaxValue;
            float tMaxZ = stepZ != 0 ? ((_cellSize * (z + (stepZ > 0 ? 1 : 0))) - origin.Z) / direction.Z : float.MaxValue;

            float tDeltaX = stepX != 0 ? _cellSize / Math.Abs(direction.X) : float.MaxValue;
            float tDeltaY = stepY != 0 ? _cellSize / Math.Abs(direction.Y) : float.MaxValue;
            float tDeltaZ = stepZ != 0 ? _cellSize / Math.Abs(direction.Z) : float.MaxValue;

            // Ограничение по максимальному количеству ячеек или расстоянию
            int maxIterations = 1000;
            while (maxIterations-- > 0)
            {
                yield return (x, y, z);

                if (tMaxX < tMaxY)
                {
                    if (tMaxX < tMaxZ)
                    {
                        x += stepX;
                        tMaxX += tDeltaX;
                    }
                    else
                    {
                        z += stepZ;
                        tMaxZ += tDeltaZ;
                    }
                }
                else
                {
                    if (tMaxY < tMaxZ)
                    {
                        y += stepY;
                        tMaxY += tDeltaY;
                    }
                    else
                    {
                        z += stepZ;
                        tMaxZ += tDeltaZ;
                    }
                }

                // Прекращаем, если достигли определённого расстояния
                float currentDistance = Math.Min(tMaxX, Math.Min(tMaxY, tMaxZ));
                if (currentDistance > 1000) // Например, максимальное расстояние луча
                    break;
            }
        }



        private class CollisionPairComparer : IEqualityComparer<(Collision, Collision)>
        {
            public bool Equals((Collision, Collision) x, (Collision, Collision) y)
            {
                return (x.Item1 == y.Item1 && x.Item2 == y.Item2) ||
                       (x.Item1 == y.Item2 && x.Item2 == y.Item1);
            }

            public int GetHashCode((Collision, Collision) obj)
            {
                int hash1 = obj.Item1.GetHashCode();
                int hash2 = obj.Item2.GetHashCode();
                return hash1 ^ hash2;
            }
        }
    }
}
