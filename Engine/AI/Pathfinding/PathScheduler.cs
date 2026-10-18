using OpenTK.Mathematics;

namespace Engine.AI;

public sealed class PathScheduler
{
    private readonly VoxelPathfinder _pathfinder = new VoxelPathfinder();
    private readonly Queue<PathRequest> _queue = new Queue<PathRequest>();

    public int NodeBudgetPerUpdate { get; set; } = 8192;
    public int PendingCount => _queue.Count;
    public VoxelPathfinder Pathfinder => _pathfinder;

    public void Request(PathRequest request, INavigationGrid grid, Vector3 start, Vector3 goal)
    {
        request.Grid = grid;
        request.Start = start;
        request.Goal = goal;
        request.Status = PathStatus.None;

        if (request.IsPending) return;

        request.IsPending = true;
        _queue.Enqueue(request);
    }

    public void Cancel(PathRequest request)
    {
        request.IsPending = false;
        request.Status = PathStatus.None;
    }

    public void Update()
    {
        int budget = NodeBudgetPerUpdate;

        while (budget > 0 && _queue.Count > 0)
        {
            var request = _queue.Dequeue();
            if (!request.IsPending || request.Grid == null) continue;

            request.IsPending = false;
            request.Status = _pathfinder.FindPath(request.Grid, request.Start, request.Goal, request.Path, request.MaxNodes);
            request.Version++;
            budget -= Math.Max(1, _pathfinder.LastExpandedNodes);
        }
    }

    public void Clear()
    {
        while (_queue.Count > 0) _queue.Dequeue().IsPending = false;
    }
}
