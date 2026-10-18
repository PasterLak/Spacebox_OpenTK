
using Spacebox.Game.GUI;
using Engine;
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


        private static readonly Dictionary<string, string> NextVersions = new Dictionary<string, string>
        {
            { "0.0.8", "0.0.9" },
            { "0.0.9", "0.1.0" },
            { "0.1.0", "0.1.1" },
            { "0.1.1", "0.1.2" }
        };

        public static bool Convert(WorldInfo worldInfo, string appVersion)
        {

            //if (worldInfo.GameVersion == appVersion) return true;
            if (!IsVersionOlder(worldInfo.GameVersion, appVersion)) return worldInfo.GameVersion == appVersion;

            while (worldInfo.GameVersion != appVersion)
            {
                if (!NextVersions.TryGetValue(worldInfo.GameVersion, out var next) || IsVersionOlder(appVersion, next))
                {
                    Debug.Error($"[VersionConverter] Failed to convert map version {worldInfo.GameVersion} to newer {appVersion} !");
                    return false;
                }

                worldInfo.GameVersion = next;
            }

            return true;
        }
    }
}
