using Engine;
using Engine.Components;
using Engine.GUI;
using Engine.Multithreading;
using Engine.Physics;
using OpenTK.Mathematics;

using Spacebox.Game.Effects;
using Spacebox.Game.Generation.Structures;
using Spacebox.Game.Generation.Tools;
using Spacebox.Game.GUI;
using Spacebox.Game.Physics;
using Spacebox.Game.Player;
using Spacebox.Game.Resource;


/*
  positionWorld
  positionEntity
  positionChunk
 */

namespace Spacebox.Game.Generation
{

    public class WorldSaveLoader
    {
        private World world;
        public WorldSaveLoader(World world) { this.world = world; }

    }
    public class World : Component, ISpaceStructure
    {
        public static World Instance;

        public const int SizeSectors = 8192;
        public Astronaut Player { get; private set; }

        public static WorldLoader.LoadedWorld Data { get; private set; }
        public static DropEffectManager DropEffectManager;
        public static BlockDestructionManager DestructionManager;
        public static int Seed { get; private set; }
        public static Sector? CurrentSector { get; private set; }

        private readonly Octree<Sector> worldOctree;

        private readonly Dictionary<Vector3i, Sector> Sectors = new Dictionary<Vector3i, Sector>();
        BlockMaterial material;

        public World(Astronaut player, BlockMaterial material)
        {
            Instance = this;
            Player = player;
            this.material = material;

            worldOctree = new Octree<Sector>(
                Sector.SizeBlocks * SizeSectors,
                Vector3.Zero, Sector.SizeBlocks, 1.0f);


            DropEffectManager = new DropEffectManager(player);
            DestructionManager = new BlockDestructionManager(player);


            Overlay.AddElement(new WorldOverlayElement(this));

        }

        public void Save()
        {
            Player.Save();
            WorldSaveLoad.SaveWorld(Data.WorldFolderPath);
            WorldInfoSaver.Save(Data.Info);

            var screenSize = Window.Instance.ClientSize;
            string path = Path.Combine(Data.WorldFolderPath, "preview.jpg");

            FramebufferCapture.SaveWorldPreview(screenSize, path);

        }

        public void Load()
        {

            Vector3i initialSectorIndex = GetSectorIndex(Player.Position);

            CurrentSector = LoadSector(initialSectorIndex);

            CurrentSector.SpawnPlayerNearAsteroid(Player, new Random(Seed));
            if (CurrentSector == null) Debug.Error("No current sector");


        }

        public static void LoadWorldInfo(string worldName)
        {
            Data = WorldLoader.LoadWorldByName(worldName);
            Seed = int.Parse(Data.Info.Seed);

        }

        private float _timeToCheckSectors = 0;
        public override void OnUpdate()
        {

            _timeToCheckSectors += Time.Delta;

            if (_timeToCheckSectors > 1)
            {
                _timeToCheckSectors = 0;
                UpdateSectors();
            }

            DropEffectManager.Update();
            DestructionManager.Update();
            worldOctree.DrawDebug();

            foreach (var sector in Sectors)
            {
                sector.Value.Update();
            }

        }
        private readonly HashSet<Vector3i> loadingSectors = new HashSet<Vector3i>();

        private static readonly Vector3i[] NeighborDirs = new[]
        {
            new Vector3i(-1,  0,  0),
            new Vector3i(+1,  0,  0),
            new Vector3i( 0, -1,  0),
            new Vector3i( 0, +1,  0),
            new Vector3i( 0,  0, -1),
            new Vector3i( 0,  0, +1),
        };

        private void UpdateSectors()
        {
            var cam = Camera.Main;
            if (cam == null || CurrentSector == null) return;

            var index = GetSectorIndex(Player.Position);

            if (CurrentSector.PositionIndex != index)
            {
                if (Sectors.TryGetValue(index, out var sector))
                {
                    CurrentSector = sector;
                }
                else
                {
                    Debug.Error("Loading a sector in the main thread! Index: " + index);
                    LoadSector(index);

                }
            }
            UnloadSectors(cam.PositionWorld);

            float sectorSize = Sector.SizeBlocks;
            Vector3 local = CurrentSector.WorldToLocalPosition(cam.PositionWorld);

            var baseIdx = CurrentSector.PositionIndex;
    
            foreach (var dir in NeighborDirs)
            {
                float dist = DistanceToEdge(local, sectorSize, dir);
                if (dist < Settings.VIEW_DISTANCE_TO_NEXT_SECTOR)
                    TryLoadNeighbor(baseIdx + dir, dist);
            }
        }
        private void TryLoadNeighbor(Vector3i idx, float distance)
        {
            if (distance >= Settings.VIEW_DISTANCE_TO_NEXT_SECTOR) return;
            if (Sectors.ContainsKey(idx)) return;
            if (loadingSectors.Contains(idx)) return;

            loadingSectors.Add(idx);
            LoadSectorAsync(idx);
        }

