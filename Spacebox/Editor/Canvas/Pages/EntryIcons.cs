using System.Text.Json.Nodes;
using Engine.Graphics.Atlas;
using Spacebox.Editor.Data;

namespace Spacebox.Editor.Canvas.Pages;

public interface IEntryIconProvider
{
    AtlasSprite GetIcon(EditorContext context, EditorEntry entry);
}

public sealed class ItemIdIconProvider : IEntryIconProvider
{
    private readonly Func<JsonNode, string> _id;

    public ItemIdIconProvider(Func<JsonNode, string> id)
    {
        _id = id;
    }

    public AtlasSprite GetIcon(EditorContext context, EditorEntry entry)
    {
        return context.Assets.ItemIcon(_id(entry.Node));
    }

    public static string TopLevelId(JsonNode node)
    {
        return node is JsonObject obj ? EditorJson.GetString(obj, "ID") : "";
    }

    public static string ProductId(JsonNode node)
    {
        return node is JsonObject obj && EditorJson.Get(obj, "Product") is JsonObject product ? EditorJson.GetString(product, "Item") : "";
    }
}

public sealed class SpriteIconProvider : IEntryIconProvider
{
    public AtlasSprite GetIcon(EditorContext context, EditorEntry entry)
    {
        if (entry.Node is not JsonObject obj) return AtlasSprite.None;
        var icon = context.Assets.ItemIcon(EditorJson.GetString(obj, "ID"));
        return icon.IsValid ? icon : context.Assets.ItemSprite(EditorJson.GetString(obj, "Sprite"));
    }
}
