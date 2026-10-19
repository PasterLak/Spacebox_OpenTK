using System.Numerics;
using ImGuiNET;
using Spacebox.Editor.UI.Panels;

namespace Spacebox.Editor.UI;

public sealed class EditorShell : IDisposable
{
    private const string UnsavedPopup = "Unsaved changes";

    private readonly EditorContext _context;
    private readonly List<IEditorPanel> _panels;
    private int _current;
    private bool _confirmExit;

    public EditorShell(EditorContext context, List<IEditorPanel> panels, string startPanel)
    {
        _context = context;
        _panels = panels;
        Select(startPanel);
    }

    public void Select(string title)
    {
        if (string.IsNullOrEmpty(title)) return;
        for (int i = 0; i < _panels.Count; i++)
        {
            if (string.Equals(_panels[i].Title, title, StringComparison.OrdinalIgnoreCase)) _current = i;
        }
    }

    public void Draw()
    {
        var viewport = ImGui.GetMainViewport();
        ImGui.SetNextWindowPos(viewport.WorkPos);
        ImGui.SetNextWindowSize(viewport.WorkSize);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0f);

        var flags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoSavedSettings;
        if (ImGui.Begin("GameSet Editor", flags))
        {
            DrawToolbar();
            ImGui.Separator();
            DrawBody();
            DrawUnsavedPopup();
        }
        ImGui.End();
        ImGui.PopStyleVar();
    }

    public void Dispose()
    {
        foreach (var panel in _panels)
        {
            if (panel is IDisposable disposable) disposable.Dispose();
        }
    }

    private void DrawToolbar()
    {
        var project = _context.Project;
        int dirty = project.DirtyCount;

        ImGui.TextColored(new Vector4(1f, 0.8f, 0.2f, 1f), $"GameSet Editor - {_context.Assets.ModId}");
        ImGui.SameLine();
        ImGui.TextDisabled($"({project.FolderName})");

        ImGui.SameLine();
        if (EditorWidgets.ToolbarButton(dirty > 0 ? $"Save ({dirty})" : "Save", dirty > 0)) Save();
        ImGui.SameLine();
        if (ImGui.Button("Save & Reload")) { Save(); _context.RequestReload(); }
        EditorWidgets.Tooltip("Saves all files and reloads the GameSet (textures, atlases, previews).");
        ImGui.SameLine();
        if (ImGui.Button("Reload")) _context.RequestReload();
        EditorWidgets.Tooltip("Reloads textures and data from disk without saving.");
        ImGui.SameLine();
        if (EditorWidgets.ToolbarButton("Discard", dirty > 0))
        {
            project.DiscardChanges();
            _context.SetStatus("Changes discarded");
        }
        ImGui.SameLine();
        if (ImGui.Button("Back to menu"))
        {
            if (dirty > 0) _confirmExit = true;
            else _context.RequestExit();
        }

        if (!string.IsNullOrEmpty(_context.StatusText))
        {
            ImGui.SameLine();
            var color = _context.StatusIsError ? new Vector4(1f, 0.4f, 0.3f, 1f) : new Vector4(0.5f, 1f, 0.5f, 1f);
            ImGui.TextColored(color, _context.StatusText);
        }
    }

    private void DrawBody()
    {
        var available = ImGui.GetContentRegionAvail();
        float sidebarWidth = MathF.Max(available.X * 0.12f, 120f * EditorWidgets.Scale);

        if (ImGui.BeginChild("##sidebar", new Vector2(sidebarWidth, 0f), ImGuiChildFlags.Border))
        {
            float buttonHeight = ImGui.GetFrameHeight() * 1.4f;
            for (int i = 0; i < _panels.Count; i++)
            {
                if (ImGui.Selectable(_panels[i].Title, i == _current, ImGuiSelectableFlags.None, new Vector2(0f, buttonHeight))) _current = i;
            }
        }
        ImGui.EndChild();

        ImGui.SameLine();
        if (ImGui.BeginChild("##panel", Vector2.Zero))
        {
            ImGui.PushID(_current);
            _panels[_current].Draw(_context);
            ImGui.PopID();
        }
        ImGui.EndChild();
    }

    private void DrawUnsavedPopup()
    {
        if (_confirmExit)
        {
            ImGui.OpenPopup(UnsavedPopup);
            _confirmExit = false;
        }

        if (!ImGui.BeginPopupModal(UnsavedPopup, ImGuiWindowFlags.AlwaysAutoResize)) return;

        ImGui.Text($"{_context.Project.DirtyCount} file(s) have unsaved changes.");
        if (ImGui.Button("Save and exit"))
        {
            Save();
            _context.RequestExit();
            ImGui.CloseCurrentPopup();
        }
        ImGui.SameLine();
        if (ImGui.Button("Exit without saving"))
        {
            _context.Project.DiscardChanges();
            _context.RequestExit();
            ImGui.CloseCurrentPopup();
        }
        ImGui.SameLine();
        if (ImGui.Button("Cancel")) ImGui.CloseCurrentPopup();
        ImGui.EndPopup();
    }

    private void Save()
    {
        try
        {
            _context.Project.SaveAll();
            _context.SetStatus("Saved");
        }
        catch (Exception ex)
        {
            _context.SetStatus("Save failed: " + ex.Message, true);
        }
    }
}