        private void UnloadSectors(Vector3 cameraPosition)
        {
            var toRemove = new List<Vector3i>();

            foreach (var kv in Sectors)
            {
                var idx = kv.Key;
                var sector = kv.Value;

                if (!sector.BoundingBox.Contains(cameraPosition)
                    && DistanceToBox(cameraPosition, sector.BoundingBox) > Settings.SECTOR_UNLOAD_DISTANCE)
                {
                    toRemove.Add(idx);
                }
            }

            foreach (var idx in toRemove)
            {
                var sector = Sectors[idx];
                worldOctree.Remove(sector, sector.BoundingBox);
                sector.Dispose();
                Sectors.Remove(idx);
                Debug.Log($"Unloaded sector {idx}");
            }
        }

        public override void OnRender()
        {

            foreach (var sector in Sectors)
            {
                sector.Value.Render(material);
            }
            DropEffectManager.Render();
            DestructionManager.Render();

        }
        private void LoadSectorAsync(Vector3i sectorIndex)
        {

            loadingSectors.Add(sectorIndex);
            Debug.Log($"Sector loading....");
            int worldSeed = Seed;
            Vector3 worldPos = GetSectorPosition(sectorIndex);

            WorkerPoolManager
                .Enqueue(token =>
                {
                    var sector = new Sector(worldPos, sectorIndex, worldSeed);
                    MainThreadDispatcher.Instance.Enqueue(() =>
                    {
                        worldOctree.Add(sector, sector.BoundingBox);
                        Sectors[sectorIndex] = sector;
                        loadingSectors.Remove(sectorIndex);
                        Debug.Log($"Sector loaded: {sectorIndex}");
                    });
                },
                WorkerPoolManager.Priority.Low
                )
                .ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        Debug.Error($"Failed to load sector {sectorIndex}: {t.Exception}");
                        loadingSectors.Remove(sectorIndex);
                    }
                }, TaskScheduler.Default);
        }



        private Sector LoadSector(Vector3i sectorIndex)
        {
            Sector newSector = null;

            Vector3 sectorPosition = GetSectorPosition(sectorIndex);
            newSector = new Sector(sectorPosition, sectorIndex, Seed);

            worldOctree.Add(newSector, newSector.BoundingBox);
            Sectors.Add(newSector.PositionIndex, newSector);

            return newSector;
        }

        public static Vector3i GetSectorIndex(Vector3 position)
        {
            int x = (int)Math.Floor(position.X / Sector.SizeBlocks);
            int y = (int)Math.Floor(position.Y / Sector.SizeBlocks);
            int z = (int)Math.Floor(position.Z / Sector.SizeBlocks);
            return new Vector3i(x, y, z);
        }

        private static Vector3 GetSectorPosition(Vector3i index)
        {
            return new Vector3(
                index.X * Sector.SizeBlocks,
                index.Y * Sector.SizeBlocks,
                index.Z * Sector.SizeBlocks
            );
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

        private static float DistanceToBox(Vector3 point, BoundingBox box)
        {
            float dx = Math.Max(box.Min.X - point.X, 0f);
            dx = Math.Max(dx, point.X - box.Max.X);
            float dy = Math.Max(box.Min.Y - point.Y, 0f);
            dy = Math.Max(dy, point.Y - box.Max.Y);
            float dz = Math.Max(box.Min.Z - point.Z, 0f);
            dz = Math.Max(dz, point.Z - box.Max.Z);
            return MathF.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        private static float DistanceToEdge(Vector3 local, float sectorSize, Vector3i dir)
        {
            if (dir.X < 0) return local.X;
            if (dir.X > 0) return sectorSize - local.X;
            if (dir.Y < 0) return local.Y;
            if (dir.Y > 0) return sectorSize - local.Y;
            if (dir.Z < 0) return local.Z;
            if (dir.Z > 0) return sectorSize - local.Z;
            return float.MaxValue;
        }

        public override void OnDetached()
        {
            base.OnDetached();

            Data = null;
            DropEffectManager.Dispose();
            DropEffectManager = null;
            DestructionManager.Dispose();
            DestructionManager = null;
            CurrentSector?.Dispose();
            CurrentSector = null;

            foreach (var s in Sectors.Values)
            {
                s.Dispose();
            }
        }
    }
}