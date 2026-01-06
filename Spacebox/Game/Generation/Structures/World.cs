using Engine;
using Engine.Components;
using Engine.GUI;
using Engine.Multithreading;
using Engine.Physics;
using Engine.Utils;
using OpenTK.Mathematics;
using Spacebox.Game.Effects;
using Spacebox.Game.GameMath;
using Spacebox.Game.Generation.Structures;
using Spacebox.Game.Generation.Tools;
using Spacebox.Game.GUI;
using Spacebox.Game.Physics;
using Spacebox.Game.Player;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Generation;

public class World : Component, ISpaceStructure
{
    public static World Instance;
    public const int SizeSectors = 8192;

    public static WorldGenerator WorldGenerator { get; set; }
    public static WorldLoader.LoadedWorld WorldData { get; private set; }
    public static DropManager DropEffectManager;
    public static BlockDestructionManager DestructionManager;
    public static int Seed { get; private set; }
    public static Sector? CurrentSector { get; private set; }
    public static BiomeGenerator BiomeGenerator { get; private set; }

    public Astronaut Player { get; private set; }

    private BlockMaterial material;
    private readonly Octree<Sector> worldOctree;
    private readonly Dictionary<Vector3i, Sector> loadedSectors;
    private readonly HashSet<Vector3i> loadingSectors;
    private readonly List<Vector3i> _sectorsToRemoveCache;
    private float _timeToCheckSectors = 0;

    private static readonly Vector3i[] _neighborDirs = new[]
    {
        new Vector3i(-1,  0,  0),
        new Vector3i(+1,  0,  0),
        new Vector3i( 0, -1,  0),
        new Vector3i( 0, +1,  0),
        new Vector3i( 0,  0, -1),
        new Vector3i( 0,  0, +1),
    };

    private Spacer spacer;
    public World(Astronaut player, BlockMaterial material)
    {
        Instance = this;
        Player = player;
        this.material = material;

        worldOctree = new Octree<Sector>(
            Sector.SizeBlocks * SizeSectors,
            Vector3.Zero, Sector.SizeBlocks, 1.0f);

        Overlay.AddElement(new WorldOverlayElement(this));

        loadingSectors = new HashSet<Vector3i>();
        loadedSectors = new Dictionary<Vector3i, Sector>();
        _sectorsToRemoveCache = new List<Vector3i>();

        BiomeGenerator = new BiomeGenerator(World.Seed, WorldGenerator);
        DropEffectManager = new DropManager(player);




    }

    public override void Start()
    {
        DestructionManager = new BlockDestructionManager();
        Owner.AttachComponent(DestructionManager);
        Owner.AttachComponent(DropEffectManager);
        spacer = Owner.AddChild(new Spacer(Player.Position + new Vector3(5, 5, 7)));
    }

    public void Save()
    {

        PlayerSaveLoadManager.SavePlayer(Player, WorldData.WorldFolderPath);
        WorldSaveLoad.SaveWorld(WorldData.WorldFolderPath, loadedSectors);

        WorldPersistenceManager.SaveAllCacheToDiskNow(WorldData.WorldFolderPath);

        WorldData.Info.GameMode = Player.GameMode;
        WorldData.Info.Day = GameTime.Day;
        WorldData.Info.Ticks = GameTime.DayTick;
        WorldInfoSaver.Save(WorldData.Info);

        var screenSize = SpaceboxWindow.Instance.ClientSize;
        string path = Path.Combine(WorldData.WorldFolderPath, "preview.jpg");

        FramebufferCapture.SaveWorldPreview(screenSize, path);
        TagsSaveLoader.SaveTags(WorldData.WorldFolderPath);

        DropEffectManager.SaveDrops(Path.Combine(WorldData.WorldFolderPath, "drop.json"));

    }

    public void Load()
    {
        CurrentSector = LoadSectorNow(SpaceMath.Sector.GetSectorIndex(Player.Position));
        //CurrentSector.SpawnPlayerNearRandomAsteroidData(Player, new Random(Seed));

        //CurrentSector.PreloadAreaBlocking(Player.Position, Settings.CHUNK_VISIBLE_RADIUS);

        if (CurrentSector == null)
            Debug.Error("No current sector");

        DropEffectManager.LoadDrops(Path.Combine(WorldData.WorldFolderPath, "drop.json"));
    }

    public static void LoadWorldInfo(string worldName)
    {
        WorldData = WorldLoader.LoadWorldByName(worldName);
        Seed = SeedHelper.ComputeSeed(WorldData.Info.Seed);

        GameTime.SetDay(WorldData.Info.Day);
        GameTime.SetTick(WorldData.Info.Ticks);
    }

    public override void OnUpdate()
    {
        _timeToCheckSectors += Time.Delta;

        if (_timeToCheckSectors > 1)
        {
            _timeToCheckSectors = 0;
            UpdateSectors();
        }

        worldOctree.DrawDebug();

        foreach (var sector in loadedSectors)
        {
            sector.Value.Update();
        }
    }

    public override void OnRender()
    {
        foreach (var sector in loadedSectors)
        {
            sector.Value.Render(material);
        }
    }

