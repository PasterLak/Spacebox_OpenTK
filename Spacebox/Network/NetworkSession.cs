using Engine;
using Engine.Networking;
using Engine.SceneManagement;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.Resource;
using Spacebox.Game.Player;
using Spacebox.Network.Messages;
using Spacebox.Game.Resource.Content;
using Spacebox.Network.Players;
using Spacebox.Scenes;

namespace Spacebox.Network;

public sealed class NetworkSession : IDisposable
{
    private const float PlayerSyncInterval = 10f;
    private static readonly TimeSpan LeaveSaveTimeout = TimeSpan.FromSeconds(2);
    public const float HostAutoSaveInterval = 300f;
    public const string AutosaveReason = "Autosave";

    private readonly LocalAstronaut _localPlayer;
    private readonly PlayerSpawnService _playerSpawner;
    private readonly Npc.SpacerSpawnService _spacerSpawner;
    private readonly Mobs.MobSpawnService _mobSpawner;
    private float _playerSyncTimer;
    private readonly AutoSaveTimer _autoSave = new AutoSaveTimer();
    private readonly PlayerDataFlush _playerDataFlush = new PlayerDataFlush();
    private readonly AdmissionWatch _admission = new AdmissionWatch();
    private bool _disposed;
    private int _playerDataSequence;
    private int _playerDataAcknowledged;
    private Auth.ServerAccessSettings _access;
    private readonly Auth.ServerAuthService _auth;

    public static NetworkSession Current { get; private set; }
    public Npc.SpacerSpawnService Spacers => _spacerSpawner;
    public Mobs.MobSpawnService Mobs => _mobSpawner;

    public NetworkSession(Node3D root, LocalAstronaut localPlayer)
    {
        Current = this;
        _localPlayer = GameSession.HasLocalPlayer ? localPlayer : null;
        Items.ItemSync.Reset();
        Items.ItemAuthority.Clear();

        var network = NetworkManager.Instance;
        network.ConnectionKey = "Spacebox:" + Application.Version;
        _playerSpawner = new PlayerSpawnService(root, _localPlayer);
        _spacerSpawner = new Npc.SpacerSpawnService(root);
        _mobSpawner = new Mobs.MobSpawnService(root);

        network.Messages.Register<JoinRequestMessage>(OnJoinRequest);
        network.Messages.Register<ClientReadyMessage>(OnClientReady);
        network.Messages.Register<PlayerDataMessage>(OnPlayerData);
        network.Messages.Register<PlayerDataRequestMessage>(OnPlayerDataRequest);
        network.Messages.Register<PlayerDataAckMessage>(OnPlayerDataAck);
        network.Messages.Register<AuthResponseMessage>(OnAuthResponse);
        network.Messages.AllowBeforeAdmission<JoinRequestMessage>();
        network.Messages.AllowBeforeAdmission<AuthResponseMessage>();
        network.RequireAdmission = true;
        network.PeerDisconnected += OnPeerDisconnected;
        network.PeerConnected += OnPeerConnected;
        network.DisconnectedFromServer += OnDisconnectedFromServer;

        if (World.DropEffectManager != null) World.DropEffectManager.Replicator = new DropNetwork();
        if (!GameSession.IsRemoteClient) Discovery.ServerAdvertiser.Start(CreateAdvertisement());

        _access = CreateAccessSettings();
        _auth = CreateAuthService(_access);
        if (network.IsListening) GameSetPackage.PrepareInBackground();
    }

    public bool RemoveAccount(string name) => _auth != null && _auth.RemoveAccount(name);

    public void ReloadAccessSettings()
    {
        _access = CreateAccessSettings();
    }

    public void RefreshAdvertisement()
    {
        if (GameSession.IsRemoteClient) return;

        Discovery.ServerAdvertiser.Stop();
        Discovery.ServerAdvertiser.Start(CreateAdvertisement());
    }

    private static Auth.ServerAccessSettings CreateAccessSettings()
    {
        var config = Dedicated.DedicatedServer.Config;
        if (!GameSession.IsDedicated || config == null) return Auth.ServerAccessSettings.Open;
        return new Auth.ServerAccessSettings(config.WhitelistEnabled, config.Whitelist, config.RegistrationEnabled);
    }

