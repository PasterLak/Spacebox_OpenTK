using Engine;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.Physics;

namespace Spacebox.Game.Player;

public sealed class GravityWalker
{
    public const float EyeHeight = 1.5f;
    public const float CapsuleHeight = 1.6f;
    public const float CapsuleRadius = 0.3f;

    private const float WalkSpeed = 4.5f;
    private const float RunSpeed = 7f;
    private const float JumpHeight = 1.25f;
    private const float MinJumpHeight = 1.2f;
    private const float ReferenceGravity = 9.81f;
    private static readonly float JumpSpeed = MathF.Sqrt(2f * ReferenceGravity * JumpHeight);
    private const float GroundAcceleration = 40f;
    private const float AirAcceleration = 6f;
    private const float GroundAlignSpeed = 10f;
    private const float AirAlignSpeed = 1f;
    private const float MaxFallSpeed = 60f;
    private const float MaxStep = 0.2f;
    private const float GroundProbe = 0.05f;
    private const float MouseSensitivity = 0.002f;
    private const float EntryLiftStep = 0.1f;
    private const float MinUpDot = 0.02f;
    private const float MaxAlignFrontDot = 0.98f;
    private const float UprightRollTolerance = 0.05f;

    private Vector3 _velocity;
    private Vector3 _up = Vector3.UnitY;
    private bool _isGrounded;
    private SpaceEntity _entity;
    private Vector3 _localPosition;
    private Vector3 _lastWorldPosition;
    private int _upAxis = 1;
    private float _upSign = 1f;
    private float _peakFallSpeed;
    private bool _isAirborne;

    public event Action<float, Vector3>? Landed;

    private static readonly float[] CapsuleSamples = { CapsuleRadius, CapsuleHeight * 0.5f, CapsuleHeight - CapsuleRadius };

    public bool IsActive { get; private set; }
    public bool IsGrounded => _isGrounded;
    public Vector3 Velocity => _velocity;

    public void Enter(LocalAstronaut player, Vector3 gravity)
    {
        IsActive = true;
        _up = -gravity.Normalized();
        _velocity = player.InertiaController.Velocity;
        _isGrounded = false;
        _isAirborne = false;
        _peakFallSpeed = 0f;
        LiftOutOfGeometry(player);
    }

    public void AddImpulse(Vector3 impulse)
    {
        _velocity += impulse;
        if (Vector3.Dot(impulse, _up) > 0f) _isGrounded = false;
    }

    public void Exit(LocalAstronaut player)
    {
        IsActive = false;
        _isGrounded = false;
        _entity = null;
        player.InertiaController.Velocity = _velocity;
    }

    public void Update(LocalAstronaut player, Vector3 gravity, bool canMove)
    {
        float deltaTime = Time.Delta;
        if (deltaTime <= 0f) return;

        _up = -gravity.Normalized();
        SyncLocal(player);
        _isGrounded = ProbeGround();

        Align(player, deltaTime);

        var vertical = _up * Vector3.Dot(_velocity, _up);
        var horizontal = _velocity - vertical;

        var wish = canMove ? GetWishDirection(player) : Vector3.Zero;
        float speed = canMove && Input.IsAction("sprint") ? RunSpeed : WalkSpeed;
        float acceleration = _isGrounded ? GroundAcceleration : AirAcceleration;
        if (_isGrounded || wish != Vector3.Zero)
            horizontal = MoveTowards(horizontal, wish * speed, acceleration * deltaTime);

        if (_isGrounded && canMove && Input.IsActionDown("up"))
        {
            vertical = _up * MathF.Max(JumpSpeed, MathF.Sqrt(2f * gravity.Length * MinJumpHeight));
            _isGrounded = false;
        }
        else
        {
            vertical += gravity * deltaTime;
            float fallSpeed = -Vector3.Dot(vertical, _up);
            if (fallSpeed > MaxFallSpeed) vertical = -_up * MaxFallSpeed;
        }

        _velocity = horizontal + vertical;
        _peakFallSpeed = MathF.Max(_peakFallSpeed, -Vector3.Dot(_velocity, _up));
        Move(player, _velocity * deltaTime);
        TrackLanding(gravity);
        player.CameraSway.Update(0f);
        player.OnMoved?.Invoke(player);
    }

