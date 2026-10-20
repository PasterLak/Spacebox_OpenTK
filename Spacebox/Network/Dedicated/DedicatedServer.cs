using System.Text.Json;
using Engine;
using Engine.Commands;
using Engine.Networking;
using Engine.SceneManagement;
using Spacebox.Game.GUI;
using Spacebox.Game.Player.GameModes;
using Spacebox.Scenes;

namespace Spacebox.Network.Dedicated;

public static class DedicatedServer
{
    public static ServerConfig Config { get; private set; }

    public static void Launch()
    {
        Debug.MirrorToStandardOutput = true;
        Config = ServerConfig.LoadOrCreate();
        FrameLimiter.IsRunning = true;
        FrameLimiter.TargetFPS = Config.GetClampedUpdateRate();

        NetworkManager.Instance.MaxPeers = Math.Max(1, Config.MaxPlayers);
        GameSession.SetPlayer(Config.ServerName, "White");
        GameSession.BeginDedicated(Config.Port);

        var world = EnsureWorld(Config);
        if (world == null)
        {
            Debug.Error("[Server] The world cannot be loaded, the server stops");
            Application.Quit();
            return;
        }

        RegisterCommands();
        StartConsoleReader();
        Dashboard.DashboardService.Start(Config);

        Debug.Success($"[Server] Starting '{Config.ServerName}' with world '{world.Name}' on port {Config.Port}");

        SceneManager.Load<LocalSpaceScene, SpaceSceneArgs>(new SpaceSceneArgs
        {
            worldName = world.Name,
            modId = world.ModId,
            seed = world.Seed,
            modfolderName = GameSetLocator.FindFolder(world.ModId)
        });
    }

    private static bool UpgradeIfNeeded(WorldInfo world, string worldDirectory, string worldJson)
    {
        var status = Spacebox.Game.VersionConverter.GetSupport(world, out var reason);
        if (status == Spacebox.Game.Versioning.WorldUpgradeStatus.UpToDate) return true;
        if (status != Spacebox.Game.Versioning.WorldUpgradeStatus.Ok)
        {
            Debug.Error($"[Server] World '{world.Name}': {reason}");
            return false;
        }

        Debug.Warning($"[Server] World '{world.Name}' is from version {world.GameVersion}, converting it to {Application.Version}");
        var result = Spacebox.Game.VersionConverter.Upgrade(world, worldDirectory, Application.Version);
        if (!result.Success) return false;

        File.WriteAllText(worldJson, JsonSerializer.Serialize(world, new JsonSerializerOptions { WriteIndented = true }));
        Debug.Success($"[Server] World converted to {result.ToVersion} ({result.Summary}), backup: {result.BackupPath}");
        return true;
    }

    private static WorldInfo EnsureWorld(ServerConfig config)
    {
        string worldsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Worlds");
        string worldDirectory = Path.Combine(worldsDirectory, config.WorldName);
        string worldJson = Path.Combine(worldDirectory, "world.json");

        if (File.Exists(worldJson))
        {
            var existing = JsonSerializer.Deserialize<WorldInfo>(File.ReadAllText(worldJson));
            if (existing != null) return UpgradeIfNeeded(existing, worldDirectory, worldJson) ? existing : null;
        }

        var info = new WorldInfo
        {
            Name = config.WorldName,
            Author = config.ServerName,
            GameMode = (GameMode)config.GameMode,
            Seed = string.IsNullOrWhiteSpace(config.Seed) ? new Random().Next().ToString() : config.Seed,
            ModId = config.ModId,
            GameVersion = Application.Version,
            LastEditDate = WorldInfo.GetCurrentDate(),
            FolderName = config.WorldName,
            ShowWelcomeWindow = false
        };

        Directory.CreateDirectory(worldDirectory);
        File.WriteAllText(worldJson, JsonSerializer.Serialize(info, new JsonSerializerOptions { WriteIndented = true }));
        Debug.Warning($"[Server] Created new world '{info.Name}'");
        return info;
    }

    private static void RegisterCommands()
    {
        CommandManager.RegisterGlobalCommand(new SayCommand());
        CommandManager.RegisterGlobalCommand(new PlayersCommand());
        CommandManager.RegisterGlobalCommand(new KickCommand());
        CommandManager.RegisterGlobalCommand(new BanCommand());
        CommandManager.RegisterGlobalCommand(new UnbanCommand());
        CommandManager.RegisterGlobalCommand(new UnregisterCommand());
        CommandManager.RegisterGlobalCommand(new MuteCommand(true));
        CommandManager.RegisterGlobalCommand(new MuteCommand(false));
        CommandManager.RegisterGlobalCommand(new SaveCommand());
        CommandManager.RegisterGlobalCommand(new StopCommand());
        CommandManager.RegisterGlobalCommand(new RestartCommand());
        CommandManager.RegisterGlobalCommand(new ServerGameModeCommand());
        CommandManager.RegisterGlobalCommand(new ServerTeleportCommand());
        CommandManager.RegisterGlobalCommand(new ServerTeleportToPlayerCommand());
    }

    private static void StartConsoleReader()
    {
        var thread = new Thread(() =>
        {
            while (true)
            {
                string line;
                try
                {
                    line = Console.ReadLine();
                }
                catch (IOException)
                {
                    return;
                }

                if (line == null) return;
                if (string.IsNullOrWhiteSpace(line)) continue;

                EngineWindow._mainThreadActions.Enqueue(() => Debug.ExecuteCommand(line));
            }
        })
        {
            IsBackground = true,
            Name = "ServerConsole"
        };
        thread.Start();
    }
}
