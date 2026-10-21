using Engine;
using Engine.SceneManagement;
using Spacebox.Game.GUI;
using Spacebox.Network;
using Spacebox.Scenes;

namespace Spacebox.Agent;

public static class AgentWorldLauncher
{
    private const string GameSetId = "default";
    private const string PlayerName = "Agent";
    private const string LogTag = "Agent";

    public static bool Launch(AgentOptions options)
    {
        if (options.FreshWorld) DeleteWorld(options.WorldName);

        var world = WorldProvisioner.Ensure(options.WorldName, PlayerName, options.GameMode, options.Seed, GameSetId, LogTag);
        if (world == null)
        {
            Debug.Error($"[Agent] World '{options.WorldName}' cannot be loaded");
            return false;
        }

        GameSession.SetPlayer(PlayerName, "Yellow");
        GameSession.BeginSingleplayer();
        Input.HideCursor();

        SceneManager.Load<LocalSpaceScene, SpaceSceneArgs>(new SpaceSceneArgs
        {
            worldName = world.Name,
            modId = world.ModId,
            seed = world.Seed,
            modfolderName = GameSetLocator.FindFolder(world.ModId),
            nickname = PlayerName,
            skinColor = "Yellow"
        });

        Debug.Success($"[Agent] Loading world '{world.Name}' ({world.GameMode})");
        return true;
    }

    private static void DeleteWorld(string worldName)
    {
        string root = Path.GetFullPath(WorldProvisioner.WorldsDirectory);
        string directory = Path.GetFullPath(Path.Combine(root, worldName));
        if (!directory.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || !Directory.Exists(directory)) return;

        Directory.Delete(directory, true);
        Debug.Warning($"[Agent] Deleted world '{worldName}' (--agent-fresh)");
    }
}
