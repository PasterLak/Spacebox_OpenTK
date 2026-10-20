using OpenTK.Mathematics;

namespace Spacebox.Game.Rendering;

public static class FlashlightBeamShape
{
    public const float MaxLength = 14f;
    public const float StartOffset = 0.25f;
    public const float MaxViewDistance = 30f;
    public const float FadeStartDistance = 20f;

    public static float Length(float range)
    {
        return Math.Clamp(range, 0f, MaxLength);
    }

    public static float Radius(float length, float cosOuter)
    {
        float angle = MathF.Acos(Math.Clamp(cosOuter, 0.05f, 1f));
        return MathF.Tan(angle) * length;
    }

    public static Matrix4 Model(Vector3 renderStart, Vector3 direction, Vector3 right, Vector3 up, float radius, float length)
    {
        return new Matrix4(
            new Vector4(right * radius, 0f),
            new Vector4(up * radius, 0f),
            new Vector4(direction * length, 0f),
            new Vector4(renderStart, 1f));
    }

    public static bool IsVisible(Vector3 start, Vector3 direction, float length, float radius, Vector3 cameraPos, Vector3 cameraFront)
    {
        var center = start + direction * (length * 0.5f);
        float bound = length * 0.5f + radius;
        var toCenter = center - cameraPos;
        if (toCenter.LengthSquared > (MaxViewDistance + bound) * (MaxViewDistance + bound)) return false;
        return Vector3.Dot(toCenter, cameraFront) > -bound;
    }

    public static float ViewFade(Vector3 start, Vector3 cameraPos)
    {
        float distance = (start - cameraPos).Length;
        float t = Math.Clamp((distance - FadeStartDistance) / (MaxViewDistance - FadeStartDistance), 0f, 1f);
        return 1f - t * t * (3f - 2f * t);
    }

    public static float Inside(Vector3 start, Vector3 direction, float length, float radius, Vector3 cameraPos)
    {
        var toCamera = cameraPos - start;
        float along = Vector3.Dot(toCamera, direction);
        if (along <= 0f || along >= length) return 0f;

        float coneRadius = radius * along / length;
        float offAxis = (toCamera - direction * along).Length;
        return Math.Clamp(1f - offAxis / MathF.Max(coneRadius, 0.001f), 0f, 1f);
    }

    public static Vector3 WrapNoiseOffset(Vector3 origin)
    {
        const float period = 512f;
        return new Vector3(origin.X % period, origin.Y % period, origin.Z % period);
    }
}
