using OpenTK.Mathematics;

namespace Spacebox.Game.Commands.Teleport;

public sealed class TeleportRequest
{
    private TeleportRequest(string subjects, string destination, TeleportCoordinate x, TeleportCoordinate y, TeleportCoordinate z)
    {
        Subjects = subjects;
        Destination = destination;
        X = x;
        Y = y;
        Z = z;
    }

    public string Subjects { get; }
    public string Destination { get; }
    public TeleportCoordinate X { get; }
    public TeleportCoordinate Y { get; }
    public TeleportCoordinate Z { get; }
    public bool HasCoordinates => Destination == null;

    public Vector3 ResolveCoordinates(Vector3 origin) => new Vector3(X.Resolve(origin.X), Y.Resolve(origin.Y), Z.Resolve(origin.Z));

    public static TeleportRequest ToPlayer(string subjects, string destination) => new TeleportRequest(subjects, destination, default, default, default);

    public static bool TryParse(string[] args, out TeleportRequest request)
    {
        request = null;
        switch (args.Length)
        {
            case 1:
                request = ToPlayer(null, args[0]);
                return true;
            case 2:
                request = ToPlayer(args[0], args[1]);
                return true;
            case 3:
                return TryParseCoordinates(null, args, 0, out request);
            case 4:
                return TryParseCoordinates(args[0], args, 1, out request);
            default:
                return false;
        }
    }

    private static bool TryParseCoordinates(string subjects, string[] args, int start, out TeleportRequest request)
    {
        request = null;
        if (!TeleportCoordinate.TryParse(args[start], out var x)
            || !TeleportCoordinate.TryParse(args[start + 1], out var y)
            || !TeleportCoordinate.TryParse(args[start + 2], out var z)) return false;

        request = new TeleportRequest(subjects, null, x, y, z);
        return true;
    }
}
