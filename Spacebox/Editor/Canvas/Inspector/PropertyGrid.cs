using System.Text.Json.Nodes;
using Engine.CanvasUI;
using OpenTK.Mathematics;
using Spacebox.Editor.Data;
using Spacebox.Editor.UI.Fields;

namespace Spacebox.Editor.Canvas.Inspector;

public sealed class PropertyGrid : VBoxContainer
{
    private const float LabelRatio = 0.4f;

    private readonly FieldWidgetRegistry _widgets;
    private readonly HashSet<string> _expanded = new HashSet<string>();
    private readonly HashSet<string> _collapsed = new HashSet<string>();
    private readonly PopupMenu _menu = new PopupMenu();
    private EditorContext _editor;
    private JsonNode _root;
    private Type _schema;
    private string _scope;
    private Action _changed;
    private bool _rebuildPending;

    public PropertyGrid(FieldWidgetRegistry widgets)
    {
        _widgets = widgets;
        Name = "PropertyGrid";
        AddThemeConstantOverride("separation", 2f);
    }

    public JsonNode BoundRoot => _root;

    public void Bind(EditorContext editor, JsonNode root, Type schema, string scope, Action changed)
    {
        bool sameDocument = ReferenceEquals(root, _root);
        _editor = editor;
        _root = root;
        _schema = schema;
        _scope = scope;
        _changed = changed;
        if (!sameDocument)
        {
            _expanded.Clear();
            _collapsed.Clear();
        }
        Rebuild();
    }

    public void Unbind()
    {
        _root = null;
        ClearChildren();
    }

    public void RequestRebuild() => _rebuildPending = true;

    public override void Process(float deltaTime)
    {
        if (!_rebuildPending) return;
        _rebuildPending = false;
        Rebuild();
    }

    public void Rebuild()
    {
        ClearChildren();
        if (_root == null)
        {
            AddChild(new Label("Empty document") { FontColor = EditorCanvasTheme.TextDim });
            return;
        }

        if (_root is JsonObject obj) BuildObject(this, obj, _schema, "", true);
        else if (_root is JsonArray array) BuildArray(this, array, JsonSchema.ElementType(_schema) ?? _schema, "");
        else AddChild(new Label(_root.ToJsonString()));
    }

    private void BuildObject(Container target, JsonObject obj, Type schema, string path, bool isRoot)
    {
        var dictionaryValue = JsonSchema.DictionaryValueType(schema);
        foreach (var key in obj.Select(p => p.Key).ToArray())
        {
            string name = key;
            var property = dictionaryValue == null ? JsonSchema.Find(schema, key) : null;
            var type = dictionaryValue ?? property?.ClrType;
            string help = dictionaryValue == null ? FieldHelpText.For(schema, property, key) : null;
            BuildRow(target, key, obj[key], type, Combine(path, key), obj, help,
                node => { if (!ReferenceEquals(obj[name], node)) obj[name] = node; },
                () => obj.Remove(name), null, -1);
        }

        if (dictionaryValue != null) BuildAddKeyRow(target, obj, dictionaryValue, path);
        else BuildAddParameterRow(target, obj, schema, path, isRoot);
    }

    private void BuildArray(Container target, JsonArray array, Type elementType, string path)
    {
        for (int i = 0; i < array.Count; i++)
        {
            int index = i;
            BuildRow(target, $"[{i}]", array[i], elementType, path, null, null,
                node => { if (!ReferenceEquals(array[index], node)) array[index] = node; },
                () => array.RemoveAt(index), array, index);
        }

        var add = target.AddChild(new Button("+ Add element") { ThemeTypeVariation = "SmallButton", SizeFlagsHorizontal = SizeFlags.ShrinkBegin });
        add.TooltipText = "Adds a copy of the last element, or a new default element.";
        add.Pressed += _ =>
        {
            var template = array.Count > 0 ? EditorJson.Clone(array[^1]) : JsonSchema.CreateEmpty(elementType) ?? JsonValue.Create("");
            array.Add(template);
            Structural();
        };
    }

