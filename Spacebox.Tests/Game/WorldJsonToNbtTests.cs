using Spacebox.Game.GUI;
using Spacebox.Game.Generation.Tools.SaveData;
using Spacebox.Game.Player;
using Spacebox.Game.Versioning;

namespace Spacebox.Tests.Game;

public sealed class WorldJsonToNbtTests : IDisposable
{
    private readonly string _world = Path.Combine(Path.GetTempPath(), "spacebox_convert_" + Guid.NewGuid().ToString("N"));

    public WorldJsonToNbtTests()
    {
        Directory.CreateDirectory(Path.Combine(_world, "players"));
        File.WriteAllText(Path.Combine(_world, "world.json"), "{\"Name\":\"Old\",\"GameVersion\":\"0.1.5\"}");
        File.WriteAllText(Path.Combine(_world, "localplayer.json"), "{\"Health\":10}");
        File.WriteAllText(Path.Combine(_world, "players", "PasterLak.json"), "{\"Health\":20}");
        File.WriteAllText(Path.Combine(_world, "players", "wwffw.json"), "{\"Health\":30}");
        File.WriteAllText(Path.Combine(_world, "claims.json"), "{\"1\":{\"Owner\":\"PasterLak\"}}");
        File.WriteAllText(Path.Combine(_world, "protected_entities.json"), "[5,7]");
        File.WriteAllText(Path.Combine(_world, "drop.json"), "[]");
        File.WriteAllText(Path.Combine(_world, "tags.json"), "[{\"Text\":\"Home\"}]");
        File.WriteAllText(Path.Combine(_world, "rules.json"), "{\"FriendlyMobs\":true}");
        File.WriteAllText(Path.Combine(_world, "reconstructors.json"), "[]");
    }

    public void Dispose()
    {
        Directory.Delete(_world, true);
    }

    [Fact]
    public void EveryJsonFile_BecomesNbtAndPlayersMoveToPlayers()
    {
        WorldJsonToNbt.Convert(_world);

        Assert.Empty(Directory.GetFiles(_world, "*.json", SearchOption.AllDirectories));
        Assert.Equal(new[] { "Players" }, Directory.GetDirectories(_world).Select(Path.GetFileName));
        Assert.Equal(new[] { "PasterLak.player", "wwffw.player" }, Directory.GetFiles(Path.Combine(_world, "Players")).Select(Path.GetFileName).OrderBy(n => n));
        foreach (var name in new[] { "world.nbt", "localplayer.player", "claims.nbt", "protected_entities.nbt", "drop.nbt", "tags.nbt", "rules.nbt", "reconstructors.nbt" })
            Assert.True(File.Exists(Path.Combine(_world, name)), name);

        Assert.True(WorldInfoFiles.TryRead(_world, out var info));
        Assert.Equal("Old", info.Name);
        Assert.Contains("20", PlayerSaveLoadManager.ReadPlayerJson(_world, "PasterLak", false));
        Assert.Equal(new long[] { 5, 7 }, SaveDataFile.Read<long[]>(Path.Combine(_world, "protected_entities.nbt")));
    }

    [Fact]
    public void ConvertingTwice_ChangesNothing()
    {
        Assert.Equal(10, WorldJsonToNbt.Convert(_world));
        Assert.Equal(0, WorldJsonToNbt.Convert(_world));
    }
}
