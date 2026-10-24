using OpenTK.Mathematics;

namespace Spacebox.Game.Player.Building;

public static class PlayerAlignedFrame
{
    public static Quaternion FromView(Vector3 front, Vector3 up)
    {
        if (front.LengthSquared < 1e-8f || up.LengthSquared < 1e-8f) return Quaternion.Identity;

        Vector3 forward = Vector3.Normalize(front);
        Vector3 right = Vector3.Cross(forward, up);
        if (right.LengthSquared < 1e-8f) return Quaternion.Identity;
        right = Vector3.Normalize(right);
        Vector3 trueUp = Vector3.Cross(right, forward);

        var basis = Matrix3.Transpose(new Matrix3(right, trueUp, -forward));
        return Quaternion.Normalize(Quaternion.FromMatrix(basis));
    }

    public static Vector3 CornerForCenter(Vector3 centerWorld, Quaternion orientation)
    {
        return centerWorld - Vector3.Transform(new Vector3(0.5f), orientation);
    }

    public static Quaternion Sanitize(Quaternion orientation)
    {
        bool finite = float.IsFinite(orientation.X) && float.IsFinite(orientation.Y) && float.IsFinite(orientation.Z) && float.IsFinite(orientation.W);
        return finite && orientation.LengthSquared > 1e-6f ? Quaternion.Normalize(orientation) : Quaternion.Identity;
    }
}
