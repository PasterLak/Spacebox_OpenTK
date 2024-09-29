//public List<ICollidable> Collidables = new List<ICollidable>();


using OpenTK.Mathematics;
using System;
using System.Collections.Generic;

namespace Spacebox.Common
{
    public class CollisionManager
    {
        private readonly float _cellSize;
        private readonly Dictionary<(int, int, int), List<ICollidable>> _spatialHash;
        private HashSet<(ICollidable, ICollidable)> _activeCollisions;
        public List<ICollidable> Collidables = new List<ICollidable>();
        public CollisionManager(float cellSize = 10.0f)
        {
            _cellSize = cellSize;
            _spatialHash = new Dictionary<(int, int, int), List<ICollidable>>();
            _activeCollisions = new HashSet<(ICollidable, ICollidable)>(new CollisionPairComparer());
        }

        public void Add(ICollidable obj)
        {
            foreach (var cell in GetOccupiedCells(obj.BoundingVolume))
            {
                if (!_spatialHash.TryGetValue(cell, out var list))
                {
                    list = new List<ICollidable>();
                    _spatialHash[cell] = list;
                }
                Collidables.Add(obj);
                list.Add(obj);
            }
        }

        public void Remove(ICollidable obj)
        {
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

            var toRemove = new List<(ICollidable, ICollidable)>();
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
        }

        public void Update(ICollidable obj, BoundingVolume oldVolume)
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
                    list = new List<ICollidable>();
                    _spatialHash[cell] = list;
                }
                list.Add(obj);
            }
        }

        public void CheckCollisions()
        {
            var currentCollisions = new HashSet<(ICollidable, ICollidable)>(new CollisionPairComparer());

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

            var collisionsToRemove = new List<(ICollidable, ICollidable)>();
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

        private class CollisionPairComparer : IEqualityComparer<(ICollidable, ICollidable)>
        {
            public bool Equals((ICollidable, ICollidable) x, (ICollidable, ICollidable) y)
            {
                return (x.Item1 == y.Item1 && x.Item2 == y.Item2) ||
                       (x.Item1 == y.Item2 && x.Item2 == y.Item1);
            }

            public int GetHashCode((ICollidable, ICollidable) obj)
            {
                int hash1 = obj.Item1.GetHashCode();
                int hash2 = obj.Item2.GetHashCode();
                return hash1 ^ hash2;
            }
        }
    }
}
