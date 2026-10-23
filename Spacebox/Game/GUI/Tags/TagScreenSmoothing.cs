using OpenTK.Mathematics;

namespace Spacebox.Game.GUI;

public static class TagScreenSmoothing
{
    public const float SnapDistance = 6f;
    public const float Follow = 0.35f;

    public static Vector2 Next(Vector2 previous, Vector2 target)
    {
        var delta = target - previous;
        if (delta.LengthSquared >= SnapDistance * SnapDistance) return target;
        return previous + delta * Follow;
    }
}
