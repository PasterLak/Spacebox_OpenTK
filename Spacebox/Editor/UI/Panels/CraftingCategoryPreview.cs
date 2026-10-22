using System.Numerics;
using System.Text.Json.Nodes;
using ImGuiNET;
using Spacebox.Editor.Data;
using Spacebox.Game;
using Spacebox.Game.GUI;

namespace Spacebox.Editor.UI.Panels;

public sealed class CraftingCategoryPreview : IEntrySummary
{
    public const int Rows = 2;

    public void Draw(EditorContext context, EditorEntry entry)
    {
        if (entry.Node is not JsonObject selected) return;

        var categories = ReadCategories(entry);
        if (categories.Count == 0) return;

        string selectedId = EditorJson.GetString(selected, "Id");
        var grid = CraftingCategoryLayout.Arrange(categories, Rows, out int columns);

        ImGui.TextDisabled("Crafting window (categories as the game arranges them):");
        float size = ImGui.GetTextLineHeight() * 3.2f;
        for (int y = 0; y < Rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                if (x > 0) ImGui.SameLine();
                DrawCell(context, grid[y * columns + x], selectedId, size);
            }
        }

        DrawItems(selectedId);
        ImGui.Separator();
    }

    private static List<CraftingCategory> ReadCategories(EditorEntry entry)
    {
        var result = new List<CraftingCategory>();
        if (EditorJson.GetArray(entry.File.Root, "Categories") is not JsonArray array) return result;

        foreach (var node in array)
        {
            if (node is not JsonObject obj) continue;
            result.Add(new CraftingCategory
            {
                Id = EditorJson.GetString(obj, "Id"),
                Name = EditorJson.GetString(obj, "Name"),
                Icon = EditorJson.GetString(obj, "Icon"),
                Row = (int)EditorJson.GetNumber(obj, "Row", 0)
            });
        }
        return result;
    }

    private static void DrawCell(EditorContext context, CraftingCategory category, string selectedId, float size)
    {
        ImGui.PushID(category?.Id ?? "empty" + ImGui.GetCursorPosX());
        var cursor = ImGui.GetCursorScreenPos();
        var drawList = ImGui.GetWindowDrawList();
        bool isSelected = category != null && string.Equals(category.Id, selectedId, StringComparison.OrdinalIgnoreCase);
        uint background = category == null ? 0x30FFFFFF : isSelected ? 0xFF2A7FD0 : 0xFF3A3A3A;

        drawList.AddRectFilled(cursor, cursor + new Vector2(size * 1.6f, size), background, 4f);
        if (category != null)
        {
            var icon = context.Assets.ItemIcon(category.Icon);
            if (icon.IsValid) icon.Draw(drawList, cursor + new Vector2(size * 0.8f, size * 0.25f), cursor + new Vector2(size * 1.55f, size));
            drawList.AddText(cursor + new Vector2(4f, 2f), 0xFFFFFFFF, category.Name);
        }

        ImGui.Dummy(new Vector2(size * 1.6f, size));
        if (category != null)
            EditorWidgets.Tooltip($"{category.Name} ({category.Id})\nRow {(category.Row == 0 ? "auto" : category.Row.ToString())}, icon {category.Icon}");
        ImGui.PopID();
    }

    private static void DrawItems(string categoryId)
    {
        if (string.IsNullOrEmpty(categoryId) || !GameAssets.CraftingCategories.TryGetValue(categoryId, out var loaded))
        {
            ImGui.TextDisabled("No craftable items in this category (or not loaded yet: Save & Reload).");
            return;
        }

        ImGui.TextDisabled($"{loaded.Items.Count} craftable items in this category (last loaded state):");
        float iconSize = ImGui.GetTextLineHeight() * 1.8f;
        float width = ImGui.GetContentRegionAvail().X;
        float x = 0f;
        foreach (var data in loaded.Items)
        {
            if (data.item == null) continue;
            if (x > 0f && x + iconSize < width) ImGui.SameLine();
            else x = 0f;

            EditorWidgets.Icon(data.item.Icon, iconSize);
            EditorWidgets.Tooltip(data.item.Name);
            x += iconSize + ImGui.GetStyle().ItemSpacing.X;
        }
    }
}
