using Engine;
using Engine.Utils;
using Spacebox.Game.Player.GameModes;

namespace Spacebox.Game.Resource;

public static class GameModesLoader
{
    public const string FolderName = "GameModes";

    public static void Load(string modPath, string defaultModPath)
    {
        string directory = Path.Combine(modPath, FolderName);
        if (!Directory.Exists(directory)) directory = Path.Combine(defaultModPath, FolderName);

        var modes = new List<GameModeDefinition>();
        if (Directory.Exists(directory))
        {
            foreach (var file in Directory.GetFiles(directory, "*.json"))
            {
                var json = JsonFixer.LoadJsonSafe<GameModeJSON>(file);
                if (!GameModeParser.TryConvert(json, out var mode, out string error))
                {
                    Debug.Error($"[GameModes] {Path.GetFileName(file)}: {error}");
                    continue;
                }
                modes.Add(mode);
            }
        }

        GameModeCatalog.Set(modes);
    }
}
