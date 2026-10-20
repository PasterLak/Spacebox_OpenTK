using OpenTK.Mathematics;

namespace Engine.Audio;

public static class ListenerVelocity
{
    public const float MaxJumpDistance = 50f;
    public const float MaxSpeedFraction = 0.4f;

    public static Vector3 Compute(Vector3 movement, float deltaTime, float speedOfSound)
    {
        if (deltaTime <= 0f || !IsFinite(movement)) return Vector3.Zero;
        if (movement.LengthSquared > MaxJumpDistance * MaxJumpDistance) return Vector3.Zero;

        var velocity = movement / deltaTime;
        if (!IsFinite(velocity)) return Vector3.Zero;

        float maxSpeed = speedOfSound * MaxSpeedFraction;
        float speed = velocity.Length;
        return speed > maxSpeed ? velocity * (maxSpeed / speed) : velocity;
    }

    private static bool IsFinite(Vector3 value)
    {
        return float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);
    }
}
