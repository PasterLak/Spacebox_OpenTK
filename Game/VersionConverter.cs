using Spacebox.Common;
using Spacebox.Game.GUI;

namespace Spacebox.Game
{
    public class VersionConverter
    {

        public static bool IsVersionOlder(string worldVersion, string currentVersion)
        {
            Version worldVer, currentVer;
            if (Version.TryParse(worldVersion, out worldVer) && Version.TryParse(currentVersion, out currentVer))
            {
                return worldVer < currentVer;
            }
            return false;
        }

        public static bool IsVersionOld(string worldVersion)
        {
            return IsVersionOlder(worldVersion, Application.Version);
        }


        public static bool Convert(WorldInfo worldInfo)
        {
           
            if (worldInfo.GameVersion == "0.0.8" )
            {
                worldInfo.GameVersion = "0.0.9";

                return ConvertToNextVersion(worldInfo);
            }

            Debug.Error($"[VersionConverter] Failed to convert map version {worldInfo.GameVersion} to newer {Application.Version} !");

            return false;
        }

        private static bool ConvertToNextVersion(WorldInfo worldInfo)
        {
            if (worldInfo.GameVersion == Application.Version) return true;

            return Convert(worldInfo);
        }
    }
}
