using System.Diagnostics;
using System.Numerics;
using ImGuiNET;
using Engine;
using Spacebox.Editor.Data;
using Spacebox.Editor.UI.Fields;
using Spacebox.Game;

namespace Spacebox.Editor.UI.Panels;

public sealed class OverviewPanel : IEditorPanel, IDisposable
{
    private readonly JsonInspector _inspector;
    private Texture2D _icon;
    private bool _iconLoaded;

    public string Title => "Overview";

    public OverviewPanel(JsonInspector inspector)
    {
        _inspector = inspector;
    }

    public void Draw(EditorContext context)
    {
        var project = context.Project;
        var available = ImGui.GetContentRegionAvail();
        float side = MathF.Min(available.X * 0.3f, available.Y * 0.4f);

        if (ImGui.BeginChild("##info", new Vector2(side + ImGui.GetStyle().WindowPadding.X * 2f, 0f), ImGuiChildFlags.Border))
        {
            DrawIcon(project, side);
            ImGui.TextWrapped(project.RootPath);
            if (ImGui.Button("Open folder")) OpenFolder(project.RootPath);
            ImGui.Separator();
            DrawStatistics();
        }
        ImGui.EndChild();

        ImGui.SameLine();
        if (ImGui.BeginChild("##config", Vector2.Zero, ImGuiChildFlags.Border))
        {
            EditorWidgets.Header("config.json");
            var file = project.GetFile(GameSetCatalog.ConfigFile);
            if (_inspector.Draw(context, "##configProps", file.Root, GameSetCatalog.FileSchema(GameSetCatalog.ConfigFile), EditorScopes.Config))
                file.MarkDirty();
        }
        ImGui.EndChild();
    }

    public void Dispose()
    {
        _icon?.Dispose();
        _icon = null;
    }

    private void DrawIcon(GameSetProject project, float size)
    {
        if (!_iconLoaded)
        {
            _iconLoaded = true;
            var path = Path.Combine(project.RootPath, "icon.png");
            if (File.Exists(path)) _icon = new Texture2D(path, true, false);
        }

        if (_icon != null) ImGui.Image(_icon.Handle, new Vector2(size, size));
        else ImGui.Dummy(new Vector2(size, size * 0.2f));
    }

    private static void DrawStatistics()
    {
        Row("Blocks", GameAssets.Blocks.Count);
        Row("Items", GameAssets.Items.Count);
        Row("Projectiles", GameAssets.Projectiles.Count);
        Row("Blueprints", GameAssets.Blueprints.Count);
        Row("Recipe types", GameAssets.Recipes.Count);
        Row("Mobs", GameAssets.Mobs.Count);
        Row("Sounds", GameAssets.Sounds.Count);
        Row("Block textures", GameAssets.AtlasBlocks?.Count ?? 0);
        Row("Item sprites", GameAssets.AtlasItems?.Count ?? 0);
        ImGui.TextDisabled("Counts show the last loaded state.");
    }

    private static void Row(string label, int value)
    {
        ImGui.Text(label);
        ImGui.SameLine(ImGui.GetContentRegionAvail().X * 0.65f);
        ImGui.Text(value.ToString());
    }

    private static void OpenFolder(string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Engine.Debug.Error($"[Editor] Cannot open folder: {ex.Message}");
        }
    }
}
