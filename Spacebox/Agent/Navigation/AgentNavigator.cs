using Engine;
using Engine.AI;
using OpenTK.Mathematics;
using Spacebox.Agent.Modules;
using Spacebox.Game.Generation;
using Spacebox.Game.Mobs.Navigation;
using Spacebox.Game.Player;

namespace Spacebox.Agent.Navigation;

public sealed class AgentNavigator
{
    private const int MaxNodes = 60000;
    private const float ArriveDistance = 0.05f;

    private readonly VoxelPathfinder _pathfinder = new VoxelPathfinder();
    private readonly VoxelPath _path = new VoxelPath();
    private readonly List<Vector3> _points = new List<Vector3>();
    private MobNavigationGrid _grid;
    private LocalAstronaut _player;
    private bool _wasFrozen;
    private int _next;
    private float _speed;
    private Vector3 _goal;
    private bool _partial;

    public string State { get; private set; } = "idle";
    public string Message { get; private set; } = "";
    public bool IsMoving => State == "moving";

    public object Status => new
    {
        state = State,
        message = Message,
        goal = AgentGame.Vec(_goal),
        waypoint = _next,
        waypoints = _points.Count,
        remaining = _player == null ? 0f : AgentGame.Round(Remaining())
    };

    public void Start(LocalAstronaut player, Vector3 goal, float speed, bool face)
    {
        Stop("replaced");
        _player = player;
        _goal = goal;
        _speed = Math.Max(0.5f, speed);
        _points.Clear();
        _next = 0;

        var status = Plan(player.Position, goal);
        if (status == PathStatus.Failed)
        {
            State = "failed";
            Message = "No path found (start or goal blocked, or too far for the search budget)";
            return;
        }

        _partial = status == PathStatus.Partial;
        Message = _partial ? "Goal not reachable (solid or out of budget); moving to the nearest reachable cell" : $"Path with {_points.Count} waypoints";
        if (face) LookModule.Face(player, (goal - player.Position).LengthSquared > 1e-6f ? (goal - player.Position).Normalized() : player.Front);

        _wasFrozen = player.IsFrozen;
        player.IsFrozen = true;
        player.InertiaController.Reset();
        State = "moving";
    }

    public void Stop(string reason)
    {
        if (!IsMoving) return;
        Finish("stopped", reason);
    }

    public void Tick()
    {
        if (!IsMoving) return;
        if (_player == null || World.Instance?.Player != _player)
        {
            State = "stopped";
            Message = "The world was unloaded";
            _player = null;
            return;
        }

        float step = _speed * Time.Delta;
        var position = _player.Position;
        while (step > 0f && _next < _points.Count)
        {
            var target = ToWorld(_points[_next]);
            var delta = target - position;
            float distance = delta.Length;
            if (distance <= step || distance <= ArriveDistance)
            {
                position = target;
                step -= distance;
                _next++;
                continue;
            }

            position += delta / distance * step;
            step = 0f;
        }

        _player.SetPosition(position);
        _player.InertiaController.Reset();

        if (_next >= _points.Count) Finish(_partial ? "partial" : "arrived", Message);
    }

    private PathStatus Plan(Vector3 start, Vector3 goal)
    {
        _grid = new MobNavigationGrid();
        _grid.Bind(goal);
        if (_grid.Anchor == null) _grid.Bind(start);
        if (_grid.Anchor == null)
        {
            _grid = null;
            _points.Add(goal);
            return PathStatus.Found;
        }

        var status = _pathfinder.FindPath(_grid, _grid.ToGrid(start), _grid.ToGrid(goal), _path, MaxNodes);
        for (int i = 0; i < _path.Count; i++) _points.Add(_path[i]);
        return status;
    }

    private Vector3 ToWorld(Vector3 point) => _grid != null ? _grid.ToWorld(point) : point;

    private float Remaining()
    {
        if (_next >= _points.Count) return 0f;
        float total = Vector3.Distance(_player.Position, ToWorld(_points[_next]));
        for (int i = _next + 1; i < _points.Count; i++) total += Vector3.Distance(ToWorld(_points[i - 1]), ToWorld(_points[i]));
        return total;
    }

    private void Finish(string state, string message)
    {
        State = state;
        Message = message;
        if (_player != null)
        {
            _player.IsFrozen = _wasFrozen;
            _player.InertiaController.Reset();
        }
    }
}
