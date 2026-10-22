
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


        public static Versioning.WorldUpgradeRegistry Registry { get; } = Versioning.WorldUpgradeRegistry.CreateDefault();
        public static Versioning.WorldUpgradeResult LastResult { get; private set; }

        public static bool Convert(WorldInfo worldInfo, string appVersion)
        {

            //if (worldInfo.GameVersion == appVersion) return true;
            return Upgrade(worldInfo, WorldInfoSaver.GetWorldFolder(worldInfo), appVersion).Success;
        }

        public static Versioning.WorldUpgradeStatus GetSupport(WorldInfo worldInfo, out string reason)
        {
            reason = null;
            if (worldInfo == null) return Versioning.WorldUpgradeStatus.Failed;
            return new Versioning.WorldUpgrader(Registry, null).Check(worldInfo.GameVersion, Engine.Application.Version, out reason);
        }

        public static Versioning.WorldUpgradeStatus CheckWorld(WorldInfo worldInfo, out string reason)
        {
            reason = null;
            if (worldInfo == null) return Versioning.WorldUpgradeStatus.Failed;
            return new Versioning.WorldUpgrader(Registry, null).Check(worldInfo, WorldInfoSaver.GetWorldFolder(worldInfo), Engine.Application.Version, out reason);
        }

        public static Versioning.WorldUpgradeResult Upgrade(WorldInfo worldInfo, string worldFolder, string appVersion)
        {
            var result = new Versioning.WorldUpgrader(Registry, null).Upgrade(worldInfo, worldFolder, appVersion, LoadIds(worldInfo, worldFolder));
            LastResult = result;

            if (result.Success) Debug.Log($"[VersionConverter] World '{worldInfo?.Name}' converted from {result.FromVersion} to {result.ToVersion}: {result.Summary}");
            else Debug.Error($"[VersionConverter] Failed to convert map version {result.FromVersion} to {appVersion}: {result.Error}");
            foreach (var warning in result.Warnings) Debug.Warning($"[VersionConverter] {warning}");
            return result;
        }

        private static Versioning.GameSetIdIndex LoadIds(WorldInfo worldInfo, string worldFolder)
        {
            if (worldInfo == null || string.IsNullOrEmpty(worldFolder)) return null;

            try
            {
                return Versioning.GameSetIdIndex.Load(Spacebox.Network.GameSetLocator.LocalDirectory, worldInfo.ModId, WorldGameSetCheck.ExtensionsOf(worldInfo));
            }
            catch (Exception ex)
            {
                Debug.Warning($"[VersionConverter] Could not read the GameSet ids, unknown ids are not checked: {ex.Message}");
                return null;
            }
        }

        public static IReadOnlyList<Versioning.IWorldUpgradeStep> GetSteps(string worldVersion, string appVersion)
        {
            return new Versioning.WorldUpgrader(Registry, null).GetPath(worldVersion, appVersion);
        }
    }
}
