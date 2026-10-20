using System.IO.Compression;
using System.Text.Json.Nodes;
using Spacebox.Game.GUI;
using Spacebox.Game.Versioning;
using Xunit;

namespace Spacebox.Tests.Game;

public class WorldUpgraderTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "spacebox_upgrade_" + Guid.NewGuid().ToString("N"));
    private readonly string _world;
    private readonly string _backups;

    public WorldUpgraderTests()
    {
        _world = Path.Combine(_root, "Worlds", "MyWorld");
        _backups = Path.Combine(_root, "Worlds", WorldUpgrader.BackupFolderName);
        Directory.CreateDirectory(Path.Combine(_world, "players"));
        File.WriteAllText(Path.Combine(_world, "world.json"), "{}");
        File.WriteAllText(Path.Combine(_world, "localplayer.json"), """{ "Health": 50 }""");
        File.WriteAllText(Path.Combine(_world, "players", "Bob.json"), """{ "Health": 70 }""");
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }

    private sealed class AddFieldStep : WorldUpgradeStep
    {
        public AddFieldStep() : base("1.0.1", "1.0.2")
        {
        }

        public override void Apply(WorldUpgradeContext context)
        {
            context.EditJsonFiles(context.PlayerFiles(), node =>
            {
                node["Shield"] = 10;
                return true;
            });
        }
    }

    private sealed class BrokenStep : WorldUpgradeStep
    {
        public BrokenStep() : base("1.0.2", "1.0.3")
        {
        }

        public override void Apply(WorldUpgradeContext context)
        {
            File.WriteAllText(context.PathOf("localplayer.json"), "broken");
            throw new InvalidOperationException("boom");
        }
    }

    private static WorldUpgradeRegistry Registry(params IWorldUpgradeStep[] extra)
    {
        var registry = new WorldUpgradeRegistry();
        registry.Register(new VersionBumpStep("1.0.0", "1.0.1"));
        foreach (var step in extra) registry.Register(step);
        return registry;
    }

    private WorldUpgrader Upgrader(WorldUpgradeRegistry registry) => new WorldUpgrader(registry, _backups, () => new DateTime(2026, 10, 20, 12, 0, 0));

    [Fact]
    public void Upgrade_RunsStepsInOrderAndEditsPlayerFiles()
    {
        var info = new WorldInfo { Name = "W", GameVersion = "1.0.0" };

        var result = Upgrader(Registry(new AddFieldStep())).Upgrade(info, _world, "1.0.2");

        Assert.True(result.Success, result.Error);
        Assert.Equal("1.0.2", info.GameVersion);
        Assert.Equal(10, (int)JsonNode.Parse(File.ReadAllText(Path.Combine(_world, "localplayer.json")))["Shield"]);
        Assert.Equal(10, (int)JsonNode.Parse(File.ReadAllText(Path.Combine(_world, "players", "Bob.json")))["Shield"]);
        Assert.Equal(70, (int)JsonNode.Parse(File.ReadAllText(Path.Combine(_world, "players", "Bob.json")))["Health"]);
    }

    [Fact]
    public void Upgrade_CreatesABackupFirst()
    {
        var info = new WorldInfo { Name = "W", GameVersion = "1.0.0" };

        var result = Upgrader(Registry(new AddFieldStep())).Upgrade(info, _world, "1.0.2");

        Assert.True(File.Exists(result.BackupPath));
        Assert.Equal("MyWorld_1.0.0_20261020_120000.zip", Path.GetFileName(result.BackupPath));
        using var archive = ZipFile.OpenRead(result.BackupPath);
        Assert.Contains(archive.Entries, e => e.FullName == "world/localplayer.json");
    }

    [Fact]
    public void Upgrade_RestoresTheWorldWhenAStepFails()
    {
        var info = new WorldInfo { Name = "W", GameVersion = "1.0.0" };

        var result = Upgrader(Registry(new AddFieldStep(), new BrokenStep())).Upgrade(info, _world, "1.0.3");

        Assert.False(result.Success);
        Assert.Contains("boom", result.Error);
        Assert.Equal("1.0.0", info.GameVersion);
        Assert.Equal("""{ "Health": 50 }""", File.ReadAllText(Path.Combine(_world, "localplayer.json")));
        Assert.False(Directory.Exists(_world + "_restore"));
    }

    [Fact]
    public void Upgrade_ChangesNothingWithoutAFullPath()
    {
        var info = new WorldInfo { Name = "W", GameVersion = "1.0.0" };

        var result = Upgrader(Registry()).Upgrade(info, _world, "1.0.5");

        Assert.False(result.Success);
        Assert.Equal("1.0.0", info.GameVersion);
        Assert.False(Directory.Exists(_backups));
    }

    [Fact]
    public void Upgrade_RejectsWorldsFromNewerGames()
    {
        var info = new WorldInfo { Name = "W", GameVersion = "9.0.0" };

        var result = Upgrader(Registry()).Upgrade(info, _world, "1.0.1");

        Assert.False(result.Success);
        Assert.Contains("newer", result.Error);
    }

    [Fact]
    public void Upgrade_WorksWithoutAWorldFolderForBumpSteps()
    {
        var info = new WorldInfo { Name = "W", GameVersion = "1.0.0" };

        var result = Upgrader(Registry()).Upgrade(info, null, "1.0.1");

        Assert.True(result.Success);
        Assert.Null(result.BackupPath);
    }

    [Fact]
    public void Registry_RejectsDuplicateAndBackwardSteps()
    {
        var registry = Registry();

        Assert.Throws<ArgumentException>(() => registry.Register(new VersionBumpStep("1.0.0", "1.0.5")));
        Assert.Throws<ArgumentException>(() => registry.Register(new VersionBumpStep("2.0.0", "1.0.0")));
        Assert.Throws<ArgumentException>(() => registry.Register(new VersionBumpStep("bad", "1.0.0")));
    }

    [Fact]
    public void DefaultRegistry_ReachesTheCurrentGameVersionFromEveryStep()
    {
        var registry = WorldUpgradeRegistry.CreateDefault();
        var path = new List<IWorldUpgradeStep>();

        foreach (var step in registry.Steps)
            Assert.True(registry.TryBuildPath(step.FromVersion, Engine.Application.Version, path, out var error), error);

        Assert.Contains(registry.Steps, s => s.ToVersion == Engine.Application.Version);
    }
}
