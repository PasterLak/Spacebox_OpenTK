using Engine.Graphics.Atlas;
using Spacebox.Game.GUI;
using Spacebox.Game.Resource.Content;

namespace Spacebox.Game.Resource;

public static class GameAtlasFactory
{
    public const string EmissionLayer = "emission";
    public const string LodTexture = "lod";

    public const string BuiltInBlocksFolder = "Resources/Textures/Blocks";
    public const string BuiltInItemsFolder = "Resources/Textures/Items";

    public static TextureAtlas CreateBlocksAtlas(GameSetAssetIndex assets)
    {
        var atlas = new TextureAtlas("BlocksAtlas");
        AddFiles(atlas, assets.BlockTextures);
        AddEmissions(atlas, assets.BlockEmissions);
        atlas.Build();
        return atlas;
    }

    public static TextureAtlas CreateItemsAtlas(GameSetAssetIndex assets)
    {
        var atlas = new TextureAtlas("ItemsAtlas", padding: 1);
        AddFiles(atlas, assets.ItemTextures);
        UIAtlas.AddUIElements(atlas);
        AddEmissions(atlas, assets.ItemEmissions);
        atlas.Build();
        return atlas;
    }

    private static void AddFiles(TextureAtlas atlas, Dictionary<string, string> files)
    {
        foreach (var pair in Sorted(files)) atlas.AddFile(pair.Key, pair.Value);
    }

    private static void AddEmissions(TextureAtlas atlas, Dictionary<string, string> files)
    {
        atlas.CreateLayer(EmissionLayer);
        foreach (var pair in Sorted(files)) atlas.AddLayerFile(EmissionLayer, pair.Key, pair.Value);
    }

    private static List<KeyValuePair<string, string>> Sorted(Dictionary<string, string> files)
    {
        var list = new List<KeyValuePair<string, string>>(files);
        list.Sort((a, b) => string.Compare(a.Value, b.Value, StringComparison.OrdinalIgnoreCase));
        return list;
    }

    public static string BlockIconName(BlockData blockData)
    {
        return "icon:" + blockData.Id_string;
    }
}
