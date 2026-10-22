namespace Spacebox.Game.Resource.Content;

public sealed class GameSetAssetIndex
{
    public Dictionary<string, string> BlockTextures { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> ItemTextures { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> BlockEmissions { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> ItemEmissions { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> Sounds { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public static GameSetAssetIndex Build(IReadOnlyList<ContentLayer> layers, string builtInBlocks = null, string builtInItems = null, IReadOnlyList<ITextureOverrideSource> overrides = null)
    {
        var index = new GameSetAssetIndex();

        foreach (var layer in layers) index.AddLayer(layer);

        AssetFolderScanner.Fill(index.BlockTextures, AssetFolderScanner.Scan(builtInBlocks, GameSetFormat.ImageExtensions, null, false));
        AssetFolderScanner.Fill(index.ItemTextures, AssetFolderScanner.Scan(builtInItems, GameSetFormat.ImageExtensions, null, false));

        if (overrides != null)
        {
            for (int i = overrides.Count - 1; i >= 0; i--) index.ApplyOverride(overrides[i]);
        }

        return index;
    }

    private void AddLayer(ContentLayer layer)
    {
        string blocks = layer.PathOf(GameSetFormat.BlocksFolder);
        string items = layer.PathOf(GameSetFormat.ItemsFolder);
        string emissions = layer.PathOf(GameSetFormat.EmissionsFolder);

        AssetFolderScanner.Overlay(BlockTextures, AssetFolderScanner.Scan(blocks, GameSetFormat.ImageExtensions, file => !GameSetFormat.IsInEmissionsFolder(blocks, file), false));
        AssetFolderScanner.Overlay(ItemTextures, AssetFolderScanner.Scan(items, GameSetFormat.ImageExtensions, file => !GameSetFormat.IsInEmissionsFolder(items, file), false));

        var sharedEmissions = AssetFolderScanner.Scan(emissions, GameSetFormat.ImageExtensions, null, true);
        var blockEmissions = new Dictionary<string, string>(sharedEmissions, StringComparer.OrdinalIgnoreCase);
        var itemEmissions = new Dictionary<string, string>(sharedEmissions, StringComparer.OrdinalIgnoreCase);
        AssetFolderScanner.Overlay(blockEmissions, AssetFolderScanner.Scan(blocks, GameSetFormat.ImageExtensions, file => GameSetFormat.IsInEmissionsFolder(blocks, file), true));
        AssetFolderScanner.Overlay(itemEmissions, AssetFolderScanner.Scan(items, GameSetFormat.ImageExtensions, file => GameSetFormat.IsInEmissionsFolder(items, file), true));
        AssetFolderScanner.Overlay(BlockEmissions, blockEmissions);
        AssetFolderScanner.Overlay(ItemEmissions, itemEmissions);

        var sounds = AssetFolderScanner.ScanByExtension(layer.PathOf(GameSetFormat.SoundsFolder), GameSetFormat.SoundExtensions, null);
        AssetFolderScanner.Fill(sounds, AssetFolderScanner.ScanByExtension(blocks, GameSetFormat.SoundExtensions, null));
        AssetFolderScanner.Fill(sounds, AssetFolderScanner.ScanByExtension(items, GameSetFormat.SoundExtensions, null));
        AssetFolderScanner.Overlay(Sounds, sounds);
    }

    private void ApplyOverride(ITextureOverrideSource source)
    {
        OverrideExisting(BlockTextures, source.BlockTextures);
        OverrideExisting(ItemTextures, source.ItemTextures);
        OverrideExisting(BlockEmissions, source.Emissions, BlockTextures);
        OverrideExisting(ItemEmissions, source.Emissions, ItemTextures);
    }

    private static void OverrideExisting(Dictionary<string, string> target, IReadOnlyDictionary<string, string> replacements, Dictionary<string, string> owners = null)
    {
        foreach (var pair in replacements)
        {
            if (target.ContainsKey(pair.Key) || owners != null && owners.ContainsKey(pair.Key)) target[pair.Key] = pair.Value;
        }
    }
}
