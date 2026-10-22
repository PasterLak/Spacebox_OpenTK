using System.Numerics;
using ImGuiNET;
using Spacebox.Editor.Data;

namespace Spacebox.Editor.UI.Panels;

public sealed class JsonFilePanel : IEditorPanel
{
    private readonly JsonInspector _inspector;
    private readonly string _relativeFile;
    private readonly string _scope;
    private readonly string _hint;

    public string Title { get; }

    public JsonFilePanel(string title, string relativeFile, string scope, JsonInspector inspector, string hint)
    {
        Title = title;
        _relativeFile = relativeFile;
        _scope = scope;
        _inspector = inspector;
        _hint = hint;
    }

    public void Draw(EditorContext context)
    {
        var schema = GameSetCatalog.FileSchema(_relativeFile);
        if (!context.Project.Exists(_relativeFile))
        {
            ImGui.TextDisabled($"{_relativeFile} does not exist in this GameSet; the game uses its defaults.");
            if (ImGui.Button("Create " + _relativeFile)) context.Project.CreateFile(_relativeFile, JsonSchema.CreateEmpty(schema));
            EditorWidgets.Tooltip("Creates the file with every setting at its default value.");
            return;
        }

        var file = context.Project.GetFile(_relativeFile);
        EditorWidgets.Header(_relativeFile + (file.IsDirty ? "  *" : ""));
        if (!string.IsNullOrEmpty(_hint)) ImGui.TextDisabled(_hint);
        if (file.Error != null) ImGui.TextColored(new Vector4(1f, 0.4f, 0.3f, 1f), "Parse error: " + file.Error);

        if (ImGui.BeginChild("##" + Title + "Props", Vector2.Zero, ImGuiChildFlags.Border))
        {
            if (_inspector.Draw(context, "##" + Title, file.Root, schema, _scope)) file.MarkDirty();
        }
        ImGui.EndChild();
    }
}
