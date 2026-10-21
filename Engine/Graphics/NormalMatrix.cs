using OpenTK.Mathematics;

namespace Engine.Graphics;

public static class NormalMatrix
{
    private const float MinDeterminant = 1e-12f;

    public static Matrix3 From(Matrix4 model)
    {
        var linear = new Matrix3(model);
        if (MathF.Abs(linear.Determinant) < MinDeterminant) return Matrix3.Identity;
        return Matrix3.Transpose(Matrix3.Invert(linear));
    }
}