    private void TrackLanding(Vector3 gravity)
    {
        if (!_isGrounded)
        {
            _isAirborne = true;
            return;
        }

        if (_isAirborne && _peakFallSpeed > 0f) Landed?.Invoke(_peakFallSpeed, gravity);
        _isAirborne = false;
        _peakFallSpeed = 0f;
    }

    public void Look(LocalAstronaut player)
    {
        var rotation = player.GetRotation();
        bool upright = IsUpright(rotation);
        var reference = upright ? _up : Vector3.Transform(Vector3.UnitY, rotation);

        var delta = Input.Mouse.Delta;
        float sensitivity = MouseSensitivity * Camera360Base.MouseSensitivity;
        rotation = Quaternion.FromAxisAngle(reference, -delta.X * sensitivity) * rotation;

        var right = Vector3.Transform(Vector3.UnitX, rotation);
        var pitched = Quaternion.FromAxisAngle(right, -Camera360Base.LookDeltaY(delta.Y) * sensitivity) * rotation;
        if (!upright || Vector3.Dot(Vector3.Transform(Vector3.UnitY, pitched), _up) > MinUpDot)
            rotation = pitched;

        if (upright)
            rotation = LookRotation(Vector3.Transform(-Vector3.UnitZ, rotation), _up);

        player.SetRotation(rotation);
    }

    public static Quaternion LookRotation(Vector3 front, Vector3 upHint)
    {
        var back = (-front).Normalized();
        var right = Vector3.Cross(upHint, back);
        if (right.LengthSquared < 1e-8f) right = Vector3.Cross(Vector3.UnitX, back);
        right.Normalize();
        var up = Vector3.Cross(back, right);

        var basis = new Matrix3(
            right.X, up.X, back.X,
            right.Y, up.Y, back.Y,
            right.Z, up.Z, back.Z);
        return Quaternion.FromMatrix(basis).Normalized();
    }

    private void Align(LocalAstronaut player, float deltaTime)
    {
        var rotation = player.GetRotation();
        if (IsUpright(rotation)) return;

        var front = Vector3.Transform(-Vector3.UnitZ, rotation);
        if (MathF.Abs(Vector3.Dot(front, _up)) > MaxAlignFrontDot) return;

        var target = LookRotation(front, _up);
        float dot = MathF.Abs(rotation.X * target.X + rotation.Y * target.Y + rotation.Z * target.Z + rotation.W * target.W);
        float angle = 2f * MathF.Acos(Math.Clamp(dot, -1f, 1f));
        float maxStep = (_isGrounded ? GroundAlignSpeed : AirAlignSpeed) * deltaTime;

        player.SetRotation(angle <= maxStep ? target : Quaternion.Slerp(rotation, target, maxStep / angle));
    }

    private bool IsUpright(Quaternion rotation)
    {
        var right = Vector3.Transform(Vector3.UnitX, rotation);
        var up = Vector3.Transform(Vector3.UnitY, rotation);
        return MathF.Abs(Vector3.Dot(right, _up)) < UprightRollTolerance && Vector3.Dot(up, _up) > 0f;
    }

    private Vector3 GetWishDirection(LocalAstronaut player)
    {
        var forward = Flatten(player.Front);
        var right = Flatten(player.Right);
        var wish = Vector3.Zero;

        if (Input.IsAction("forward")) wish += forward;
        if (Input.IsAction("backward")) wish -= forward;
        if (Input.IsAction("right")) wish += right;
        if (Input.IsAction("left")) wish -= right;

        return wish.LengthSquared > 1e-6f ? wish.Normalized() : Vector3.Zero;
    }

    private Vector3 Flatten(Vector3 direction)
    {
        var flat = direction - _up * Vector3.Dot(direction, _up);
        return flat.LengthSquared > 1e-6f ? flat.Normalized() : Vector3.Zero;
    }

    private void SyncLocal(LocalAstronaut player)
    {
        SpaceEntity entity = null;
        World.CurrentSector?.TryGetNearestEntity(player.Position, out entity);

        if (entity != _entity || player.Position != _lastWorldPosition)
            _localPosition = entity != null ? entity.WorldToEntityLocal(player.Position) : Vector3.Zero;
        _entity = entity;
        if (_entity == null) return;

        var localUp = _entity.DirectionToEntitySpace(_up);
        var abs = new Vector3(MathF.Abs(localUp.X), MathF.Abs(localUp.Y), MathF.Abs(localUp.Z));
        _upAxis = abs.X >= abs.Y && abs.X >= abs.Z ? 0 : abs.Y >= abs.Z ? 1 : 2;
        _upSign = localUp[_upAxis] >= 0f ? 1f : -1f;
    }

