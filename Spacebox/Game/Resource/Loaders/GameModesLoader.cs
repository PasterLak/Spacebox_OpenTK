using Engine;
using Engine.Utils;
using Spacebox.Game.Player.GameModes;
using Spacebox.Game.Resource.Content;

namespace Spacebox.Game.Resource;

public static class GameModesLoader
{
    public const string FolderName = "GameModes";

    public static void Load(GameSetFiles files)
    {
        var modes = new List<GameModeDefinition>();
        foreach (var file in files.Folder(FolderName, false))
        {
            if (TryConvert(GameSetFiles.Deserialize<GameModeJSON>(file.Data), file.Source, out var mode)) modes.Add(mode);
        }
        GameModeCatalog.Set(modes);
    }

    private static bool TryConvert(GameModeJSON json, string source, out GameModeDefinition mode)
    {
        if (GameModeParser.TryConvert(json, out mode, out string error)) return true;
        Debug.Error($"[GameModes] {Path.GetFileName(source)}: {error}");
        return false;
    }

    public static List<GameModeDefinition> ReadFolder(string directory)
    {
        var modes = new List<GameModeDefinition>();
        if (!Directory.Exists(directory)) return modes;

        foreach (var file in Directory.GetFiles(directory, "*.json"))
        {
            if (TryConvert(JsonFixer.LoadJsonSafe<GameModeJSON>(file), file, out var mode)) modes.Add(mode);
        }
        return modes;
    }
}
