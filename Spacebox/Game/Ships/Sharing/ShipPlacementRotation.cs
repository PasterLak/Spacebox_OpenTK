using OpenTK.Mathematics;

namespace Spacebox.Game.Ships.Sharing;

public static class ShipPlacementRotation
{
    public const float CoarseStep = 90f;
    public const float FineStep = 15f;

    public static Quaternion AlignedTo(Vector3 right, Vector3 up, Vector3 front)
    {
        if (up.LengthSquared < 1e-6f || front.LengthSquared < 1e-6f) return Quaternion.Identity;
        var y = up.Normalized();
        var x = Vector3.Cross(front.Normalized(), y);
        if (x.LengthSquared < 1e-6f) x = right.LengthSquared > 1e-6f ? right.Normalized() : Vector3.UnitX;
        x = x.Normalized();
        var z = Vector3.Cross(x, y).Normalized();
        var basis = new Matrix3(x, y, z);
        return Quaternion.FromMatrix(Matrix3.Transpose(basis)).Normalized();
    }

    public static Quaternion AlignedTo(Vector3 right, Vector3 up, Vector3 front, Quaternion shipFrame)
    {
        return (AlignedTo(right, up, front) * shipFrame).Normalized();
    }

    public static Quaternion FromViewFrame(Matrix3 view)
    {
        return Quaternion.FromMatrix(Matrix3.Transpose(view)).Normalized();
    }

    public static Quaternion Turn(Quaternion rotation, Vector3 localAxis, float degrees)
    {
        return (rotation * Quaternion.FromAxisAngle(localAxis, MathHelper.DegreesToRadians(degrees))).Normalized();
    }

    public static Quaternion Turn(Quaternion rotation, Quaternion shipFrame, Vector3 frameAxis, float degrees)
    {
        return Turn(rotation, Vector3.Transform(frameAxis, shipFrame.Inverted()), degrees);
    }
}
