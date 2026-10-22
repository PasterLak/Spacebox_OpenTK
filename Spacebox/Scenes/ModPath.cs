using Spacebox.Game;
using Spacebox.Game.Resource.Content;

namespace Spacebox.Scenes
{
    public class ModPath
    {

        public ModPath() { }


        public static ModFolders GetModFolders(bool useLocalGameSetsFolder, string serverName)
        {
            return useLocalGameSetsFolder ? ModFolders.Local : Spacebox.Network.MultiplayerFolders.ModCache(serverName);
        }

        public static string GetBlocksPath(string modsFolder, string modFolderName)
        {
            return Path.Combine(modsFolder, modFolderName, Globals.GameSet.Blocks);
        }
        public static string GetItemsPath(string modsFolder, string modFolderName)
        {
            return Path.Combine(modsFolder, modFolderName, Globals.GameSet.Items);

        }
        public static string GetEmissionsPath(string modsFolder, string modFolderName)
        {
            return Path.Combine(modsFolder, modFolderName, Globals.GameSet.Emissions);
        }

    }
}