    public bool RaycastDynamicObjects(Ray ray, out float distance, List<Node3D> objects)
    {

        if (ray.Intersects(spacer.OBB, out distance))
        {
            objects.Add(spacer);
            return true;
        }
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

    public override void OnDetached()
    {
        base.OnDetached();

        WorldData = null;
        WorldGenerator = null;
        BiomeGenerator = null;
        DropEffectManager = null;
        DestructionManager = null;
        WorldPersistenceManager.Dispose();
        CurrentSector?.Dispose();
        CurrentSector = null;
        Instance = null;

        foreach (var s in loadedSectors.Values)
        {
            s.Dispose();
        }
        loadedSectors.Clear();
        loadingSectors.Clear();
        _sectorsToRemoveCache.Clear();

    }

    private void UpdateSectors()
    {
        var cam = Camera.Main;
        if (cam == null || CurrentSector == null) return;

        var index = SpaceMath.Sector.GetSectorIndex(Player.Position);

        if (CurrentSector.PositionIndex != index)
        {
            if (loadedSectors.TryGetValue(index, out var sector))
            {
                CurrentSector = sector;
            }
            else
            {
                // Debug.Error("Loading a sector in the main thread! Index: " + index);
                CurrentSector = LoadSectorNow(index);
            }
        }

        PopulateSectorsToUnload(cam.PositionWorld);

        for (int i = 0; i < _sectorsToRemoveCache.Count; i++)
        {
            UnloadSector(_sectorsToRemoveCache[i]);
        }

        var sectorSize = Sector.SizeBlocks;
        Vector3 local = CurrentSector.WorldToLocalPosition(cam.PositionWorld);
        var baseIdx = CurrentSector.PositionIndex;

        foreach (var dir in _neighborDirs)
        {
            float dist = SpaceMath.World.DistanceToEdge(local, sectorSize, dir);
            if (dist < Settings.VIEW_DISTANCE_TO_NEXT_SECTOR)
                TryLoadNeighbor(baseIdx + dir, dist);
        }
    }

    private void TryLoadNeighbor(Vector3i idx, float distance)
    {
        if (distance >= Settings.VIEW_DISTANCE_TO_NEXT_SECTOR) return;
        if (loadedSectors.ContainsKey(idx)) return;
        if (loadingSectors.Contains(idx)) return;

        loadingSectors.Add(idx);
        LoadSectorAsync(idx);
    }

    private void PopulateSectorsToUnload(Vector3 cameraPosition)
    {
        _sectorsToRemoveCache.Clear();

        foreach (var kv in loadedSectors)
        {
            var idx = kv.Key;
            var sector = kv.Value;

            if (!sector.BoundingBox.Contains(cameraPosition)
                && SpaceMath.World.DistanceToBox(cameraPosition, sector.BoundingBox) > Settings.SECTOR_UNLOAD_DISTANCE_SQUARED)
            {
                _sectorsToRemoveCache.Add(idx);
            }
        }
    }

    private void UnloadSector(Vector3i index)
    {
        if (loadedSectors.TryGetValue(index, out Sector sector))
        {
            worldOctree.Remove(sector, sector.BoundingBox);
            sector.Dispose();
            loadedSectors.Remove(index);
            Debug.Log($"[World] Unloaded sector {index}");
        }
        else
        {
            Debug.Error($"[World] Tried to unload sector {index} which is not loaded");
        }
    }

    private void LoadSectorAsync(Vector3i sectorIndex)
    {
        //loadingSectors.Add(sectorIndex);

        int worldSeed = Seed;
        Vector3 worldPos = SpaceMath.Sector.GetSectorPosition(sectorIndex);

        WorkerPoolManager
            .Enqueue(token =>
            {
                try
                {
                    var sector = new Sector(worldPos, sectorIndex, worldSeed);
                    MainThreadDispatcher.Instance.Enqueue(() =>
                    {
                        if (Instance == null)
                        {
                            sector.Dispose();
                            return;
                        }

                        worldOctree.Add(sector, sector.BoundingBox);
                        loadedSectors[sectorIndex] = sector;
                        loadingSectors.Remove(sectorIndex);
                        //Debug.Log($"Sector loaded: {sectorIndex}");
                    });
                }
                catch (Exception ex)
                {

                    Debug.Error($"[World] Crash inside Sector Constructor for {sectorIndex}: {ex.Message}\n{ex.StackTrace}");
                    throw;
                }
            },
            WorkerPoolManager.Priority.Low
            )
            .ContinueWith(t =>
            {
                if (t.IsFaulted)
                {

                    var realError = t.Exception?.Flatten().InnerException;

                    Debug.Error($"[World] Failed to load sector {sectorIndex}: {realError?.Message}");
                    Debug.Error(realError?.StackTrace);

                    MainThreadDispatcher.Instance.Enqueue(() =>
                    {
                        if (Instance != null)
                            loadingSectors.Remove(sectorIndex);
                    });
                }
            }, TaskScheduler.Default);
    }

    private Sector LoadSectorNow(Vector3i sectorIndex)
    {
        Vector3 sectorPosition = SpaceMath.Sector.GetSectorPosition(sectorIndex);
        var newSector = new Sector(sectorPosition, sectorIndex, Seed);

        worldOctree.Add(newSector, newSector.BoundingBox);
        loadedSectors.Add(newSector.PositionIndex, newSector);

        return newSector;
    }
}