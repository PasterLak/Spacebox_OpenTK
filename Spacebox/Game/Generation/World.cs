using Engine;
using Engine.GUI;
using Engine.Physics;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

using Spacebox.Game.Effects;
using Spacebox.Game.GUI;
using Spacebox.Game.Physics;
using Spacebox.Game.Player;
using static Spacebox.Game.WorldLoader;

/*
  positionWorld
  positionLocal
  positionIndex
 */

namespace Spacebox.Game.Generation
{
    public class World : IDisposable
    {
        public static World Instance;

        public const int SizeSectors = 4;
        public Astronaut Player { get; private set; }
        public static Random Random;
        public static WorldLoader.LoadedWorld Data { get; private set; }
        public static DropEffectManager DropEffectManager;
        public static BlockDestructionManager DestructionManager;
        public static int Seed;
        public static Sector CurrentSector { get; private set; }

        private readonly Octree<Sector> worldOctree;

        //private readonly ConcurrentDictionary<Vector3i, Sector> sectorsByIndex;
        private readonly object octreeLock = new object();

        //private readonly HashSet<Vector3i> sectorsBeingLoaded = new HashSet<Vector3i>();
        // private readonly HashSet<Vector3i> sectorsBeingUnloaded = new HashSet<Vector3i>();
        private readonly Dictionary<Vector3i, Sector> cachedSectors = new Dictionary<Vector3i, Sector>();
        private readonly Queue<Sector> sectorsToInitialize = new Queue<Sector>();
     
        public World(Astronaut player)
        {
            Instance = this;
            Player = player;

            float initialWorldSize = Sector.SizeBlocks * SizeSectors;

            worldOctree = new Octree<Sector>(initialWorldSize, Vector3.Zero, Sector.SizeBlocks, 1.0f);

            //sectorsByIndex = new ConcurrentDictionary<Vector3i, Sector>();

            LoadWorld();

            DropEffectManager = new DropEffectManager(player);
            DestructionManager = new BlockDestructionManager(player);

            player.OnMoved += OnPlayerMoved;

            //SpaceEntity.InitializeSharedResources();

            Overlay.AddElement(new WorldOverlayElement(this));


           // InputManager.AddAction("save", Keys.P, false);
          //  InputManager.RegisterCallback("save", () => { SaveWorld(); });

         
        }

        public void SaveWorld()
        {
            Player.Save();
            WorldSaveLoad.SaveWorld(Data.WorldFolderPath);
            WorldInfoSaver.Save(Data.Info);
            Debug.Success("The world was successfully saved!");
        }

        public void LoadWorld()
        {
            Vector3i initialSectorIndex = GetSectorIndex(Player.Position);


            CurrentSector = LoadSector(initialSectorIndex);
            CurrentSector.SpawnPlayerNearAsteroid(Player, Random);
            if (CurrentSector == null) Debug.Error("No current sector");
        }

        public static void LoadWorldInfo(string worldName)
        {
            Data = WorldLoader.LoadWorldByName(worldName);
            Seed = int.Parse(Data.Info.Seed);
            if (Data == null)
            {
                Debug.Log("Data not found!");
                Random = new Random();
            }
            else
            {
                Random = new Random(Seed);
            }

           
        }

        private void OnPlayerMoved(Astronaut player)
        {
            if (CurrentSector == null) return;
            Vector3i sectorToCheck = GetSectorIndex(Player.Position);

            if (CurrentSector.PositionIndex == sectorToCheck) return;

            UnloadSector();

            CurrentSector = LoadSector(sectorToCheck);
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
            DestructionManager.Update();
         
            //InitializeSectors();
            worldOctree.DrawDebug();
            //UpdateSectors();

            if (CurrentSector != null)
            {
                CurrentSector.Update();
            }
            /*foreach (var sector in GetSectorsInRange(Player.Position, 3))
            {
                sector.Update();
            }*/

            if(Input.IsKeyDown(Keys.KeyPad8))
            {
           
                    if(CurrentSector.TryGetNearestEntity(Player.Position, out var ent))
                {
                    if(ent.IsPositionInChunk(Player.Position, out var chunk))
                    {
                        chunk.ClearChunk();
                        chunk.GenerateMesh();
                    }
                }

            }

        }

        public void Render(Shader shader)
        {
            /*foreach (var sector in GetSectorsInRange(Player.Position, 3))
            {
                sector.Render(shader);
            }*/

            if (CurrentSector != null)
            {
                CurrentSector.Render(shader);
            }

            DropEffectManager.Render();
            DestructionManager.Render();
            
        }

        private Sector LoadSector(Vector3i sectorIndex)
        {
            Sector newSector = null;

            if (cachedSectors.TryGetValue(sectorIndex, out newSector))
            {
                worldOctree.Add(newSector, newSector.BoundingBox);
                return newSector;
            }
            else
            {
                Vector3 sectorPosition = GetSectorPosition(sectorIndex);
                newSector = new Sector(sectorPosition, sectorIndex, this);
                CurrentSector = newSector;

                Vector3 sectorCenter = sectorPosition + new Vector3(Sector.SizeBlocksHalf);
                Vector3 sectorSize = new Vector3(Sector.SizeBlocks, Sector.SizeBlocks, Sector.SizeBlocks);

                BoundingBox sectorBounds = new BoundingBox(sectorCenter, sectorSize);

                worldOctree.Add(newSector, sectorBounds);
            }

            //sectorsByIndex[sectorIndex] = newSector;
            //sectorsBeingLoaded.Remove(sectorIndex);

            return newSector;
        }


        private void UnloadSector()
        {
            if (CurrentSector == null) return;

            if (cachedSectors.ContainsKey(CurrentSector.PositionIndex))
            {
                return;
            }

            cachedSectors.Add(CurrentSector.PositionIndex, CurrentSector);
            worldOctree.Remove(CurrentSector, CurrentSector.BoundingBox);
            //CurrentSector.Dispose();
            CurrentSector = null;

            //sectorsByIndex.TryRemove(CurrentSector.Index, out Sector sector);
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
            // return sectorsByIndex.ContainsKey(index);

            return false;
        }

        public bool IsColliding(Vector3 pos, BoundingVolume volume, out CollideInfo collideInfo)
        {
            if (CurrentSector == null)
            {
                collideInfo = new CollideInfo();
                return false;

            }

            return CurrentSector.IsColliding(pos, volume, out collideInfo);
        }

        private IEnumerable<Sector> GetSectorsInRange(Vector3 position, float range)
        {
            Vector3 searchCenter = position;
            Vector3 searchSize = new Vector3(range * 2f);

            BoundingBox searchBounds = new BoundingBox(searchCenter, searchSize);

            VisualDebug.DrawBoundingBox(searchBounds, new Color4(0, 255, 0, 255));

            var sectors = new List<Sector>();
            lock (octreeLock)
            {
                worldOctree.GetColliding(sectors, searchBounds);
            }

            return sectors;
        }

        public void Dispose()
        {
            Data = null;
          
        }
    }
}