namespace Spacebox.Game.Resource.Content;

public static class ExtensionCompatibility
{
    public static string Problem(InstalledGameSet extension, InstalledGameSet baseSet, IReadOnlyList<InstalledGameSet> installed)
    {
        if (extension == null || baseSet == null) return "The GameSet is not installed";

        var graph = new ExtensionGraph(installed);
        string loop = graph.Loop(extension);
        if (loop != null) return $"Extends loop: {loop}";

        var config = extension.Config;
        var through = graph.Through(extension, baseSet);
        if (through == null)
            return $"Made for {ExtensionTargets.Describe(config.Extends)}, not for {baseSet.DisplayName}";

        foreach (var parent in graph.Parents(extension))
        {
            if (!ReferenceEquals(parent, through)) continue;
            string parentProblem = Problem(parent, baseSet, installed);
            if (parentProblem != null) return $"Needs '{parent.DisplayName}': {parentProblem}";
        }

        string version = through.Config.Version ?? "";
        if (!string.IsNullOrWhiteSpace(config.MinBaseVersion) && GameSetVersion.Compare(version, config.MinBaseVersion) < 0)
            return $"Needs {through.DisplayName} {config.MinBaseVersion} or newer (installed: {version})";
        if (!string.IsNullOrWhiteSpace(config.MaxBaseVersion) && GameSetVersion.Compare(version, config.MaxBaseVersion) > 0)
            return $"Works with {through.DisplayName} up to {config.MaxBaseVersion} (installed: {version})";
        return null;
    }
}
