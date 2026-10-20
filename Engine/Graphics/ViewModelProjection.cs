using OpenTK.Mathematics;

namespace Engine.Graphics;

public static class ViewModelProjection
{
    public static bool Active { get; private set; }
    public static Matrix4 Projection { get; private set; } = Matrix4.Identity;

    public static void Set(Matrix4 projection)
    {
        Projection = projection;
        Active = true;
    }

    public static void Reset()
    {
        Active = false;
    }
}
