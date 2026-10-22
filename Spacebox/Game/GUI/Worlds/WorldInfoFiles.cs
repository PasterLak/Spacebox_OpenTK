using System.Text.Json;
using Engine;
using Spacebox.Game.Generation.Tools.SaveData;
using Spacebox.Game.Versioning;

namespace Spacebox.Game.GUI;

public static class WorldInfoFiles
{
    private static readonly JsonSerializerOptions ReadOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, AllowTrailingCommas = true };

    public static string PathIn(string worldFolder) => Path.Combine(worldFolder, WorldFileLayout.WorldFile);

    public static string LegacyPathIn(string worldFolder) => Path.Combine(worldFolder, WorldFileLayout.LegacyWorldFile);

    public static bool Exists(string worldFolder)
    {
        return !string.IsNullOrEmpty(worldFolder) && SaveDataFile.Exists(PathIn(worldFolder), LegacyPathIn(worldFolder));
    }

    public static bool TryRead(string worldFolder, out WorldInfo worldInfo)
    {
        worldInfo = null;
        if (!Exists(worldFolder)) return false;

        try
        {
            worldInfo = SaveDataFile.Read<WorldInfo>(PathIn(worldFolder), LegacyPathIn(worldFolder), ReadOptions);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException or NotSupportedException)
        {
            Debug.Warning($"[Worlds] Skipping unreadable world file in {worldFolder}: {ex.Message}");
        }

        return worldInfo != null;
    }

    public static void Write(string worldFolder, WorldInfo worldInfo)
    {
        SaveDataFile.Write(PathIn(worldFolder), worldInfo, LegacyPathIn(worldFolder));
    }
}
