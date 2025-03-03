using Spacebox.Game;

namespace Spacebox.Scenes
{
    public class ModPath
    {

        public ModPath() { }


        public static string GetModsPath(bool isMultiplayer, string serverName)
        {
            if (!isMultiplayer)
            {

                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Globals.GameSet.LocalFolder);
            }

            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Globals.GameSet.MultiplayerFolder, serverName, "GameSet");
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
