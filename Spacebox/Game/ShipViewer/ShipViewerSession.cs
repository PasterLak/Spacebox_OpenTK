using System.Diagnostics;
using Engine.Light;
using Spacebox.Editor.Preview;
using Spacebox.Game.Versioning;
using Spacebox.Game.GUI;
using Spacebox.Game.Player;
using Spacebox.Game.Resource;

namespace Spacebox.Game.ShipViewer;

public sealed class ShipViewerSession : IDisposable
{
    private const int WorkerWaitMilliseconds = 3000;
    private const float PictureScale = 2f;

    private readonly string _worldsDirectory;
    private readonly WorldUpgradeRegistry _registry;
    private readonly string _version;
    private readonly ReadOnlyShipLoader _loader;
    private readonly ShipViewerGameSet _gameSet = new ShipViewerGameSet();
    private readonly List<ShipPreviewContent> _contents = new List<ShipPreviewContent>();
    private readonly List<SavedWorld> _worlds = new List<SavedWorld>();
    private readonly Dictionary<SavedWorld, ShipScanResult> _shipsByWorld = new Dictionary<SavedWorld, ShipScanResult>();
    private readonly ShipWorldScanner _scanner = new ShipWorldScanner();
    private PreviewViewport _viewport;
    private SavedShip _requestedShip;
    private bool _gameSetLoadAnnounced;
    private bool _powered = true;
    private bool _enginesOn = true;
    private bool _autoFire;
    private string _projectileName;

    public ShipViewerSession(string worldsDirectory, WorldUpgradeRegistry registry, string version)
    {
        _worldsDirectory = worldsDirectory;
        _registry = registry;
        _version = version;
        _loader = new ReadOnlyShipLoader(registry, version, ReadOnlyShipLoader.DefaultScratchDirectory);
    }

    public IReadOnlyList<SavedWorld> Worlds => _worlds;
    public SavedWorld SelectedWorld { get; private set; }
    public SavedShip SelectedShip { get; private set; }
    public ShipScanResult Ships { get; private set; }
    public bool IsScanning => _scanner.IsRunning;
    public string Status { get; private set; } = "";
    public string GameSetNotice => _gameSet.Notice;
    public ShipPreviewContent Content => _viewport?.Content as ShipPreviewContent;
    public Spacebox.Editor.Preview.PreviewViewport Viewport => _viewport;
    public float AmbientLight { get; set; } = 1f;

    public bool Powered
    {
        get => _powered;
        set
        {
            _powered = value;
            Content?.SetPowered(value);
        }
    }

    public bool EnginesOn
    {
        get => _enginesOn;
        set
        {
            _enginesOn = value;
            if (Content != null) Content.EnginesOn = value;
        }
    }

    public bool AutoFire
    {
        get => _autoFire;
        set
        {
            _autoFire = value;
            if (Content != null) Content.AutoFire = value;
        }
    }

    public string ProjectileName
    {
        get => _projectileName;
        set
        {
            _projectileName = value;
            if (Content != null) Content.ShotProjectile = ResolveProjectile();
        }
    }

    public void RestoreShip()
    {
        Content?.Restore();
    }

    public void FireWeapons()
    {
        Content?.FireWeapons();
    }

    private ShipExportJob _export;

    public bool IsExporting => _export != null && !_export.IsDone;

