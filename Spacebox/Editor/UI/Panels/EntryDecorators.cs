using System.Text.Json.Nodes;
using Engine.Graphics.Atlas;
using ImGuiNET;
using Spacebox.Editor.Data;

namespace Spacebox.Editor.UI.Panels;

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

public sealed class RecipeSummary : IEntrySummary
{
    public void Draw(EditorContext context, EditorEntry entry)
    {
        if (entry.Node is not JsonObject obj) return;

        float iconSize = ImGui.GetTextLineHeight() * 1.6f;
        bool first = true;

        if (EditorJson.Get(obj, "Ingredients") is JsonArray ingredients)
        {
            foreach (var ingredient in ingredients)
            {
                DrawStack(context, ingredient as JsonObject, iconSize, ref first);
            }
        }
        else
        {
            DrawStack(context, EditorJson.Get(obj, "Ingredient") as JsonObject, iconSize, ref first);
        }

        ImGui.SameLine();
        ImGui.Text("  ->  ");
        ImGui.SameLine();
        first = true;
        DrawStack(context, EditorJson.Get(obj, "Product") as JsonObject, iconSize, ref first);

        double duration = EditorJson.GetNumber(obj, "DurationSec", -1);
        if (duration >= 0)
        {
            ImGui.SameLine();
            ImGui.TextDisabled($"   {duration:0.##}s, {EditorJson.GetNumber(obj, "PowerPerTickRequared"):0} power/tick");
        }
        ImGui.Separator();
    }

    private static void DrawStack(EditorContext context, JsonObject stack, float iconSize, ref bool first)
    {
        if (stack == null) return;
        if (!first) ImGui.SameLine();
        first = false;

        var id = EditorJson.GetString(stack, "Item");
        EditorWidgets.Icon(context.Assets.ItemIcon(id), iconSize);
        EditorWidgets.Tooltip(id);
        ImGui.SameLine();
        bool known = context.Assets.TryGetItem(id, out _);
        var text = $"x{EditorJson.GetNumber(stack, "Quantity", 1):0}";
        if (known) ImGui.Text(text);
        else ImGui.TextColored(new System.Numerics.Vector4(1f, 0.4f, 0.3f, 1f), text + " (unknown item)");
    }
}