    private void ApplyLocal(LocalAstronaut player)
    {
        player.Position = _entity.EntityLocalToWorld(_localPosition);
        _lastWorldPosition = player.Position;
    }

    private bool ProbeGround()
    {
        if (_entity == null) return false;
        GetBox(_localPosition, out var min, out var max);
        float moved = EntityBoxCollider.Sweep(_entity, min, max, _upAxis, -_upSign * GroundProbe);
        return MathF.Abs(moved) < GroundProbe - 1e-5f;
    }

    private void Move(LocalAstronaut player, Vector3 delta)
    {
        if (_entity == null)
        {
            player.Position += delta;
            return;
        }

        var localDelta = _entity.DirectionToEntitySpace(delta);
        var localVelocity = _entity.DirectionToEntitySpace(_velocity);

        MoveAlong(player, _upAxis, localDelta[_upAxis], ref localVelocity, true);
        MoveAlong(player, (_upAxis + 1) % 3, localDelta[(_upAxis + 1) % 3], ref localVelocity, false);
        MoveAlong(player, (_upAxis + 2) % 3, localDelta[(_upAxis + 2) % 3], ref localVelocity, false);

        _velocity = _entity.DirectionToWorldSpace(localVelocity);
        ApplyLocal(player);
    }

    private void MoveAlong(LocalAstronaut player, int axis, float distance, ref Vector3 localVelocity, bool isVertical)
    {
        if (MathF.Abs(distance) < 1e-6f) return;

        GetBox(_localPosition, out var min, out var max);
        float allowed = EntityBoxCollider.Sweep(_entity, min, max, axis, distance);

        var candidate = _localPosition;
        candidate[axis] += allowed;
        if (allowed != 0f && OverlapsDynamic(player, candidate)) allowed = 0f;
        else _localPosition = candidate;

        if (MathF.Abs(allowed) >= MathF.Abs(distance) - 1e-6f) return;

        localVelocity[axis] = 0f;
        if (isVertical && distance * _upSign < 0f) _isGrounded = true;
    }

    private void LiftOutOfGeometry(LocalAstronaut player)
    {
        SyncLocal(player);
        if (_entity == null) return;

        GetBox(_localPosition, out var min, out var max);
        if (!EntityBoxCollider.Overlaps(_entity, min, max)) return;

        var start = _localPosition;
        for (float lift = EntryLiftStep; lift <= CapsuleHeight; lift += EntryLiftStep)
        {
            var candidate = start;
            candidate[_upAxis] += _upSign * lift;
            GetBox(candidate, out min, out max);
            if (EntityBoxCollider.Overlaps(_entity, min, max)) continue;

            _localPosition = candidate;
            ApplyLocal(player);
            return;
        }
    }

    private void GetBox(Vector3 eyeLocal, out Vector3 min, out Vector3 max)
    {
        min = eyeLocal - new Vector3(CapsuleRadius);
        max = eyeLocal + new Vector3(CapsuleRadius);

        float feet = eyeLocal[_upAxis] - _upSign * EyeHeight;
        float low = _upSign > 0f ? feet : feet - CapsuleHeight;
        min[_upAxis] = low;
        max[_upAxis] = low + CapsuleHeight;
    }

    private bool OverlapsDynamic(LocalAstronaut player, Vector3 eyeLocal)
    {
        var localUp = Vector3.Zero;
        localUp[_upAxis] = _upSign;
        var feetLocal = eyeLocal - localUp * EyeHeight;
        var physics = World.Instance?.Physics;

        for (int i = 0; i < CapsuleSamples.Length; i++)
        {
            var center = feetLocal + localUp * CapsuleSamples[i];
            if (_entity.Doors.IntersectsMovingDoor(center, CapsuleRadius)) return true;
            if (physics != null && physics.OverlapsSolidBody(_entity.EntityLocalToWorld(center), CapsuleRadius, player.Body, out _)) return true;
        }
        return false;
    }

    private static Vector3 MoveTowards(Vector3 current, Vector3 target, float maxDelta)
    {
        var difference = target - current;
        float length = difference.Length;
        return length <= maxDelta || length < 1e-6f ? target : current + difference / length * maxDelta;
    }
}
