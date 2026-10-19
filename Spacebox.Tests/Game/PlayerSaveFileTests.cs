using Spacebox.Game.Player;
using Xunit;

namespace Spacebox.Tests.Game;

public class PlayerSaveFileTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "spacebox_player_" + Guid.NewGuid().ToString("N"));

    public PlayerSaveFileTests()
    {
        Directory.CreateDirectory(_folder);
    }

    public void Dispose()
    {
        Directory.Delete(_folder, true);
    }

    [Fact]
    public void LocalPlayer_IgnoresNickname()
    {
        PlayerSaveLoadManager.WriteLocalPlayerJson(_folder, "{\"a\":1}");

        Assert.Equal("{\"a\":1}", PlayerSaveLoadManager.ReadLocalPlayerJson(_folder, "First"));
        Assert.Equal("{\"a\":1}", PlayerSaveLoadManager.ReadLocalPlayerJson(_folder, "Second"));
    }

    [Fact]
    public void LocalPlayer_FallsBackToNamedSave()
    {
        PlayerSaveLoadManager.WritePlayerJson(_folder, "Old", "{\"b\":2}");

        Assert.Equal("{\"b\":2}", PlayerSaveLoadManager.ReadLocalPlayerJson(_folder, "Old"));
    }

    [Fact]
    public void LocalPlayer_DoesNotOverwriteRemotePlayers()
    {
        PlayerSaveLoadManager.WritePlayerJson(_folder, "Remote", "{\"r\":1}");
        PlayerSaveLoadManager.WriteLocalPlayerJson(_folder, "{\"l\":1}");

        Assert.Equal("{\"r\":1}", PlayerSaveLoadManager.ReadPlayerJson(_folder, "Remote", false));
    }
}
