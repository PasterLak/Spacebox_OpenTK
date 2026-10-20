namespace Spacebox.AssetAudit;

public sealed class ReferenceIndex
{
    private readonly HashSet<string> _names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _prefixes = new List<string>();

    public IReadOnlyCollection<string> Paths => _paths;

    public void AddLiteral(string text, bool interpolated)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        string value = AssetPath.Normalize(text.Trim());
        int hole = interpolated ? value.IndexOf('{') : -1;
        if (hole >= 0)
        {
            AddPrefix(value.Substring(0, hole));
            return;
        }

        if (value.Length > 260 || value.Contains('\n')) return;

        _names.Add(AssetPath.NameOf(value));
        if (value.Contains('/')) _paths.Add(AssetPath.WithoutExtension(value));
    }

    public void AddPath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return;
        string value = AssetPath.Normalize(relativePath);
        _paths.Add(AssetPath.WithoutExtension(value));
        _names.Add(AssetPath.NameOf(value));
    }

    public bool ContainsName(string name) => _names.Contains(name);

    public bool IsReferenced(string relativePath)
    {
        string path = AssetPath.WithoutExtension(relativePath);
        if (_names.Contains(AssetPath.NameOf(relativePath))) return true;
        if (_paths.Contains(path)) return true;

        foreach (var prefix in _prefixes)
        {
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    private void AddPrefix(string prefix)
    {
        if (prefix.Contains('/') && prefix.Length > 1) _prefixes.Add(prefix);
    }
}

public static class AssetPath
{
    public static string Normalize(string path)
    {
        return path.Replace('\\', '/').TrimStart('.', '/');
    }

    public static string WithoutExtension(string path)
    {
        string normalized = Normalize(path);
        int slash = normalized.LastIndexOf('/');
        int dot = normalized.LastIndexOf('.');
        return dot > slash + 1 ? normalized.Substring(0, dot) : normalized;
    }

    public static string NameOf(string path)
    {
        string withoutExtension = WithoutExtension(path);
        int slash = withoutExtension.LastIndexOf('/');
        return slash >= 0 ? withoutExtension.Substring(slash + 1) : withoutExtension;
    }

    public static bool HasSegmentStartingWith(string relativePath, char prefix)
    {
        var segments = Normalize(relativePath).Split('/');
        for (int i = 0; i < segments.Length - 1; i++)
        {
            if (segments[i].StartsWith(prefix)) return true;
        }
        return false;
    }
}
