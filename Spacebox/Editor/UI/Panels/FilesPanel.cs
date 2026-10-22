using System.Numerics;
using System.Text.Json.Nodes;
using ImGuiNET;
using Spacebox.Editor.Data;
using Spacebox.Editor.UI.Fields;

namespace Spacebox.Editor.UI.Panels;

public sealed class FilesPanel : IEditorPanel
{
    private readonly JsonInspector _inspector;
    private string _selected;
    private string _filter = "";
    private string _newFile = "";
    private bool _rawMode;
    private string _rawText = "";

    public string Title => "All files";

    public FilesPanel(JsonInspector inspector)
    {
        _inspector = inspector;
    }

    public void Draw(EditorContext context)
    {
        var project = context.Project;
        var available = ImGui.GetContentRegionAvail();
        float listWidth = MathF.Max(available.X * 0.27f, 160f * EditorWidgets.Scale);

        if (ImGui.BeginChild("##files", new Vector2(listWidth, 0f), ImGuiChildFlags.Border))
        {
            ImGui.SetNextItemWidth(-1f);
            ImGui.InputTextWithHint("##filter", "Search...", ref _filter, 128);
            EditorWidgets.Tooltip("Show only files whose path contains this text.");
            ImGui.SetNextItemWidth(-ImGui.GetFrameHeight() * 2.5f);
            ImGui.InputTextWithHint("##new", "folder/new_file.json", ref _newFile, 128);
            EditorWidgets.Tooltip("Path of a new JSON file inside the GameSet folder.");
            ImGui.SameLine();
            if (ImGui.Button("New") && IsValidName(_newFile) && !project.Exists(_newFile))
            {
                project.CreateFile(_newFile, new JsonObject());
                _selected = GameSetProject.Normalize(_newFile);
                _newFile = "";
            }
            ImGui.Separator();

            if (ImGui.BeginChild("##fileList"))
            {
                foreach (var path in project.ListJsonFiles())
                {
                    if (!string.IsNullOrEmpty(_filter) && path.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) < 0) continue;
                    bool dirty = project.GetFile(path).IsDirty;
                    if (ImGui.Selectable(dirty ? path + " *" : path, path == _selected))
                    {
                        _selected = path;
                        if (_rawMode) _rawText = EditorJsonFile.Serialize(project.GetFile(path).Root);
                    }
                }
            }
            ImGui.EndChild();
        }
        ImGui.EndChild();

        ImGui.SameLine();
        if (ImGui.BeginChild("##fileEditor", Vector2.Zero, ImGuiChildFlags.Border))
            DrawFile(context);
        ImGui.EndChild();
    }

    private void DrawFile(EditorContext context)
    {
        if (string.IsNullOrEmpty(_selected))
        {
            ImGui.TextDisabled("Every JSON file of the GameSet can be edited here.");
            return;
        }

        var file = context.Project.GetFile(_selected);
        EditorWidgets.Header(_selected + (file.IsDirty ? "  *" : ""));
        if (ImGui.Checkbox("Raw JSON", ref _rawMode) && _rawMode) _rawText = EditorJsonFile.Serialize(file.Root);
        EditorWidgets.Tooltip("Edit the file as JSON text instead of the property list.");

        if (file.Error != null) ImGui.TextColored(new Vector4(1f, 0.4f, 0.3f, 1f), "Parse error: " + file.Error);

        if (_rawMode || file.Root == null)
        {
            var size = ImGui.GetContentRegionAvail() - new Vector2(0f, ImGui.GetFrameHeightWithSpacing());
            ImGui.InputTextMultiline("##raw", ref _rawText, 1 << 22, size);
            if (ImGui.Button("Apply JSON"))
            {
                try
                {
                    file.Root = EditorJsonFile.Parse(_rawText);
                    file.MarkDirty();
                    context.SetStatus("JSON applied to " + _selected);
                }
                catch (Exception ex)
                {
                    context.SetStatus("Invalid JSON: " + ex.Message, true);
                }
            }
            return;
        }

        if (ImGui.BeginChild("##props"))
        {
            if (_inspector.Draw(context, "##fileProps", file.Root, GameSetCatalog.FileSchema(_selected), ScopeOf(_selected)))
                file.MarkDirty();
        }
        ImGui.EndChild();
    }

    private static string ScopeOf(string path)
    {
        if (path.StartsWith(GameSetCatalog.BlocksFile, StringComparison.OrdinalIgnoreCase)) return EditorScopes.Blocks;
        if (path.StartsWith(GameSetCatalog.ItemsFile, StringComparison.OrdinalIgnoreCase)) return EditorScopes.Items;
        if (path.StartsWith(GameSetCatalog.CraftingFile, StringComparison.OrdinalIgnoreCase)) return EditorScopes.Crafting;
        if (path.StartsWith(GameSetCatalog.ConfigFile, StringComparison.OrdinalIgnoreCase)) return EditorScopes.Config;
        if (path.StartsWith(GameSetCatalog.LootFolder, StringComparison.OrdinalIgnoreCase)) return EditorScopes.Loot;
        if (path.StartsWith(GameSetCatalog.MobsFolder, StringComparison.OrdinalIgnoreCase)) return EditorScopes.Mobs;
        if (path.StartsWith(GameSetCatalog.BiomesFolder, StringComparison.OrdinalIgnoreCase)) return EditorScopes.Biomes;
        if (path.Equals(GameSetCatalog.GeneratorFile, StringComparison.OrdinalIgnoreCase)) return EditorScopes.Generator;
        return EditorScopes.Generic;
    }

    private static bool IsValidName(string name)
    {
        return !string.IsNullOrWhiteSpace(name) && name.EndsWith(".json", StringComparison.OrdinalIgnoreCase) && !name.Contains("..");
    }
}
