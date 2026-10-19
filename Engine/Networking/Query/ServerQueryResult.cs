using System.Net;

namespace Engine.Networking.Query;

public sealed class ServerQueryResult
{
    public string Key { get; init; }
    public IPEndPoint EndPoint { get; init; }
    public byte[] Info { get; init; }
    public int RoundTripMs { get; init; }
    public ulong BlobHash { get; init; }
    public int BlobLength { get; init; }
    public bool FromBroadcast { get; init; }
}
