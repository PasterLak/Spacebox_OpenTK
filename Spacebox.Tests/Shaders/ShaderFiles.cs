namespace Spacebox.Tests.Shaders;

public static class ShaderFiles
{
    public static string ShadersFolder() => Path.Combine(GameFolder(), "Resources", "Shaders");

    public static string PathWithoutExtension(string relative) => Path.Combine(ShadersFolder(), Path.ChangeExtension(relative, null));

    public static bool IsInclude(string relative) => relative.StartsWith("includes/", StringComparison.OrdinalIgnoreCase);

    public static string GameFolder()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            string candidate = Path.Combine(dir.FullName, "Spacebox", "Spacebox.csproj");
            if (File.Exists(candidate)) return Path.GetDirectoryName(candidate)!;
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException("Spacebox project folder");
    }
}
