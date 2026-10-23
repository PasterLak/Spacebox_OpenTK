using System.Text.Json.Nodes;
using Spacebox.Game.Resource.Content;

namespace Spacebox.Tests.GameSets.Mock;

public static class MockGameSets
{
    private static readonly Lazy<string> _root = new Lazy<string>(FindRoot);

    public static string Root => _root.Value;
    public static ModFolders Folders => new ModFolders(Root);
    public static string BaseFolder => Path.Combine(Root, ModFolders.GameSetsName, "MockBase");

    public static List<InstalledGameSet> Installed(bool withDefault = false)
    {
        var installed = InstalledGameSets.Scan(Folders);
        if (withDefault) installed.Add(new InstalledGameSet(DefaultGameSetContent.Folder, InstalledGameSets.ReadConfig(DefaultGameSetContent.Folder)));
        return installed;
    }

    public static GameSetStackResult Resolve(string baseId, params string[] extensions)
    {
        return GameSetStackResolver.Resolve(Installed(), null, baseId, extensions);
    }

    public static GameSetStackResult ResolveWithDefault(string baseId, params string[] extensions)
    {
        return GameSetStackResolver.Resolve(Installed(true), DefaultGameSetContent.Folder, baseId, extensions);
    }

    public static GameSetContent Read(string baseId, params string[] extensions)
    {
        var result = Resolve(baseId, extensions);
        Assert.True(result.Success, result.ErrorText);
        return result.Stack.ReadContent();
    }

    public static string[] Keys(IEnumerable<ContentEntry> entries) => entries.Select(e => e.Key).ToArray();

    public static string Name(ContentEntry entry) => (string)entry.Data["Name"]!;

    public static int Int(ContentEntry entry, string field) => entry.Data[field]!.GetValue<int>();

    public static string[] Ingredients(ContentEntry blueprint)
    {
        return ((JsonArray)blueprint.Data["Ingredients"]!).Select(i => (string)i!["Item"]!).ToArray();
    }

    public static string ReferencedItem(ContentEntry entry, string field) => (string)((JsonObject)entry.Data[field]!)["Item"]!;

    private static string FindRoot()
    {
        string copied = Path.Combine(AppContext.BaseDirectory, "Fixtures", "GameSets", "Mock");
        if (Directory.Exists(copied)) return copied;

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            string candidate = Path.Combine(dir.FullName, "Spacebox.Tests", "Fixtures", "GameSets", "Mock");
            if (Directory.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException("Mock GameSets");
    }
}
