using OpenTK.Mathematics;

namespace Engine.PostProcessing.Ssao;

public static class SsaoProjection
{
    public static Vector4 Pack(Matrix4 projection)
    {
        return new Vector4(projection.M11, projection.M22, projection.M33, projection.M43);
    }

    public static float LinearDepth(Vector4 packed, float depthWindow)
    {
        float ndc = depthWindow * 2f - 1f;
        return packed.W / (ndc + packed.Z);
    }
}
