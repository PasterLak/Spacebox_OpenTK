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
using Spacebox.Game.Physics.Gravity;
using Spacebox.Game.Player;
using Spacebox.Game.Resource;
using Spacebox.Game.Physics.Explosions;
using Spacebox.Game.Ships;

namespace Spacebox.Game.Generation;

public class World : Component, ISpaceStructure
{
    public static World Instance;
    public const int SizeSectors = 8192;

    public static WorldGenerator WorldGenerator { get; set; }
    public static WorldLoader.LoadedWorld WorldData { get; private set; }
    public static DropManager DropEffectManager;
    public static BlockDestructionManager DestructionManager;
    public BlockMiningEffect BlockMiningEffect;

    public ProjectilesPool ProjectilesPool;
    public static int Seed { get; private set; }
    public static int GeneratorVersion => GeneratorVersions.Normalize(WorldData?.Info?.GeneratorVersion ?? GeneratorVersions.Legacy);
    public static Sector? CurrentSector { get; private set; }
    public static BiomeGenerator BiomeGenerator { get; private set; }

    public LocalAstronaut Player { get; private set; }
    public ExplosionSystem Explosions { get; private set; }
    public ShipThrusterEffects ThrusterEffects { get; private set; }
    public Spacebox.Game.Effects.JetpackEffects JetpackEffects { get; private set; }

    private BlockMaterial material;
    private Octree<Sector> worldOctree;
    private readonly Dictionary<Vector3i, Sector> loadedSectors;
    private readonly HashSet<Vector3i> loadingSectors;
    private readonly List<Vector3i> _sectorsToRemoveCache;
    private readonly EntitySectorTracker _entityTracker;
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

    public PhysicsWorld Physics { get; private set; }
    public World(LocalAstronaut player, BlockMaterial material)
    {
        Instance = this;
        Player = player;
        this.material = material;
        Physics = new PhysicsWorld(this);

        worldOctree = new Octree<Sector>(
            Sector.SizeBlocks * SizeSectors,
            Vector3.Zero, Sector.SizeBlocks, 1.0f);

        Overlay.AddElement(new WorldOverlayElement(this));

        loadingSectors = new HashSet<Vector3i>();
        loadedSectors = new Dictionary<Vector3i, Sector>();
        _sectorsToRemoveCache = new List<Vector3i>();
        _entityTracker = new EntitySectorTracker(this);

        BiomeGenerator = new BiomeGenerator(World.Seed, WorldGenerator);
        DropEffectManager = new DropManager(player);
        Explosions = new ExplosionSystem();
        ThrusterEffects = new ShipThrusterEffects();
        JetpackEffects = new Spacebox.Game.Effects.JetpackEffects();

        player.EnablePhysics(Physics);
    }

    public override void Start()
    {
        DestructionManager = new BlockDestructionManager();
        Owner.AttachComponent(DestructionManager);
        Owner.AttachComponent(DropEffectManager);
        if (!Spacebox.Network.GameSession.IsRemoteClient)
            Spacebox.Network.NetworkSession.Current?.Spacers.Spawn(Player.Position + new Vector3(5, 5, 7));

        ProjectilesPool = new ProjectilesPool(20);
        Owner.AttachComponent(ProjectilesPool);

        BlockMiningEffect = BlockMiningEffect.CreateDefault();
        Owner.AddChild(BlockMiningEffect);


    }

    public void Save(bool capturePreview = true)
    {
        if (Spacebox.Network.GameSession.IsRemoteClient)
        {
            Spacebox.Network.NetworkSession.Current?.SendLocalPlayerData();
            return;
        }

        if (Spacebox.Network.GameSession.HasLocalPlayer)
            PlayerSaveLoadManager.SavePlayer(Player, WorldData.WorldFolderPath);
        WorldPersistenceManager.SaveAllCacheToDiskNow(WorldData.WorldFolderPath);

        WorldSaveLoad.SaveWorld(WorldData.WorldFolderPath, loadedSectors);
        WorldPersistenceManager.SaveCatalog(WorldData.WorldFolderPath);
        Spacebox.Game.Player.Respawn.DnaReconstructorRegistry.Save(WorldData.WorldFolderPath);

        if (Spacebox.Network.GameSession.HasLocalPlayer)
            WorldData.Info.GameMode = Player.GameModeId;
        WorldData.Info.Day = GameTime.Day;
        WorldData.Info.Ticks = GameTime.DayTick;
        WorldInfoSaver.Save(WorldData.Info);

        TagsSaveLoader.SaveTags(WorldData.WorldFolderPath);

        DropEffectManager.SaveDrops(Versioning.WorldFileLayout.DropsPath(WorldData.WorldFolderPath), Versioning.WorldFileLayout.LegacyDropsPath(WorldData.WorldFolderPath));

        if (Spacebox.Network.GameSession.HasLocalPlayer) WorldSaveState.MarkSaved(PlayerSaveLoadManager.Serialize(Player));

        if (capturePreview && !Engine.Application.IsHeadless) SavePreview();
    }

