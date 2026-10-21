namespace Spacebox.Network.Dashboard.Map;

public sealed class MapSnapshot
{
    public const int FlagShip = 1;
    public const int FlagLoaded = 2;
    public const int FlagClaimed = 4;
    public const int FlagClaimActive = 8;
    public const int PersistenceShift = 4;

    public double[] Center { get; init; } = new double[3];
    public float Radius { get; init; }
    public List<MapPlayerView> Players { get; init; } = new List<MapPlayerView>();
    public List<float[]> Entities { get; init; } = new List<float[]>();
    public List<string> Ids { get; init; } = new List<string>();
    public List<string> Owners { get; init; } = new List<string>();
    public int Truncated { get; init; }
}

public sealed class MapPlayerView
{
    public string Name { get; init; } = "";
    public float[] Position { get; init; } = new float[3];
    public bool Alive { get; init; }
}
