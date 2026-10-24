using System.Text.Json;
using Spacebox.Game.GUI;
using Spacebox.Game.Versioning;
using Xunit;

namespace Spacebox.Tests.Versioning;

public class SavedWorldFixtureTests : IDisposable
{
    private const string NoFixtures = "(none)";

    private readonly string _root = Path.Combine(Path.GetTempPath(), "spacebox_fixtures_" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }

    public static string FixturesDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Spacebox.Tests", "Spacebox.Tests.csproj")))
                return Path.Combine(dir.FullName, "Spacebox.Tests", "Fixtures", "Worlds");
            dir = dir.Parent;
        }
        return null;
    }

    public static IEnumerable<object[]> SavedWorlds()
    {
        string directory = FixturesDirectory();
        var worlds = directory != null && Directory.Exists(directory)
            ? Directory.GetFiles(directory, WorldFileLayout.WorldFile, SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(directory, WorldFileLayout.LegacyWorldFile, SearchOption.AllDirectories))
                .Select(Path.GetDirectoryName).Distinct().ToList()
            : new List<string>();

        if (worlds.Count == 0)
        {
            yield return new object[] { NoFixtures };
            yield break;
        }

        foreach (var world in worlds) yield return new object[] { Path.GetRelativePath(directory, world) };
    }

    [Theory]
    [MemberData(nameof(SavedWorlds))]
    public void SavedWorld_UpgradesToTheCurrentVersionWithoutUnknownIds(string relativePath)
    {
        if (relativePath == NoFixtures) return;

        string source = Path.Combine(FixturesDirectory(), relativePath);
        string copy = Path.Combine(_root, "Worlds", Path.GetFileName(source));
        CopyDirectory(source, copy);

        Assert.True(WorldInfoFiles.TryRead(copy, out var info), $"{relativePath}: world file not readable");
        var ids = GameSetIdIndex.Load(WorldToolkitTests.DefaultGameSet(), info.ModId);

        var result = new WorldUpgrader(WorldUpgradeRegistry.CreateDefault(), Path.Combine(_root, "backups")).Upgrade(info, copy, Engine.Application.Version, ids);

        Assert.True(result.Success, $"{relativePath}: {result.Error}");
        Assert.Equal(Engine.Application.Version, info.GameVersion);
        Assert.True(result.Warnings.Count == 0, $"{relativePath}: {string.Join("; ", result.Warnings)}");
    }

    private static void CopyDirectory(string source, string target)
    {
        foreach (var directory in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(directory.Replace(source, target));

        Directory.CreateDirectory(target);
        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            File.Copy(file, file.Replace(source, target), true);
    }
}
