using Engine.SceneManagment;
using Spacebox.Game.GUI;
using Spacebox.Scenes;
using static Spacebox.Game.Resources.GameSetLoader;

namespace Spacebox.Client
{
    public static class SceneLauncher
    {
        public static void LaunchLocalGame(WorldInfo world, ModConfig modConfig) // // name mod seed modfolder
        {
            var args = new List<string>
            {
                world.Name,
                world.ModId,
                world.Seed,
                modConfig.FolderName
            };
            SceneManager.LoadScene(typeof(LocalSpaceScene), args.ToArray());
        }

        public static void LaunchMultiplayerGame(WorldInfo world, ModConfig modConfig, ServerInfo serverInfo, string playerName, string appKey)
        {
            var args = new List<string>
            {
                world.Name,
                world.ModId,
                world.Seed,
                modConfig.FolderName,
                appKey,
                serverInfo.IP,
                serverInfo.Port.ToString(),
                playerName
            };
            SceneManager.LoadScene(typeof(MultiplayerLoadScene), args.ToArray());
        }
    }
}