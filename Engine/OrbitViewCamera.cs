using OpenTK.Mathematics;

namespace Engine;

public class OrbitViewCamera : Camera
{
    private const float MinPitch = -89f;
    private const float MaxPitch = 89f;

    private Vector3 _target;
    private float _distance = 3f;
    private float _yaw = 35f;
    private float _pitch = 25f;

    public float MinDistance { get; set; } = 0.2f;
    public float MaxDistance { get; set; } = 2000f;

    public Vector3 Target
    {
        get => _target;
        set
        {
            _target = value;
            UpdateVectors();
        }
    }

    public float Distance
    {
        get => _distance;
        set
        {
            _distance = MathHelper.Clamp(value, MinDistance, MaxDistance);
            UpdateVectors();
        }
    }

    public float Yaw => _yaw;
    public float Pitch => _pitch;

    public OrbitViewCamera() : base(Vector3.Zero, false)
    {
        FOV = 45f;
        DepthNear = 0.05f;
        DepthFar = 5000f;
        UpdateVectors();
    }

    public void Orbit(float yawDegrees, float pitchDegrees)
    {
        _yaw = (_yaw + yawDegrees) % 360f;
        _pitch = MathHelper.Clamp(_pitch + pitchDegrees, MinPitch, MaxPitch);
        UpdateVectors();
    }

    public void SetAngles(float yawDegrees, float pitchDegrees)
    {
        _yaw = yawDegrees % 360f;
        _pitch = MathHelper.Clamp(pitchDegrees, MinPitch, MaxPitch);
        UpdateVectors();
    }

    public void Zoom(float steps)
    {
        Distance = _distance * MathF.Pow(0.88f, steps);
    }

    public void Frame(Vector3 center, float radius)
    {
        _target = center;
        float halfFov = FitHalfAngle(MathHelper.DegreesToRadians(FOV) * 0.5f, AspectRatio);
        _distance = MathHelper.Clamp(MathF.Max(radius, 0.01f) / MathF.Sin(halfFov) * 1.1f, MinDistance, MaxDistance);
        DepthFar = MathF.Max(100f, _distance + radius * 4f);
        DepthNear = MathF.Max(0.01f, _distance * 0.01f);
        UpdateVectors();
    }

    public static float FitHalfAngle(float halfFovY, float aspect)
    {
        float halfFovX = MathF.Atan(MathF.Tan(halfFovY) * MathF.Max(0.01f, aspect));
        return MathF.Min(halfFovY, halfFovX);
    }

    public static Vector3 ComputeOffset(float yawDegrees, float pitchDegrees, float distance)
    {
        float yaw = MathHelper.DegreesToRadians(yawDegrees);
        float pitch = MathHelper.DegreesToRadians(pitchDegrees);
        return new Vector3(MathF.Cos(pitch) * MathF.Sin(yaw), MathF.Sin(pitch), MathF.Cos(pitch) * MathF.Cos(yaw)) * distance;
    }

    protected override void UpdateVectors()
    {
        Position = _target + ComputeOffset(_yaw, _pitch, _distance);
        _front = Vector3.Normalize(_target - Position);
        _right = Vector3.Normalize(Vector3.Cross(_front, Vector3.UnitY));
        _up = Vector3.Normalize(Vector3.Cross(_right, _front));
    }
}
