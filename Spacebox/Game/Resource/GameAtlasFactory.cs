using Engine.Graphics.Atlas;
using Spacebox.Game.GUI;

namespace Spacebox.Game.Resource;

public static class GameAtlasFactory
{
    public const string EmissionLayer = "emission";
    public const string LodTexture = "lod";

    private const string BuiltInBlocksFolder = "Resources/Textures/Blocks";
    private const string BuiltInItemsFolder = "Resources/Textures/Items";

    public static TextureAtlas CreateBlocksAtlas(string blocksPath, string emissionPath)
    {
        var atlas = new TextureAtlas("BlocksAtlas");
        atlas.AddDirectory(blocksPath);
        atlas.AddDirectory(BuiltInBlocksFolder);
        atlas.AddLayerDirectory(EmissionLayer, emissionPath);
        atlas.Build();
        return atlas;
    }

    public static TextureAtlas CreateItemsAtlas(string itemsPath, string emissionPath)
    {
        var atlas = new TextureAtlas("ItemsAtlas", padding: 1);
        atlas.AddDirectory(itemsPath);
        atlas.AddDirectory(BuiltInItemsFolder);
        UIAtlas.AddUIElements(atlas);
        atlas.AddLayerDirectory(EmissionLayer, emissionPath);
        atlas.Build();
        return atlas;
    }

    public static string BlockIconName(BlockData blockData)
    {
        return "icon:" + blockData.Id_string;
    }
}
