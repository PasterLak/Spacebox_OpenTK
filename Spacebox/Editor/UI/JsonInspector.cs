using System.Text.Json.Nodes;
using ImGuiNET;
using Spacebox.Editor.Data;
using Spacebox.Editor.UI.Fields;

namespace Spacebox.Editor.UI;

public sealed class JsonInspector
{
    private readonly JsonFieldEditors _editors;
    private readonly FieldContext _context = new FieldContext();
    private readonly List<string> _keys = new List<string>();
    private readonly Dictionary<string, string> _newKeys = new Dictionary<string, string>();
    private EditorContext _editor;
    private string _scope;
    private bool _changed;
    private Action _deferred;
    private JsonNode _root;

    public JsonInspector(JsonFieldEditors editors)
    {
        _editors = editors;
    }

    public bool Draw(EditorContext editor, string id, JsonNode root, Type schema, string scope)
    {
        _editor = editor;
        _scope = scope;
        _changed = false;
        _deferred = null;
        _root = root;

        if (root == null)
        {
            ImGui.TextDisabled("Empty document");
            return false;
        }

        var flags = ImGuiTableFlags.Resizable | ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp;
        if (ImGui.BeginTable(id, 2, flags))
        {
            ImGui.TableSetupColumn("Property", ImGuiTableColumnFlags.WidthStretch, 0.38f);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch, 0.62f);

            if (root is JsonObject obj) DrawObjectRows(obj, schema, "");
            else if (root is JsonArray array) DrawArrayRows(array, JsonSchema.ElementType(schema) ?? schema, "");

            ImGui.EndTable();
        }

