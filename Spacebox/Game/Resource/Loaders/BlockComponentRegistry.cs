using Engine;
using Spacebox.Game.Generation.Blocks.Components;

namespace Spacebox.Game.Resource;

public static class BlockComponentRegistry
{
    public const string ModularType = "modular";

    private static readonly Dictionary<string, Func<BlockData, BlockJSON, BlockComponentDefinition>> Builders =
        new Dictionary<string, Func<BlockData, BlockJSON, BlockComponentDefinition>>(StringComparer.OrdinalIgnoreCase)
        {
            { ProcessingComponentDefinition.ComponentName, (data, json) => new ProcessingComponentDefinition(RecipeTypeOf(data, json), data.ProcessingPower, data.Efficiency) },
            { StorageComponentDefinition.ComponentName, (data, json) => new StorageComponentDefinition(StorageSizeOf(data, json), data.Name) },
            { PowerGeneratorComponentDefinition.ComponentName, (data, json) => new PowerGeneratorComponentDefinition(GeneratorPowerOf(data, json)) },
            { PowerConsumerComponentDefinition.ComponentName, (_, json) => new PowerConsumerComponentDefinition(ConsumerPowerOf(json)) },
            { PoweredLightComponentDefinition.ComponentName, (_, _) => PoweredLightComponentDefinition.Instance }
        };

    private static readonly Dictionary<string, string[]> HostTypes = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        { ModularType, Array.Empty<string>() },
        { "furnace", new[] { ProcessingComponentDefinition.ComponentName } },
        { "crusher", new[] { ProcessingComponentDefinition.ComponentName } },
        { "generator", new[] { PowerGeneratorComponentDefinition.ComponentName } }
    };

    public static IEnumerable<string> Names => Builders.Keys;

    public static IEnumerable<string> ComponentHostTypes => HostTypes.Keys;

    public static bool IsHostType(string type) => type != null && HostTypes.ContainsKey(type);

    public static bool IsKnown(string name) => name != null && Builders.ContainsKey(name);

    public static BlockComponentDefinition[] Resolve(BlockData data, BlockJSON json)
    {
        string[] requested = json?.Components ?? Array.Empty<string>();

        if (!HostTypes.TryGetValue(data.Type ?? "", out var implied))
        {
            if (requested.Length > 0)
                Debug.Warning($"[GameSetLoader] Block '{data.Id_string}' of type '{data.Type}' cannot have Components; use \"Type\": \"{ModularType}\". Components ignored.");
            return Array.Empty<BlockComponentDefinition>();
        }

        var names = new List<string>(implied.Length + requested.Length);
        AddNames(names, implied, data);
        AddNames(names, requested, data);

        if (names.Count == 0) return Array.Empty<BlockComponentDefinition>();

        var definitions = new BlockComponentDefinition[names.Count];
        for (int i = 0; i < names.Count; i++)
            definitions[i] = Builders[names[i]](data, json);
        return definitions;
    }

    private static void AddNames(List<string> names, string[] source, BlockData data)
    {
        for (int i = 0; i < source.Length; i++)
        {
            string name = source[i]?.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(name)) continue;

            if (!Builders.ContainsKey(name))
            {
                Debug.Warning($"[GameSetLoader] Block '{data.Id_string}' has unknown component '{source[i]}' and it was skipped. Valid: {string.Join(", ", Builders.Keys)}");
                continue;
            }

            if (!names.Contains(name)) names.Add(name);
        }
    }

    private static string RecipeTypeOf(BlockData data, BlockJSON json)
    {
        string recipeType = json?.Processing?.RecipeType;
        return string.IsNullOrWhiteSpace(recipeType) ? (data.Type ?? "").ToLowerInvariant() : recipeType.Trim().ToLowerInvariant();
    }

    private static Vector2Byte StorageSizeOf(BlockData data, BlockJSON json)
    {
        if (json != null) return StorageBlockData.ClampSize(json.StorageSize);
        return data is StorageBlockData storage ? storage.Size : new Vector2Byte(3, 3);
    }

    private static ushort GeneratorPowerOf(BlockData data, BlockJSON json)
    {
        if (data is GeneratorBlockData generator) return generator.Power;
        return GeneratorBlockData.ClampPower(json?.Generator?.Power ?? GeneratorBlockData.DefaultPower);
    }

    private static ushort ConsumerPowerOf(BlockJSON json)
    {
        return (ushort)Math.Clamp(json?.Consumer?.Power ?? ConsumerJSON.DefaultPower, 0, BlocksLoader.MaxProcessingPower);
    }
}
