using OpenTK.Mathematics;

namespace Spacebox.Game.Generation.Shapes;

public static class BlockShapeTexturing
{
    public static readonly Vector2[] DefaultFaceUVs =
    {
        new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f)
    };

    public static Vector2 MapToFace(Vector2[] faceUVs, Vector2 uv)
    {
        var bottom = Vector2.Lerp(faceUVs[0], faceUVs[1], uv.X);
        var top = Vector2.Lerp(faceUVs[3], faceUVs[2], uv.X);
        return Vector2.Lerp(bottom, top, uv.Y);
    }
}
