using System.Text.RegularExpressions;

namespace Spacebox.Tests.Assets;

public sealed class ShippedResourceTests
{
    private static readonly string[] BuiltinFunctionNames = { "noise1", "noise2", "noise3", "noise4" };
    private static readonly Regex TexturePath = new Regex(@"""(Resources/Textures/[^""]+\.png)""", RegexOptions.Compiled);

    [Fact]
    public void Shaders_DoNotRedefineBuiltinFunctions()
    {
        string shaders = Path.Combine(GameFolder(), "Resources", "Shaders");
        var problems = new List<string>();
        foreach (var file in Directory.EnumerateFiles(shaders, "*.glsl", SearchOption.AllDirectories))
        {
            string source = File.ReadAllText(file);
            foreach (var name in BuiltinFunctionNames)
            {
                if (Regex.IsMatch(source, $@"\b(float|vec[234])\s+{name}\s*\(")) problems.Add($"{Path.GetFileName(file)} defines {name}");
            }
        }

        Assert.Empty(problems);
    }

    [Fact]
    public void TexturePathsInCode_PointToExistingFiles()
    {
        string game = GameFolder();
        var missing = new SortedSet<string>();
        foreach (var file in Directory.EnumerateFiles(Path.Combine(game, "Game"), "*.cs", SearchOption.AllDirectories))
        {
            foreach (var line in File.ReadLines(file))
            {
                if (line.TrimStart().StartsWith("//")) continue;
                foreach (Match match in TexturePath.Matches(line))
                {
                    string relative = match.Groups[1].Value;
                    if (!File.Exists(Path.Combine(game, relative))) missing.Add($"{relative} ({Path.GetFileName(file)})");
                }
            }
        }

        Assert.Empty(missing);
    }

    private static string GameFolder()
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
