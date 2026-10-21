using OpenTK.Mathematics;

namespace Spacebox.Game;

public static class ViewModelPoint
{
    public static bool TryToWorld(Vector3 viewPoint, Matrix4 viewModelProjection, Vector3 cameraPosition, Vector3 front, Vector3 right, Vector3 up, float fovDegrees, float aspect, out Vector3 world)
    {
        world = cameraPosition;
        float depth = -viewPoint.Z;
        if (depth <= 0.001f) return false;

        var clip = new Vector4(viewPoint, 1f) * viewModelProjection;
        if (clip.W <= 1e-5f) return false;

        float ndcX = clip.X / clip.W;
        float ndcY = clip.Y / clip.W;
        float tan = MathF.Tan(MathHelper.DegreesToRadians(fovDegrees) * 0.5f);
        var direction = front + right * (ndcX * tan * aspect) + up * (ndcY * tan);
        world = cameraPosition + direction * depth;
        return true;
    }
}
