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

    private static ServerShutdownSignals _shutdownSignals;

    public static void WarnIfAdminsUnprotected(ServerConfig config)
    {
        if (config.Admins.Count > 0 && !Spacebox.Network.Admin.ServerAdmins.RightsActive(config))
            Debug.Warning("[Server] Admins are listed but RegistrationEnabled is false: player names are not checked, so admin rights stay off until registration is enabled");
    }

    public static void Launch()
    {
        Debug.MirrorToStandardOutput = true;
        RegisterCommands();
        StartConsoleReader();
        _shutdownSignals = new ServerShutdownSignals(action => EngineWindow._mainThreadActions.Enqueue(action), ServerRestart.Stop);
        _shutdownSignals.Register();
        StartWorld();
    }

    public static void Relaunch()
    {
        Dashboard.DashboardService.Stop();
        StartWorld();
    }

    private static void StartWorld()
    {
        Config = ServerConfig.LoadOrCreate();
        FrameLimiter.IsRunning = true;
        FrameLimiter.TargetFPS = Config.GetClampedUpdateRate();

        NetworkManager.Instance.MaxPeers = Math.Max(1, Config.MaxPlayers);
        Spacebox.Game.Mobs.MobRules.FriendlyMobs = Config.FriendlyMobs;
        WarnIfAdminsUnprotected(Config);
        GameSession.SetPlayer(Config.ServerName, "White");
        GameSession.BeginDedicated(Config.Port);

        var world = WorldProvisioner.Ensure(Config.WorldName, Config.ServerName, (GameMode)Config.GameMode, Config.Seed, Config.ModId, "Server");
        if (world == null)
        {
            Debug.Error("[Server] The world cannot be loaded, the server stops");
            Application.Quit();
            return;
        }

        if (!WorldGameSetCheck.TryResolve(world, out var gameSetError))
        {
            Debug.Error($"[Server] {gameSetError}");
            Debug.Error("[Server] Install the missing GameSets or edit \"Extensions\" in the world's world.json, the server stops");
            Application.Quit();
            return;
        }

        Dashboard.DashboardService.Start(Config);

        Debug.Success($"[Server] Starting '{Config.ServerName}' with world '{world.Name}' on port {Config.Port}");

        SceneManager.Load<LocalSpaceScene, SpaceSceneArgs>(new SpaceSceneArgs
        {
            worldName = world.Name,
            modId = world.ModId,
            extensions = WorldGameSetCheck.ExtensionsOf(world),
            seed = world.Seed,
            modfolderName = GameSetLocator.FindFolder(world.ModId)
        });
    }

    private static void RegisterCommands()
    {
        foreach (var command in ServerCommandCatalog.Create()) CommandManager.RegisterGlobalCommand(command);
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
