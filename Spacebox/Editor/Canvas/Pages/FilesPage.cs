using System.Text.Json.Nodes;
using Engine.CanvasUI;
using Spacebox.Editor.Canvas.Inspector;
using Spacebox.Editor.Data;

namespace Spacebox.Editor.Canvas.Pages;

public sealed class FilesPage : EditorPage
{
    private readonly FieldWidgetRegistry _widgets;
    private readonly LineEdit _filter = new LineEdit { PlaceholderText = "Search...", TooltipText = "Show only files whose path contains this text." };
    private readonly LineEdit _newFile = new LineEdit { PlaceholderText = "folder/new_file.json", SizeFlagsHorizontal = SizeFlags.Fill | SizeFlags.Expand, TooltipText = "Path of a new JSON file inside the GameSet folder." };
    private readonly ItemList _list = new ItemList { Name = "Files", SizeFlagsVertical = SizeFlags.Fill | SizeFlags.Expand };
    private readonly Label _header = new Label { FontColor = EditorCanvasTheme.Accent };
    private readonly Label _empty = new Label("Every JSON file of the GameSet can be edited here.") { FontColor = EditorCanvasTheme.TextDim };
    private JsonDocumentView _document;
    private string _selected;
    private string _shownFilter;
    private int _shownVersion = -1;
    private int _shownDirty = -1;

    public FilesPage(FieldWidgetRegistry widgets)
    {
        _widgets = widgets;
    }

    public override string Title => "All files";
    public override string Description => "Every JSON file of the GameSet, with the property list or as raw JSON.";

    protected override void Build()
    {
        var split = AddChild(new SplitContainer { SizeFlagsVertical = SizeFlags.Fill | SizeFlags.Expand, MinFirst = 180f, MinSecond = 300f });
        split.Ratio = 0.27f;
        var left = split.AddChild(new VBoxContainer());
        left.AddChild(_filter);
        var row = left.AddChild(new HBoxContainer());
        row.AddChild(_newFile);
        var create = row.AddChild(new Button("New") { TooltipText = "Creates the JSON file (the name must end with .json)." });
        create.Pressed += _ => CreateFile();
        left.AddChild(_list);
        _list.ItemSelected += index => Select(_list.Items[index].Metadata as string);

        var right = split.AddChild(new VBoxContainer());
        right.AddChild(_empty);
        right.AddChild(_header);
        _document = right.AddChild(new JsonDocumentView(_widgets) { SizeFlagsVertical = SizeFlags.Fill | SizeFlags.Expand });
        Select(null);
    }

    public override void Tick(float deltaTime)
    {
        int dirty = Editor.Project.DirtyCount;
        if (_shownFilter != _filter.Text || _shownVersion != Editor.Project.Version || _shownDirty != dirty) RefreshList(dirty);
        if (_selected != null) _header.Text = _selected + (Editor.Project.GetFile(_selected).IsDirty ? "  *" : "");
    }

    private void RefreshList(int dirty)
    {
        _shownFilter = _filter.Text;
        _shownVersion = Editor.Project.Version;
        _shownDirty = dirty;
        _list.Clear();
        foreach (var path in Editor.Project.ListJsonFiles())
        {
            if (!string.IsNullOrEmpty(_shownFilter) && path.IndexOf(_shownFilter, StringComparison.OrdinalIgnoreCase) < 0) continue;
            bool isDirty = Editor.Project.GetFile(path).IsDirty;
            _list.AddItem(isDirty ? path + " *" : path, null, path);
            if (path == _selected) _list.Select(_list.Items.Count - 1);
        }
    }

    private void Select(string path)
    {
        _selected = path;
        _empty.Visible = path == null;
        _header.Visible = path != null;
        _document.Visible = path != null;
        if (path == null)
        {
            _document.Clear();
            return;
        }

        _document.BindFile(Editor, Editor.Project.GetFile(path), GameSetCatalog.FileSchema(path), EditorScopes.ForPath(path));
    }

    private void CreateFile()
    {
        string name = _newFile.Text;
        if (!IsValidName(name))
        {
            Editor.SetStatus("File names must end with .json and stay inside the GameSet folder.", true);
            return;
        }

        if (Editor.Project.Exists(name))
        {
            Editor.SetStatus(name + " already exists.", true);
            return;
        }

        Editor.Project.CreateFile(name, new JsonObject());
        _newFile.Text = "";
        _shownVersion = -1;
        Select(GameSetProject.Normalize(name));
    }

    private static bool IsValidName(string name)
    {
        return !string.IsNullOrWhiteSpace(name) && name.EndsWith(".json", StringComparison.OrdinalIgnoreCase) && !name.Contains("..");
    }
}
