using OpenTK.Mathematics;
using Spacebox.Common;
using Spacebox.Common.Physics;
using System.Collections.Concurrent;

namespace Spacebox.Game
{
    public class World
    {
        public static World Instance;

        public const int SizeSectors = 4;
        public Astronaut Player { get; private set; }
        public static Random Random;
        public WorldLoader.LoadedWorld WorldData { get; private set; }
        public static DropEffectManager DropEffectManager;

        private readonly Octree<Sector> worldOctree;
        private readonly ConcurrentDictionary<Vector3i, Sector> sectorsByIndex;
        private readonly object octreeLock = new object();

        private const float SECTOR_LOAD_RADIUS = 32 ;
        private const float SECTOR_UPDATE_RADIUS = 32;

        private readonly HashSet<Vector3i> sectorsBeingLoaded = new HashSet<Vector3i>();
        private readonly HashSet<Vector3i> sectorsBeingUnloaded = new HashSet<Vector3i>();

        private readonly Queue<Sector> sectorsToInitialize = new Queue<Sector>();

        public World(Astronaut player)
        {
            Instance = this;
            Player = player;

           
            float initialWorldSize = Sector.SizeBlocks * SizeSectors;

            worldOctree = new Octree<Sector>(initialWorldSize, Vector3.Zero, Sector.SizeBlocks, 1.0f);

            sectorsByIndex = new ConcurrentDictionary<Vector3i, Sector>();

            Vector3i initialSectorIndex = GetSectorIndex(Player.Position);
            LoadSectorAsync(initialSectorIndex);

            DropEffectManager = new DropEffectManager(player);
        }

        public void LoadWorldInfo(string worldName)
        {
            WorldData = WorldLoader.LoadWorldByName(worldName);

            if (WorldData == null)
            {
                Debug.Log("Data not found!");
                Random = new Random();
            }
            else
            {
                Random = new Random(int.Parse(WorldData.Info.Seed));
            }
        }

        public void EnqueueSectorInitialization(Sector sector)
        {
            lock (sectorsToInitialize)
            {
                sectorsToInitialize.Enqueue(sector);
            }
        }

        private void InitializeSectors()
        {
            lock (sectorsToInitialize)
            {
                while (sectorsToInitialize.Count > 0)
                {
                    Sector sector = sectorsToInitialize.Dequeue();
                    sector.InitializeSharedResources();
                }
            }
        }

        public void Update()
        {
            DropEffectManager.Update();
            InitializeSectors();
            worldOctree.DrawDebug();
            UpdateSectors();

            if(Input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.H))
            {
                Debug.Log("Sector index: " + GetSectorIndex(Player.Position));
            }

            foreach (var sector in GetSectorsInRange(Player.Position, SECTOR_UPDATE_RADIUS))
            {
                sector.Update();
            }
        }

        public void Render(Shader shader)
        {
            foreach (var sector in GetSectorsInRange(Player.Position, SECTOR_LOAD_RADIUS))
            {
                sector.Render(shader);
            }

            DropEffectManager.Render();
        }

        private void UpdateSectors()
        {
            Vector3 playerPosition = Player.Position;
            Vector3i currentSectorIndex = GetSectorIndex(playerPosition);

            const int loadRadiusInSectors = (int)(SECTOR_LOAD_RADIUS / Sector.SizeBlocks) + 1;
            //int updateRadiusInSectors = 0;

            HashSet<Vector3i> sectorsToConsider = new HashSet<Vector3i>();

            for (int x = currentSectorIndex.X - loadRadiusInSectors; x <= currentSectorIndex.X + loadRadiusInSectors; x++)
            {
                for (int y = currentSectorIndex.Y - loadRadiusInSectors; y <= currentSectorIndex.Y + loadRadiusInSectors; y++)
                {
                    for (int z = currentSectorIndex.Z - loadRadiusInSectors; z <= currentSectorIndex.Z + loadRadiusInSectors; z++)
                    {
                        Vector3i sectorIndex = new Vector3i(x, y, z);
                        sectorsToConsider.Add(sectorIndex);
                    }
                }
            }

            foreach (var sectorIndex in sectorsToConsider)
            {
                if (!SectorExists(sectorIndex) && !sectorsBeingLoaded.Contains(sectorIndex))
                {
                    Vector3 sectorPosition = GetSectorPosition(sectorIndex);

                    float distanceToSector = GetDistanceToSector(sectorPosition, playerPosition);

                    if (distanceToSector <= SECTOR_LOAD_RADIUS)
                    {
                        sectorsBeingLoaded.Add(sectorIndex);
                        LoadSectorAsync(sectorIndex);
                    }
                }
            }

            foreach (var kvp in sectorsByIndex)
            {
                Vector3i sectorIndex = kvp.Key;

                if (!sectorsToConsider.Contains(sectorIndex) && !sectorsBeingUnloaded.Contains(sectorIndex))
                {
                    Vector3 sectorPosition = GetSectorPosition(sectorIndex);

                    float distanceToSector = GetDistanceToSector(sectorPosition, playerPosition);

                    if (distanceToSector > SECTOR_LOAD_RADIUS)
                    {
                        sectorsBeingUnloaded.Add(sectorIndex);
                        UnloadSectorAsync(sectorIndex);
                    }
                }
            }
        }

