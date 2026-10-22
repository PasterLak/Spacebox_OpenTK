using Engine;
using Engine.Utils;
using Spacebox.Game.Generation.Structures;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Player.Respawn;

public class SpawnParametersJSON
{
    public List<GameSetLoader.DefaultItem> GiveItemsOnStart { get; set; } = new List<GameSetLoader.DefaultItem>();
    public PlayerSpawnJSON SpawnSettings { get; set; } = new PlayerSpawnJSON();
}

public static class SpawnParameters
{
    public const string RelativePath = "Player/spawn_parameters.json";

    public static SpawnParametersJSON TryLoad(string gameSetFolder)
    {
        string path = Path.Combine(gameSetFolder ?? "", RelativePath);
        if (!File.Exists(path)) return null;

        try
        {
            var json = JsonFixer.LoadJsonSafe<SpawnParametersJSON>(path);
            if (json == null) Debug.Error($"[SpawnParameters] Could not read {path}");
            return json;
        }
        catch (Exception ex)
        {
            Debug.Error($"[SpawnParameters] Could not read {path}: {ex.Message}");
            return null;
        }
    }

    public static List<GameSetLoader.DefaultItem> StartItems(SpawnParametersJSON parameters, GameSetLoader.ModConfig baseConfig, IEnumerable<GameSetLoader.ModConfig> extensions)
    {
        var items = new List<GameSetLoader.DefaultItem>();
        if (parameters?.GiveItemsOnStart != null) items.AddRange(parameters.GiveItemsOnStart);
        else if (baseConfig?.ItemsOnStart != null) items.AddRange(baseConfig.ItemsOnStart);

        if (extensions == null) return items;
        foreach (var extension in extensions)
        {
            if (extension?.ItemsOnStart != null) items.AddRange(extension.ItemsOnStart);
        }
        return items;
    }

    public static PlayerSpawnRules Rules(SpawnParametersJSON parameters, PlayerSpawnRules generatorRules)
    {
        return parameters?.SpawnSettings != null ? PlayerSpawnRules.FromJson(parameters.SpawnSettings) : generatorRules ?? PlayerSpawnRules.Default;
    }
}
