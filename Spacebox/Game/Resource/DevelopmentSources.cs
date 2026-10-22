namespace Spacebox.Game.Resource;

public static class DevelopmentSources
{
#if FINAL
    public const bool IsDevelopmentBuild = false;
#else
    public const bool IsDevelopmentBuild = true;
#endif

    public static string FindSourceFolder(string installedFolder)
    {
        return IsDevelopmentBuild ? FindSourceFolder(installedFolder, AppContext.BaseDirectory) : null;
    }

    public static string FindSourceFolder(string installedFolder, string baseDirectory)
    {
        if (string.IsNullOrEmpty(installedFolder) || string.IsNullOrEmpty(baseDirectory)) return null;

        string installed = Path.GetFullPath(installedFolder);
        string relative = Path.GetRelativePath(Path.GetFullPath(baseDirectory), installed);
        if (relative.StartsWith("..") || Path.IsPathRooted(relative)) return null;

        for (var dir = new DirectoryInfo(baseDirectory); dir != null; dir = dir.Parent)
        {
            if (!File.Exists(Path.Combine(dir.FullName, "Spacebox.csproj"))) continue;

            string source = Path.GetFullPath(Path.Combine(dir.FullName, relative));
            if (string.Equals(source, installed, StringComparison.OrdinalIgnoreCase)) return null;
            return Directory.Exists(source) ? source : null;
        }

        return null;
    }
}