    private void BuildRow(Container target, string label, JsonNode value, Type schemaType, string path, JsonObject parent, string help,
        Action<JsonNode> set, Action remove, JsonArray ownerArray, int index)
    {
        var context = new FieldContext { Editor = _editor, Scope = _scope, Name = label, Path = path, Value = value, SchemaType = schemaType, Parent = parent };
        Action<JsonNode> commit = node =>
        {
            set(node);
            _changed?.Invoke();
            if (node == null || node is JsonObject && !_widgets.IsInline(new FieldContext { Editor = _editor, Scope = _scope, Name = label, Path = path, Value = node, SchemaType = schemaType, Parent = parent })) RequestRebuild();
        };

        if (_widgets.IsInline(context))
        {
            var row = target.AddChild(new HBoxContainer());
            var name = row.AddChild(new Label(label) { Overrun = TextOverrun.Ellipsis, SizeFlagsHorizontal = SizeFlags.Fill | SizeFlags.Expand, SizeFlagsStretchRatio = LabelRatio, MouseFilter = MouseFilter.Stop, VerticalAlignment = VerticalAlignment.Center });
            name.TooltipText = help ?? schemaType?.Name ?? "";
            var widget = row.AddChild(_widgets.Create(context, commit));
            widget.SizeFlagsHorizontal |= SizeFlags.Expand;
            widget.SizeFlagsStretchRatio = 1f - LabelRatio;
            if (string.IsNullOrEmpty(widget.TooltipText)) widget.TooltipText = help ?? "";
            row.AddChild(MenuButton(label, value, schemaType, set, remove, ownerArray, index));
            return;
        }

        var holder = target.AddChild(new HBoxContainer());
        var fold = holder.AddChild(new FoldableContainer(label, IsExpanded(path, value)) { SizeFlagsHorizontal = SizeFlags.Fill | SizeFlags.Expand, Summary = Summary(value) });
        fold.TooltipText = help ?? "";
        string foldPath = path + "#" + label;
        fold.Toggled += open =>
        {
            if (open) { _expanded.Add(foldPath); _collapsed.Remove(foldPath); }
            else { _collapsed.Add(foldPath); _expanded.Remove(foldPath); }
        };
        var content = fold.AddChild(new VBoxContainer());
        content.AddThemeConstantOverride("separation", 2f);
        if (value is JsonObject childObject) BuildObject(content, childObject, schemaType, path, false);
        else if (value is JsonArray childArray) BuildArray(content, childArray, JsonSchema.ElementType(schemaType), path);
        var menu = holder.AddChild(MenuButton(label, value, schemaType, set, remove, ownerArray, index));
        menu.SizeFlagsVertical = SizeFlags.ShrinkBegin;

        bool IsExpanded(string p, JsonNode node)
        {
            string key = p + "#" + label;
            if (_expanded.Contains(key)) return true;
            if (_collapsed.Contains(key)) return false;
            return node switch
            {
                JsonObject o => o.Count <= 4,
                JsonArray a => a.Count <= 4,
                _ => false
            };
        }
    }

    private Button MenuButton(string label, JsonNode value, Type schemaType, Action<JsonNode> set, Action remove, JsonArray ownerArray, int index)
    {
        var button = new Button("...") { ThemeTypeVariation = "SmallButton", TooltipText = "Actions for " + label };
        button.Pressed += _ =>
        {
            _menu.ClearItems();
            if (ownerArray != null)
            {
                _menu.AddItem("Move up", () => { Swap(ownerArray, index, index - 1); Structural(); }, index <= 0);
                _menu.AddItem("Move down", () => { Swap(ownerArray, index, index + 1); Structural(); }, index >= ownerArray.Count - 1);
                _menu.AddItem("Duplicate", () => { ownerArray.Insert(index + 1, EditorJson.Clone(value)); Structural(); });
            }
            if (schemaType != null) _menu.AddItem("Reset to default", () => { set(JsonSchema.CreateEmpty(schemaType)); Structural(); });
            _menu.AddItem("Copy JSON", () => Root?.Clipboard.SetText(EditorJsonFile.Serialize(value)));
            _menu.AddItem("Paste JSON", () => Paste(set));
            _menu.AddSeparator();
            _menu.AddItem("Remove " + label, () => { remove(); Structural(); });
            _menu.Open(Root, new Vector2(button.Rect.X, button.Rect.End.Y));
        };
        return button;
    }

