using System.Text.Json;
using Spacebox.Game.Resource.Content;

namespace Spacebox.Tests;

public static class DefaultGameSetContent
{
    private static readonly Lazy<string> _folder = new Lazy<string>(FindFolder);
    private static readonly Lazy<GameSetContent> _content = new Lazy<GameSetContent>(() => GameSetContentReader.ReadFolder(Folder, "default"));
    private static readonly Lazy<GameSetAssetIndex> _assets = new Lazy<GameSetAssetIndex>(() => GameSetAssetIndex.Build(new[] { new ContentLayer("default", Folder) }));

    public static string Folder => _folder.Value;
    public static GameSetContent Content => _content.Value;
    public static GameSetAssetIndex Assets => _assets.Value;

    public static JsonElement Flat(string fileName)
    {
        using var document = JsonDocument.Parse(Content.FlatFile(fileName).ToJsonString());
        return document.RootElement.Clone();
    }

    public static bool IsContentFile(string fileName)
    {
        return fileName is GameSetFormat.BlocksFile or GameSetFormat.ItemsFile or GameSetFormat.CraftingFile or GameSetFormat.RecipesFile;
    }

    private static string FindFolder()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "Spacebox", "GameSets", "Default");
            if (Directory.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException("Default GameSet");
    }
}
