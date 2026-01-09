using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerCommon
{
    public class ServerPreparer
    {
        private static bool LoadConfig()
        {
            try
            {
                ConfigManager.LoadConfig();
                return true;
            }
            catch (Exception ex)
            {
              
                return false;
            }
        }

        public static (bool Exists, string Name, string Path) GetModFolderInfo()
        {
            string gameSetPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GameSet");

            if (!System.IO.Directory.Exists(gameSetPath))
            {
                return (false, string.Empty, string.Empty);
            }

            string[] dirs = System.IO.Directory.GetDirectories(gameSetPath);

            if (dirs.Length == 1)
            {
                string fullPath = dirs[0];
                string folderName = System.IO.Path.GetFileName(fullPath);

                return (true, folderName, fullPath);
            }

            return (false, string.Empty, string.Empty);
        }

        public static bool IsServerReady(ILogger logger)
        {
            if (!LoadConfig())
            {
                logger.Log("Failed to load configuration.", LogType.Error);
                return false;
            }

            var mod = GetModFolderInfo();
            if (!mod.Exists)
            {
                logger.Log("Mod folder is missing or empty.", LogType.Error);
                return false;
            }

            Settings.ModFolderHash = HashHelper.CalculateFolderHash(mod.Path);
            Settings.ModFolder = mod.Name;

            if(HashHelper.VerifyFolderHash(mod.Path, Settings.ModFolderHash)) {
                logger.Log("Mod folder hash verified.", LogType.Success);
            } else {
                logger.Log("Mod folder hash does not match expected value.", LogType.Warning);
            }

            if (string.IsNullOrWhiteSpace(Settings.Key))
            {

                logger.Log("Server key is not set.", LogType.Error);
                return false;
            }
            if (Settings.Port <= 0 || Settings.Port > 65535)
            {
                logger.Log("Server port is invalid.", LogType.Error);
                return false;
            }
            if (Settings.MaxPlayers <= 0)
            {

                logger.Log("Max players must be greater than zero.", LogType.Error);
                return false;
            }
            return true;
        }
    }



     
}
