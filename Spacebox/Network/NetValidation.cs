using OpenTK.Mathematics;

namespace Spacebox.Network;

public static class NetValidation
{
    public static bool IsFinite(Vector3 v) => float.IsFinite(v.X) && float.IsFinite(v.Y) && float.IsFinite(v.Z);

    public static bool IsFinite(Quaternion q) => float.IsFinite(q.X) && float.IsFinite(q.Y) && float.IsFinite(q.Z) && float.IsFinite(q.W);

    public static bool IsValidRotation(Quaternion q) => IsFinite(q) && q.LengthSquared > 0.0001f;
}
