using Engine;
using Engine.Networking;
using Engine.SceneManagement;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.Resource;
using Spacebox.Game.Player;
using Spacebox.Network.Messages;
using Spacebox.Network.Players;
using Spacebox.Scenes;

namespace Spacebox.Network;

public sealed class NetworkSession : IDisposable
{
    private const float PlayerSyncInterval = 10f;

    private readonly LocalAstronaut _localPlayer;
    private readonly PlayerSpawnService _playerSpawner;
    private float _playerSyncTimer;
    private bool _disposed;

    public static NetworkSession Current { get; private set; }

    public NetworkSession(Node3D root, LocalAstronaut localPlayer)
    {
        Current = this;
        _localPlayer = GameSession.HasLocalPlayer ? localPlayer : null;

        var network = NetworkManager.Instance;
        network.ConnectionKey = "Spacebox:" + Application.Version;
        _playerSpawner = new PlayerSpawnService(root, _localPlayer);

        network.Messages.Register<JoinRequestMessage>(OnJoinRequest);
        network.Messages.Register<ClientReadyMessage>(OnClientReady);
        network.Messages.Register<PlayerDataMessage>(OnPlayerData);
        network.PeerDisconnected += OnPeerDisconnected;
        network.DisconnectedFromServer += OnDisconnectedFromServer;

        if (World.DropEffectManager != null) World.DropEffectManager.Replicator = new DropNetwork();
    }

    public bool Start()
    {
        var network = NetworkManager.Instance;
        bool started = GameSession.Mode switch
        {
            SessionMode.Host => network.StartHost(GameSession.Port),
            SessionMode.Dedicated => network.StartDedicated(GameSession.Port),
            SessionMode.Client => network.IsConnected,
            _ => StartOffline(network)
        };

        if (!started)
        {
            Debug.Error($"[Network] Failed to start session in mode {GameSession.Mode}");
            if (GameSession.Mode == SessionMode.Host) StartOffline(network);
            else return false;
        }

        if (network.IsServer && _localPlayer != null)
            SpawnLocalHostPlayer();

        if (network.IsRemoteClient)
            network.SendToServer(new ClientReadyMessage());

        return true;
    }

    public bool OpenToNetwork(int port)
    {
        var network = NetworkManager.Instance;
        if (!network.IsServer || network.IsListening) return network.IsListening;

        if (!network.StartListening(port)) return false;

        ChatService.Broadcast($"Server is open on port {port}", Color4.Yellow);
        return true;
    }

    public void Update()
    {
        if (!NetworkManager.Instance.IsRemoteClient || _localPlayer == null) return;

        _playerSyncTimer += Time.Delta;
        if (_playerSyncTimer < PlayerSyncInterval) return;

        _playerSyncTimer = 0f;
        SendLocalPlayerData();
    }

    public void SaveRemotePlayers()
    {
        if (!NetworkManager.Instance.IsServer || World.WorldData == null) return;

        foreach (var player in PlayerRegistry.All)
        {
            if (player.Peer != NetworkManager.Instance.LocalPeerId && !string.IsNullOrEmpty(player.LastPlayerJson))
                PlayerSaveLoadManager.WritePlayerJson(World.WorldData.WorldFolderPath, player.Name, player.LastPlayerJson);
        }
    }

    public void SendLocalPlayerData()
    {
        if (_localPlayer == null || !NetworkManager.Instance.IsRemoteClient) return;

        var json = PlayerSaveLoadManager.Serialize(_localPlayer);
        if (json != null) NetworkManager.Instance.SendToServer(new PlayerDataMessage { Json = json });
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        var network = NetworkManager.Instance;
        SendLocalPlayerData();
        SaveRemotePlayers();

        network.PeerDisconnected -= OnPeerDisconnected;
        network.DisconnectedFromServer -= OnDisconnectedFromServer;
        network.Messages.Unregister<JoinRequestMessage>();
        network.Messages.Unregister<ClientReadyMessage>();
        network.Messages.Unregister<PlayerDataMessage>();

        network.Shutdown("Server stopped");
        _playerSpawner.Dispose();
        PlayerRegistry.Clear();

        if (Current == this) Current = null;
    }

