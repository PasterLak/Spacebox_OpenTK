using OpenTK.Mathematics;

namespace Engine.Networking;

public class NetworkTransform : NetworkBehaviour
{
    private const int MaxSnapshots = 32;

    private readonly NetworkVariable<TransformState> _state;
    private readonly TransformInterpolator _interpolator = new TransformInterpolator(MaxSnapshots);

    public float PositionThreshold { get; set; } = 0.01f;
    public float RotationThreshold { get; set; } = 0.00002f;
    public float SnapDistance { get; set; } = 20f;
    public float RestResendInterval { get; set; } = 0.5f;
    public static float InterpolationDelay { get; set; } = 0.1f;

    public NetworkTransform()
    {
        _state = CreateVariable(new TransformState(0f, Vector3.Zero, Quaternion.Identity), NetworkWritePermission.Owner, DeliveryMode.Sequenced);
        _state.Changed += OnStateChanged;
        _state.OwnerWriteValidator = ValidateOwnerState;
        _state.OwnerWriteRejected += OnOwnerStateRejected;
    }

    protected virtual bool ValidateOwnerState(TransformState previous, TransformState next) => true;

    protected virtual void OnOwnerStateRejected(TransformState previous, TransformState next)
    {
    }

    public Vector3 TargetPosition => _state.Value.Position;
    public Quaternion TargetRotation => _state.Value.Rotation;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            Capture(true);
            return;
        }

        _interpolator.Clear();
        AddSnapshot(_state.Value);
        ApplyPosition(_state.Value.Position);
        ApplyRotation(_state.Value.Rotation);
    }

    public override void OnUpdate()
    {
        if (!IsSpawned || Owner == null) return;

        if (IsOwner)
        {
            Capture(false);
            return;
        }

        Interpolate();
    }

    public void Teleport(Vector3 position, Quaternion rotation)
    {
        ApplyPosition(position);
        ApplyRotation(rotation);
        if (IsOwner) Capture(true);
    }

    protected virtual Vector3 ReadPosition() => Owner.Position;
    protected virtual void ApplyPosition(Vector3 position) => Owner.Position = position;
    protected virtual Quaternion ReadRotation() => Owner.Orientation;
    protected virtual void ApplyRotation(Quaternion rotation) => Owner.Rotation = Node3D.QuaternionToEuler(rotation);

    private void OnStateChanged(TransformState previous, TransformState current)
    {
        if (!IsOwner) AddSnapshot(current);
    }

    private void AddSnapshot(TransformState state)
    {
        _interpolator.ExpectedInterval = 1f / MathF.Max(1f, NetworkManager.Instance.TickRate);
        _interpolator.SnapDistance = SnapDistance;
        _interpolator.Add((float)Time.Total, state);
    }

    private void Capture(bool force)
    {
        var position = ReadPosition();
        var rotation = ReadRotation();
        var last = _state.Value;

        bool moved = Vector3.DistanceSquared(position, last.Position) > PositionThreshold * PositionThreshold;
        float dot = rotation.X * last.Rotation.X + rotation.Y * last.Rotation.Y + rotation.Z * last.Rotation.Z + rotation.W * last.Rotation.W;
        bool rotated = 1f - MathF.Abs(dot) > RotationThreshold;

        float now = (float)Time.Total;
        bool resend = now - last.Time >= RestResendInterval;

        if (force || moved || rotated || resend)
            _state.Value = new TransformState(now, position, rotation);
    }

    private void Interpolate()
    {
        if (!_interpolator.TrySample((float)Time.Total, InterpolationDelay, out var position, out var rotation)) return;

        ApplyPosition(position);
        ApplyRotation(rotation);
    }
}
