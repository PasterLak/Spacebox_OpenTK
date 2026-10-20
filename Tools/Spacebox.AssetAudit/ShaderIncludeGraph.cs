namespace Spacebox.AssetAudit;

public static class ShaderIncludeGraph
{
    private const string IncludeDirective = "#include";

    public static HashSet<string> CollectUsed(IReadOnlyList<AssetFile> shaderFiles, Func<AssetFile, bool> isRoot)
    {
        var byPath = new Dictionary<string, AssetFile>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in shaderFiles) byPath[file.RelativePath] = file;

        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pending = new Stack<AssetFile>();
        foreach (var file in shaderFiles)
        {
            if (isRoot(file) && used.Add(file.RelativePath)) pending.Push(file);
        }

        while (pending.Count > 0)
        {
            var file = pending.Pop();
            foreach (var include in ReadIncludes(File.ReadAllLines(file.FullPath)))
            {
                string target = Resolve(file.RelativePath, include);
                if (byPath.TryGetValue(target, out var included) && used.Add(included.RelativePath))
                    pending.Push(included);
            }
        }
        return used;
    }

    public static IEnumerable<string> ReadIncludes(IEnumerable<string> lines)
    {
        foreach (var line in lines)
        {
            string trimmed = line.TrimStart();
            if (!trimmed.StartsWith(IncludeDirective, StringComparison.Ordinal)) continue;

            int open = trimmed.IndexOf('"');
            int close = open >= 0 ? trimmed.IndexOf('"', open + 1) : -1;
            if (close > open) yield return trimmed.Substring(open + 1, close - open - 1);
        }
    }

    public static string Resolve(string includingFile, string include)
    {
        var segments = new List<string>(AssetPath.Normalize(includingFile).Split('/'));
        segments.RemoveAt(segments.Count - 1);
        foreach (var part in include.Replace('\\', '/').Split('/'))
        {
            if (part == "..") { if (segments.Count > 0) segments.RemoveAt(segments.Count - 1); }
            else if (part != "." && part.Length > 0) segments.Add(part);
        }
        return string.Join('/', segments);
    }
}
