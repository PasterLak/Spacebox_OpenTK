using Spacebox.Game.Resource.Content.Conversion;

namespace Spacebox.Editor.Data;

public sealed class ProjectFolderConversion
{
    public ConversionResult Root { get; }
    public ConversionResult Mirror { get; }

    private ProjectFolderConversion(ConversionResult root, ConversionResult mirror)
    {
        Root = root;
        Mirror = mirror;
    }

    public bool Success => Root.Success && (Mirror == null || Mirror.Success);

    public IEnumerable<string> Problems
    {
        get
        {
            foreach (var line in Root.Errors) yield return line;
            foreach (var line in Root.Differences) yield return line;
            if (Mirror == null) yield break;
            foreach (var line in Mirror.Errors) yield return "Source folder: " + line;
            foreach (var line in Mirror.Differences) yield return "Source folder: " + line;
        }
    }

    public IEnumerable<string> Log
    {
        get
        {
            foreach (var line in Root.Log) yield return line;
            if (Mirror == null) yield break;
            foreach (var line in Mirror.Log) yield return "Source folder: " + line;
        }
    }

    public static ProjectFolderConversion Convert(string root, string mirrorRoot)
    {
        var rootResult = GameSetFolderConverter.Convert(root);
        if (!rootResult.Success || !NeedsMirrorConversion(root, mirrorRoot)) return new ProjectFolderConversion(rootResult, null);
        return new ProjectFolderConversion(rootResult, GameSetFolderConverter.Convert(mirrorRoot));
    }

    private static bool NeedsMirrorConversion(string root, string mirrorRoot)
    {
        if (string.IsNullOrEmpty(mirrorRoot) || !Directory.Exists(mirrorRoot)) return false;
        if (string.Equals(Path.GetFullPath(root).TrimEnd('\\', '/'), Path.GetFullPath(mirrorRoot).TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase)) return false;
        return GameSetFolderConverter.HasFlatFiles(mirrorRoot);
    }
}