        _deferred?.Invoke();
        return _changed;
    }

    private void DrawObjectRows(JsonObject obj, Type schema, string path)
    {
        _keys.Clear();
        foreach (var pair in obj) _keys.Add(pair.Key);
        var keys = _keys.ToArray();
        var dictionaryValueType = JsonSchema.DictionaryValueType(schema);

        for (int i = 0; i < keys.Length; i++)
        {
            var key = keys[i];
            var propertyType = dictionaryValueType ?? JsonSchema.Find(schema, key)?.ClrType;
            ImGui.PushID(key);
            DrawRow(key, obj[key], propertyType, Combine(path, key), node => obj[key] = node, () => obj.Remove(key), obj);
            ImGui.PopID();
        }

        if (dictionaryValueType != null) DrawAddKeyRow(obj, dictionaryValueType, path);
        else DrawAddParameterRow(obj, schema, path);
    }

    private void DrawArrayRows(JsonArray array, Type elementType, string path)
    {
        for (int i = 0; i < array.Count; i++)
        {
            int index = i;
            ImGui.PushID(i);
            DrawRow($"[{i}]", array[i], elementType, path, node => array[index] = node, () => array.RemoveAt(index), null, array, index);
            ImGui.PopID();
        }

        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        if (ImGui.SmallButton("+ Add element"))
        {
            var template = array.Count > 0 ? EditorJson.Clone(array[^1]) : JsonSchema.CreateEmpty(elementType) ?? JsonValue.Create("");
            _deferred += () => array.Add(template);
            _changed = true;
        }
    }

    private void DrawRow(string label, JsonNode value, Type schemaType, string path, Action<JsonNode> set, Action remove, JsonObject parent, JsonArray ownerArray = null, int index = -1)
    {
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);

        PrepareContext(label, path, value, schemaType, parent);
        bool inline = _editors.CanDrawInline(_context);

        if (inline)
        {
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(label);
            DrawRowMenu(label, value, schemaType, set, remove, ownerArray, index);
            EditorWidgets.Tooltip(schemaType?.Name);

            ImGui.TableSetColumnIndex(1);
            if (_editors.Draw(_context, out var newValue))
            {
                if (!ReferenceEquals(newValue, value)) set(newValue);
                _changed = true;
            }
            return;
        }

        var treeFlags = ImGuiTreeNodeFlags.SpanFullWidth;
        if (IsSmall(value)) treeFlags |= ImGuiTreeNodeFlags.DefaultOpen;
        bool open = ImGui.TreeNodeEx(label + "##node", treeFlags);
        DrawRowMenu(label, value, schemaType, set, remove, ownerArray, index);
        ImGui.TableSetColumnIndex(1);
        ImGui.TextDisabled(Summary(value));

        if (!open) return;

        if (value is JsonObject childObject) DrawObjectRows(childObject, schemaType, path);
        else if (value is JsonArray childArray) DrawArrayRows(childArray, JsonSchema.ElementType(schemaType), path);
        ImGui.TreePop();
    }

    private void DrawRowMenu(string label, JsonNode value, Type schemaType, Action<JsonNode> set, Action remove, JsonArray ownerArray, int index)
    {
        if (!ImGui.BeginPopupContextItem("##menu")) return;

        if (ownerArray != null)
        {
            if (index > 0 && ImGui.MenuItem("Move up")) Defer(() => Swap(ownerArray, index, index - 1));
            if (index < ownerArray.Count - 1 && ImGui.MenuItem("Move down")) Defer(() => Swap(ownerArray, index, index + 1));
            if (ImGui.MenuItem("Duplicate")) Defer(() => ownerArray.Insert(index + 1, EditorJson.Clone(value)));
        }

        if (schemaType != null && ImGui.MenuItem("Reset to default"))
        {
            var empty = JsonSchema.CreateEmpty(schemaType);
            Defer(() => set(empty));
        }

        if (ImGui.MenuItem("Copy JSON")) ImGui.SetClipboardText(EditorJsonFile.Serialize(value));
        if (ImGui.MenuItem("Paste JSON"))
        {
            try
            {
                var pasted = EditorJsonFile.Parse(ImGui.GetClipboardText() ?? "");
                Defer(() => set(pasted));
            }
            catch (Exception ex)
            {
                _editor?.SetStatus("Clipboard is not valid JSON: " + ex.Message, true);
            }
        }

        ImGui.Separator();
        if (ImGui.MenuItem("Remove " + label)) Defer(remove);
        ImGui.EndPopup();
    }

    private void DrawAddParameterRow(JsonObject obj, Type schema, string path)
    {
        var properties = JsonSchema.For(schema);
        int missing = 0;
        for (int i = 0; i < properties.Count; i++)
        {
            if (EditorJson.FindKey(obj, properties[i].Name) == null) missing++;
        }

        bool isRoot = ReferenceEquals(obj, _root);
        if (missing == 0 && !isRoot) return;

        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);

        if (missing > 0)
        {
            ImGui.SetNextItemWidth(-1f);
            if (ImGui.BeginCombo("##add", $"+ Add parameter ({missing})", ImGuiComboFlags.HeightLarge))
            {
                for (int i = 0; i < properties.Count; i++)
                {
                    var property = properties[i];
                    if (EditorJson.FindKey(obj, property.Name) != null) continue;
                    if (ImGui.Selectable($"{property.Name}  ({FriendlyType(property.ClrType)})"))
                    {
                        var created = property.CreateValue() ?? JsonValue.Create("");
                        Defer(() => obj[property.Name] = created);
                    }
                }
                ImGui.EndCombo();
            }
        }

        if (!isRoot) return;

        ImGui.TableSetColumnIndex(1);
        string key = _newKeys.TryGetValue(path, out var stored) ? stored : "";
        ImGui.SetNextItemWidth(-ImGui.GetFrameHeight() * 2.5f);
        if (ImGui.InputTextWithHint("##customKey", "custom key", ref key, 64)) _newKeys[path] = key;
        ImGui.SameLine();
        if (ImGui.SmallButton("Add") && !string.IsNullOrWhiteSpace(key) && EditorJson.FindKey(obj, key) == null)
        {
            var name = key.Trim();
            Defer(() => obj[name] = JsonValue.Create(""));
            _newKeys[path] = "";
        }
    }

    private void DrawAddKeyRow(JsonObject obj, Type valueType, string path)
    {
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        ImGui.TextDisabled("+ New key");
        ImGui.TableSetColumnIndex(1);

        PrepareContext(LastSegment(path), path, obj, null, null);
        var suggestions = _editors.GetKeySuggestions(_context);
        string key = _newKeys.TryGetValue(path, out var stored) ? stored : "";

        if (suggestions.Count > 0)
        {
            if (EditorWidgets.SearchCombo("##newKey", ref key, suggestions)) _newKeys[path] = key;
        }
        else
        {
            ImGui.SetNextItemWidth(-ImGui.GetFrameHeight() * 2.5f);
            if (ImGui.InputTextWithHint("##newKey", "key", ref key, 64)) _newKeys[path] = key;
            ImGui.SameLine();
        }

        if (ImGui.SmallButton("Add key") && !string.IsNullOrWhiteSpace(key) && EditorJson.FindKey(obj, key) == null)
        {
            var name = key.Trim();
            var value = JsonSchema.CreateEmpty(valueType) ?? JsonValue.Create(0);
            Defer(() => obj[name] = value);
            _newKeys[path] = "";
        }
    }

    private void PrepareContext(string name, string path, JsonNode value, Type schemaType, JsonObject parent)
    {
        _context.Editor = _editor;
        _context.Scope = _scope;
        _context.Name = name;
        _context.Path = path;
        _context.Value = value;
        _context.SchemaType = schemaType;
        _context.Parent = parent;
    }

    private void Defer(Action action)
    {
        _deferred += action;
        _changed = true;
    }

    private static void Swap(JsonArray array, int a, int b)
    {
        var first = array[a];
        var second = array[b];
        array[a] = null;
        array[b] = null;
        array[a] = second;
        array[b] = first;
    }

    private static bool IsSmall(JsonNode node)
    {
        return node switch
        {
            JsonObject obj => obj.Count <= 4,
            JsonArray array => array.Count <= 4,
            _ => false
        };
    }

    private static string Summary(JsonNode node)
    {
        return node switch
        {
            JsonObject obj => $"{{ {obj.Count} }}",
            JsonArray array => $"[ {array.Count} ]",
            _ => node?.ToJsonString() ?? "null"
        };
    }

    private static string FriendlyType(Type type)
    {
        if (type == null) return "?";
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (JsonSchema.IsDictionary(type)) return "map";
        if (JsonSchema.ElementType(type) != null && type != typeof(string)) return "list";
        return type.Name.ToLowerInvariant();
    }

    private static string Combine(string path, string key)
    {
        return string.IsNullOrEmpty(path) ? key : path + "." + key;
    }

    private static string LastSegment(string path)
    {
        int dot = path.LastIndexOf('.');
        return dot < 0 ? path : path.Substring(dot + 1);
    }
}