    private Auth.ServerAuthService CreateAuthService(Auth.ServerAccessSettings access)
    {
        if (!access.RegistrationEnabled || World.WorldData == null) return null;

        var accounts = new Auth.AccountStore(Path.Combine(World.WorldData.WorldFolderPath, Auth.AccountStore.FileName));
        return new Auth.ServerAuthService(accounts, (peer, challenge) => NetworkManager.Instance.Send(peer, challenge), RejectJoin, AcceptJoin,
            peer => NetworkManager.Instance.GetPeerAddress(peer), isReservedName: Admin.ServerAdmins.IsAdmin);
    }

    internal static Discovery.ServerAdvertisement CreateAdvertisement()
    {
        var config = Dedicated.DedicatedServer.Config;
        if (GameSession.IsDedicated && config != null)
        {
            return new Discovery.ServerAdvertisement
            {
                Name = config.ServerName,
                Description = config.Description,
                IconPath = config.IconFile,
                LanDiscovery = config.LanDiscovery,
                MaxPlayers = config.MaxPlayers,
                IsDedicated = true
            };
        }

        return new Discovery.ServerAdvertisement
        {
            Description = $"{GameSession.PlayerName}'s game",
            MaxPlayers = NetworkManager.Instance.MaxPeers + 1
        };
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
        GameSetPackage.PrepareInBackground();

        ChatService.Broadcast($"Server is open on port {port}", Color4.Yellow);
        return true;
    }

    public void FlushPlayerData(Action completed)
    {
        var network = NetworkManager.Instance;
        if (!network.IsServer)
        {
            completed?.Invoke();
            return;
        }

        var peers = new List<int>();
        foreach (var player in PlayerRegistry.All)
            if (player.Peer != network.LocalPeerId) peers.Add(player.Peer);

        if (peers.Count > 0) Debug.Log($"[Server] Asking {peers.Count} player(s) for their latest data");
        _playerDataFlush.Start(peers, () => network.SendToClients(new PlayerDataRequestMessage(), DeliveryMode.Reliable), completed);
    }

    private Dedicated.ServerActivity _activity;
    private bool _sleepRequested;

    private void UpdateActivity()
    {
        var config = Dedicated.DedicatedServer.Config;
        if (!GameSession.IsDedicated || config == null || _sleepRequested) return;

        _activity ??= new Dedicated.ServerActivity(config.GetClampedUpdateRate(), config.IdleUpdateRate, config.SleepAfterMinutes);
        _activity.Configure(config.GetClampedUpdateRate(), config.IdleUpdateRate, config.SleepAfterMinutes);
        var state = _activity.Update(NetworkManager.Instance.Peers.Count, Time.Delta);
        FrameLimiter.TargetFPS = _activity.TargetRate;

        if (state != Dedicated.ServerActivityState.Sleep) return;
        _sleepRequested = true;
        EngineWindow._mainThreadActions.Enqueue(() => Dedicated.ServerSleep.Enter(config.SleepAfterMinutes));
    }

    public void Update()
    {
        UpdateActivity();
        if (_playerDataFlush.Update(Time.Delta))
            Debug.Warning("[Server] Some players did not send their latest data in time, saving the last known state");

        if (NetworkManager.Instance.IsServer)
        {
            _auth?.Update(Time.Delta);
            UpdateAdmission();
            WorldEditNetwork.Update();
            Combat.PlayerHitboxes.Record();
            UpdateObservers();
        }
        Containers.ContainerNetwork.Update();
        AnalyzerNetwork.Update();
        if (NetworkManager.Instance.IsServer) Spacebox.Game.Generation.Blocks.PipeRegistry.Update(Time.Delta);
        BlockStateNetwork.Update();
        TimeNetwork.Update();
        SectorNetwork.Update();
        EntitySync.Update();
        PlayerListNetwork.Update();
        Players.ItemGiftNetwork.Update();
        Players.ReconstructionNetwork.Update();
        Players.AstronautNetwork.UpdateGifts();
        UpdateAutoSave();
        if (NetworkManager.Instance.IsRemoteClient) RemoteEntityBacklog.Update();
        if (!NetworkManager.Instance.IsRemoteClient || _localPlayer == null) return;

        _playerSyncTimer += Time.Delta;
        if (_playerSyncTimer < PlayerSyncInterval) return;

        _playerSyncTimer = 0f;
        SendLocalPlayerData();
    }

