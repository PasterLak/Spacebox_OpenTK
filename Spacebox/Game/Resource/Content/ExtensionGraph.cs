namespace Spacebox.Game.Resource.Content;

public sealed class ExtensionGraph
{
    private readonly IReadOnlyList<InstalledGameSet> _installed;

    public ExtensionGraph(IReadOnlyList<InstalledGameSet> installed)
    {
        _installed = installed ?? Array.Empty<InstalledGameSet>();
    }

    public IReadOnlyList<InstalledGameSet> Parents(InstalledGameSet extension)
    {
        var parents = new List<InstalledGameSet>();
        if (extension == null) return parents;
        foreach (var target in ExtensionTargets.Parse(extension.Config.Extends))
        {
            if (target == ExtensionTargets.Any) continue;
            var parent = InstalledGameSets.Find(_installed, target);
            if (parent != null && parent.IsExtension && !ReferenceEquals(parent, extension)) parents.Add(parent);
        }
        return parents;
    }

    public string Loop(InstalledGameSet extension)
    {
        var path = new List<InstalledGameSet>();
        return FindLoop(extension, path);
    }

    public bool Supports(InstalledGameSet extension, InstalledGameSet baseSet) => Through(extension, baseSet) != null;

    public InstalledGameSet Through(InstalledGameSet extension, InstalledGameSet baseSet)
    {
        if (extension == null || baseSet == null || baseSet.IsEmpty || baseSet.IsExtension) return null;
        return Through(extension, baseSet, new HashSet<InstalledGameSet>());
    }

    private InstalledGameSet Through(InstalledGameSet extension, InstalledGameSet baseSet, HashSet<InstalledGameSet> visited)
    {
        if (!visited.Add(extension)) return null;

        foreach (var target in ExtensionTargets.Parse(extension.Config.Extends))
        {
            if (target == ExtensionTargets.Any || baseSet.Matches(target)) return baseSet;
        }

        foreach (var parent in Parents(extension))
        {
            if (Through(parent, baseSet, visited) != null) return parent;
        }
        return null;
    }

    private string FindLoop(InstalledGameSet extension, List<InstalledGameSet> path)
    {
        int index = path.IndexOf(extension);
        if (index >= 0)
        {
            var names = new List<string>();
            for (int i = index; i < path.Count; i++) names.Add(path[i].Id);
            names.Add(extension.Id);
            return string.Join(" -> ", names);
        }

        path.Add(extension);
        foreach (var parent in Parents(extension))
        {
            string loop = FindLoop(parent, path);
            if (loop != null) return loop;
        }
        path.RemoveAt(path.Count - 1);
        return null;
    }
}
