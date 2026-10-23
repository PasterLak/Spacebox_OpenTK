using Engine;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Physics;
using Spacebox.Game.Player.Footsteps;

namespace Spacebox.Game.Player;

public sealed class GravityWalker
{
    public const float EyeHeight = 1.5f;
    public const float CapsuleHeight = 1.6f;
    public const float CapsuleRadius = 0.3f;
    public const float CrouchEyeHeight = 0.8f;
    public const float HeadClearance = CapsuleHeight - EyeHeight;

    private const float WalkSpeed = 4.5f;
    private const float RunSpeed = 7f;
    private const float CrouchSpeed = 2.2f;
    private const float SlowWalkSpeed = 1.8f;
    private const float RunGaitSpeed = (WalkSpeed + RunSpeed) * 0.5f;
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
    private readonly CrouchStance _stance = new CrouchStance(EyeHeight, CrouchEyeHeight);
    private readonly Func<float, bool> _fitsEyeHeight;

    public event Action<float, Vector3>? Landed;

    public bool IsActive { get; private set; }
    public bool IsGrounded => _isGrounded;
    public Vector3 Velocity => _velocity;
    public bool IsCrouching => _stance.IsCrouching;
    public float CurrentEyeHeight => _stance.EyeHeight;
    public float HorizontalSpeed => (_velocity - _up * Vector3.Dot(_velocity, _up)).Length;
    public bool IsSlowWalking { get; private set; }
    public FootstepGait Gait => _stance.IsCrouching || IsSlowWalking ? FootstepGait.Crouch : HorizontalSpeed > RunGaitSpeed ? FootstepGait.Run : FootstepGait.Walk;

    public GravityWalker()
    {
        _fitsEyeHeight = FitsEyeHeight;
    }