    public void Export(string name, string author, string description)
    {
        if (SelectedWorld == null || SelectedShip == null || IsExporting) return;

        try
        {
            var ship = ShipExportJob.Create(_loader.Load(SelectedWorld, SelectedShip), SelectedWorld, name, author, description);
            string path = Spacebox.Game.Ships.Sharing.ShipFileNames.UniquePath(Spacebox.Game.Ships.Sharing.ShipFileNames.Folder, ship.Id);
            string picture = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"spacebox_ship_{Guid.NewGuid():N}.png");
            _export = new ShipExportJob(ship, path, picture);
            _viewport?.RequestCapture(picture, 1f);
            Status = "Exporting...";
        }
        catch (Exception ex)
        {
            Status = "Export failed: " + ex.Message;
        }
    }

    private void UpdateExport()
    {
        if (_export == null || _export.IsDone) return;
        _export.Update();
        if (_export.IsDone) Status = _export.Error == null ? $"Exported to Ships/{System.IO.Path.GetFileName(_export.Path)}" : "Export failed: " + _export.Error;
    }

    public void SavePicture()
    {
        _viewport?.RequestCapture(Engine.FramebufferCapture.CreateScreenshotPath("ship"), PictureScale);
    }

    public void Open()
    {
        _viewport ??= new PreviewViewport { Background = ShipViewerBackdrop.Draw };
        Refresh();
    }

    public void Refresh()
    {
        SelectWorld(null);
        _worlds.Clear();
        _shipsByWorld.Clear();
        _scanner.Start(SavedWorldCatalog.Scan(_worldsDirectory, _registry, _version));
    }

    public void SelectWorld(SavedWorld world)
    {
        if (ReferenceEquals(world, SelectedWorld)) return;

        ClearShip();
        SelectedWorld = world;
        Ships = null;
        Status = "";
        if (world == null || !_shipsByWorld.TryGetValue(world, out var ships)) return;

        Ships = ships;
        if (ships.Ships.Count > 0) SelectShip(ships.Ships[0]);
    }

    public void SelectShip(SavedShip ship)
    {
        if (ReferenceEquals(ship, SelectedShip)) return;

        SelectedShip = ship;
        _requestedShip = ship;
        _gameSetLoadAnnounced = false;
    }

    public void Update(float deltaTime)
    {
        CollectScannedWorlds();
        StartRequestedShip();
        _viewport?.Update(deltaTime);
        UpdateExport();
        ForgetIdleContents();
    }

    public void Render()
    {
        if (_viewport == null) return;

        var ambient = Lighting.AmbientColor;
        int fog = Lighting.FogDensity;
        Lighting.AmbientColor = new OpenTK.Mathematics.Vector3(AmbientLight);
        Lighting.FogDensity = 0;
        try
        {
            _viewport.Render();
        }
        finally
        {
            Lighting.AmbientColor = ambient;
            Lighting.FogDensity = fog;
        }
    }

    public void DrawViewport(System.Numerics.Vector2 size)
    {
        _viewport?.Draw(size);
    }

    public void Close()
    {
        _scanner.Cancel();
        ClearShip();
        SelectedWorld = null;
        Ships = null;
        Status = "";
        WaitForWorkers();
        _gameSet.Release();
    }

    public void Dispose()
    {
        Close();
        _scanner.Dispose();
        _viewport?.Dispose();
        _viewport = null;
    }

    private void CollectScannedWorlds()
    {
        while (_scanner.TryTake(out var world, out var ships))
        {
            _worlds.Add(world);
            _shipsByWorld[world] = ships;
            if (SelectedWorld == null) SelectWorld(world);
        }
    }

    private void StartRequestedShip()
    {
        if (_requestedShip == null || SelectedWorld == null || _viewport == null) return;

        var world = SelectedWorld;
        if (!_gameSet.IsLoadedFor(world.ModId, WorldGameSetCheck.ExtensionsOf(world.Info)))
        {
            if (!_gameSetLoadAnnounced)
            {
                Status = $"Loading GameSet '{world.ModId}'...";
                _gameSetLoadAnnounced = true;
                return;
            }

            _viewport.Clear();
            WaitForWorkers();
            if (!_gameSet.Load(world.ModId, WorldGameSetCheck.ExtensionsOf(world.Info)))
            {
                Status = _gameSet.Notice;
                _requestedShip = null;
                return;
            }
        }

        var ship = _requestedShip;
        _requestedShip = null;
        Status = "";
        _viewport.SetContent(ship.FilePath, _gameSet.Version, () => Track(CreateContent(world, ship)));
    }

    private ShipPreviewContent CreateContent(SavedWorld world, SavedShip ship)
    {
        var content = new ShipPreviewContent(() => _loader.Load(world, ship), ShipFraming.FromStats(ship.Stats)) { EnginesOn = _enginesOn, AutoFire = _autoFire, ShotProjectile = ResolveProjectile() };
        content.SetPowered(_powered);
        return content;
    }

    private ProjectileParameters? ResolveProjectile()
    {
        return _projectileName != null && GameAssets.TryGetProjectileByName(_projectileName, out var projectile) ? projectile : null;
    }

    private ShipPreviewContent Track(ShipPreviewContent content)
    {
        _contents.Add(content);
        return content;
    }

    private void ForgetIdleContents()
    {
        var current = Content;
        for (int i = _contents.Count - 1; i >= 0; i--)
        {
            if (!_contents[i].IsBusy && !ReferenceEquals(_contents[i], current)) _contents.RemoveAt(i);
        }
    }

    private void ClearShip()
    {
        SelectedShip = null;
        _requestedShip = null;
        _viewport?.Clear();
    }

    private void WaitForWorkers()
    {
        var timer = Stopwatch.StartNew();
        for (int i = 0; i < _contents.Count; i++)
        {
            while (_contents[i].IsBusy && timer.ElapsedMilliseconds < WorkerWaitMilliseconds) Thread.Sleep(2);
        }

        if (timer.ElapsedMilliseconds >= WorkerWaitMilliseconds) Engine.Debug.Warning("[ShipViewer] Background ship loading did not stop in time");
        _contents.Clear();
    }
}
