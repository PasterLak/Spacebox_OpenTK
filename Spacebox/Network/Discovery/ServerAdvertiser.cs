using Engine;
using Engine.Networking;
using Spacebox.Game.Generation;

namespace Spacebox.Network.Discovery;

public static class ServerAdvertiser
{
    private static ServerAdvertisement _advertisement;

    public static long InstanceId { get; private set; }
    public static bool IsActive => _advertisement != null;

    public static string ServerName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(_advertisement?.Name)) return _advertisement.Name;
            return World.WorldData?.Info?.Name ?? "Spacebox Server";
        }
    }

    public static void Start(ServerAdvertisement advertisement)
    {
        _advertisement = advertisement ?? throw new ArgumentNullException(nameof(advertisement));
        InstanceId = Random.Shared.NextInt64(1, long.MaxValue);

        var icon = ServerIconFile.TryRead(advertisement.IconPath);
        var query = NetworkManager.Instance.Query;
        query.AnswerBroadcasts = advertisement.LanDiscovery;
        query.BlobProvider = () => icon;
        query.InfoProvider = () => BuildStatus().Serialize();
    }

    public static void Stop()
    {
        _advertisement = null;
        NetworkManager.Instance.Query.Reset();
    }

    public static ServerStatus BuildStatus()
    {
        var info = World.WorldData?.Info;
        return new ServerStatus
        {
            InstanceId = InstanceId,
            Name = ServerName,
            Description = _advertisement?.Description ?? "",
            GameVersion = Application.Version,
            ModId = info?.ModId ?? "",
            WorldName = info?.Name ?? "",
            GameMode = (byte)(info?.GameMode ?? 0),
            Players = PlayerRegistry.All.Count,
            MaxPlayers = _advertisement?.MaxPlayers ?? 0,
            IsDedicated = _advertisement?.IsDedicated ?? false
        };
    }
}
