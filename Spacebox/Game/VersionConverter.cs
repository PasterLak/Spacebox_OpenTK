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

        public static bool IsVersionOld(string worldVersion,string appVersion)
        {
            return IsVersionOlder(worldVersion, appVersion);
        }


        public static bool Convert(WorldInfo worldInfo, string appVersion)
        {

            //if (worldInfo.GameVersion == appVersion) return true;
           
            if (worldInfo.GameVersion == "0.0.8" )
            {
                worldInfo.GameVersion = "0.0.9";

                return ConvertToNextVersion(worldInfo,appVersion);
            }

            Debug.Error($"[VersionConverter] Failed to convert map version {worldInfo.GameVersion} to newer {appVersion} !");

            return false;
        }

        private static bool ConvertToNextVersion(WorldInfo worldInfo, string appVersion)
        {
            if (worldInfo.GameVersion == appVersion) return true;

            return Convert(worldInfo,appVersion);
        }
    }
}
