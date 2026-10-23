using Spacebox.Game.Resource.Content;

namespace Spacebox.Editor;

public sealed record EditorTarget(string Id, string Name);

public static class EditorTargets
{
    public const string AllName = "All GameSets";

    public static IReadOnlyList<EditorTarget> For(string modId)
    {
        var installed = InstalledGameSets.Scan(ModFolders.Local);
        return For(InstalledGameSets.Find(installed, modId), installed);
    }

    public static IReadOnlyList<EditorTarget> For(InstalledGameSet extension, IReadOnlyList<InstalledGameSet> installed)
    {
        var targets = new List<EditorTarget>();
        if (extension == null || !extension.IsExtension) return targets;

        targets.Add(new EditorTarget("", AllName));
        var graph = new ExtensionGraph(installed);
        foreach (var gameSet in installed)
        {
            if (gameSet.IsExtension || gameSet.IsEmpty || !graph.Supports(extension, gameSet)) continue;
            targets.Add(new EditorTarget(gameSet.Id, "Only " + gameSet.DisplayName));
        }
        return targets;
    }

    public static string ProjectRoot(string extensionFolder, string target)
    {
        if (string.IsNullOrEmpty(target)) return extensionFolder;
        string root = Path.Combine(extensionFolder, GameSetFormat.AdaptersFolder, target);
        Directory.CreateDirectory(root);
        return root;
    }
}