    private void SavePreview()
    {
        var screenSize = SpaceboxWindow.Instance.ClientSize;
        if (screenSize.X <= 0 || screenSize.Y <= 0) return;

        try
        {
            string path = Path.Combine(WorldData.WorldFolderPath, "preview.jpg");
            Spacebox.Game.Rendering.WorldPreviewCapture.Run(() =>
            {
                SpaceboxWindow.Instance.RenderWorldFrame();
                FramebufferCapture.SaveWorldPreview(screenSize, path);
            });
        }
        catch (Exception ex)
        {
            Debug.Warning($"[World] Could not save the world preview: {ex.Message}");
        }
    }

    public bool HasUnsavedChanges()
    {
        if (Spacebox.Network.GameSession.IsRemoteClient || !Spacebox.Network.GameSession.HasLocalPlayer || Player == null) return false;
        return WorldSaveState.HasUnsavedChanges(PlayerSaveLoadManager.Serialize(Player));
    }

    public void StartTrackingChanges()
    {
        if (Spacebox.Network.GameSession.HasLocalPlayer && Player != null) WorldSaveState.MarkSaved(PlayerSaveLoadManager.Serialize(Player));
    }

    public void Load()
    {
        if (!Spacebox.Network.GameSession.IsRemoteClient)
        {
            Spacebox.Game.Versioning.EntityStorageLayout.UpgradeInPlace(WorldData.WorldFolderPath);
            WorldPersistenceManager.LoadCatalog(WorldData.WorldFolderPath);
            Spacebox.Game.Player.Respawn.DnaReconstructorRegistry.Load(WorldData.WorldFolderPath);
        }

        CurrentSector = LoadSectorNow(SpaceMath.Sector.GetSectorIndex(Player.Position));

        if (CurrentSector == null)
            Debug.Error("No current sector");

        DropEffectManager.LoadDrops(Versioning.WorldFileLayout.DropsPath(WorldData.WorldFolderPath), Versioning.WorldFileLayout.LegacyDropsPath(WorldData.WorldFolderPath));
    }

    public void PreloadAroundPlayer()
    {
        CurrentSector = GetOrLoadSector(SpaceMath.Sector.GetSectorIndex(Player.Position));
        if (!Spacebox.Network.GameSession.IsRemoteClient)
            CurrentSector.PreloadAreaBlocking(Player.Position, Settings.ENTITY_SEARCH_RADIUS);
        UpdateSectors();
    }

    public StreamingWork CollectStreamingWork(Vector3 cameraPosition)
    {
        var work = new StreamingWork();
        foreach (var sector in loadedSectors.Values)
            sector.CollectStreamingWork(cameraPosition, ref work);
        return work;
    }

    public Sector GetOrLoadSector(Vector3i index)
    {
        return loadedSectors.TryGetValue(index, out var sector) ? sector : LoadSectorNow(index);
    }

    public static void LoadRemoteWorldInfo(Spacebox.Network.RemoteWorldInfo remote)
    {
        string folder = Spacebox.Network.MultiplayerFolders.WorldCache(Spacebox.Network.ServerFolderName.Current, remote.WorldName);
        Directory.CreateDirectory(folder);

        WorldData = new WorldLoader.LoadedWorld
        {
            WorldFolderPath = folder,
            Info = new WorldInfo
            {
                Name = remote.WorldName,
                Seed = remote.Seed,
                ModId = remote.ModId,
                GameMode = (Spacebox.Game.Player.GameModes.GameMode)remote.GameMode,
                Day = remote.Day,
                Ticks = remote.Ticks,
                FolderName = folder,
                ShowWelcomeWindow = false,
                IsLocalWorld = false,
                SpawnPoint = remote.SpawnPoint,
                GeneratorVersion = remote.GeneratorVersion
            }
        };

        Seed = SeedHelper.ComputeSeed(remote.Seed);
        GameTime.SetDay(remote.Day);
        GameTime.SetTick(remote.Ticks);
    }

