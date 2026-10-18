using Engine;
using Engine.Networking;
using Spacebox.Network.Messages;

namespace Spacebox.Network;

public static class ClientConnector
{
    private static Action<RemoteWorldInfo> _onAccepted;
    private static Action<string> _onFailed;
    private static bool _isConnecting;

    public static bool IsConnecting => _isConnecting;
    public static string Status { get; private set; }

    public static void Connect(string address, int port, Action<RemoteWorldInfo> onAccepted, Action<string> onFailed)
    {
        Cancel();

        _onAccepted = onAccepted;
        _onFailed = onFailed;
        _isConnecting = true;
        Status = $"Connecting to {address}:{port}...";

        GameSession.BeginClient(address, port);

        var network = NetworkManager.Instance;
        network.Messages.Register<JoinAcceptedMessage>(OnJoinAccepted);
        network.Messages.Register<JoinRejectedMessage>(OnJoinRejected);
        network.ConnectedToServer += OnConnected;
        network.ConnectionFailed += OnConnectionLost;
        network.DisconnectedFromServer += OnConnectionLost;

        if (!network.Connect(GameSession.HostAddress, port))
            Fail("Could not start the connection");
    }

    public static void Cancel()
    {
        if (!_isConnecting) return;

        Unsubscribe();
        NetworkManager.Instance.Shutdown("Cancelled");
    }

    private static void OnConnected()
    {
        NetworkManager.Instance.SendToServer(new JoinRequestMessage
        {
            Name = GameSession.PlayerName,
            SkinColor = GameSession.SkinColor,
            GameVersion = Application.Version,
            ModId = string.Empty
        });
    }

    private static void OnJoinAccepted(int sender, JoinAcceptedMessage message)
    {
        if (!_isConnecting) return;

        var world = new RemoteWorldInfo
        {
            ServerName = message.ServerName,
            WorldName = message.WorldName,
            Seed = message.Seed,
            ModId = message.ModId,
            GameMode = message.GameMode,
            Day = message.Day,
            Ticks = message.Ticks,
            GameSetHash = message.GameSetHash,
            GameSetFolder = message.GameSetFolder
        };

        GameSession.SetRemoteWorld(world);
        GameSession.PendingPlayerJson = message.PlayerJson;
        Status = "Checking GameSet...";

        GameSetDownloader.Prepare(world, OnDownloadProgress, () => Complete(world), OnDownloadFailed);
    }

    private static void OnDownloadProgress(float progress)
    {
        Status = $"Downloading GameSet {(int)(progress * 100f)}%";
    }

    private static void OnDownloadFailed(string reason)
    {
        Fail(reason);
        NetworkManager.Instance.Shutdown("GameSet download failed");
    }

    private static void Complete(RemoteWorldInfo world)
    {
        if (!_isConnecting) return;

        var callback = _onAccepted;
        Unsubscribe();
        callback?.Invoke(world);
    }

    private static void OnJoinRejected(int sender, JoinRejectedMessage message)
    {
        Fail(message.Reason);
        NetworkManager.Instance.Shutdown("Rejected");
    }

    private static void OnConnectionLost(string reason)
    {
        Fail(reason);
    }

    private static void Fail(string reason)
    {
        if (!_isConnecting) return;

        var callback = _onFailed;
        Unsubscribe();
        Debug.Error($"[Network] Join failed: {reason}");
        callback?.Invoke(reason);
    }

    private static void Unsubscribe()
    {
        var network = NetworkManager.Instance;
        network.ConnectedToServer -= OnConnected;
        network.ConnectionFailed -= OnConnectionLost;
        network.DisconnectedFromServer -= OnConnectionLost;
        network.Messages.Unregister<JoinAcceptedMessage>();
        network.Messages.Unregister<JoinRejectedMessage>();
        _isConnecting = false;
        Status = null;
        _onAccepted = null;
        _onFailed = null;
    }
}
