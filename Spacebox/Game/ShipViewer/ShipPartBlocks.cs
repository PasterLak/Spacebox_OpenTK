using Spacebox.Game.Resource.Content;

namespace Spacebox.Game.ShipViewer;

public sealed class ShipPartBlocks
{
    private static readonly string[] PartTypes = { "thruster", "shipconsole", "cannon" };

    private readonly HashSet<string> _ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public static ShipPartBlocks FromContent(GameSetContent content, string modId)
    {
        var parts = new ShipPartBlocks();
        string prefix = string.IsNullOrWhiteSpace(modId) ? "default" : modId;
        foreach (var entry in content.Blocks)
        {
            string type = ContentJson.GetString(entry.Data, "Type");
            string id = ContentJson.GetString(entry.Data, "ID");
            if (string.IsNullOrWhiteSpace(id) || !IsPartType(type)) continue;
            parts._ids.Add(id.Contains(':') ? id : prefix + ":" + id);
        }
        return parts;
    }

    public static ShipPartBlocks Load(ModFolders gameSetsDirectory, string modId, IReadOnlyList<string> extensions)
    {
        string id = string.IsNullOrWhiteSpace(modId) ? "default" : modId;
        var result = GameSetStackResolver.Resolve(gameSetsDirectory, id, extensions);
        return result.Stack == null ? null : FromContent(result.Stack.ReadContent(), id);
    }

    public bool Contains(string fullId) => fullId != null && _ids.Contains(fullId);

    private static bool IsPartType(string type)
    {
        foreach (var part in PartTypes)
            if (string.Equals(part, type, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }
}