    public static string MakeSafeFolderName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = (name ?? "World").ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            if (Array.IndexOf(invalid, chars[i]) >= 0) chars[i] = '_';
        }
        return new string(chars);
    }

    public bool TryGetSector(Vector3i index, out Sector sector)
    {
        return loadedSectors.TryGetValue(index, out sector);
    }

    public Dictionary<Vector3i, Sector>.ValueCollection LoadedSectors => loadedSectors.Values;

    public bool IsEntityLoading(long entityId)
    {
        foreach (var sector in loadedSectors.Values)
        {
            if (sector.IsLoadingEntity(entityId)) return true;
        }
        return false;
    }

    private readonly Dictionary<long, SpaceEntity> _entityLookup = new Dictionary<long, SpaceEntity>();

    public bool TryFindEntity(long entityId, out SpaceEntity entity)
    {
        if (_entityLookup.TryGetValue(entityId, out entity))
        {
            var cachedSector = entity.Sector;
            if (!entity.IsDisposed && entity.EntityID == entityId && cachedSector != null &&
                loadedSectors.TryGetValue(cachedSector.PositionIndex, out var loaded) && loaded == cachedSector)
                return true;
            _entityLookup.Remove(entityId);
        }

        if (!ScanForEntity(entityId, out entity)) return false;
        _entityLookup[entityId] = entity;
        return true;
    }

    private bool ScanForEntity(long entityId, out SpaceEntity entity)
    {
        foreach (var sector in loadedSectors.Values)
        {
            var entities = sector.Entities;
            if (entities == null) continue;

            for (int i = 0; i < entities.Count; i++)
            {
                if (entities[i].EntityID != entityId) continue;

                entity = entities[i];
                return true;
            }
        }

        entity = null;
        return false;
    }

    public static void LoadWorldInfo(string worldName)
    {
        WorldData = WorldLoader.LoadWorldByName(worldName);
        Versioning.WorldJsonToNbt.Convert(WorldData.WorldFolderPath);
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

        UpdateCurrentSector();

        foreach (var sector in loadedSectors)
        {
            sector.Value.Update();
        }

        _entityTracker.Process();
        WorldObservers.Update(Time.Delta);
        EntityLoadRequests.Update(Time.Delta);

        Explosions.Update(Time.Delta);
        Spacebox.Game.Generation.Blocks.BlockDamageVisuals.Instance.Update(Time.Delta);
        ThrusterEffects.Update(CurrentSector, Time.Delta);
        JetpackEffects.Update(Time.Delta);
    }

    public override void OnRender()
    {
        if (StreamingBudget.IsLoading) return;

        foreach (var sector in loadedSectors)
        {
            sector.Value.Render(material);
        }

        Explosions.Render();
        ThrusterEffects.Render();
        JetpackEffects.Render();
    }
    public bool IsBreathable(Vector3 worldPosition)
    {
        if (CurrentSector == null) return false;

        var entities = CurrentSector.Entities;
        for (int i = 0; i < entities.Count; i++)
        {
            var entity = entities[i];
            if (entity.Oxygen.GeneratorCount == 0) continue;

            var entitySpacePosition = entity.ToEntitySpace(worldPosition);
            if (!entity.IsPositionWithinEntitySize(entitySpacePosition)) continue;
            if (entity.Oxygen.IsOxygenated(entity.WorldPositionToLocal(entitySpacePosition))) return true;
        }

        return false;
    }

    public bool IsColliding(Vector3 pos, BoundingVolume volume, out CollideInfo collideInfo)
    {
        var sector = SectorAt(pos);
        if (sector == null)
        {
            collideInfo = new CollideInfo();
            return false;
        }
        return sector.IsColliding(pos, volume, out collideInfo);
    }

    public override void OnDetached()
    {
        base.OnDetached();
        GravityField.Clear();
        WorldObservers.Clear();
        EntityLoadRequests.Clear();
        Explosions?.Dispose();
        ThrusterEffects?.Dispose();
        JetpackEffects?.Dispose();
        Spacebox.Game.Player.Respawn.DnaReconstructorRegistry.Clear();



        WorldData = null;
        WorldGenerator = null;
        BiomeGenerator = null;
        DropEffectManager = null;
        DestructionManager = null;
        ProjectilesPool = null;

        CurrentSector?.Dispose();
        worldOctree = null;
        CurrentSector = null;
        Instance = null;

        foreach (var s in loadedSectors.Values)
        {
            s.Dispose();
        }

        WorldPersistenceManager.Dispose();
        WorldSaveState.Reset();
        loadedSectors.Clear();
        loadingSectors.Clear();
        _sectorsToRemoveCache.Clear();

        Instance = null;

    }

    private void UpdateSectors()
    {
        var cam = Camera.Main;
        if (cam == null || CurrentSector == null) return;

        var index = SpaceMath.Sector.GetSectorIndex(Player.Position);

        if (CurrentSector.PositionIndex != index)
        {
            CurrentSector = GetOrLoadSector(index);
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

        LoadObserverSectors();
    }

    private void LoadObserverSectors()
    {
        var observers = WorldObservers.Remote;
        for (int i = 0; i < observers.Count; i++)
        {
            var index = SpaceMath.Sector.GetSectorIndex(observers[i]);
            if (loadedSectors.ContainsKey(index) || loadingSectors.Contains(index)) continue;

            loadingSectors.Add(index);
            LoadSectorAsync(index);
        }
    }

    public static event Action<Sector> SectorLoaded;

    public void RefreshChunkVisuals(bool relight)
    {
        foreach (var sector in loadedSectors.Values)
        {
            var entities = sector.Entities;
            if (entities == null) continue;

            for (int e = 0; e < entities.Count; e++)
            {
                var chunks = entities[e].Chunks;
                for (int c = 0; c < chunks.Count; c++)
                {
                    if (chunks[c].IsDisposed) continue;
                    if (relight) chunks[c].CalculateLighting();
                    chunks[c].QueueMeshUpdate(UpdateReason.PlayerAction);
                }
            }
        }
    }

    public void OnEntityMoved(SpaceEntity entity)
    {
        _entityTracker.Request(entity);
    }

    public void RequestEntityCheck(SpaceEntity entity)
    {
        _entityTracker.Request(entity);
    }

    public void ForgetEntity(SpaceEntity entity)
    {
        _entityTracker.Forget(entity);
    }

    public Sector GetOrLoadSectorFor(Vector3 positionWorld)
    {
        return GetOrLoadSector(SpaceMath.Sector.GetSectorIndex(positionWorld));
    }

    private void UpdateCurrentSector()
    {
        if (CurrentSector == null || Player == null) return;

        var index = SpaceMath.Sector.GetSectorIndex(Player.Position);
        if (CurrentSector.PositionIndex != index) CurrentSector = GetOrLoadSector(index);
    }

    public static Sector SectorAt(Vector3 position)
    {
        var world = Instance;
        if (world != null)
        {
            foreach (var sector in world.loadedSectors.Values)
            {
                if (sector.BoundingBox.Contains(position)) return sector;
            }
        }
        return CurrentSector;
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
                && SpaceMath.World.DistanceToBox(cameraPosition, sector.BoundingBox) > Settings.SECTOR_UNLOAD_DISTANCE_SQUARED
                && !IsNeededByObserver(sector))
            {
                _sectorsToRemoveCache.Add(idx);
            }
        }
    }

    private static bool IsNeededByObserver(Sector sector)
    {
        var observers = WorldObservers.Remote;
        for (int i = 0; i < observers.Count; i++)
        {
            if (sector.BoundingBox.Contains(observers[i]) ||
                SpaceMath.World.DistanceToBox(observers[i], sector.BoundingBox) <= Settings.SECTOR_UNLOAD_DISTANCE_SQUARED)
                return true;
        }
        return false;
    }

    private void UnloadSector(Vector3i index)
    {
        if (loadedSectors.TryGetValue(index, out Sector sector))
        {
            worldOctree.Remove(sector, sector.BoundingBox);
            if (sector.IsModified && !Spacebox.Network.GameSession.IsRemoteClient)
                WorldPersistenceManager.CacheUnloadedSector(sector);
            _entityTracker.ForgetSector(sector);
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
                        if (Instance != this || loadedSectors.ContainsKey(sectorIndex))
                        {
                            sector.Dispose();
                            if (Instance == this) loadingSectors.Remove(sectorIndex);
                            return;
                        }

                        WorldPersistenceManager.ForgetCachedSector(sectorIndex);
                        worldOctree.Add(sector, sector.BoundingBox);
                        loadedSectors[sectorIndex] = sector;
                        loadingSectors.Remove(sectorIndex);
                        _entityTracker.OnSectorLoaded(sector);
                        SectorLoaded?.Invoke(sector);
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
                if (t.IsFaulted || t.IsCanceled)
                {

                    var realError = t.Exception?.Flatten().InnerException;

                    if (t.IsFaulted) Debug.Error($"[World] Failed to load sector {sectorIndex}: {realError?.Message}");
                    if (t.IsFaulted) Debug.Error(realError?.StackTrace);

                    MainThreadDispatcher.Instance.Enqueue(() =>
                    {
                        if (Instance == this)
                            loadingSectors.Remove(sectorIndex);
                    });
                }
            }, TaskScheduler.Default);
    }

    private Sector LoadSectorNow(Vector3i sectorIndex)
    {
        Vector3 sectorPosition = SpaceMath.Sector.GetSectorPosition(sectorIndex);
        var newSector = new Sector(sectorPosition, sectorIndex, Seed);
        WorldPersistenceManager.ForgetCachedSector(sectorIndex);

        worldOctree.Add(newSector, newSector.BoundingBox);
        loadedSectors.Add(newSector.PositionIndex, newSector);
        loadingSectors.Remove(sectorIndex);
        _entityTracker.OnSectorLoaded(newSector);
        SectorLoaded?.Invoke(newSector);

        return newSector;
    }
}