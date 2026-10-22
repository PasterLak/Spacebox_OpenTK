using Engine.CanvasUI;
using Spacebox.Editor.Canvas.Inspector;
using Spacebox.Editor.Data;

namespace Spacebox.Editor.Canvas.Pages;

public sealed class JsonFilePage : EditorPage
{
    private readonly FieldWidgetRegistry _widgets;
    private readonly string _relativeFile;
    private readonly string _scope;
    private readonly string _hint;
    private readonly Label _header = new Label { FontColor = EditorCanvasTheme.Accent };
    private readonly VBoxContainer _missing = new VBoxContainer();
    private JsonDocumentView _document;
    private bool _bound;

    public JsonFilePage(string title, string relativeFile, string scope, FieldWidgetRegistry widgets, string hint)
    {
        Title = title;
        _relativeFile = relativeFile;
        _scope = scope;
        _widgets = widgets;
        _hint = hint;
    }

    public override string Title { get; }
    public override string Description => _hint;

    protected override void Build()
    {
        AddChild(_header);
        if (!string.IsNullOrEmpty(_hint)) AddChild(new Label(_hint) { Autowrap = true, FontColor = EditorCanvasTheme.TextDim });
        AddChild(_missing);
        _missing.AddChild(new Label($"{_relativeFile} does not exist in this GameSet; the game uses its defaults.") { Autowrap = true, FontColor = EditorCanvasTheme.TextDim });
        var create = _missing.AddChild(new Button("Create " + _relativeFile) { TooltipText = "Creates the file with every setting at its default value.", SizeFlagsHorizontal = SizeFlags.ShrinkBegin });
        create.Pressed += _ => Editor.Project.CreateFile(_relativeFile, JsonSchema.CreateEmpty(GameSetCatalog.FileSchema(_relativeFile)));
        _document = AddChild(new JsonDocumentView(_widgets));
    }

    public override void Tick(float deltaTime)
    {
        bool exists = Editor.Project.Exists(_relativeFile);
        _missing.Visible = !exists;
        _document.Visible = exists;
        if (!exists)
        {
            _bound = false;
            return;
        }

        var file = Editor.Project.GetFile(_relativeFile);
        _header.Text = _relativeFile + (file.IsDirty ? "  *" : "");
        if (_bound) return;
        _bound = true;
        _document.BindFile(Editor, file, GameSetCatalog.FileSchema(_relativeFile), _scope);
    }
}
