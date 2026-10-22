using System.Text.Json.Nodes;
using Engine;
using ImGuiNET;
using Spacebox.Editor.Data;
using Spacebox.Game.Disassembly;

namespace Spacebox.Editor.UI.Panels;

public sealed class DisassemblySummary : IEntrySummary
{
    public void Draw(EditorContext context, EditorEntry entry)
    {
        if (entry.Node is not JsonObject obj) return;

        string id = EditorJson.GetString(obj, "Item");
        if (!context.Assets.TryGetItem(id, out var item))
        {
            ImGui.TextDisabled("Pick an item to see what the disassembler gives back.");
            return;
        }

        if (!DisassemblyRecipes.TryGet(item, out var recipe))
        {
            ImGui.TextDisabled($"{item.Name} cannot be disassembled (last loaded state).");
            ImGui.Separator();
            return;
        }

        float iconSize = ImGui.GetTextLineHeight() * 1.6f;
        EditorWidgets.Icon(item.Icon, iconSize);
        EditorWidgets.Tooltip(item.Name);
        ImGui.SameLine();
        ImGui.Text($"x{recipe.InputQuantity}  ->");
        foreach (var product in recipe.Products)
        {
            ImGui.SameLine();
            EditorWidgets.Icon(product.Item.Icon, iconSize);
            EditorWidgets.Tooltip(product.Item.Name);
            ImGui.SameLine();
            ImGui.Text($"x{product.Quantity}");
        }
        ImGui.SameLine();
        ImGui.TextDisabled($"   {Time.TicksToSeconds(recipe.DurationTicks):0.##}s (last loaded state)");
        ImGui.Separator();
    }
}
