using OpenTK.Mathematics;

namespace Engine;

public class TrackballViewCamera : TargetViewCamera
{
    private Quaternion _orientation = Quaternion.Identity;

    public Quaternion Orientation => _orientation;

    public TrackballViewCamera()
    {
        SetAngles(35f, 25f);
    }

    protected override Vector3 ViewDirection => Vector3.Transform(Vector3.UnitZ, _orientation);
    protected override Vector3 UpHint => Vector3.Transform(Vector3.UnitY, _orientation);

    public void SetAngles(float yawDegrees, float pitchDegrees)
    {
        var yaw = Quaternion.FromAxisAngle(Vector3.UnitY, MathHelper.DegreesToRadians(yawDegrees));
        var pitch = Quaternion.FromAxisAngle(Vector3.UnitX, -MathHelper.DegreesToRadians(pitchDegrees));
        SetOrientation(yaw * pitch);
    }

    public void Orbit(float yawDegrees, float pitchDegrees)
    {
        var yaw = Quaternion.FromAxisAngle(Vector3.UnitY, MathHelper.DegreesToRadians(yawDegrees));
        var pitch = Quaternion.FromAxisAngle(Vector3.UnitX, -MathHelper.DegreesToRadians(pitchDegrees));
        SetOrientation(_orientation * yaw * pitch);
    }

    public void Spin(Vector3 axis, float degrees)
    {
        SetOrientation(Quaternion.FromAxisAngle(axis, MathHelper.DegreesToRadians(degrees)) * _orientation);
    }

    private void SetOrientation(Quaternion orientation)
    {
        _orientation = Quaternion.Normalize(orientation);
        UpdateVectors();
    }
}
