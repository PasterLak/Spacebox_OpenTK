using System.Text.Json.Nodes;

namespace Spacebox.Editor.Data;

public sealed class EditorEntry
{
    public string Key { get; }
    public JsonNode Node { get; }
    public EditorJsonFile File { get; }
    public string Group { get; }

    public EditorEntry(string key, JsonNode node, EditorJsonFile file, string group = null)
    {
        Key = key;
        Node = node;
        File = file;
        Group = group;
    }
}

public sealed class EntryDefinition
{
    public string IdKey { get; init; } = "ID";
    public Func<JsonNode, Type> Schema { get; init; } = _ => null;
    public Func<JsonNode, string> Label { get; init; }
    public Func<JsonNode> Template { get; init; } = () => new JsonObject();
    public string NewIdPrefix { get; init; } = "new_entry";
    public Action<JsonNode, IReadOnlyList<EditorEntry>> Prepare { get; init; }

    public string GetId(JsonNode node)
    {
        return node is JsonObject obj ? EditorJson.GetString(obj, IdKey) : "";
    }

    public string GetLabel(JsonNode node, string fallback)
    {
        var label = Label?.Invoke(node);
        if (!string.IsNullOrWhiteSpace(label)) return label;
        var id = GetId(node);
        return string.IsNullOrWhiteSpace(id) ? fallback : id;
    }

    public void PrepareNew(JsonNode node, IReadOnlyList<EditorEntry> existing)
    {
        Prepare?.Invoke(node, existing);
    }

    public void SetId(JsonNode node, string id)
    {
        if (node is JsonObject obj && !string.IsNullOrEmpty(IdKey)) EditorJson.Set(obj, IdKey, JsonValue.Create(id));
    }
}

public interface IReorderableEntrySource
{
    bool CanMove(EditorEntry entry, int delta);
    void Move(EditorEntry entry, int delta);
}

public interface IEntrySource
{
    EntryDefinition Definition { get; }
    IReadOnlyList<string> Groups { get; }
    IReadOnlyList<EditorEntry> GetEntries();
    EditorEntry Add(string group);
    EditorEntry Duplicate(EditorEntry entry);
    void Remove(EditorEntry entry);
}
