using Engine.AI;
using OpenTK.Mathematics;
using Spacebox.Game.Mobs.Navigation;
using Spacebox.Game.Physics;

namespace Spacebox.Game.Mobs;

public sealed class MobLocomotion
{
    private const float WaypointRadius = 0.4f;
    private const float RepathInterval = 0.75f;
    private const float RepathGoalShift = 1f;
    private const float RebindInterval = 2f;
    private const float StuckCheckInterval = 1.5f;
    private const float StuckDistance = 0.3f;
    private const float Acceleration = 6f;
    private const float SlowdownDistance = 1.2f;

    private readonly Mob _mob;
    private readonly SphereBody _body;
    private readonly MobNavigationGrid _grid;
    private readonly PathRequest _request = new PathRequest();
    private readonly List<Vector3> _waypoints = new List<Vector3>(32);

    private PathScheduler? _scheduler;
    private int _waypointIndex;
    private int _handledVersion;
    private bool _hasGoal;
    private bool _isPartial;
    private Vector3 _goal;
    private Vector3 _requestedGoal;
    private float _speed;
    private float _arriveDistance;
    private float _repathTimer;
    private float _rebindTimer;
    private float _stuckTimer;
    private Vector3 _stuckCheckPosition;

    public MobLocomotion(Mob mob, SphereBody body, MobNavigationGrid grid)
    {
        _mob = mob;
        _body = body;
        _grid = grid;
    }

    public bool HasArrived { get; private set; }
    public bool IsUnreachable { get; private set; }
    public bool IsStuck { get; private set; }
    public bool IsMoving => _hasGoal && !HasArrived;
    public Vector3 Goal => _goal;
    public Vector3 Velocity => _body.Velocity;

    public void Attach(PathScheduler scheduler)
    {
        _scheduler = scheduler;
    }

    public void MoveTo(Vector3 goal, float speed, float arriveDistance)
    {
        bool newGoal = !_hasGoal || Vector3.DistanceSquared(goal, _goal) > RepathGoalShift * RepathGoalShift;

        _goal = goal;
        _speed = speed;
        _arriveDistance = arriveDistance;

        if (!newGoal && _hasGoal) return;

        _hasGoal = true;
        HasArrived = false;
        IsUnreachable = false;
        IsStuck = false;
        _stuckTimer = 0f;
        _stuckCheckPosition = _mob.Position;
        RequestPath();
    }

    public void Stop()
    {
        _hasGoal = false;
        HasArrived = false;
        IsUnreachable = false;
        IsStuck = false;
        _waypoints.Clear();
        _scheduler?.Cancel(_request);
    }

    public void Update(float deltaTime)
    {
        UpdateRebind(deltaTime);
        ConsumePathResult();

        if (!_hasGoal || HasArrived)
        {
            Steer(Vector3.Zero, deltaTime);
            return;
        }

        if (Vector3.DistanceSquared(_mob.Position, _goal) <= _arriveDistance * _arriveDistance)
        {
            HasArrived = true;
            _waypoints.Clear();
            Steer(Vector3.Zero, deltaTime);
            return;
        }

        UpdateRepath(deltaTime);
        UpdateStuck(deltaTime);
        Follow(deltaTime);
    }

    private void Follow(float deltaTime)
    {
        if (_waypointIndex >= _waypoints.Count)
        {
            if (_isPartial && _waypoints.Count > 0) IsUnreachable = true;
            Steer(Vector3.Zero, deltaTime);
            return;
        }

        var target = _waypoints[_waypointIndex];
        var offset = target - _mob.Position;
        float distance = offset.Length;

        if (distance <= WaypointRadius)
        {
            _waypointIndex++;
            Steer(Vector3.Zero, deltaTime);
            return;
        }

        bool isLast = _waypointIndex == _waypoints.Count - 1;
        float speed = isLast ? _speed * MathF.Min(1f, distance / SlowdownDistance) : _speed;
        var direction = offset / distance;

        Steer(direction * speed, deltaTime);
        _mob.SetMoveDirection(direction);
    }

    private void Steer(Vector3 desiredVelocity, float deltaTime)
    {
        if (_body.IsKinematic) return;

        float t = 1f - MathF.Exp(-Acceleration * deltaTime);
        _body.Velocity = Vector3.Lerp(_body.Velocity, desiredVelocity, t);
    }

    private void UpdateRepath(float deltaTime)
    {
        _repathTimer -= deltaTime;
        if (_repathTimer > 0f || _request.IsPending) return;
        if (Vector3.DistanceSquared(_goal, _requestedGoal) < 0.25f && _waypointIndex < _waypoints.Count) return;

        RequestPath();
    }

    private void UpdateStuck(float deltaTime)
    {
        _stuckTimer += deltaTime;
        if (_stuckTimer < StuckCheckInterval) return;

        IsStuck = Vector3.DistanceSquared(_mob.Position, _stuckCheckPosition) < StuckDistance * StuckDistance;
        _stuckTimer = 0f;
        _stuckCheckPosition = _mob.Position;
        if (IsStuck) RequestPath();
    }

    private void UpdateRebind(float deltaTime)
    {
        _rebindTimer -= deltaTime;
        if (_rebindTimer > 0f || _request.IsPending) return;

        _rebindTimer = RebindInterval;
        _grid.Bind(_mob.Position);
    }

    private void RequestPath()
    {
        _repathTimer = RepathInterval;
        _requestedGoal = _goal;

        if (_scheduler == null)
        {
            SetDirectPath();
            return;
        }

        _scheduler.Request(_request, _grid, _grid.ToGrid(_mob.Position), _grid.ToGrid(_goal));
    }

    private void ConsumePathResult()
    {
        if (_request.IsPending || _request.Version == _handledVersion) return;
        _handledVersion = _request.Version;
        if (!_hasGoal) return;

        _waypoints.Clear();
        _waypointIndex = 0;
        _isPartial = _request.Status == PathStatus.Partial;
        IsUnreachable = _request.Status == PathStatus.Failed;
        if (IsUnreachable) return;

        var path = _request.Path;
        for (int i = 0; i < path.Count; i++) _waypoints.Add(_grid.ToWorld(path[i]));
    }

    private void SetDirectPath()
    {
        _waypoints.Clear();
        _waypoints.Add(_goal);
        _waypointIndex = 0;
        _isPartial = false;
    }
}
