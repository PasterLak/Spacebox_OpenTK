using Engine;
using Engine.Networking;
using Spacebox.Network.Messages;
using Spacebox.Game.Resource.Content;

namespace Spacebox.Network;

public static class ClientConnector
{
    private static Action<RemoteWorldInfo> _onAccepted;
    private static Action<string> _onFailed;
    private static bool _isConnecting;
    private static bool _isDownloading;
    private static string _address;
    private static int _port;
    private static int _wakeAttempts;
    private static int _generation;

    public const int MaxWakeAttempts = 15;
    public const int WakeRetryMilliseconds = 4000;

    public static bool IsConnecting => _isConnecting;

    public static event Action<string> StepAdded;
    public static event Action<float> ProgressChanged;

    public static void Connect(string address, int port, Action<RemoteWorldInfo> onAccepted, Action<string> onFailed)
    {
        Cancel();
        Discovery.ServerAddressValidator.Resolve(address, port, out address, out port);

        _onAccepted = onAccepted;
        _onFailed = onFailed;
        _address = address;
        _port = port;
        _wakeAttempts = 0;
        Open();
    }

    private static void Open()
    {
        string address = _address;
        int port = _port;
        _isConnecting = true;
        _generation++;
        Step($"Connecting to {address}:{port}");

        GameSession.BeginClient(address, port);
        Auth.ClientAuth.Begin($"{GameSession.HostAddress}:{port}", response => NetworkManager.Instance.SendToServer(response));

        var network = NetworkManager.Instance;
        network.Messages.Register<JoinAcceptedMessage>(OnJoinAccepted);
        network.Messages.Register<JoinRejectedMessage>(OnJoinRejected);
        network.Messages.Register<AuthChallengeMessage>(OnAuthChallenge);
        network.ConnectedToServer += OnConnected;
        network.ConnectionFailed += OnConnectionLost;
        network.DisconnectedFromServer += OnConnectionLost;

        if (!network.Connect(GameSession.HostAddress, port))
            Fail("Could not start the connection");
    }

    public static void Cancel()
    {
        if (!_isConnecting) return;
        _generation++;

        Unsubscribe();
        NetworkManager.Instance.Shutdown("Cancelled");
    }

    private static void OnConnected()
    {
        Step($"Connected, joining as {GameSession.PlayerName}");
        NetworkManager.Instance.SendToServer(new JoinRequestMessage
        {
            Name = GameSession.PlayerName,
            SkinColor = GameSession.SkinColor,
            GameVersion = Application.Version,
            ModId = string.Empty
        });
    }

    private static void OnAuthChallenge(int sender, AuthChallengeMessage message)
    {
        if (!_isConnecting) return;

        if (!string.IsNullOrEmpty(message.Error)) Step(message.Error);
        else Step(message.IsRegistered ? "The server asks for your password" : "Create an account on this server");
        Auth.ClientAuth.HandleChallenge(message);
    }

    private static void OnJoinAccepted(int sender, JoinAcceptedMessage message)
    {
        if (!_isConnecting) return;

        Auth.ClientAuth.OnJoinAccepted();

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
            GameSetFolder = message.GameSetFolder,
            Extensions = WorldExtensionList.Parse(message.Extensions),
            TexturePacks = WorldExtensionList.Parse(message.TexturePacks),
            SpawnPoint = message.HasSpawnPoint ? new[] { message.SpawnPoint.X, message.SpawnPoint.Y, message.SpawnPoint.Z } : null,
            GeneratorVersion = message.GeneratorVersion
        };

        GameSession.SetRemoteWorld(world);
        GameSession.PendingPlayerJson = message.PlayerJson;
        Step($"Joined world '{message.WorldName}'");
        Step("Checking GameSet");

        GameSetDownloader.Prepare(world, OnDownloadProgress, () => Complete(world), OnDownloadFailed);
    }

    private static void OnDownloadProgress(float progress)
    {
        if (!_isDownloading)
        {
            _isDownloading = true;
            Step("Downloading GameSet from the server");
        }

        ProgressChanged?.Invoke(progress);
    }

    private static void OnDownloadFailed(string reason)
    {
        Fail(reason);
        NetworkManager.Instance.Shutdown("GameSet download failed");
    }

    private static void Complete(RemoteWorldInfo world)
    {
        if (!_isConnecting) return;

        Step("GameSet ready");
        Step("Loading world");
        var callback = _onAccepted;
        Unsubscribe();
        callback?.Invoke(world);
    }

    private static void OnJoinRejected(int sender, JoinRejectedMessage message)
    {
        if (message.Reason == Dedicated.ServerSleep.WakingReason && _wakeAttempts < MaxWakeAttempts)
        {
            RetryWhileWaking();
            return;
        }

        Fail(message.Reason);
        NetworkManager.Instance.Shutdown("Rejected");
    }

    private static void RetryWhileWaking()
    {
        _wakeAttempts++;
        Step(_wakeAttempts == 1 ? "The server is starting, please wait..." : $"Still starting, trying again ({_wakeAttempts}/{MaxWakeAttempts})");

        var onAccepted = _onAccepted;
        var onFailed = _onFailed;
        int attempts = _wakeAttempts;
        Unsubscribe();
        NetworkManager.Instance.Shutdown("Waiting for the server");

        _onAccepted = onAccepted;
        _onFailed = onFailed;
        _wakeAttempts = attempts;
        _isConnecting = true;
        int generation = ++_generation;
        Task.Delay(WakeRetryMilliseconds).ContinueWith(_ => EngineWindow._mainThreadActions.Enqueue(() =>
        {
            if (generation != _generation || !_isConnecting) return;
            _isConnecting = false;
            Open();
        }));
    }

    private static void OnConnectionLost(string reason)
    {
        if (reason == Dedicated.ServerSleep.WakingReason && _wakeAttempts < MaxWakeAttempts)
        {
            RetryWhileWaking();
            return;
        }

        Fail(DisconnectReasons.Describe(reason));
    }

    private static void Step(string text)
    {
        Debug.Log("[Network] " + text);
        StepAdded?.Invoke(text);
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
        network.Messages.Unregister<AuthChallengeMessage>();
        Auth.ClientAuth.Reset();
        _isConnecting = false;
        _isDownloading = false;
        _onAccepted = null;
        _onFailed = null;
    }
}
