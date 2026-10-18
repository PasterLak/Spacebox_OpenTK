using OpenTK.Mathematics;

namespace Spacebox.Game.Ships;

public readonly struct ConsoleFrame
{
    public readonly Vector3 Forward;
    public readonly Vector3 Up;
    public readonly Vector3 Left;
    public readonly Vector3 PitchAxis;

    public ConsoleFrame(Vector3 forward, Vector3 up)
    {
        Forward = ShipAiming.SafeNormalize(forward, -Vector3.UnitZ);
        Up = ShipAiming.SafeNormalize(up - Vector3.Dot(up, Forward) * Forward, Vector3.UnitY);
        Left = Vector3.Cross(Up, Forward);
        PitchAxis = Vector3.Cross(Forward, Up);
    }
}