    public void Enter(LocalAstronaut player, Vector3 gravity)
    {
        IsActive = true;
        _up = -gravity.Normalized();
        _velocity = player.InertiaController.Velocity;
        _isGrounded = false;
        _isAirborne = false;
        _peakFallSpeed = 0f;
        _stance.Reset(EyeHeight);
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

    public void Reset()
    {
        IsActive = false;
        _isGrounded = false;
        _isAirborne = false;
        _peakFallSpeed = 0f;
        _velocity = Vector3.Zero;
        _entity = null;
        _stance.Reset(EyeHeight);
    }

    public void Update(LocalAstronaut player, Vector3 gravity, bool canMove)
    {
        float deltaTime = Time.Delta;
        if (deltaTime <= 0f) return;

        _up = -gravity.Normalized();
        SyncLocal(player);
        UpdateStance(player, canMove && Input.IsAction("crouch"), deltaTime);
        _isGrounded = ProbeGround();

        Align(player, deltaTime);

        var vertical = _up * Vector3.Dot(_velocity, _up);
        var horizontal = _velocity - vertical;

        var wish = canMove ? GetWishDirection(player) : Vector3.Zero;
        float speed = MoveSpeed(canMove);
        float acceleration = _isGrounded ? GroundAcceleration : AirAcceleration;
        if (_isGrounded || wish != Vector3.Zero)
            horizontal = MoveTowards(horizontal, wish * speed, acceleration * deltaTime);

        if (_isGrounded && canMove && (Input.IsActionDown("up") || ShouldAutoJump(wish)))
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

    public bool TryGetGroundBlock(out Block block)
    {
        block = null;
        if (_entity == null || !_isGrounded) return false;

        var feet = _localPosition;
        feet[_upAxis] -= _upSign * (_stance.EyeHeight + GroundProbe);
        int a = (_upAxis + 1) % 3;
        int b = (_upAxis + 2) % 3;

        for (int i = 0; i < GroundSampleOffsets.Length; i += 2)
        {
            var sample = feet;
            sample[a] += GroundSampleOffsets[i] * CapsuleRadius;
            sample[b] += GroundSampleOffsets[i + 1] * CapsuleRadius;
            if (TryGetSolidBlock(sample, out block)) return true;
        }

        return false;
    }

    private static readonly float[] GroundSampleOffsets = { 0f, 0f, -1f, -1f, 1f, -1f, -1f, 1f, 1f, 1f };

    private bool TryGetSolidBlock(Vector3 local, out Block block)
    {
        block = null;
        var cell = new Vector3i((int)MathF.Floor(local.X), (int)MathF.Floor(local.Y), (int)MathF.Floor(local.Z));
        if (!_entity.TryGetBlockLocal(cell, out var chunk, out var index)) return false;

        block = chunk.Blocks[index.X, index.Y, index.Z];
        return block != null && !block.IsAir;
    }

    private const float AutoJumpProbe = 0.15f;
    private const float StepHeight = 1.05f;

    private bool ShouldAutoJump(Vector3 wish)
    {
        if (!Settings.Gameplay.AutoJump || wish == Vector3.Zero || _entity == null) return false;

        var local = _entity.DirectionToEntitySpace(wish);
        int a = (_upAxis + 1) % 3;
        int b = (_upAxis + 2) % 3;
        int axis = MathF.Abs(local[a]) >= MathF.Abs(local[b]) ? a : b;
        float direction = MathF.Sign(local[axis]);
        if (direction == 0f) return false;

        GetBox(_localPosition, out var min, out var max);
        float ahead = MathF.Abs(EntityBoxCollider.Sweep(_entity, min, max, axis, direction * AutoJumpProbe));
        bool blocked = ahead < AutoJumpProbe - 1e-4f;
        bool clear = false;
        if (blocked && MathF.Abs(EntityBoxCollider.Sweep(_entity, min, max, _upAxis, _upSign * StepHeight)) >= StepHeight - 1e-4f)
        {
            var raised = _localPosition;
            raised[_upAxis] += _upSign * StepHeight;
            GetBox(raised, out var raisedMin, out var raisedMax);
            clear = MathF.Abs(EntityBoxCollider.Sweep(_entity, raisedMin, raisedMax, axis, direction * AutoJumpProbe)) > ahead + 0.05f;
        }

        return MovementAssists.ShouldAutoJump(true, _isGrounded, true, _stance.IsCrouching, blocked, clear);
    }

    private float MoveSpeed(bool canMove)
    {
        IsSlowWalking = canMove && !_stance.IsCrouching && Input.IsAction("slow_walk");
        if (_stance.IsCrouching) return CrouchSpeed;
        if (IsSlowWalking) return SlowWalkSpeed;
        return canMove && MovementAssists.WantsSprint() ? RunSpeed : WalkSpeed;
    }

    private void UpdateStance(LocalAstronaut player, bool wantsCrouch, float deltaTime)
    {
        float previous = _stance.EyeHeight;
        float next = _stance.Update(wantsCrouch, deltaTime, _fitsEyeHeight);
        if (next == previous) return;

        float delta = next - previous;
        if (_entity == null)
        {
            player.Position += _up * delta;
            return;
        }

        _localPosition[_upAxis] += _upSign * delta;
        ApplyLocal(player);
    }

    private bool FitsEyeHeight(float eyeHeight)
    {
        if (_entity == null) return true;

        var candidate = _localPosition;
        candidate[_upAxis] += _upSign * (eyeHeight - _stance.EyeHeight);
        GetBox(candidate, eyeHeight, out var min, out var max);
        return !EntityBoxCollider.Overlaps(_entity, min, max);
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
        for (float lift = 0f; lift <= CapsuleHeight; lift += EntryLiftStep)
        {
            if (lift > 0f && TryPlace(player, start, lift, EyeHeight)) return;
            if (TryPlace(player, start, lift, CrouchEyeHeight) || TryPlace(player, start, -lift, CrouchEyeHeight)) return;
        }
    }

    private bool TryPlace(LocalAstronaut player, Vector3 start, float lift, float eyeHeight)
    {
        var candidate = start;
        candidate[_upAxis] += _upSign * lift;
        GetBox(candidate, eyeHeight, out var min, out var max);
        if (EntityBoxCollider.Overlaps(_entity, min, max)) return false;

        _localPosition = candidate;
        _stance.Reset(eyeHeight);
        ApplyLocal(player);
        return true;
    }

    private void GetBox(Vector3 eyeLocal, out Vector3 min, out Vector3 max)
    {
        GetBox(eyeLocal, _stance.EyeHeight, out min, out max);
    }

    private void GetBox(Vector3 eyeLocal, float eyeHeight, out Vector3 min, out Vector3 max)
    {
        min = eyeLocal - new Vector3(CapsuleRadius);
        max = eyeLocal + new Vector3(CapsuleRadius);

        float height = eyeHeight + HeadClearance;
        float feet = eyeLocal[_upAxis] - _upSign * eyeHeight;
        float low = _upSign > 0f ? feet : feet - height;
        min[_upAxis] = low;
        max[_upAxis] = low + height;
    }

    private bool OverlapsDynamic(LocalAstronaut player, Vector3 eyeLocal)
    {
        var localUp = Vector3.Zero;
        localUp[_upAxis] = _upSign;
        float eyeHeight = _stance.EyeHeight;
        float height = eyeHeight + HeadClearance;
        var feetLocal = eyeLocal - localUp * eyeHeight;
        var physics = World.Instance?.Physics;

        for (int i = 0; i < 3; i++)
        {
            float sample = i == 0 ? CapsuleRadius : i == 1 ? height * 0.5f : height - CapsuleRadius;
            var center = feetLocal + localUp * sample;
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
