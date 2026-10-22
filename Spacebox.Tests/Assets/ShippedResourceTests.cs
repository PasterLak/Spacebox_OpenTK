using System.Text.RegularExpressions;

namespace Spacebox.Tests.Assets;

public sealed class ShippedResourceTests
{
    private static readonly Regex TexturePath = new Regex(@"""(Resources/Textures/[^""]+\.png)""", RegexOptions.Compiled);

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