        private void LoadSectorAsync(Vector3i sectorIndex)
        {
            Task.Run(() =>
            {
                Vector3 sectorPosition = GetSectorPosition(sectorIndex);
                Sector newSector = new Sector(sectorPosition, sectorIndex, this);

              
                Vector3 sectorCenter = sectorPosition + new Vector3(Sector.SizeBlocksHalf);
                Vector3 sectorSize = new Vector3(Sector.SizeBlocks, Sector.SizeBlocks, Sector.SizeBlocks);

                BoundingBox sectorBounds = new BoundingBox(sectorCenter, sectorSize);

                lock (octreeLock)
                {
                    worldOctree.Add(newSector, sectorBounds);
                }

                sectorsByIndex[sectorIndex] = newSector;
                sectorsBeingLoaded.Remove(sectorIndex);
            });
        }

        private void UnloadSectorAsync(Vector3i sectorIndex)
        {
            Task.Run(() =>
            {
                if (sectorsByIndex.TryRemove(sectorIndex, out Sector sector))
                {
                    Vector3 sectorPosition = GetSectorPosition(sectorIndex);

                    Vector3 sectorCenter = sectorPosition + new Vector3(Sector.SizeBlocksHalf);
                    Vector3 sectorSize = new Vector3(Sector.SizeBlocks, Sector.SizeBlocks, Sector.SizeBlocks);

                    BoundingBox sectorBounds = new BoundingBox(sectorCenter, sectorSize);

                    lock (octreeLock)
                    {
                        worldOctree.Remove(sector, sectorBounds);
                    }

                    sector.Dispose();
                }

                sectorsBeingUnloaded.Remove(sectorIndex);
            });
        }

        private Vector3i GetSectorIndex(Vector3 position)
        {
            int x = (int)Math.Floor(position.X / Sector.SizeBlocks);
            int y = (int)Math.Floor(position.Y / Sector.SizeBlocks);
            int z = (int)Math.Floor(position.Z / Sector.SizeBlocks);
            return new Vector3i(x, y, z);
        }

        private Vector3 GetSectorPosition(Vector3i index)
        {
           
            return new Vector3(
                index.X * Sector.SizeBlocks,
                index.Y * Sector.SizeBlocks,
                index.Z * Sector.SizeBlocks
            );
        }

        private float GetDistanceToSector(Vector3 sectorPosition, Vector3 playerPosition)
        {
            Vector3 sectorCenter = sectorPosition + new Vector3(Sector.SizeBlocksHalf);
            return Vector3.Distance(sectorCenter, playerPosition);
        }

        private bool SectorExists(Vector3i index)
        {
            return sectorsByIndex.ContainsKey(index);
        }

        private IEnumerable<Sector> GetSectorsInRange(Vector3 position, float range)
        {
            Vector3 searchCenter = position;
            Vector3 searchSize = new Vector3(range * 2f);

            BoundingBox searchBounds = new BoundingBox(searchCenter, searchSize);

            VisualDebug.DrawBoundingBox(searchBounds, new Color4(0,255,0,255));

            var sectors = new List<Sector>();
            lock (octreeLock)
            {
                worldOctree.GetColliding(sectors, searchBounds);
            }
            return sectors;
        }
    }
}
