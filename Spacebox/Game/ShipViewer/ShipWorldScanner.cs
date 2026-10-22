using System.Collections.Concurrent;
using Engine;
using Engine.Multithreading;

namespace Spacebox.Game.ShipViewer;

public sealed class ShipWorldScanner : IDisposable
{
    private readonly ConcurrentQueue<(SavedWorld World, ShipScanResult Ships)> _found = new ConcurrentQueue<(SavedWorld, ShipScanResult)>();
    private CancellationTokenSource _cancel;
    private Task _task;

    public bool IsRunning => _task != null && !_task.IsCompleted;

    public void Start(IReadOnlyList<SavedWorld> worlds)
    {
        Cancel();
        _cancel = new CancellationTokenSource();
        var token = _cancel.Token;
        var list = new List<SavedWorld>(worlds);
        _task = WorkerPoolManager.Enqueue(_ => Run(list, token), WorkerPoolManager.Priority.Medium);
    }

    public bool TryTake(out SavedWorld world, out ShipScanResult ships)
    {
        if (_found.TryDequeue(out var entry))
        {
            world = entry.World;
            ships = entry.Ships;
            return true;
        }

        world = null;
        ships = null;
        return false;
    }

    public void Cancel()
    {
        _cancel?.Cancel();
        _cancel?.Dispose();
        _cancel = null;
        _task = null;
        _found.Clear();
    }

    public void Dispose()
    {
        Cancel();
    }

    private void Run(List<SavedWorld> worlds, CancellationToken token)
    {
        for (int i = 0; i < worlds.Count; i++)
        {
            if (token.IsCancellationRequested) return;

            var world = worlds[i];
            if (!world.CanBeViewed) continue;

            try
            {
                var parts = ShipPartBlocks.Load(Spacebox.Network.GameSetLocator.Local, world.Info.ModId, Spacebox.Game.GUI.WorldGameSetCheck.ExtensionsOf(world.Info));
                var ships = ShipScanner.Scan(world.FolderPath, parts == null ? null : parts.Contains, token);
                if (!token.IsCancellationRequested && ships.Ships.Count > 0) _found.Enqueue((world, ships));
            }
            catch (Exception ex)
            {
                Debug.Warning($"[ShipViewer] Ships of world '{world.Name}' could not be read: {ex.Message}");
            }
        }
    }
}
