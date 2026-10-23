using System.Text.Json.Nodes;
using Spacebox.Game.Resource;
using Spacebox.Game.Resource.Content;

namespace Spacebox.Tests.GameSets;

public sealed class ModArchiveInstallTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "spacebox_archive_" + Guid.NewGuid().ToString("N"));

    public ModArchiveInstallTests() => Directory.CreateDirectory(_root);

    public void Dispose() => Directory.Delete(_root, true);

    [Fact]
    public void ConfigWithoutModName_UsesTheArchiveName()
    {
        var config = JsonNode.Parse("{ \"ModId\": \"lamps\" }");

        string name = ModArchiveConfig.ModName(config);

        Assert.Null(name);
        Assert.Equal("LampPack", ModInstallTarget.SafeFolderName(name, "LampPack"));
    }

    [Fact]
    public void ModName_IsReadCaseInsensitive()
    {
        Assert.Equal("Lamps", ModArchiveConfig.ModName(JsonNode.Parse("{ \"modname\": \" Lamps \" }")));
    }

    [Fact]
    public void ConfigWithoutModId_UsesTheTargetFolderName()
    {
        Assert.Equal("lamppack", ModArchiveConfig.InstallId(JsonNode.Parse("{ \"ModName\": \"x\" }"), "LampPack"));
        Assert.Equal("lamps", ModArchiveConfig.InstallId(JsonNode.Parse("{ \"ModId\": \"lamps\" }"), "LampPack"));
        Assert.Equal("lamppack", ModArchiveConfig.InstallId(null, "LampPack"));
    }

    [Fact]
    public void ModWithoutModId_ReplacesItsInstalledFolder()
    {
        string installed = Path.Combine(_root, "LampPack");
        Directory.CreateDirectory(installed);
        File.WriteAllText(Path.Combine(installed, GameSetFormat.ConfigFile), "{ \"ModName\": \"LampPack\" }");

        string target = ModInstallTarget.Resolve(_root, "LampPack", ModArchiveConfig.InstallId(JsonNode.Parse("{}"), "LampPack"), folder => InstalledGameSets.ReadConfig(folder)?.ModId, out bool replaces);

        Assert.True(replaces);
        Assert.Equal(installed, target);
    }

    [Fact]
    public void Install_ReplacesTheOldFolderWithoutLeftovers()
    {
        string target = Path.Combine(_root, "Lamps");
        Directory.CreateDirectory(target);
        File.WriteAllText(Path.Combine(target, "old.txt"), "old");
        string source = ModFolderInstaller.CreateTempFolder(_root);
        File.WriteAllText(Path.Combine(source, "new.txt"), "new");

        ModFolderInstaller.Install(source, target, true);

        Assert.True(File.Exists(Path.Combine(target, "new.txt")));
        Assert.False(File.Exists(Path.Combine(target, "old.txt")));
        Assert.False(Directory.Exists(source));
        Assert.False(Directory.Exists(target + ModFolderInstaller.OldSuffix));
    }

    [Fact]
    public void Scan_SkipsTemporaryFolders()
    {
        string temp = ModFolderInstaller.CreateTempFolder(_root);
        File.WriteAllText(Path.Combine(temp, GameSetFormat.ConfigFile), "{ \"ModId\": \"lamps\" }");
        string old = Path.Combine(_root, "Lamps" + ModFolderInstaller.OldSuffix);
        Directory.CreateDirectory(old);
        File.WriteAllText(Path.Combine(old, GameSetFormat.ConfigFile), "{ \"ModId\": \"lamps\" }");
        string real = Path.Combine(_root, "Lamps");
        Directory.CreateDirectory(real);
        File.WriteAllText(Path.Combine(real, GameSetFormat.ConfigFile), "{ \"ModId\": \"lamps\" }");

        var found = InstalledGameSets.Scan(_root);

        Assert.Single(found);
    }
}
