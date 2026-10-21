using Spacebox.Game.Player;
using Spacebox.Game.Player.GameModes;
using Spacebox.Network.Admin;
using Spacebox.Network.Players;

namespace Spacebox.Tests.Network;

public class PlayerGameModesTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "spacebox-modes-" + Guid.NewGuid().ToString("N"));

    public PlayerGameModesTests()
    {
        Directory.CreateDirectory(_folder);
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder)) Directory.Delete(_folder, true);
    }

    [Fact]
    public void Modes_AreStoredInThePlayerSave()
    {
        PlayerSaveLoadManager.WritePlayerJson(_folder, "Anna", "{\"Health\": 50}");
        var modes = new PlayerGameModes();
        modes.Load(_folder);
        Assert.False(modes.TryGet("Anna", out _));

        modes.Set("Anna", GameMode.Creative);

        string saved = PlayerSaveLoadManager.ReadPlayerJson(_folder, "Anna", false);
        Assert.Contains("\"Health\": 50", saved);
        Assert.True(PlayerGameModes.TryRead(saved, out var mode));
        Assert.Equal(GameMode.Creative, mode);

        var reloaded = new PlayerGameModes();
        reloaded.Load(_folder);
        Assert.Equal(GameMode.Creative, reloaded.Get("Anna", GameMode.Survival));
    }

    [Fact]
    public void ApplyTo_ReplacesTheModeSentByTheClient()
    {
        var modes = new PlayerGameModes();
        modes.Load(_folder);

        string cheated = "{\"Health\": 100, \"GameMode\": 1}";
        Assert.False(PlayerGameModes.TryRead(modes.ApplyTo("Bob", cheated), out _));

        modes.Set("Bob", GameMode.Spectator);
        Assert.True(PlayerGameModes.TryRead(modes.ApplyTo("Bob", cheated), out var mode));
        Assert.Equal(GameMode.Spectator, mode);
    }

    [Fact]
    public void TryRead_IgnoresInvalidValues()
    {
        Assert.False(PlayerGameModes.TryRead("{\"GameMode\": 7}", out _));
        Assert.False(PlayerGameModes.TryRead("not json", out _));
        Assert.False(PlayerGameModes.TryRead(null, out _));
    }

    [Fact]
    public void Teleport_CoordinatesAreSentForOwnPlayer()
    {
        Assert.Equal(new[] { "Me", "1", "2", "3" }, ServerCommandForwarder.TeleportArguments(new[] { "1", "2", "3" }, "Me"));
        Assert.Equal(new[] { "Bob" }, ServerCommandForwarder.TeleportArguments(new[] { "Bob" }, "Me"));
        Assert.Equal(new[] { "1" }, ServerCommandForwarder.TeleportArguments(new[] { "1" }, "Me"));
    }
}
