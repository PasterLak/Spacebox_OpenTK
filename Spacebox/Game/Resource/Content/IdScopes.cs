namespace Spacebox.Game.Resource.Content;

public sealed class IdScopes
{
    private readonly Dictionary<string, IReadOnlyList<string>> _search = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);

    public IdScopes(string baseNamespace)
    {
        Base = (baseNamespace ?? "").ToLowerInvariant();
        _search[Base] = new[] { Base };
    }

    public string Base { get; }

    public static IdScopes For(GameSetStack stack)
    {
        var scopes = new IdScopes(stack.Base.Id);
        foreach (var extension in stack.Extensions)
        {
            var search = new List<string> { extension.Id.ToLowerInvariant() };
            AddParents(extension, stack.Extensions, search);
            search.Add(scopes.Base);
            scopes._search[extension.Id] = search;
        }
        return scopes;
    }

    public bool IsExtension(string layerId) => !string.IsNullOrEmpty(layerId) && !string.Equals(layerId, Base, StringComparison.OrdinalIgnoreCase) && _search.ContainsKey(layerId);

    public string Owner(string layerId) => IsExtension(layerId) ? layerId.ToLowerInvariant() : Base;

    public IReadOnlyList<string> Search(string ns)
    {
        if (!string.IsNullOrEmpty(ns) && _search.TryGetValue(ns, out var search)) return search;
        return _search[Base];
    }

    public bool CanSee(string layerId, string ownerId)
    {
        foreach (var space in Search(layerId))
            if (string.Equals(space, ownerId, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    public string Normalize(string fullOrShort)
    {
        int colon = fullOrShort.IndexOf(':');
        if (colon < 0) return fullOrShort;
        return string.Equals(fullOrShort.Substring(0, colon), Base, StringComparison.OrdinalIgnoreCase) ? fullOrShort.Substring(colon + 1) : fullOrShort;
    }

    public string Resolve(string raw, string ns, Func<string, bool> exists)
    {
        if (string.IsNullOrEmpty(raw)) return raw;
        if (raw.Contains(':')) return Normalize(raw);
        foreach (var space in Search(ns))
        {
            string candidate = space == Base ? raw : space + ":" + raw;
            if (exists(candidate)) return candidate;
        }
        return raw;
    }

    public string Define(string raw, string ns, Func<string, bool> exists)
    {
        if (string.IsNullOrEmpty(raw)) return raw;
        if (raw.Contains(':')) return Normalize(raw);
        string resolved = Resolve(raw, ns, exists);
        if (exists(resolved)) return resolved;
        string owner = Owner(ns);
        return owner == Base ? raw : owner + ":" + raw;
    }

    private static void AddParents(InstalledGameSet extension, IReadOnlyList<InstalledGameSet> extensions, List<string> search)
    {
        var related = new List<string>(ExtensionTargets.Parse(extension.Config.Extends));
        related.AddRange(extension.Includes);
        foreach (var id in related)
        {
            var parent = InstalledGameSets.Find(extensions, id);
            if (parent == null || search.Contains(parent.Id.ToLowerInvariant())) continue;
            search.Add(parent.Id.ToLowerInvariant());
            AddParents(parent, extensions, search);
        }
    }
}