    private readonly List<Vector3> _observerPositions = new List<Vector3>();

    private void UpdateObservers()
    {
        _observerPositions.Clear();
        int localPeer = NetworkManager.Instance.LocalPeerId;

        foreach (var networkObject in NetworkManager.Instance.Spawner.Objects)
        {
            if (networkObject.OwnerPeer == localPeer || networkObject.Owner is not Astronaut) continue;
            var transform = networkObject.GetBehaviour<AstronautNetwork>();
            if (transform != null) _observerPositions.Add(transform.TargetPosition);
        }

        WorldObservers.SetRemote(_observerPositions);
    }

    public void SaveWorld(string reason)
    {
        var world = World.Instance;
        if (world == null || World.WorldData == null || GameSession.IsRemoteClient) return;

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            world.Save(!GameSession.IsDedicated);
            SaveRemotePlayers();
            Statistics.ServerStatisticsService.Save();
            _autoSave.Reset();
            Debug.Success($"[Server] {reason}: world '{World.WorldData.Info?.Name}' saved in {stopwatch.ElapsedMilliseconds} ms, {PlayerRegistry.All.Count} player(s) online");
            if (GameSession.HasLocalPlayer) Spacebox.Game.GUI.Chat.Write(reason == AutosaveReason ? "Game autosaved" : "Game saved", OpenTK.Mathematics.Color4.LightGreen);
        }
        catch (Exception ex)
        {
            Debug.Error($"[Server] {reason} failed: {ex.Message}");
        }
    }

    private void UpdateAutoSave()
    {
        if (!NetworkManager.Instance.IsServer || GameSession.IsRemoteClient) return;
        if (Spacebox.Game.GUI.WorldLoadingScreen.IsActive) return;

        if (Spacebox.Game.Backups.WorldBackupService.IsRunning) return;

        float interval = GameSession.IsDedicated ? Dedicated.DedicatedServer.Config?.AutoSaveInterval ?? HostAutoSaveInterval : Spacebox.Game.Settings.Gameplay.AutosaveIntervalSeconds;
        if (_autoSave.Tick(Time.Delta, interval)) SaveWorld(AutosaveReason);
        UpdateBackups();
    }

    private float _backupTimer;
    private bool _playedSinceBackup;

    private void UpdateBackups()
    {
        var settings = Dedicated.DedicatedServer.Config?.Backups;
        if (!GameSession.IsDedicated || settings == null || !settings.Enabled || World.WorldData == null) return;

        if (NetworkManager.Instance.Peers.Count > 0) _playedSinceBackup = true;
        _backupTimer += Time.Delta;
        if (_backupTimer < settings.IntervalSeconds) return;

        _backupTimer = 0f;
        if (!_playedSinceBackup) return;
        _playedSinceBackup = false;

        SaveWorld("Backup");
        Spacebox.Game.Backups.WorldBackupService.TryStart(World.WorldData.WorldFolderPath, Spacebox.Game.Backups.WorldBackupService.AutoLabel, settings.KeepLast, (path, error) =>
        {
            if (error == null) Debug.Success($"[Backup] Automatic backup saved: {Spacebox.Game.Backups.WorldBackupService.Describe(path)}");
            else Debug.Error($"[Backup] Automatic backup failed: {error}");
        });
    }

    public void SaveRemotePlayers()
    {
        if (!NetworkManager.Instance.IsServer || World.WorldData == null) return;

        foreach (var player in PlayerRegistry.All)
        {
            if (player.Peer == NetworkManager.Instance.LocalPeerId || string.IsNullOrEmpty(player.LastPlayerJson)) continue;
            player.LastPlayerJson = Items.ItemAuthority.ReconcileForSave(player.Peer, player.LastPlayerJson);
            PlayerSaveLoadManager.WritePlayerJson(World.WorldData.WorldFolderPath, player.Name, Players.PlayerGameModes.Current.ApplyTo(player.Name, player.LastPlayerJson));
        }
    }

    private void SendLocalPlayerDataAndWait()
    {
        var network = NetworkManager.Instance;
        if (_localPlayer == null || !network.IsConnected) return;

        SendLocalPlayerData();
        int expected = _playerDataSequence;
        if (!network.PollUntil(() => _playerDataAcknowledged == expected, LeaveSaveTimeout))
            Debug.Warning("[Network] The server did not confirm the player data before leaving");
    }

    private void RequestRemotePlayerDataAndWait()
    {
        var network = NetworkManager.Instance;
        if (!network.IsListening) return;

        FlushPlayerData(null);
        if (_playerDataFlush.IsRunning && !network.PollUntil(() => !_playerDataFlush.IsRunning, LeaveSaveTimeout))
            Debug.Warning("[Server] Some players did not send their latest data before the server closed");
    }

    private void OnPlayerDataAck(int sender, PlayerDataAckMessage message)
    {
        _playerDataAcknowledged = message.Sequence;
    }

    public void SendLocalPlayerData()
    {
        if (_localPlayer == null || !NetworkManager.Instance.IsRemoteClient) return;

        var json = PlayerSaveLoadManager.Serialize(_localPlayer);
        if (json != null) NetworkManager.Instance.SendToServer(new PlayerDataMessage { Json = json, AppliedSeq = Items.ItemSync.AppliedSeq, Sequence = ++_playerDataSequence });
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        var network = NetworkManager.Instance;
        SendLocalPlayerDataAndWait();
        RequestRemotePlayerDataAndWait();
        SaveRemotePlayers();

        network.PeerDisconnected -= OnPeerDisconnected;
        network.PeerConnected -= OnPeerConnected;
        network.DisconnectedFromServer -= OnDisconnectedFromServer;
        network.Messages.Unregister<JoinRequestMessage>();
        network.Messages.Unregister<ClientReadyMessage>();
        network.Messages.Unregister<PlayerDataMessage>();
        network.Messages.Unregister<PlayerDataRequestMessage>();
        network.Messages.Unregister<PlayerDataAckMessage>();
        network.Messages.Unregister<AuthResponseMessage>();
        network.RequireAdmission = false;

        network.Shutdown("Server stopped");
        Discovery.ServerAdvertiser.Stop();
        Combat.CombatNetwork.Reset();
        WorldEditNetwork.Reset();
        RemoteEntityBacklog.Clear();
        Items.ItemAuthority.Clear();
        Items.ItemSync.Reset();
        Players.ItemGiftNetwork.Reset();
        Players.ReconstructionNetwork.Reset();
        Players.AstronautNetwork.ClearGifts();
        EntitySync.Clear();
        Spacebox.Network.Mobs.MobSpawnNetwork.Reset();
        Containers.ContainerNetwork.Reset();
        BlockStateNetwork.Clear();
        AnalyzerNetwork.Reset();
        Spacebox.Game.Generation.Blocks.PipeRegistry.Clear();
        SectorNetwork.Clear();
        PlayerListNetwork.Clear();
        Moderation.ServerModeration.Reset();
        _playerSpawner.Dispose();
        _spacerSpawner.Dispose();
        _mobSpawner.Dispose();
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
        _admission.Requested(sender);
        if (PlayerRegistry.TryGet(sender, out _)) return;

        string reason = ValidateJoin(sender, message);
        if (reason != null)
        {
            RejectJoin(sender, reason);
            return;
        }

        if (_auth != null)
        {
            Debug.Log($"[Server] {message.Name.Trim()} is logging in (peer {sender})");
            _auth.Begin(sender, message);
            return;
        }

        AcceptJoin(sender, message);
    }

    private void OnAuthResponse(int sender, AuthResponseMessage message)
    {
        if (!NetworkManager.Instance.IsServer || sender == NetworkManager.Instance.LocalPeerId) return;
        _auth?.OnResponse(sender, message);
    }

    private static void RejectJoin(int peer, string reason)
    {
        var network = NetworkManager.Instance;
        network.Send(peer, new JoinRejectedMessage { Reason = reason });
        network.Kick(peer, reason);
    }

    private void AcceptJoin(int sender, JoinRequestMessage message)
    {
        var network = NetworkManager.Instance;
        string name = message.Name.Trim();
        PlayerRegistry.Add(new ConnectedPlayer { Peer = sender, Name = name, SkinColor = SafeSkinColor(message.SkinColor) });
        network.AdmitPeer(sender);

        var info = World.WorldData.Info;
        string playerJson = PlayerSaveLoadManager.ReadPlayerJson(World.WorldData.WorldFolderPath, name, false);
        Items.ItemAuthority.OnJoin(sender, name, playerJson);
        network.Send(sender, new JoinAcceptedMessage
        {
            ServerName = Discovery.ServerAdvertiser.ServerName,
            WorldName = info.Name,
            Seed = info.Seed,
            ModId = info.ModId,
            GameMode = (byte)Players.PlayerGameModes.Current.Get(name, info.GameMode),
            Day = GameTime.Day,
            Ticks = GameTime.DayTick,
            PlayerJson = playerJson,
            GameSetHash = GameSetPackage.Hash,
            GameSetFolder = GameSetPackage.FolderName,
            Extensions = WorldExtensionList.Format(GameSetLoader.Stack?.ExtensionIds),
            TexturePacks = WorldExtensionList.Format(GameSetPackage.TexturePackIds),
            HasSpawnPoint = Spacebox.Game.Player.Respawn.WorldSpawn.TryGet(out var spawnPoint),
            SpawnPoint = spawnPoint,
            GeneratorVersion = Spacebox.Game.Generation.World.GeneratorVersion
        });

        Debug.Log($"[Server] {name} is joining (peer {sender})");
    }

    private string ValidateJoin(int sender, JoinRequestMessage message)
    {
        if (message.GameVersion != Application.Version) return $"Version mismatch: server {Application.Version}, client {message.GameVersion}";
        string nameError = PlayerNameRules.Validate(message.Name);
        if (nameError != null) return nameError;
        if (Moderation.ServerModeration.IsBanned(message.Name.Trim(), NetworkManager.Instance.GetPeerAddress(sender))) return Moderation.ServerModeration.BannedReason;
        if (string.Equals(message.Name.Trim(), GameSession.PlayerName, StringComparison.OrdinalIgnoreCase) && GameSession.HasLocalPlayer) return "Name is already taken";
        if (PlayerRegistry.TryGetByName(message.Name.Trim(), out var existing) && !TryReplaceStalePeer(existing, sender)) return "Name is already taken";
        if (!_access.IsAllowed(message.Name)) return "You are not on the whitelist of this server";
        return null;
    }

    public static string SafeSkinColor(string color)
    {
        if (string.IsNullOrEmpty(color) || color.Length > 16) return "Yellow";
        foreach (char c in color)
            if (!char.IsAsciiLetter(c)) return "Yellow";
        return color;
    }

    private bool TryReplaceStalePeer(ConnectedPlayer existing, int sender)
    {
        var network = NetworkManager.Instance;
        string address = network.GetPeerAddress(sender);
        if (existing.Peer == network.LocalPeerId || string.IsNullOrEmpty(address) || address != network.GetPeerAddress(existing.Peer)) return false;

        Debug.Log($"[Server] {existing.Name} reconnected from the same address, the old connection (peer {existing.Peer}) is closed");
        OnPeerDisconnected(existing.Peer, "Reconnected");
        network.Kick(existing.Peer, "You connected again from another window");
        return true;
    }

    private void OnClientReady(int sender, ClientReadyMessage message)
    {
        var network = NetworkManager.Instance;
        if (!network.IsServer || !PlayerRegistry.TryGet(sender, out var player) || player.PlayerObject != null) return;

        network.SendSpawnedObjectsTo(sender);
        DropNetwork.SendAllTo(sender);
        TimeNetwork.SendTo(sender);
        ProtectionNetwork.SendTo(sender);
        ClaimNetwork.SendSettingsTo(sender);
        ServerRulesNetwork.SendTo(sender);
        player.PlayerObject = _playerSpawner.SpawnPlayer(sender, player.Name, player.SkinColor, Vector3.Zero, Quaternion.Identity);
        PlayerListNetwork.MarkDirty();
        Statistics.ServerStatisticsService.PlayerJoined(player.Name, PlayerRegistry.All.Count);
        ChatService.Broadcast($"{player.Name} joined the game", Color4.Yellow);
    }

    private void OnPlayerDataRequest(int sender, PlayerDataRequestMessage message)
    {
        if (NetworkManager.Instance.IsRemoteClient) SendLocalPlayerData();
    }

    private void OnPlayerData(int sender, PlayerDataMessage message)
    {
        _playerDataFlush.Received(sender);
        if (!NetworkManager.Instance.IsServer || !PlayerRegistry.TryGet(sender, out var player) || string.IsNullOrEmpty(message.Json)) return;

        string json = Items.ItemAuthority.AcceptSnapshot(sender, message.Json, message.AppliedSeq);
        if (json == null) return;

        player.LastPlayerJson = Players.PlayerGameModes.Current.ApplyTo(player.Name, json);
        if (World.WorldData != null)
            PlayerSaveLoadManager.WritePlayerJson(World.WorldData.WorldFolderPath, player.Name, player.LastPlayerJson);
        NetworkManager.Instance.Send(sender, new PlayerDataAckMessage { Sequence = message.Sequence }, DeliveryMode.Reliable);
    }

    private void OnPeerConnected(int peer)
    {
        if (NetworkManager.Instance.IsServer && peer != NetworkManager.Instance.LocalPeerId) _admission.Connected(peer);
    }

    private void UpdateAdmission()
    {
        var network = NetworkManager.Instance;
        var expired = _admission.Update(Time.Delta, network.IsAdmitted);
        for (int i = 0; i < expired.Count; i++)
        {
            Debug.Warning($"[Server] Peer {expired[i]} did not finish joining in time and was disconnected");
            network.Kick(expired[i], "Join timed out");
        }
    }

    private void OnPeerDisconnected(int peer, string reason)
    {
        _admission.Forget(peer);
        var network = NetworkManager.Instance;
        network.Spawner.DespawnOwnedBy(peer);
        _auth?.Remove(peer);
        Players.AstronautNetwork.ForgetGiftsOf(peer);

        _playerDataFlush.Received(peer);
        WorldEditNetwork.ForgetPeer(peer);
        ChatService.ForgetPeer(peer);
        Players.ReconstructionNetwork.ForgetPeer(peer);
        if (PlayerRegistry.TryGet(peer, out var leaving)) leaving.LastPlayerJson = Items.ItemAuthority.ReconcileForSave(peer, leaving.LastPlayerJson);
        Items.ItemAuthority.OnLeave(peer);
        if (!PlayerRegistry.Remove(peer, out var player)) return;

        if (!string.IsNullOrEmpty(player.LastPlayerJson) && World.WorldData != null)
            PlayerSaveLoadManager.WritePlayerJson(World.WorldData.WorldFolderPath, player.Name, Players.PlayerGameModes.Current.ApplyTo(player.Name, player.LastPlayerJson));

        PlayerListNetwork.MarkDirty();
        Statistics.ServerStatisticsService.PlayerLeft(player.JoinedAtUtc);
        ChatService.Broadcast($"{player.Name} left the game", Color4.Yellow);
    }

    private void OnDisconnectedFromServer(string reason)
    {
        Debug.Error($"[Network] Disconnected from server: {reason}");
        Spacebox.Game.GUI.ConnectionScreen.ShowMessage("Disconnected", DisconnectReasons.Describe(reason));
        SceneManager.Load<MenuScene>();
    }
}
