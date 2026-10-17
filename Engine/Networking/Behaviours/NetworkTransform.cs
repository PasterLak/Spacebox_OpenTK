using OpenTK.Mathematics;

namespace Engine.Networking;

public class NetworkTransform : NetworkBehaviour
{
    private readonly NetworkVariable<Vector3> _position;
    private readonly NetworkVariable<Quaternion> _rotation;

    public float PositionThreshold { get; set; } = 0.01f;
    public float RotationThreshold { get; set; } = 0.001f;
    public float SmoothSpeed { get; set; } = 15f;
    public float SnapDistance { get; set; } = 20f;

    public NetworkTransform()
    {
        _position = CreateVariable(Vector3.Zero, NetworkWritePermission.Owner, DeliveryMode.Sequenced);
        _rotation = CreateVariable(Quaternion.Identity, NetworkWritePermission.Owner, DeliveryMode.Sequenced);
    }

    public Vector3 TargetPosition => _position.Value;
    public Quaternion TargetRotation => _rotation.Value;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            Capture();
            return;
        }

        ApplyPosition(_position.Value);
        ApplyRotation(_rotation.Value);
    }

    public override void OnUpdate()
    {
        if (!IsSpawned || Owner == null) return;

        if (IsOwner)
        {
            Capture();
            return;
        }

        Interpolate();
    }

    public void Teleport(Vector3 position, Quaternion rotation)
    {
        ApplyPosition(position);
        ApplyRotation(rotation);
        if (IsOwner) Capture();
    }

    protected virtual Vector3 ReadPosition() => Owner.Position;
    protected virtual void ApplyPosition(Vector3 position) => Owner.Position = position;
    protected virtual Quaternion ReadRotation() => Owner.Orientation;
    protected virtual void ApplyRotation(Quaternion rotation) => Owner.Rotation = Node3D.QuaternionToEuler(rotation);

    private void Capture()
    {
        var position = ReadPosition();
        if (Vector3.DistanceSquared(position, _position.Value) > PositionThreshold * PositionThreshold)
            _position.Value = position;

        var rotation = ReadRotation();
        var last = _rotation.Value;
        float dot = rotation.X * last.X + rotation.Y * last.Y + rotation.Z * last.Z + rotation.W * last.W;
        if (1f - MathF.Abs(dot) > RotationThreshold)
            _rotation.Value = rotation;
    }

    private void Interpolate()
    {
        float t = Math.Clamp(Time.Delta * SmoothSpeed, 0f, 1f);
        var current = ReadPosition();
        var target = _position.Value;

        ApplyPosition(Vector3.DistanceSquared(current, target) > SnapDistance * SnapDistance ? target : Vector3.Lerp(current, target, t));
        ApplyRotation(Quaternion.Slerp(ReadRotation(), _rotation.Value, t));
    }
}
