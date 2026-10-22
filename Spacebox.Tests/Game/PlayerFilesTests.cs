using Spacebox.Game.Player;

namespace Spacebox.Tests.Game;

public sealed class PlayerFilesTests : IDisposable
{
    private readonly string _world = Path.Combine(Path.GetTempPath(), "spacebox_players_" + Guid.NewGuid().ToString("N"));

    public PlayerFilesTests()
    {
        Directory.CreateDirectory(_world);
    }

    public void Dispose()
    {
        Directory.Delete(_world, true);
    }

    private string[] ChildFolders() => Directory.GetDirectories(_world).Select(Path.GetFileName).ToArray();

    [Fact]
    public void OldPlayerFolder_IsReadAndMovedToPlayers()
    {
        Directory.CreateDirectory(Path.Combine(_world, "players"));
        File.WriteAllText(Path.Combine(_world, "players", "Anna.json"), "{\"Health\": 40}");

        string json = PlayerSaveLoadManager.ReadPlayerJson(_world, "Anna", false);

        Assert.Contains("40", json);
        Assert.Equal(new[] { "Players" }, ChildFolders());
        Assert.Equal(new[] { "Anna" }, PlayerFiles.SavedPlayerNames(_world));
    }

    [Fact]
    public void Saving_WritesAPlayerFileAndRemovesTheOldJson()
    {
        Directory.CreateDirectory(Path.Combine(_world, "players"));
        File.WriteAllText(Path.Combine(_world, "players", "Anna.json"), "{\"Health\": 40}");

        PlayerSaveLoadManager.WritePlayerJson(_world, "Anna", "{\"Health\": 70, \"Name\": \"Anna\"}");

        string folder = Path.Combine(_world, "Players");
        Assert.Equal(new[] { "Anna.player" }, Directory.GetFiles(folder).Select(Path.GetFileName));
        Assert.Contains("70", PlayerSaveLoadManager.ReadPlayerJson(_world, "Anna", false));
    }

    [Fact]
    public void LocalPlayer_UsesLocalplayerFile()
    {
        File.WriteAllText(Path.Combine(_world, "localplayer.json"), "{\"Health\": 12}");
        Assert.Contains("12", PlayerSaveLoadManager.ReadLocalPlayerJson(_world, "Anna"));

        PlayerSaveLoadManager.WriteLocalPlayerJson(_world, "{\"Health\": 13}");

        Assert.False(File.Exists(Path.Combine(_world, "localplayer.json")));
        Assert.True(File.Exists(Path.Combine(_world, "localplayer.player")));
        Assert.Contains("13", PlayerSaveLoadManager.ReadLocalPlayerJson(_world, "Anna"));
    }

    [Fact]
    public void UnsafeNames_StayInsideThePlayersFolder()
    {
        string path = PlayerFiles.PlayerPath(_world, "a/b:c");
        Assert.Equal(Path.Combine(_world, "Players"), Path.GetDirectoryName(path));
        Assert.EndsWith(".player", path);
    }
}
