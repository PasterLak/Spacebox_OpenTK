using Engine;
using Engine.Components;
using Engine.GUI;
using Engine.Multithreading;
using Engine.Physics;
using OpenTK.Mathematics;

using Spacebox.Game.Effects;
using Spacebox.Game.GameMath;
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

namespace Spacebox.Game.Generation;


public class World : Component, ISpaceStructure
{
    public static World Instance;

    public const int SizeSectors = 8192;
    public Astronaut Player { get; private set; }
    public static WorldGenerator WorldGenerator { get; set; }
    public static WorldLoader.LoadedWorld WorldData { get; private set; }
    public static DropManager DropEffectManager;
    public static BlockDestructionManager DestructionManager;
    public static int Seed { get; private set; }
    public static Sector? CurrentSector { get; private set; }
    public static BiomeGenerator BiomeGenerator { get; private set; }
    private readonly Octree<Sector> worldOctree;

    private readonly Dictionary<Vector3i, Sector> Sectors = new Dictionary<Vector3i, Sector>();
    private BlockMaterial material;


    public World(Astronaut player, BlockMaterial material)
    {
        Instance = this;
        Player = player;
        this.material = material;

        worldOctree = new Octree<Sector>(
            Sector.SizeBlocks * SizeSectors,
            Vector3.Zero, Sector.SizeBlocks, 1.0f);


        Overlay.AddElement(new WorldOverlayElement(this));

        BiomeGenerator = new BiomeGenerator(World.Seed, WorldGenerator);

        DropEffectManager = new DropManager(player);
    }
    public override void Start()
    {
        DestructionManager = new BlockDestructionManager();


        Owner.AttachComponent(DestructionManager);
        Owner.AttachComponent(DropEffectManager);
    }
    bool saveWasPressed = false;
    public void Save()
    {
        /*if(!saveWasPressed){
            saveWasPressed = true;
            PanelUI.HideItemModel();
            return;
        }*/
        PanelUI.EnableRenderForCurrentItem = false;
        PlayerSaveLoadManager.SavePlayer(Player, WorldData.WorldFolderPath);
        WorldSaveLoad.SaveWorld(WorldData.WorldFolderPath);
        WorldData.Info.GameMode = Player.GameMode;
        WorldData.Info.Day = GameTime.Day;
        WorldData.Info.Ticks = GameTime.DayTick;
        WorldInfoSaver.Save(WorldData.Info);

        var screenSize = SpaceboxWindow.Instance.ClientSize;
        string path = Path.Combine(WorldData.WorldFolderPath, "preview.jpg");

        FramebufferCapture.SaveWorldPreview(screenSize, path);
        TagsSaveLoader.SaveTags(WorldData.WorldFolderPath);

        DropEffectManager.SaveDrops(Path.Combine(WorldData.WorldFolderPath, "drop.json"));

        saveWasPressed = false;
        PanelUI.ShowItemModel();
        PanelUI.EnableRenderForCurrentItem = true;
    }

    public void Load()
    {

        CurrentSector = LoadSectorNow(SpaceMath.Sector.GetSectorIndex(Player.Position));

        CurrentSector.SpawnPlayerNearRandomAsteroid(Player, new Random(Seed));

        if (CurrentSector == null) Debug.Error("No current sector");

        DropEffectManager.LoadDrops(Path.Combine(WorldData.WorldFolderPath, "drop.json"));

    }

    public static void LoadWorldInfo(string worldName)
    {
        WorldData = WorldLoader.LoadWorldByName(worldName);
        Seed = int.Parse(WorldData.Info.Seed);

        GameTime.SetDay(WorldData.Info.Day);
        GameTime.SetTick(WorldData.Info.Ticks);

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

        var index = SpaceMath.Sector.GetSectorIndex(Player.Position);

        if (CurrentSector.PositionIndex != index)
        {
            if (Sectors.TryGetValue(index, out var sector))
            {
                CurrentSector = sector;
            }
            else
            {
                Debug.Error("Loading a sector in the main thread! Index: " + index);
                LoadSectorNow(index);

            }
        }
        UnloadSectors(cam.PositionWorld);

        var sectorSize = Sector.SizeBlocks;
        Vector3 local = CurrentSector.WorldToLocalPosition(cam.PositionWorld);

        var baseIdx = CurrentSector.PositionIndex;

        foreach (var dir in NeighborDirs)
        {
            float dist = SpaceMath.World.DistanceToEdge(local, sectorSize, dir);
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
                && SpaceMath.World.DistanceToBox(cameraPosition, sector.BoundingBox) > Settings.SECTOR_UNLOAD_DISTANCE_SQUARED)
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

    }
    private void LoadSectorAsync(Vector3i sectorIndex)
    {

        loadingSectors.Add(sectorIndex);

        int worldSeed = Seed;
        Vector3 worldPos = SpaceMath.Sector.GetSectorPosition(sectorIndex);

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


    private Sector LoadSectorNow(Vector3i sectorIndex)
    {
        Sector newSector = null;

        Vector3 sectorPosition = SpaceMath.Sector.GetSectorPosition(sectorIndex);
        newSector = new Sector(sectorPosition, sectorIndex, Seed);

        worldOctree.Add(newSector, newSector.BoundingBox);
        Sectors.Add(newSector.PositionIndex, newSector);

        return newSector;
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
        CurrentSector?.Dispose();
        CurrentSector = null;
        Instance = null;

        foreach (var s in Sectors.Values)
        {
            s.Dispose();
        }
    }
}