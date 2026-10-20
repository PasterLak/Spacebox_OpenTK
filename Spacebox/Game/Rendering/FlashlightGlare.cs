using OpenTK.Mathematics;

namespace Spacebox.Game.Rendering;

public static class FlashlightGlare
{
    public const float FacingAngle = 25f;
    public const float ConeScale = 0.8f;
    public const float MaxDistance = 5f;
    public const float RiseSpeed = 12f;
    public const float FallSpeed = 4f;

    private static readonly float CosFacing = MathF.Cos(MathHelper.DegreesToRadians(FacingAngle));

    public static float Strength(Vector3 lightPos, Vector3 lightDir, float cosInner, float cosOuter, float range, float intensity, Vector3 eyePos, Vector3 eyeFront)
    {
        var toEye = eyePos - lightPos;
        float distance = toEye.Length;
        range = MathF.Min(range, MaxDistance);
        if (range <= 0f || distance >= range || distance < 0.001f) return 0f;

        var direction = toEye / distance;
        float outer = NarrowCone(cosOuter);
        float inner = MathF.Max(NarrowCone(cosInner), outer + 0.0001f);
        float cone = SmoothStep(outer, inner, Vector3.Dot(lightDir.Normalized(), direction));
        if (cone <= 0f) return 0f;

        float facing = SmoothStep(CosFacing, 1f, Vector3.Dot(eyeFront.Normalized(), -direction));
        if (facing <= 0f) return 0f;

        float ratio = distance / range;
        float falloff = 1f - ratio * ratio;
        return Math.Clamp(cone * facing * falloff * intensity, 0f, 1f);
    }

    public static bool TryGetScreenPosition(Vector3 point, Vector3 eyePos, Vector3 front, Vector3 right, Vector3 up, float fovDegrees, float aspect, out Vector2 ndc)
    {
        var toPoint = point - eyePos;
        float depth = Vector3.Dot(toPoint, front);
        if (depth <= 0.001f)
        {
            ndc = Vector2.Zero;
            return false;
        }

        float tanHalf = MathF.Tan(MathHelper.DegreesToRadians(fovDegrees) * 0.5f);
        float x = Vector3.Dot(toPoint, right) / (depth * tanHalf * aspect);
        float y = Vector3.Dot(toPoint, up) / (depth * tanHalf);
        ndc = new Vector2(x, y);
        return true;
    }

    public static float Smooth(float current, float target, float deltaTime)
    {
        float speed = target > current ? RiseSpeed : FallSpeed;
        float t = 1f - MathF.Exp(-speed * MathF.Max(deltaTime, 0f));
        return current + (target - current) * t;
    }

    private static float NarrowCone(float cosAngle)
    {
        return MathF.Cos(MathF.Acos(Math.Clamp(cosAngle, -1f, 1f)) * ConeScale);
    }

    private static float SmoothStep(float edge0, float edge1, float x)
    {
        float t = Math.Clamp((x - edge0) / (edge1 - edge0), 0f, 1f);
        return t * t * (3f - 2f * t);
    }
}
