using System.Diagnostics;
using Engine.Light;
using Engine.Multithreading;
using Spacebox.Editor.Preview;
using Spacebox.Game.Versioning;
using Spacebox.Game.GUI;

namespace Spacebox.Game.ShipViewer;

public sealed class ShipViewerSession : IDisposable
{
    private const int WorkerWaitMilliseconds = 3000;
    private static readonly OpenTK.Mathematics.Vector3 PreviewAmbient = new OpenTK.Mathematics.Vector3(1f, 1f, 1f);

    private readonly string _worldsDirectory;
    private readonly WorldUpgradeRegistry _registry;
    private readonly string _version;
    private readonly ReadOnlyShipLoader _loader;
    private readonly ShipViewerGameSet _gameSet = new ShipViewerGameSet();
    private readonly List<ShipPreviewContent> _contents = new List<ShipPreviewContent>();
    private List<SavedWorld> _worlds = new List<SavedWorld>();
    private PreviewViewport _viewport;
    private Task<ShipScanResult> _scan;
    private CancellationTokenSource _scanCancel;
    private SavedShip _requestedShip;
    private bool _gameSetLoadAnnounced;

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
    public bool IsScanning => _scan != null;
    public string Status { get; private set; } = "";
    public string GameSetNotice => _gameSet.Notice;
    public ShipPreviewContent Content => _viewport?.Content as ShipPreviewContent;

    public void Open()
    {
        _viewport ??= new PreviewViewport { Background = ShipViewerBackdrop.Draw };
        Refresh();
    }

    public void Refresh()
    {
        SelectWorld(null);
        _worlds = SavedWorldCatalog.Scan(_worldsDirectory, _registry, _version);
    }

    public void SelectWorld(SavedWorld world)
    {
        if (ReferenceEquals(world, SelectedWorld)) return;

        CancelScan();
        ClearShip();
        SelectedWorld = world;
        Ships = null;
        Status = "";
        if (world == null || !world.CanBeViewed) return;

        _scanCancel = new CancellationTokenSource();
        var token = _scanCancel.Token;
        string folder = world.FolderPath;
        _scan = WorkerPoolManager.Enqueue(_ => ShipScanner.Scan(folder, token), WorkerPoolManager.Priority.Medium);
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
        PollScan();
        StartRequestedShip();
        _viewport?.Update(deltaTime);
        ForgetIdleContents();
    }

    public void Render()
    {
        if (_viewport == null) return;

        var ambient = Lighting.AmbientColor;
        int fog = Lighting.FogDensity;
        Lighting.AmbientColor = PreviewAmbient;
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
        CancelScan();
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
        _viewport?.Dispose();
        _viewport = null;
    }

    private void PollScan()
    {
        if (_scan == null || !_scan.IsCompleted) return;

        if (_scan.IsCompletedSuccessfully) Ships = _scan.Result;
        else Status = "Ships could not be read: " + (_scan.Exception?.GetBaseException().Message ?? "cancelled");
        _scan = null;
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
        _viewport.SetContent(ship.FilePath, _gameSet.Version, () => Track(new ShipPreviewContent(() => _loader.Load(world, ship), ShipFraming.FromStats(ship.Stats))));
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

    private void CancelScan()
    {
        _scanCancel?.Cancel();
        _scanCancel?.Dispose();
        _scanCancel = null;
        _scan = null;
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
