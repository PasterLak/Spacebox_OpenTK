using OpenTK.Mathematics;
using Spacebox.Common;

namespace Spacebox.Game
{
    public class World
    {
        public static Random Random;
        private Octree<Sector> sectorOctree;
        private Dictionary<Vector3i, Sector> sectorsByIndex;
        public Astronaut Player { get; private set; }
        private const float SectorSize = 20f; 
        private const float HalfSectorSize = SectorSize / 2f;
        private float playerVisibilityDistance = 50f;

        private const float ShiftThreshold = 40;

        private WorldData worldData;

        public World(Astronaut player)
        {
            Random = new Random(LoadSeed());

            Player = player;
            sectorOctree = new Octree<Sector>(SectorSize * 8, Vector3.Zero, SectorSize, 1.0f);
            sectorsByIndex = new Dictionary<Vector3i, Sector>();
            //InitializeSectors();
            AddSector(new Vector3i(0,0,0));

        }

        private int LoadSeed()
        {
            return 481516234;
        }

        private void InitializeSectors()
        {
            UpdateSectors();
        }

        public void Update()
        {
            Vector3 playerPosition = Player.Position;

            // Shift world if player is too far from origin
            if (playerPosition.Length >= ShiftThreshold)
            {
                ShiftWorld(playerPosition);
            }

            //UpdateSectors();

            //sectorOctree.

            foreach (var sector in GetSectorsInRange(Player.Position, 1000))
            {
                sector.Update();
            }

            foreach (var sector in GetSectorsInRange(Player.Position, playerVisibilityDistance))
            {
                sector.Update();
            }
        }

        public void Render(Shader shader)
        {
            foreach (var sector in GetSectorsInRange(Player.Position, playerVisibilityDistance))
            {
                sector.Render(shader);
            }
        }

        private void UpdateSectors()
        {
            Vector3 playerPosition = Player.Position;

            Vector3i currentSectorIndex = GetSectorIndex(playerPosition);

            int visibilityRadiusInSectors = (int)(playerVisibilityDistance / SectorSize) + 1;

            for (int x = currentSectorIndex.X - visibilityRadiusInSectors; x <= currentSectorIndex.X + visibilityRadiusInSectors; x++)
            {
                for (int y = currentSectorIndex.Y - visibilityRadiusInSectors; y <= currentSectorIndex.Y + visibilityRadiusInSectors; y++)
                {
                    for (int z = currentSectorIndex.Z - visibilityRadiusInSectors; z <= currentSectorIndex.Z + visibilityRadiusInSectors; z++)
                    {
                        Vector3i sectorIndex = new Vector3i(x, y, z);

                        if (!SectorExists(sectorIndex))
                        {
                            Vector3 sectorPosition = GetSectorPosition(sectorIndex);

                            BoundingBox sectorBounds = new BoundingBox(
                                sectorPosition - new Vector3(HalfSectorSize),
                                sectorPosition + new Vector3(HalfSectorSize)
                            );

                            float distanceToSector = GetDistanceToSectorBounds(sectorBounds, playerPosition);

                            if (distanceToSector <= playerVisibilityDistance)
                            {
                                AddSector(sectorIndex);
                            }
                        }
                    }
                }
            }

            RemoveFarSectors(playerPosition, playerVisibilityDistance);
        }

        private void RemoveFarSectors(Vector3 playerPosition, float maxDistance)
        {
            var sectorsToRemove = new List<Vector3i>();
            foreach (var kvp in sectorsByIndex)
            {
                Sector sector = kvp.Value;

                BoundingBox sectorBounds = new BoundingBox(
                    sector.Position - new Vector3(HalfSectorSize),
                    sector.Position + new Vector3(HalfSectorSize)
                );

                float distanceToSector = GetDistanceToSectorBounds(sectorBounds, playerPosition);

                if (distanceToSector > maxDistance)
                {
                    sectorsToRemove.Add(kvp.Key);
                }
            }

            foreach (var index in sectorsToRemove)
            {
                RemoveSector(index);
            }
        }

        private float GetDistanceToSectorBounds(BoundingBox sectorBounds, Vector3 position)
        {
            float sqrDistance = 0f;

            // X axis
            if (position.X < sectorBounds.Min.X)
            {
                float diff = sectorBounds.Min.X - position.X;
                sqrDistance += diff * diff;
            }
            else if (position.X > sectorBounds.Max.X)
            {
                float diff = position.X - sectorBounds.Max.X;
                sqrDistance += diff * diff;
            }

            // Y axis
            if (position.Y < sectorBounds.Min.Y)
            {
                float diff = sectorBounds.Min.Y - position.Y;
                sqrDistance += diff * diff;
            }
            else if (position.Y > sectorBounds.Max.Y)
            {
                float diff = position.Y - sectorBounds.Max.Y;
                sqrDistance += diff * diff;
            }

            // Z axis
            if (position.Z < sectorBounds.Min.Z)
            {
                float diff = sectorBounds.Min.Z - position.Z;
                sqrDistance += diff * diff;
            }
            else if (position.Z > sectorBounds.Max.Z)
            {
                float diff = position.Z - sectorBounds.Max.Z;
                sqrDistance += diff * diff;
            }

            return MathF.Sqrt(sqrDistance);
        }


        private Vector3i GetSectorIndex(Vector3 position)
        {
            // Use integer division to ensure correct sector indexing
            int x = (int)Math.Floor(position.X / SectorSize);
            int y = (int)Math.Floor(position.Y / SectorSize);
            int z = (int)Math.Floor(position.Z / SectorSize);
            return new Vector3i(x, y, z);
        }

        private Vector3 GetSectorPosition(Vector3i index)
        {
            // Center the sector position
            return new Vector3(
                index.X * SectorSize + HalfSectorSize,
                index.Y * SectorSize + HalfSectorSize,
                index.Z * SectorSize + HalfSectorSize
            );
        }

        private bool SectorExists(Vector3i index)
        {
            return sectorsByIndex.ContainsKey(index);
        }

        private void AddSector(Vector3i index)
        {
            Vector3 sectorPosition = GetSectorPosition(index);
            Sector newSector = new Sector(sectorPosition, index, this);

            BoundingBox sectorBounds = new BoundingBox(
                sectorPosition - new Vector3(HalfSectorSize),
                sectorPosition + new Vector3(HalfSectorSize)
            );

            sectorOctree.Add(newSector, sectorBounds);
            sectorsByIndex.Add(index, newSector);
        }

        private void RemoveSector(Vector3i index)
        {
            if (sectorsByIndex.TryGetValue(index, out Sector sector))
            {
                BoundingBox sectorBounds = new BoundingBox(
                    sector.Position - new Vector3(HalfSectorSize),
                    sector.Position + new Vector3(HalfSectorSize)
                );
                sectorOctree.Remove(sector, sectorBounds);
                sectorsByIndex.Remove(index);
            }
        }

        private IEnumerable<Sector> GetSectorsInRange(Vector3 position, float range)
        {
            BoundingBox searchBounds = new BoundingBox(
                position - new Vector3(range),
                position + new Vector3(range)
            );
            var sectors = new List<Sector>();
            sectorOctree.GetColliding(sectors, searchBounds);
            return sectors;
        }

        private void ShiftWorld(Vector3 shiftAmount)
        {
            // Shift the player's position back to near the origin
            Player.Position -= shiftAmount;

            // Shift all sectors and their contents
            foreach (var sector in sectorsByIndex.Values)
            {
                sector.Shift(shiftAmount);
            }

            // Update the octree root position
            sectorOctree.Shift(shiftAmount);
        }
    }
}
