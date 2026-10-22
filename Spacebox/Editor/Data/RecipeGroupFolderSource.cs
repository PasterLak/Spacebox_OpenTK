using System.Text.Json.Nodes;
using Spacebox.Game.Resource.Content.Conversion;

namespace Spacebox.Editor.Data;

public sealed class RecipeGroupFolderSource : IEntrySource
{
    private const string TypeKey = "Type";
    private const string RecipesKey = "Recipes";

    private readonly GameSetProject _project;
    private readonly string _folder;
    private readonly List<EditorEntry> _entries = new List<EditorEntry>();
    private readonly List<string> _groups = new List<string>();
    private int _version = -1;

    public EntryDefinition Definition { get; }

    public RecipeGroupFolderSource(GameSetProject project, string folder, EntryDefinition definition)
    {
        _project = project;
        _folder = folder;
        Definition = definition;
    }

    public IReadOnlyList<string> Groups
    {
        get
        {
            GetEntries();
            return _groups;
        }
    }

    public IReadOnlyList<EditorEntry> GetEntries()
    {
        int version = _project.Version;
        if (version == _version) return _entries;

        _version = version;
        _entries.Clear();
        _groups.Clear();
        foreach (var file in _project.GetFolderFiles(_folder))
        {
            if (file.Root is not JsonObject root) continue;
            string type = EditorJson.GetString(root, TypeKey, Path.GetFileNameWithoutExtension(file.RelativePath));
            if (!_groups.Contains(type)) _groups.Add(type);
            if (EditorJson.Get(root, RecipesKey) is not JsonArray recipes) continue;

            for (int i = 0; i < recipes.Count; i++)
                _entries.Add(new EditorEntry(file.RelativePath + "#" + i, recipes[i], file, type));
        }
        return _entries;
    }

    public EditorEntry Add(string group)
    {
        var node = Definition.Template();
        var (file, recipes) = EnsureGroup(string.IsNullOrWhiteSpace(group) ? "new_group" : group);
        recipes.Add(node);
        return Commit(file, node);
    }

    public EditorEntry Duplicate(EditorEntry entry)
    {
        var (file, recipes) = EnsureGroup(entry.Group);
        var node = EditorJson.Clone(entry.Node);
        int index = IndexOf(recipes, entry.Node);
        recipes.Insert(index < 0 ? recipes.Count : index + 1, node);
        return Commit(file, node);
    }

    public void Remove(EditorEntry entry)
    {
        if (entry.File.Root is not JsonObject root || EditorJson.Get(root, RecipesKey) is not JsonArray recipes) return;
        int index = IndexOf(recipes, entry.Node);
        if (index < 0) return;
        recipes.RemoveAt(index);
        entry.File.MarkDirty();
    }

    private (EditorJsonFile File, JsonArray Recipes) EnsureGroup(string type)
    {
        foreach (var file in _project.GetFolderFiles(_folder))
        {
            if (file.Root is JsonObject root && string.Equals(EditorJson.GetString(root, TypeKey, Path.GetFileNameWithoutExtension(file.RelativePath)), type, StringComparison.OrdinalIgnoreCase))
                return (file, EnsureArray(root));
        }

        var created = new JsonObject { [TypeKey] = type, [RecipesKey] = new JsonArray() };
        var newFile = _project.CreateFile(_folder.TrimEnd('/') + "/" + RecipeFilesWriter.SafeFileName(type) + ".json", created);
        return (newFile, (JsonArray)created[RecipesKey]);
    }

    private static JsonArray EnsureArray(JsonObject root)
    {
        if (EditorJson.Get(root, RecipesKey) is JsonArray recipes) return recipes;
        recipes = new JsonArray();
        EditorJson.Set(root, RecipesKey, recipes);
        return recipes;
    }

    private EditorEntry Commit(EditorJsonFile file, JsonNode node)
    {
        file.MarkDirty();
        _version = -1;
        foreach (var entry in GetEntries())
        {
            if (ReferenceEquals(entry.Node, node)) return entry;
        }
        return null;
    }

    private static int IndexOf(JsonArray array, JsonNode node)
    {
        for (int i = 0; i < array.Count; i++)
        {
            if (ReferenceEquals(array[i], node)) return i;
        }
        return -1;
    }
}
