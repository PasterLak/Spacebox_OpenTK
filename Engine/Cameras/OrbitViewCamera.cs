using OpenTK.Mathematics;

namespace Engine;

public class OrbitViewCamera : TargetViewCamera
{
    private const float MinPitch = -89f;
    private const float MaxPitch = 89f;

    private float _yaw = 35f;
    private float _pitch = 25f;

    public float Yaw => _yaw;
    public float Pitch => _pitch;

    public OrbitViewCamera()
    {
        UpdateVectors();
    }

    protected override Vector3 ViewDirection => ComputeOffset(_yaw, _pitch, 1f);
    protected override Vector3 UpHint => Vector3.UnitY;

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

    public static Vector3 ComputeOffset(float yawDegrees, float pitchDegrees, float distance)
    {
        float yaw = MathHelper.DegreesToRadians(yawDegrees);
        float pitch = MathHelper.DegreesToRadians(pitchDegrees);
        return new Vector3(MathF.Cos(pitch) * MathF.Sin(yaw), MathF.Sin(pitch), MathF.Cos(pitch) * MathF.Cos(yaw)) * distance;
    }
}
