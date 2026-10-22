using System.Text.Json;
using Engine;
using Spacebox.Game.Player.GameModes;

namespace Spacebox.Game.GUI;

public static class WorldProvisioner
{
    public static string WorldsDirectory => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Worlds");

    public static WorldInfo Ensure(string worldName, string author, GameMode gameMode, string seed, string modId, string logTag, IReadOnlyList<string> extensions = null)
    {
        string worldDirectory = Path.Combine(WorldsDirectory, worldName);
        string worldJson = Path.Combine(worldDirectory, "world.json");

        if (File.Exists(worldJson))
        {
            var existing = JsonSerializer.Deserialize<WorldInfo>(File.ReadAllText(worldJson));
            if (existing != null) return UpgradeIfNeeded(existing, worldDirectory, worldJson, logTag) ? existing : null;
        }

        var info = new WorldInfo
        {
            Name = worldName,
            Author = author,
            GameMode = gameMode,
            Seed = string.IsNullOrWhiteSpace(seed) ? new Random().Next().ToString() : seed,
            ModId = modId,
            Extensions = new List<string>(Spacebox.Game.Resource.Content.WorldExtensionList.Normalize(extensions)),
            GameVersion = Application.Version,
            LastEditDate = WorldInfo.GetCurrentDate(),
            FolderName = worldName,
            ShowWelcomeWindow = false,
            GeneratorVersion = Spacebox.Game.Generation.GeneratorVersions.Current
        };

        Directory.CreateDirectory(worldDirectory);
        File.WriteAllText(worldJson, JsonSerializer.Serialize(info, new JsonSerializerOptions { WriteIndented = true }));
        Debug.Warning($"[{logTag}] Created new world '{info.Name}'");
        return info;
    }

    private static bool UpgradeIfNeeded(WorldInfo world, string worldDirectory, string worldJson, string logTag)
    {
        var status = VersionConverter.GetSupport(world, out var reason);
        if (status == Versioning.WorldUpgradeStatus.UpToDate) return true;
        if (status != Versioning.WorldUpgradeStatus.Ok)
        {
            Debug.Error($"[{logTag}] World '{world.Name}': {reason}");
            return false;
        }

        Debug.Warning($"[{logTag}] World '{world.Name}' is from version {world.GameVersion}, converting it to {Application.Version}");
        var result = VersionConverter.Upgrade(world, worldDirectory, Application.Version);
        if (!result.Success) return false;

        File.WriteAllText(worldJson, JsonSerializer.Serialize(world, new JsonSerializerOptions { WriteIndented = true }));
        Debug.Success($"[{logTag}] World converted to {result.ToVersion} ({result.Summary}), backup: {result.BackupPath}");
        return true;
    }
}
