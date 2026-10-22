using OpenTK.Mathematics;

namespace Spacebox.Game.ShipViewer;

public sealed class ShipViewOrientation
{
    private const int Axes = 6;

    private readonly float[] _flightUp = new float[Axes];
    private readonly float[] _consoleUp = new float[Axes];
    private readonly float[] _thrust = new float[Axes];
    private readonly float[] _cannons = new float[Axes];

    public void AddConsole(Vector3 top, bool flight)
    {
        Vote(flight ? _flightUp : _consoleUp, top, 1f);
    }

    public void AddThruster(Vector3 thrustDirection, float force)
    {
        Vote(_thrust, thrustDirection, MathF.Max(0.01f, force));
    }

    public void AddCannon(Vector3 front)
    {
        Vote(_cannons, front, 1f);
    }

    public Matrix3 Resolve(Vector3 extent)
    {
        int up = Best(_flightUp, -1);
        if (up < 0) up = Best(_consoleUp, -1);

        int forward = Best(_thrust, up);
        if (forward < 0) forward = Best(_cannons, up);

        if (up < 0) up = ExtremeAxis(extent, forward, -1f);
        if (forward < 0) forward = ExtremeAxis(extent, up, 1f);

        return FromPilotFrame(Direction(forward), Direction(up));
    }

    public static Matrix3 FromPilotFrame(Vector3 forward, Vector3 up)
    {
        var right = Vector3.Cross(up, forward);
        return new Matrix3(
            right.X, up.X, forward.X,
            right.Y, up.Y, forward.Y,
            right.Z, up.Z, forward.Z);
    }

    private static void Vote(float[] votes, Vector3 direction, float weight)
    {
        int axis = AxisOf(direction);
        if (axis >= 0) votes[axis] += weight;
    }

    private static int Best(float[] votes, int perpendicularTo)
    {
        int best = -1;
        for (int i = 0; i < Axes; i++)
        {
            if (votes[i] <= 0f || SameLine(i, perpendicularTo)) continue;
            if (best < 0 || votes[i] > votes[best]) best = i;
        }

        return best;
    }

    private static int ExtremeAxis(Vector3 extent, int perpendicularTo, float sign)
    {
        int best = -1;
        for (int axis = 0; axis < 3; axis++)
        {
            if (SameLine(axis * 2, perpendicularTo)) continue;
            if (best < 0 || extent[axis] * sign > extent[best / 2] * sign) best = axis * 2;
        }

        return best;
    }

    private static bool SameLine(int a, int b)
    {
        return b >= 0 && a / 2 == b / 2;
    }

    private static int AxisOf(Vector3 direction)
    {
        var abs = new Vector3(MathF.Abs(direction.X), MathF.Abs(direction.Y), MathF.Abs(direction.Z));
        if (abs.X < 0.5f && abs.Y < 0.5f && abs.Z < 0.5f) return -1;
        if (abs.X >= abs.Y && abs.X >= abs.Z) return direction.X > 0f ? 0 : 1;
        if (abs.Y >= abs.Z) return direction.Y > 0f ? 2 : 3;
        return direction.Z > 0f ? 4 : 5;
    }

    private static Vector3 Direction(int axis)
    {
        var direction = Vector3.Zero;
        direction[axis / 2] = axis % 2 == 0 ? 1f : -1f;
        return direction;
    }
}