    private static bool StartOffline(NetworkManager network)
    {
        network.StartOffline();
        return true;
    }

    private void SpawnLocalHostPlayer()
    {
        int peer = NetworkManager.Instance.LocalPeerId;
        var player = new ConnectedPlayer { Peer = peer, Name = GameSession.PlayerName, SkinColor = GameSession.SkinColor };
        PlayerRegistry.Add(player);
        player.PlayerObject = _playerSpawner.SpawnPlayer(peer, player.Name, player.SkinColor, _localPlayer.Position, _localPlayer.GetRotation());
    }

    private void OnJoinRequest(int sender, JoinRequestMessage message)
    {
        var network = NetworkManager.Instance;
        if (!network.IsServer || sender == network.LocalPeerId) return;

        string reason = ValidateJoin(message);
        if (reason != null)
        {
            network.Send(sender, new JoinRejectedMessage { Reason = reason });
            network.Kick(sender, reason);
            return;
        }

        string name = message.Name.Trim();
        PlayerRegistry.Add(new ConnectedPlayer { Peer = sender, Name = name, SkinColor = message.SkinColor });

        var info = World.WorldData.Info;
        network.Send(sender, new JoinAcceptedMessage
        {
            ServerName = info.Name,
            WorldName = info.Name,
            Seed = info.Seed,
            ModId = info.ModId,
            GameMode = (byte)info.GameMode,
            Day = GameTime.Day,
            Ticks = GameTime.DayTick,
            PlayerJson = PlayerSaveLoadManager.ReadPlayerJson(World.WorldData.WorldFolderPath, name, false)
        });

        Debug.Log($"[Server] {name} is joining (peer {sender})");
    }

    private static string ValidateJoin(JoinRequestMessage message)
    {
        if (message.GameVersion != Application.Version) return $"Version mismatch: server {Application.Version}, client {message.GameVersion}";
        if (string.IsNullOrWhiteSpace(message.Name)) return "Invalid name";
        if (string.Equals(message.Name.Trim(), GameSession.PlayerName, StringComparison.OrdinalIgnoreCase) && GameSession.HasLocalPlayer) return "Name is already taken";
        if (PlayerRegistry.TryGetByName(message.Name.Trim(), out _)) return "Name is already taken";
        return null;
    }

    private void OnClientReady(int sender, ClientReadyMessage message)
    {
        var network = NetworkManager.Instance;
        if (!network.IsServer || !PlayerRegistry.TryGet(sender, out var player) || player.PlayerObject != null) return;

        network.SendSpawnedObjectsTo(sender);
        DropNetwork.SendAllTo(sender);
        player.PlayerObject = _playerSpawner.SpawnPlayer(sender, player.Name, player.SkinColor, Vector3.Zero, Quaternion.Identity);
        ChatService.Broadcast($"{player.Name} joined the game", Color4.Yellow);
    }

    private void OnPlayerData(int sender, PlayerDataMessage message)
    {
        if (!NetworkManager.Instance.IsServer || !PlayerRegistry.TryGet(sender, out var player) || string.IsNullOrEmpty(message.Json)) return;

        player.LastPlayerJson = message.Json;
        if (World.WorldData != null)
            PlayerSaveLoadManager.WritePlayerJson(World.WorldData.WorldFolderPath, player.Name, message.Json);
    }

    private void OnPeerDisconnected(int peer, string reason)
    {
        var network = NetworkManager.Instance;
        network.Spawner.DespawnOwnedBy(peer);

        if (!PlayerRegistry.Remove(peer, out var player)) return;

        if (!string.IsNullOrEmpty(player.LastPlayerJson) && World.WorldData != null)
            PlayerSaveLoadManager.WritePlayerJson(World.WorldData.WorldFolderPath, player.Name, player.LastPlayerJson);

        ChatService.Broadcast($"{player.Name} left the game", Color4.Yellow);
    }

    private void OnDisconnectedFromServer(string reason)
    {
        Debug.Error($"[Network] Disconnected from server: {reason}");
        SceneManager.Load<MenuScene>();
    }
}
