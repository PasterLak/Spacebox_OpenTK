using Engine;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;

namespace Spacebox.Game.Player;

public sealed class GravityWalker
{
    public const float EyeHeight = 1.5f;
    public const float CapsuleHeight = 1.6f;
    public const float CapsuleRadius = 0.3f;

    private const float WalkSpeed = 4.5f;
    private const float RunSpeed = 7f;
    private const float JumpHeight = 1.25f;
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

    public bool IsActive { get; private set; }
    public bool IsGrounded => _isGrounded;
    public Vector3 Velocity => _velocity;

    public void Enter(LocalAstronaut player, Vector3 gravity)
    {
        IsActive = true;
        _up = -gravity.Normalized();
        _velocity = player.InertiaController.Velocity;
        _isGrounded = false;
        LiftOutOfGeometry(player);
    }

    public void Exit(LocalAstronaut player)
    {
        IsActive = false;
        _isGrounded = false;
        player.InertiaController.Velocity = _velocity;
    }

    public void Update(LocalAstronaut player, Vector3 gravity, bool canMove)
    {
        float deltaTime = Time.Delta;
        if (deltaTime <= 0f) return;

        _up = -gravity.Normalized();
        _isGrounded = IsBlocked(player, player.Position - _up * GroundProbe);

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
            vertical = _up * MathF.Sqrt(2f * gravity.Length * JumpHeight);
            _isGrounded = false;
        }
        else
        {
            vertical += gravity * deltaTime;
            float fallSpeed = -Vector3.Dot(vertical, _up);
            if (fallSpeed > MaxFallSpeed) vertical = -_up * MaxFallSpeed;
        }

        _velocity = horizontal + vertical;
        Move(player, _velocity * deltaTime);
        player.CameraSway.Update(0f);
        player.OnMoved?.Invoke(player);
    }

    public void Look(LocalAstronaut player)
    {
        var rotation = player.GetRotation();
        bool upright = IsUpright(rotation);
        var reference = upright ? _up : Vector3.Transform(Vector3.UnitY, rotation);

        var delta = Input.Mouse.Delta;
        rotation = Quaternion.FromAxisAngle(reference, -delta.X * MouseSensitivity) * rotation;

        var right = Vector3.Transform(Vector3.UnitX, rotation);
        var pitched = Quaternion.FromAxisAngle(right, -delta.Y * MouseSensitivity) * rotation;
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

    private void Move(LocalAstronaut player, Vector3 delta)
    {
        int steps = Math.Max(1, (int)MathF.Ceiling(delta.Length / MaxStep));
        var step = delta / steps;

        var axisA = Flatten(player.Front);
        if (axisA == Vector3.Zero) axisA = Flatten(player.Up);
        if (axisA == Vector3.Zero) axisA = Flatten(Vector3.UnitX);
        if (axisA == Vector3.Zero) axisA = Flatten(Vector3.UnitZ);
        var axisB = Vector3.Cross(_up, axisA).Normalized();

        for (int i = 0; i < steps; i++)
        {
            MoveAlong(player, _up, Vector3.Dot(step, _up), true);
            MoveAlong(player, axisA, Vector3.Dot(step, axisA), false);
            MoveAlong(player, axisB, Vector3.Dot(step, axisB), false);
        }
    }

    private void MoveAlong(LocalAstronaut player, Vector3 axis, float distance, bool isVertical)
    {
        if (MathF.Abs(distance) < 1e-6f) return;

        var target = player.Position + axis * distance;
        if (!IsBlocked(player, target))
        {
            player.Position = target;
            return;
        }

        if (isVertical && distance < 0f) _isGrounded = true;
        _velocity -= axis * Vector3.Dot(_velocity, axis);
    }

    private void LiftOutOfGeometry(LocalAstronaut player)
    {
        if (!IsBlocked(player, player.Position)) return;

        for (float lift = EntryLiftStep; lift <= CapsuleHeight; lift += EntryLiftStep)
        {
            var candidate = player.Position + _up * lift;
            if (!IsBlocked(player, candidate))
            {
                player.Position = candidate;
                return;
            }
        }
    }

    private bool IsBlocked(LocalAstronaut player, Vector3 eyePosition)
    {
        var physics = World.Instance?.Physics;
        if (physics == null) return false;

        var feet = eyePosition - _up * EyeHeight;
        var ignore = player.Body;

        return physics.IsBlocked(feet + _up * CapsuleRadius, CapsuleRadius, ignore)
               || physics.IsBlocked(feet + _up * (CapsuleHeight * 0.5f), CapsuleRadius, ignore)
               || physics.IsBlocked(feet + _up * (CapsuleHeight - CapsuleRadius), CapsuleRadius, ignore);
    }

    private static Vector3 MoveTowards(Vector3 current, Vector3 target, float maxDelta)
    {
        var difference = target - current;
        float length = difference.Length;
        return length <= maxDelta || length < 1e-6f ? target : current + difference / length * maxDelta;
    }
}
