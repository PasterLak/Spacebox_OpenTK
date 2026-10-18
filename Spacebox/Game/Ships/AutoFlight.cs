using OpenTK.Mathematics;

namespace Spacebox.Game.Ships;

public static class AutoFlight
{
    public const float TranslationThreshold = 0.7f;
    public const float TorqueThreshold = 0.25f;
    public const float MinLever = 0.25f;
    public const float BrakeThreshold = 0.5f;
    public const float BrakeMinSpeed = 0.05f;

    private const float SteerGain = 2f;
    private const float SteerMaxRate = 1f;
    private const float SteerDeadband = 0.04f;

    public static FlightInput Classify(Vector3 thrustDirection, Vector3 point, Vector3 centerOfMass, ConsoleFrame frame)
    {
        var flags = FlightInput.None;
        var d = ShipAiming.SafeNormalize(thrustDirection, Vector3.Zero);
        if (d == Vector3.Zero) return flags;

        flags |= Axis(Vector3.Dot(d, frame.Forward), FlightInput.Forward, FlightInput.Backward, TranslationThreshold);
        flags |= Axis(Vector3.Dot(d, frame.Left), FlightInput.Left, FlightInput.Right, TranslationThreshold);
        flags |= Axis(Vector3.Dot(d, frame.Up), FlightInput.Up, FlightInput.Down, TranslationThreshold);

        var lever = point - centerOfMass;
        float leverLength = lever.Length;
        if (leverLength < MinLever) return flags;

        var torque = Vector3.Cross(lever, d) / leverLength;
        flags |= Axis(Vector3.Dot(torque, frame.Up), FlightInput.YawLeft, FlightInput.YawRight, TorqueThreshold);
        flags |= Axis(Vector3.Dot(torque, frame.PitchAxis), FlightInput.PitchUp, FlightInput.PitchDown, TorqueThreshold);
        flags |= Axis(Vector3.Dot(torque, frame.Forward), FlightInput.RollRight, FlightInput.RollLeft, TorqueThreshold);
        return flags;
    }

    public static bool ShouldFire(FlightInput thrusterFlags, FlightInput requested, Vector3 thrustDirection, Vector3 brakeDirection)
    {
        if ((thrusterFlags & requested & ~(FlightInput.Brake | FlightInput.Steer)) != 0) return true;
        if ((requested & FlightInput.Brake) == 0 || (requested & FlightInput.Translation) != 0) return false;
        if (brakeDirection == Vector3.Zero) return false;

        return Vector3.Dot(ShipAiming.SafeNormalize(thrustDirection, Vector3.Zero), brakeDirection) > BrakeThreshold;
    }

    public static Vector3 BrakeDirection(Vector3 velocityLocal)
    {
        float speed = velocityLocal.Length;
        return speed > BrakeMinSpeed ? -velocityLocal / speed : Vector3.Zero;
    }

    public static FlightInput Steer(Vector3 aimLocal, Vector3 angularVelocityLocal, ConsoleFrame frame)
    {
        var aim = ShipAiming.SafeNormalize(aimLocal, frame.Forward);
        var axis = Vector3.Cross(frame.Forward, aim);
        float angle = ShipAiming.AngleBetween(frame.Forward, aim);

        var desired = axis.LengthSquared > 1e-8f ? axis.Normalized() * MathF.Min(angle * SteerGain, SteerMaxRate) : Vector3.Zero;
        var need = desired - angularVelocityLocal;

        return Axis(Vector3.Dot(need, frame.Up), FlightInput.YawLeft, FlightInput.YawRight, SteerDeadband)
               | Axis(Vector3.Dot(need, frame.PitchAxis), FlightInput.PitchUp, FlightInput.PitchDown, SteerDeadband);
    }

    private static FlightInput Axis(float value, FlightInput positive, FlightInput negative, float threshold)
    {
        if (value > threshold) return positive;
        if (value < -threshold) return negative;
        return FlightInput.None;
    }
}
