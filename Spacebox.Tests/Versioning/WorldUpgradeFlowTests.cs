using Spacebox.Game.GUI;
using Spacebox.Game.Versioning;
using Spacebox.Game.Versioning.Steps;
using Xunit;

namespace Spacebox.Tests.Versioning;

public class WorldUpgradeFlowTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "spacebox_flow_" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }

    private sealed class ExampleStep : WorldUpgradeStep
    {
        public ExampleStep() : base("0.1.5", "0.1.6")
        {
        }

        public override string Description => "Rename old_rock and power";

        public override void Apply(WorldUpgradeContext world)
        {
            world.RenameBlock("default:old_rock", "default:light_rock");
            world.RenameItem("default:old_power", "default:power");
        }
    }

    private sealed class BrokenNbtStep : WorldUpgradeStep
    {
        public BrokenNbtStep() : base("0.1.6", "0.1.7")
        {
        }

        public override void Apply(WorldUpgradeContext world)
        {
            world.RenameBlock("default:light_rock", "default:medium_rock");
            throw new InvalidOperationException("broken step");
        }
    }

    private string Build(Action<WorldFixtureBuilder> setup, string name = "World")
    {
        var builder = new WorldFixtureBuilder(Path.Combine(_root, "Worlds", name));
        setup(builder);
        return builder.Build();
    }

    private WorldUpgrader Upgrader(params IWorldUpgradeStep[] steps)
    {
        var registry = new WorldUpgradeRegistry("0.1.5");
        foreach (var step in steps) registry.Register(step);
        return new WorldUpgrader(registry, Path.Combine(_root, "Worlds", WorldUpgrader.BackupFolderName));
    }

    private static GameSetIdIndex DefaultIds() => GameSetIdIndex.Load(WorldToolkitTests.DefaultGameSet(), "default");

    [Fact]
    public void ExampleStep_RenamesEverywhereAndLeavesNoUnknownIds()
    {
        string folder = Build(w => w.Version("0.1.5")
            .Chunk(new[] { "default:air", "default:old_rock" }, new[] { "default:old_power" })
            .Player("default:old_power", 2)
            .Drop("default:old_rock"));
        var info = new WorldInfo { Name = "W", GameVersion = "0.1.5" };

        var result = Upgrader(new ExampleStep()).Upgrade(info, folder, "0.1.6", DefaultIds());

        Assert.True(result.Success, result.Error);
        Assert.Equal("0.1.6", info.GameVersion);
        Assert.Equal(new[] { "default:air", "default:light_rock" }, WorldInspector.BlockPalettes(folder));
        Assert.Equal(new[] { "default:power" }, WorldInspector.ItemPalettes(folder));
        Assert.Equal(new[] { "default:power" }, WorldInspector.PlayerItems(folder));
        Assert.Equal(new[] { "default:light_rock" }, WorldInspector.DropItems(folder));
        Assert.Empty(result.Warnings);
        Assert.Contains("block palette entries", result.Summary);
        Assert.True(File.Exists(result.BackupPath));
    }

    [Fact]
    public void ForgottenRename_IsReportedAsWarning()
    {
        string folder = Build(w => w.Version("0.1.5").Chunk(new[] { "default:removed_in_new_version" }));

        var result = Upgrader(new ExampleStep()).Upgrade(new WorldInfo { Name = "W", GameVersion = "0.1.5" }, folder, "0.1.6", DefaultIds());

        Assert.True(result.Success);
        Assert.Single(result.Warnings);
        Assert.Contains("default:removed_in_new_version", result.Warnings[0]);
    }

    [Fact]
    public void FailingNbtStep_RestoresEveryFileByteForByte()
    {
        string folder = Build(w => w.Version("0.1.5")
            .Chunk(new[] { "default:old_rock" }, new[] { "default:old_power" })
            .Player("default:old_power"));
        var before = WorldInspector.Snapshot(folder);
        var info = new WorldInfo { Name = "W", GameVersion = "0.1.5" };

        var result = Upgrader(new ExampleStep(), new BrokenNbtStep()).Upgrade(info, folder, "0.1.7");

        Assert.Equal(WorldUpgradeStatus.Failed, result.Status);
        Assert.Contains("broken step", result.Error);
        Assert.Equal("0.1.5", info.GameVersion);
        Assert.Equal(before, WorldInspector.Snapshot(folder));
        Assert.Equal(new[] { "World", WorldUpgrader.BackupFolderName }, WorldsFolderEntries());
    }

    [Fact]
    public void FailingRestoreMove_KeepsTheCurrentWorldFolder()
    {
        string folder = Build(w => w.Version("0.1.5")
            .Chunk(new[] { "default:old_rock" }, new[] { "default:old_power" })
            .Player("default:old_power"));
        string target = Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var restorer = new WorldFolderRestorer((source, destination) =>
        {
            if (Path.GetFullPath(destination) == target && Path.GetFileName(source) == WorldFolderRestorer.ArchiveRoot) throw new IOException("move failed");
            Directory.Move(source, destination);
        });
        var registry = new WorldUpgradeRegistry("0.1.5");
        registry.Register(new ExampleStep());
        registry.Register(new BrokenNbtStep());
        var upgrader = new WorldUpgrader(registry, Path.Combine(_root, "Worlds", WorldUpgrader.BackupFolderName), null, restorer);

        var result = upgrader.Upgrade(new WorldInfo { Name = "W", GameVersion = "0.1.5" }, folder, "0.1.7");

        Assert.Equal(WorldUpgradeStatus.Failed, result.Status);
        Assert.Contains("Restoring the backup failed: move failed", result.Error);
        Assert.Equal(new[] { "default:medium_rock" }, WorldInspector.BlockPalettes(folder));
        Assert.True(File.Exists(result.BackupPath));
        Assert.Equal(new[] { "World", WorldUpgrader.BackupFolderName }, WorldsFolderEntries());
    }

    [Fact]
    public void Restore_WithMissingBackup_LeavesTheWorldUntouched()
    {
        string folder = Build(w => w.Version("0.1.5").Chunk(new[] { "default:light_rock" }));
        var before = WorldInspector.Snapshot(folder);

        string error = new WorldFolderRestorer().Restore(Path.Combine(_root, "missing.zip"), folder);

        Assert.Equal("the backup file is missing", error);
        Assert.Equal(before, WorldInspector.Snapshot(folder));
    }

    private string[] WorldsFolderEntries()
    {
        var entries = Directory.GetFileSystemEntries(Path.Combine(_root, "Worlds")).Select(Path.GetFileName).ToArray();
        Array.Sort(entries, StringComparer.Ordinal);
        return entries;
    }

    [Fact]
    public void DefaultChain_UpgradesA012WorldWithDefaultIds()
    {
        string folder = Build(w => w.Version("0.1.2")
            .Chunk(new[] { "default:air", "default:light_rock", "default:aluminium_ore" }, new[] { "default:aluminium_drill" })
            .Player("default:aluminium_drill")
            .Player("default:aluminium_flashlight", 1, list: "EquipmentSlots")
            .Drop("default:aluminium_ore"));
        var info = new WorldInfo { Name = "W", GameVersion = "0.1.2" };

        var result = new WorldUpgrader(WorldUpgradeRegistry.CreateDefault(), Path.Combine(_root, "backups")).Upgrade(info, folder, Engine.Application.Version, DefaultIds());

        Assert.True(result.Success, result.Error);
        Assert.Equal(Engine.Application.Version, info.GameVersion);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Step012To015_ReplacesCobaltCrystalsWithIngots()
    {
        string folder = Build(w => w.Version("0.1.2")
            .Chunk(new[] { "default:air", "default:light_rock" }, new[] { "default:cobalt_crystal" })
            .Player("default:cobalt_crystal", 5)
            .Drop("default:cobalt_crystal"));
        var info = new WorldInfo { Name = "W", GameVersion = "0.1.2" };

        var result = new WorldUpgrader(WorldUpgradeRegistry.CreateDefault(), Path.Combine(_root, "backups")).Upgrade(info, folder, "0.1.5", DefaultIds());

        Assert.True(result.Success, result.Error);
        Assert.Equal(new[] { "default:cobalt_ingot" }, WorldInspector.ItemPalettes(folder));
        Assert.Equal(new[] { "default:cobalt_ingot" }, WorldInspector.PlayerItems(folder));
        Assert.Equal(new[] { "default:cobalt_ingot" }, WorldInspector.DropItems(folder));
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Step012To015_RemovesBasicEngineAndCannon()
    {
        string folder = Build(w => w.Version("0.1.2")
            .Chunk(new[] { "default:air", "default:basic_engine", "default:basic_cannon", "default:light_rock" })
            .Player("default:basic_engine", 3)
            .Drop("default:basic_cannon"));
        var info = new WorldInfo { Name = "W", GameVersion = "0.1.2" };

        var result = new WorldUpgrader(WorldUpgradeRegistry.CreateDefault(), Path.Combine(_root, "backups")).Upgrade(info, folder, "0.1.5", DefaultIds());

        Assert.True(result.Success, result.Error);
        Assert.DoesNotContain("default:basic_engine", WorldInspector.BlockPalettes(folder));
        Assert.DoesNotContain("default:basic_cannon", WorldInspector.BlockPalettes(folder));
        Assert.DoesNotContain("default:basic_engine", WorldInspector.PlayerItems(folder));
        Assert.DoesNotContain("default:basic_cannon", WorldInspector.DropItems(folder));
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Step012To015_RemovesObscuriumAmmo()
    {
        string folder = Build(w => w.Version("0.1.2")
            .Chunk(new[] { "default:air", "default:light_rock" }, new[] { "default:obscurium_ammo" })
            .Player("default:obscurium_ammo", 8)
            .Drop("default:obscurium_ammo"));
        var info = new WorldInfo { Name = "W", GameVersion = "0.1.2" };

        var result = new WorldUpgrader(WorldUpgradeRegistry.CreateDefault(), Path.Combine(_root, "backups")).Upgrade(info, folder, "0.1.5", DefaultIds());

        Assert.True(result.Success, result.Error);
        Assert.DoesNotContain("default:obscurium_ammo", WorldInspector.ItemPalettes(folder));
        Assert.DoesNotContain("default:obscurium_ammo", WorldInspector.PlayerItems(folder));
        Assert.DoesNotContain("default:obscurium_ammo", WorldInspector.DropItems(folder));
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void TooOldWorld_IsRefusedAndUntouched()
    {
        string folder = Build(w => w.Version("0.1.1").Chunk(new[] { "default:light_rock" }));
        var before = WorldInspector.Snapshot(folder);
        var info = new WorldInfo { Name = "W", GameVersion = "0.1.1" };

        var result = new WorldUpgrader(WorldUpgradeRegistry.CreateDefault(), Path.Combine(_root, "backups")).Upgrade(info, folder, Engine.Application.Version);

        Assert.Equal(WorldUpgradeStatus.TooOld, result.Status);
        Assert.Equal(before, WorldInspector.Snapshot(folder));
        Assert.False(Directory.Exists(Path.Combine(_root, "backups")));
    }

    [Fact]
    public void FirstStep_GoesFrom012To015()
    {
        var step = new Upgrade_0_1_2_To_0_1_5();

        Assert.Equal("0.1.2", step.FromVersion);
        Assert.Equal("0.1.5", step.ToVersion);
    }
}