    private void Paste(Action<JsonNode> set)
    {
        try
        {
            set(EditorJsonFile.Parse(Root?.Clipboard.GetText() ?? ""));
            Structural();
        }
        catch (Exception ex)
        {
            _editor?.SetStatus("Clipboard is not valid JSON: " + ex.Message, true);
        }
    }

    private void BuildAddParameterRow(Container target, JsonObject obj, Type schema, string path, bool isRoot)
    {
        var missing = new List<SchemaProperty>();
        foreach (var property in JsonSchema.For(schema))
            if (EditorJson.FindKey(obj, property.Name) == null) missing.Add(property);

        if (missing.Count == 0 && !isRoot) return;
        var row = target.AddChild(new HBoxContainer());

        if (missing.Count > 0)
        {
            var combo = row.AddChild(new ComboBox { Editable = false, PlaceholderText = $"+ Add parameter ({missing.Count})", SizeFlagsHorizontal = SizeFlags.Fill | SizeFlags.Expand, PopupMinWidth = 300f });
            combo.TooltipText = "Adds a missing setting with its default value.";
            foreach (var property in missing)
            {
                var item = combo.AddOption(property.Name);
                item.Secondary = FriendlyType(property.ClrType);
                item.Tooltip = FieldHelpText.For(schema, property, property.Name);
            }
            combo.ValueChanged += name =>
            {
                var property = missing.FirstOrDefault(p => p.Name == name);
                if (property == null) return;
                obj[property.Name] = property.CreateValue() ?? JsonValue.Create("");
                Structural();
            };
        }

        if (!isRoot) return;
        var key = row.AddChild(new LineEdit { PlaceholderText = "custom key", SizeFlagsHorizontal = SizeFlags.Fill | SizeFlags.Expand });
        key.TooltipText = "Adds a key the schema does not know (for mods with their own fields).";
        var add = row.AddChild(new Button("Add") { ThemeTypeVariation = "SmallButton" });
        add.Pressed += _ =>
        {
            string name = key.Text.Trim();
            if (name.Length == 0 || EditorJson.FindKey(obj, name) != null) return;
            obj[name] = JsonValue.Create("");
            Structural();
        };
    }

    private void BuildAddKeyRow(Container target, JsonObject obj, Type valueType, string path)
    {
        var context = new FieldContext { Editor = _editor, Scope = _scope, Name = LastSegment(path), Path = path, Value = obj };
        var suggestions = _widgets.KeySuggestions(context);
        var row = target.AddChild(new HBoxContainer());
        var combo = row.AddChild(new ComboBox { PlaceholderText = "new key", SizeFlagsHorizontal = SizeFlags.Fill | SizeFlags.Expand });
        foreach (var suggestion in suggestions) combo.AddOption(suggestion);
        var add = row.AddChild(new Button("Add key") { ThemeTypeVariation = "SmallButton" });
        add.Pressed += _ =>
        {
            string name = combo.Value.Trim();
            if (name.Length == 0 || EditorJson.FindKey(obj, name) != null) return;
            obj[name] = JsonSchema.CreateEmpty(valueType) ?? JsonValue.Create(0);
            Structural();
        };
    }

    private void Structural()
    {
        _changed?.Invoke();
        RequestRebuild();
    }

    private static void Swap(JsonArray array, int a, int b)
    {
        if (a < 0 || b < 0 || a >= array.Count || b >= array.Count) return;
        var first = array[a];
        var second = array[b];
        array[a] = null;
        array[b] = null;
        array[a] = second;
        array[b] = first;
    }

    private static string Summary(JsonNode node) => node switch
    {
        JsonObject obj => $"{{ {obj.Count} }}",
        JsonArray array => $"[ {array.Count} ]",
        _ => node?.ToJsonString() ?? "null"
    };

    public static string FriendlyType(Type type)
    {
        if (type == null) return "?";
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (JsonSchema.IsDictionary(type)) return "map";
        if (JsonSchema.ElementType(type) != null && type != typeof(string)) return "list";
        return type.Name.ToLowerInvariant();
    }

    private static string Combine(string path, string key) => string.IsNullOrEmpty(path) ? key : path + "." + key;

    private static string LastSegment(string path)
    {
        int dot = path.LastIndexOf('.');
        return dot < 0 ? path : path.Substring(dot + 1);
    }
}
