using OpenTK.Mathematics;

namespace Engine.Networking;

public class NetworkTransform : NetworkBehaviour
{
    private const int MaxSnapshots = 32;

    private readonly NetworkVariable<Vector3> _position;
    private readonly NetworkVariable<Quaternion> _rotation;
    private readonly List<Snapshot> _snapshots = new List<Snapshot>(MaxSnapshots);
    private bool _hasPendingSnapshot;

    public float PositionThreshold { get; set; } = 0.01f;
    public float RotationThreshold { get; set; } = 0.00002f;
    public float SnapDistance { get; set; } = 20f;
    public static float InterpolationDelay { get; set; } = 0.1f;

    public NetworkTransform()
    {
        _position = CreateVariable(Vector3.Zero, NetworkWritePermission.Owner, DeliveryMode.Sequenced);
        _rotation = CreateVariable(Quaternion.Identity, NetworkWritePermission.Owner, DeliveryMode.Sequenced);
        _position.Changed += OnStateChanged;
        _rotation.Changed += OnRotationChanged;
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

        _snapshots.Clear();
        PushSnapshot();
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

        if (_hasPendingSnapshot) PushSnapshot();
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

    private void OnStateChanged(Vector3 previous, Vector3 current)
    {
        if (!IsOwner) _hasPendingSnapshot = true;
    }

    private void OnRotationChanged(Quaternion previous, Quaternion current)
    {
        if (!IsOwner) _hasPendingSnapshot = true;
    }

    private void PushSnapshot()
    {
        _hasPendingSnapshot = false;
        if (_snapshots.Count == MaxSnapshots) _snapshots.RemoveAt(0);
        _snapshots.Add(new Snapshot((float)Time.Total, _position.Value, _rotation.Value));
    }

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
        if (_snapshots.Count == 0) return;

        float renderTime = (float)Time.Total - InterpolationDelay;
        Sample(renderTime, out var position, out var rotation);

        if (Vector3.DistanceSquared(ReadPosition(), position) > SnapDistance * SnapDistance)
            _snapshots.Clear();

        ApplyPosition(position);
        ApplyRotation(rotation);
    }

    private void Sample(float time, out Vector3 position, out Quaternion rotation)
    {
        var first = _snapshots[0];
        if (_snapshots.Count == 1 || time <= first.Time)
        {
            position = first.Position;
            rotation = first.Rotation;
            return;
        }

        for (int i = 1; i < _snapshots.Count; i++)
        {
            var next = _snapshots[i];
            if (time > next.Time) continue;

            var previous = _snapshots[i - 1];
            float span = MathF.Max(next.Time - previous.Time, 1e-4f);
            float t = Math.Clamp((time - previous.Time) / span, 0f, 1f);
            position = Vector3.Lerp(previous.Position, next.Position, t);
            rotation = Quaternion.Slerp(previous.Rotation, next.Rotation, t);
            return;
        }

        var latest = _snapshots[^1];
        position = latest.Position;
        rotation = latest.Rotation;
    }

    private readonly struct Snapshot
    {
        public readonly float Time;
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;

        public Snapshot(float time, Vector3 position, Quaternion rotation)
        {
            Time = time;
            Position = position;
            Rotation = rotation;
        }
    }
}
