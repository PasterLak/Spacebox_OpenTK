using System.Text.Json.Nodes;
using Engine.CanvasUI;
using Spacebox.Editor.Data;

namespace Spacebox.Editor.Canvas.Inspector;

public sealed class JsonDocumentView : VBoxContainer
{
    private readonly PropertyGrid _grid;
    private readonly ScrollContainer _scroll = new ScrollContainer { Name = "DocumentScroll", SizeFlagsVertical = SizeFlags.Fill | SizeFlags.Expand };
    private readonly VBoxContainer _rawBox = new VBoxContainer { Name = "RawJson", SizeFlagsVertical = SizeFlags.Fill | SizeFlags.Expand, Visible = false };
    private readonly TextEdit _raw = new TextEdit { ShowLineNumbers = true, SizeFlagsVertical = SizeFlags.Fill | SizeFlags.Expand };
    private readonly CheckBox _rawToggle = new CheckBox("Raw JSON") { TooltipText = "Edit the document as JSON text instead of the property list." };
    private readonly Label _error = new Label { Autowrap = true, FontColor = EditorCanvasTheme.Error, Visible = false };
    private EditorContext _editor;
    private EditorJsonFile _file;
    private JsonNode _node;
    private Type _schema;
    private string _scope;
    private JsonNode _boundRoot;

    public JsonDocumentView(FieldWidgetRegistry widgets, bool showRawToggle = true)
    {
        _grid = new PropertyGrid(widgets);
        Name = "JsonDocument";
        SizeFlagsVertical = SizeFlags.Fill | SizeFlags.Expand;
        if (showRawToggle) AddChild(_rawToggle);
        AddChild(_error);
        AddChild(_scroll);
        _scroll.AddChild(_grid);
        AddChild(_rawBox);
        _rawBox.AddChild(_raw);
        var buttons = _rawBox.AddChild(new HBoxContainer());
        var apply = buttons.AddChild(new Button("Apply JSON") { TooltipText = "Checks the JSON text and replaces the document with it." });
        var revert = buttons.AddChild(new Button("Revert") { TooltipText = "Throws away the text changes." });
        apply.Pressed += _ => ApplyRaw();
        revert.Pressed += _ => _raw.Text = Serialize();
        _rawToggle.Toggled += (_, raw) => SetRaw(raw);
    }

    public ISet<string> HiddenRootKeys => _grid.HiddenRootKeys;

    public void BindFile(EditorContext editor, EditorJsonFile file, Type schema, string scope)
    {
        _editor = editor;
        _file = file;
        _node = null;
        _schema = schema;
        _scope = scope;
        Rebind();
    }

    public void BindNode(EditorContext editor, EditorJsonFile file, JsonNode node, Type schema, string scope)
    {
        _editor = editor;
        _file = file;
        _node = node;
        _schema = schema;
        _scope = scope;
        Rebind();
    }

    public void Clear()
    {
        _file = null;
        _node = null;
        _boundRoot = null;
        _grid.Unbind();
    }

    public override void Process(float deltaTime)
    {
        if (_file == null) return;
        var root = CurrentRoot();
        if (!ReferenceEquals(root, _boundRoot)) Rebind();
    }

    private JsonNode CurrentRoot() => _node ?? _file?.Root;

    private void Rebind()
    {
        var root = CurrentRoot();
        _boundRoot = root;
        _error.Visible = _file?.Error != null;
        _error.Text = _file?.Error == null ? "" : "Parse error: " + _file.Error;
        _grid.Bind(_editor, root, _schema, _scope, Changed);
        if (_rawBox.Visible) _raw.Text = Serialize();
    }

    private void Changed()
    {
        _file?.MarkDirty();
    }

    private void SetRaw(bool raw)
    {
        _scroll.Visible = !raw;
        _rawBox.Visible = raw;
        if (raw) _raw.Text = Serialize();
        else _grid.Rebuild();
    }

    private string Serialize()
    {
        var root = CurrentRoot();
        return root == null ? "" : EditorJsonFile.Serialize(root);
    }

    private void ApplyRaw()
    {
        try
        {
            var parsed = EditorJsonFile.Parse(_raw.Text);
            var root = CurrentRoot();
            if (!ReplaceContents(root, parsed))
            {
                if (_node == null && _file != null)
                {
                    _file.Root = parsed;
                }
                else
                {
                    _editor?.SetStatus("Raw JSON must keep the same kind (object or array)", true);
                    return;
                }
            }
            _file?.MarkDirty();
            _editor?.SetStatus("JSON applied");
            Rebind();
        }
        catch (Exception ex)
        {
            _editor?.SetStatus("Invalid JSON: " + ex.Message, true);
        }
    }

    public static bool ReplaceContents(JsonNode target, JsonNode source)
    {
        if (target is JsonObject targetObject && source is JsonObject sourceObject)
        {
            targetObject.Clear();
            foreach (var pair in sourceObject.ToArray())
            {
                sourceObject.Remove(pair.Key);
                targetObject[pair.Key] = pair.Value;
            }
            return true;
        }

        if (target is JsonArray targetArray && source is JsonArray sourceArray)
        {
            targetArray.Clear();
            while (sourceArray.Count > 0)
            {
                var item = sourceArray[0];
                sourceArray.RemoveAt(0);
                targetArray.Add(item);
            }
            return true;
        }

        return false;
    }
}
