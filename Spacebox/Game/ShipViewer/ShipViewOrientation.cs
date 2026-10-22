using OpenTK.Mathematics;

namespace Spacebox.Game.ShipViewer;

public sealed class ShipViewOrientation
{
    private readonly Dictionary<(Vector3i Forward, Vector3i Up), int> _votes = new Dictionary<(Vector3i, Vector3i), int>();
    private readonly Dictionary<(Vector3i Forward, Vector3i Up), int> _flightVotes = new Dictionary<(Vector3i, Vector3i), int>();

    public void AddConsole(Vector3 pilotForward, Vector3 pilotUp, bool flight)
    {
        var forward = Snap(pilotForward);
        var up = Snap(pilotUp);
        if (forward == Vector3i.Zero || up == Vector3i.Zero || forward == up || forward == -up) return;

        var votes = flight ? _flightVotes : _votes;
        votes.TryGetValue((forward, up), out int count);
        votes[(forward, up)] = count + 1;
    }

    public Matrix3 Resolve()
    {
        var votes = _flightVotes.Count > 0 ? _flightVotes : _votes;
        (Vector3i Forward, Vector3i Up) best = default;
        int bestCount = 0;
        foreach (var pair in votes)
        {
            if (pair.Value <= bestCount) continue;
            best = pair.Key;
            bestCount = pair.Value;
        }

        return bestCount == 0 ? Matrix3.Identity : FromPilotFrame(best.Forward, best.Up);
    }

    public static Matrix3 FromPilotFrame(Vector3 forward, Vector3 up)
    {
        var right = Vector3.Cross(up, forward);
        return new Matrix3(
            right.X, up.X, forward.X,
            right.Y, up.Y, forward.Y,
            right.Z, up.Z, forward.Z);
    }

    private static Vector3i Snap(Vector3 direction)
    {
        var abs = new Vector3(MathF.Abs(direction.X), MathF.Abs(direction.Y), MathF.Abs(direction.Z));
        if (abs.X < 0.5f && abs.Y < 0.5f && abs.Z < 0.5f) return Vector3i.Zero;
        if (abs.X >= abs.Y && abs.X >= abs.Z) return new Vector3i(MathF.Sign(direction.X), 0, 0);
        if (abs.Y >= abs.Z) return new Vector3i(0, MathF.Sign(direction.Y), 0);
        return new Vector3i(0, 0, MathF.Sign(direction.Z));
    }
}
